using Il2CppRewired;
using Il2CppRewired.Integration.UnityUI;
using Il2CppVampireSurvivors.UI;
using UnityEngine.EventSystems;

namespace SurvivorModMenu.ModMenu.Components;

/// <summary>
/// Identifies which panel bucket a selectable belongs to so cross-panel navigation can be deterministic.
/// </summary>
[Flags]
internal enum ModMenuSelectableListType
{
    Unknown = 0,
    Tab = 1,
    Options = 2,
    Bottom = 4
}

/// <summary>
/// Handles keyboard/gamepad style navigation for the custom mod menu and keeps arrow indicators
/// positioned against the currently selected control.
/// </summary>
[RegisterTypeInIl2Cpp]
public sealed class ModMenuNavigator : MonoBehaviour
{
    private const float DirectionalRepeatDelay = 0.18f;
    private const float DirectionalAxisThreshold = 0.45f;
    private const float SliderDirectionalStep = 0.01f;
    private const float MouseMoveThresholdSquared = 36f;
    private const float ScrollIntoViewPadding = 8f;

    private static readonly Vector3[] _cornerBuffer = new Vector3[4];
    private readonly List<ModMenuSelectable> _allSelectables = new();
    private readonly List<ModMenuSelectable> _tabSelectables = new();
    private readonly List<ModMenuSelectable> _optionSelectables = new();
    private readonly List<ModMenuSelectable> _bottomSelectables = new();

    public ModMenuNavigator(IntPtr ptr) : base(ptr)
    {
    }

    private RectTransform _panelRect;
    private RectTransform _containerRect;
    private RectTransform _leftRect;
    private RectTransform _rightRect;
    private UISpriteAnimation _leftAnimation;
    private UISpriteAnimation _rightAnimation;
    private float _arrowSize = 72f;
    private float _objectGap = 6f;

    private bool _visible;
    private bool _mouseInputMode;
    private float _nextDirectionalInputTime;
    private Vector2 _lastMousePosition;
    private GameObject _lastKnownSelectedObject;
    private GameObject _lastMouseSelectedObject;
    private ModMenuSelectable _lastTabSelectable;
    private ModMenuSelectable _lastOptionSelectable;
    private ModMenuSelectable _lastBottomSelectable;
    private ModMenuSelectable _lastBeforeBottomSelectable;
    private RewiredStandaloneInputModule _rewiredInputModule;
    private Player _rewiredPlayer;
    private bool _rewiredUseSystemPlayer;
    private int _rewiredVerticalActionId = -1;
    private int _rewiredHorizontalActionId = -1;

    [HideFromIl2Cpp]
    internal void Configure(RectTransform panelRect, UISpriteAnimation leftAnimation, UISpriteAnimation rightAnimation,
        float arrowSize, float objectGap)
    {
        _panelRect = panelRect;
        _containerRect = GetComponent<RectTransform>();
        _leftAnimation = leftAnimation;
        _rightAnimation = rightAnimation;
        _leftRect = leftAnimation?.GetComponent<RectTransform>();
        _rightRect = rightAnimation?.GetComponent<RectTransform>();
        _arrowSize = arrowSize;
        _objectGap = objectGap;
    }

    [HideFromIl2Cpp]
    internal void ResetNavigationState()
    {
        _mouseInputMode = false;
        _nextDirectionalInputTime = 0f;
        _lastMousePosition = Input.mousePosition;
        _lastKnownSelectedObject = null;
        _lastMouseSelectedObject = null;
        _lastTabSelectable = null;
        _lastOptionSelectable = null;
        _lastBottomSelectable = null;
        _lastBeforeBottomSelectable = null;
        SetVisible(false);
    }

    [HideFromIl2Cpp]
    internal void BeginSelectableRegistration()
    {
        _allSelectables.Clear();
        _tabSelectables.Clear();
        _optionSelectables.Clear();
        _bottomSelectables.Clear();
    }

    [HideFromIl2Cpp]
    internal void RegisterSelectable(ModMenuSelectable selectable, ModMenuSelectableListType listType)
    {
        if (!IsSelectableValid(selectable))
        {
            return;
        }

        AddSelectableUnique(_allSelectables, selectable);
        switch (listType)
        {
            case ModMenuSelectableListType.Tab:
                AddSelectableUnique(_tabSelectables, selectable);
                break;
            case ModMenuSelectableListType.Options:
                AddSelectableUnique(_optionSelectables, selectable);
                break;
            case ModMenuSelectableListType.Bottom:
                AddSelectableUnique(_bottomSelectables, selectable);
                break;
            case ModMenuSelectableListType.Unknown:
            default:
                break;
        }
    }

    [HideFromIl2Cpp]
    internal void FinalizeSelectableRegistration()
    {
        Canvas.ForceUpdateCanvases();
        if (_panelRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);
        }

        PruneInvalidSelectables(_allSelectables);
        PruneInvalidSelectables(_tabSelectables);
        PruneInvalidSelectables(_optionSelectables);
        PruneInvalidSelectables(_bottomSelectables);

        SortSelectables(_tabSelectables, horizontalSort: false);
        SortSelectables(_optionSelectables, horizontalSort: false);
        SortSelectables(_bottomSelectables, horizontalSort: true);
        RebuildAllSelectablesInListOrder();
    }
    [HideFromIl2Cpp]
    internal void SelectInitialTarget()
    {
        var initialTarget = FindInitialTarget();
        if (initialTarget == null)
        {
            SetVisible(false);
            return;
        }

        ApplySelection(initialTarget, ensureVisible: true);
        UpdateForSelectable(initialTarget);
    }

    /// <summary>
    /// Processes one navigation input tick and updates the arrow indicators to the current selection.
    /// </summary>
    [HideFromIl2Cpp]
    internal void UpdateNavigation()
    {
        if (_allSelectables.Count <= 0)
        {
            SetVisible(false);
            return;
        }

        if (TryFindSelectableByGameObject(GetCurrentSelectedObject(), out var selectedBySystem))
        {
            _lastKnownSelectedObject = selectedBySystem.SelectionObject;
            if (_mouseInputMode)
            {
                _lastMouseSelectedObject = selectedBySystem.SelectionObject;
            }
        }

        if (WasMouseUsedThisFrame())
        {
            _mouseInputMode = true;
            if (TryFindSelectableByGameObject(GetCurrentSelectedObject(), out var pointerSelectable))
            {
                _lastMouseSelectedObject = pointerSelectable.SelectionObject;
                _lastKnownSelectedObject = pointerSelectable.SelectionObject;
            }

            SetVisible(false);
            return;
        }

        if (TryGetDirectionalInput(out var direction))
        {
            _mouseInputMode = false;
            var currentSelectable = ResolveCurrentSelectable() ?? ResolveFallbackSelectable();
            if (currentSelectable == null)
            {
                SetVisible(false);
                return;
            }

            if (TryHandleSliderDirectionalInput(currentSelectable, direction))
            {
                ApplySelection(currentSelectable, ensureVisible: true);
                UpdateForSelectable(currentSelectable);
                return;
            }

            var nextSelectable = FindDirectionalSelectable(currentSelectable, direction) ?? currentSelectable;
            var changedSelectable = !ReferenceEquals(nextSelectable, currentSelectable);
            ApplySelection(nextSelectable, ensureVisible: changedSelectable);
            UpdateForSelectable(nextSelectable);
            return;
        }

        if (_mouseInputMode)
        {
            return;
        }

        var selectedTarget = ResolveCurrentSelectable() ?? ResolveFallbackSelectable();
        if (selectedTarget == null)
        {
            SetVisible(false);
            return;
        }

        var currentSelectedTarget = ResolveCurrentSelectable();
        var shouldApplySelection = currentSelectedTarget == null || !ReferenceEquals(currentSelectedTarget, selectedTarget);
        if (shouldApplySelection)
        {
            ApplySelection(selectedTarget, ensureVisible: false);
        }

        UpdateForSelectable(selectedTarget);
    }
    [HideFromIl2Cpp]

    private static void AddSelectableUnique(List<ModMenuSelectable> selectables, ModMenuSelectable selectable)
    {
        if (selectables == null || selectable == null)
        {
            return;
        }

        if (selectables.Any(existingSelectable => ReferenceEquals(existingSelectable, selectable)))
        {
            return;
        }

        selectables.Add(selectable);
    }
    [HideFromIl2Cpp]

    private static void PruneInvalidSelectables(List<ModMenuSelectable> selectables)
    {
        if (selectables == null || selectables.Count == 0)
        {
            return;
        }

        for (var index = selectables.Count - 1; index >= 0; index--)
        {
            var selectable = selectables[index];
            if (IsSelectableValid(selectable))
            {
                continue;
            }

            selectables.RemoveAt(index);
        }
    }
    [HideFromIl2Cpp]

    private void RebuildAllSelectablesInListOrder()
    {
        _allSelectables.Clear();
        AppendValidSelectables(_allSelectables, _tabSelectables);
        AppendValidSelectables(_allSelectables, _optionSelectables);
        AppendValidSelectables(_allSelectables, _bottomSelectables);
    }
    [HideFromIl2Cpp]

    private static void AppendValidSelectables(List<ModMenuSelectable> destination, List<ModMenuSelectable> source)
    {
        if (destination == null || source is not { Count: > 0 })
        {
            return;
        }

        foreach (var selectable in source.Where(IsSelectableValid))
        {
            AddSelectableUnique(destination, selectable);
        }
    }
    [HideFromIl2Cpp]

    private void SortSelectables(List<ModMenuSelectable> selectables, bool horizontalSort)
    {
        if (selectables is not { Count: > 1 })
        {
            return;
        }

        selectables.Sort((left, right) => CompareSelectables(left, right, horizontalSort));
    }
    [HideFromIl2Cpp]

    private int CompareSelectables(ModMenuSelectable left, ModMenuSelectable right, bool horizontalSort)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        var leftHasCenter = TryGetCenterInPanelSpace(left.AnchorRect, out var leftCenter);
        var rightHasCenter = TryGetCenterInPanelSpace(right.AnchorRect, out var rightCenter);
        if (!leftHasCenter || !rightHasCenter)
        {
            return left.SelectionObject.GetInstanceID().CompareTo(right.SelectionObject.GetInstanceID());
        }

        if (horizontalSort)
        {
            var xComparison = leftCenter.x.CompareTo(rightCenter.x);
            return xComparison != 0 ? xComparison : rightCenter.y.CompareTo(leftCenter.y);
        }

        var yComparison = rightCenter.y.CompareTo(leftCenter.y);
        return yComparison != 0 ? yComparison : leftCenter.x.CompareTo(rightCenter.x);
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable FindInitialTarget()
    {
        var optionTarget = GetFirstValidSelectable(_optionSelectables);
        if (optionTarget != null)
        {
            return optionTarget;
        }

        var bottomTarget = GetFirstValidSelectable(_bottomSelectables);
        return bottomTarget != null ? bottomTarget : GetFirstValidSelectable(_allSelectables);
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable ResolveCurrentSelectable()
    {
        if (TryFindSelectableByGameObject(GetCurrentSelectedObject(), out var selectable))
        {
            return selectable;
        }

        return TryFindSelectableBySelectionObject(_lastKnownSelectedObject, out var lastKnownSelectable) ? lastKnownSelectable : null;
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable ResolveFallbackSelectable()
    {
        if (TryFindSelectableBySelectionObject(_lastMouseSelectedObject, out var mouseSelectable))
        {
            return mouseSelectable;
        }

        return TryFindSelectableBySelectionObject(_lastKnownSelectedObject, out var lastKnownSelectable) ? lastKnownSelectable : FindInitialTarget();
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable FindDirectionalSelectable(ModMenuSelectable currentSelectable, Vector2 direction)
    {
        if (currentSelectable == null || direction.sqrMagnitude <= 0.001f)
        {
            return null;
        }

        var normalizedDirection = direction.normalized;
        var listType = ResolveSelectableListType(currentSelectable);
        return listType switch
        {
            ModMenuSelectableListType.Options => FindDirectionalFromOptions(currentSelectable,
                normalizedDirection),
            ModMenuSelectableListType.Tab => FindDirectionalFromTab(currentSelectable,
                normalizedDirection),
            ModMenuSelectableListType.Bottom => FindDirectionalFromBottom(currentSelectable,
                normalizedDirection),
            _ => null
        };
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable FindDirectionalFromOptions(ModMenuSelectable currentSelectable, Vector2 normalizedDirection)
    {
        if (normalizedDirection.x < -0.5f && !IsSliderSelectable(currentSelectable))
        {
            return ResolveTabSelectable();
        }

        if (Mathf.Abs(normalizedDirection.y) > Mathf.Abs(normalizedDirection.x))
        {
            var moveUp = normalizedDirection.y > 0f;
            var verticalSelectable = FindListDirectionalByIndex(currentSelectable, _optionSelectables, moveUp);
            if (verticalSelectable != null)
            {
                return verticalSelectable;
            }

            if (!IsListBoundary(currentSelectable, _optionSelectables, moveUp))
            {
                return null;
            }

            _lastBeforeBottomSelectable = currentSelectable;
            return ResolveBottomSelectable();
        }

        if (currentSelectable.OwnerPanel == null)
        {
            return null;
        }

        var samePanelSelectable = currentSelectable.OwnerPanel.FindDirectionalTarget(currentSelectable,
            normalizedDirection, optionOnly: true);
        if (samePanelSelectable != null)
        {
            return samePanelSelectable;
        }

        return FindDirectionalCandidate(currentSelectable, _optionSelectables,
            normalizedDirection.y >= 0f ? Vector2.up : Vector2.down);
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable FindDirectionalFromTab(ModMenuSelectable currentSelectable, Vector2 normalizedDirection)
    {
        if (normalizedDirection.x > 0.5f)
        {
            return ResolveOptionSelectable();
        }

        if (Mathf.Abs(normalizedDirection.y) <= Mathf.Abs(normalizedDirection.x))
        {
            return null;
        }

        var moveUp = normalizedDirection.y > 0f;
        var verticalSelectable = FindListDirectionalByIndex(currentSelectable, _tabSelectables, moveUp);
        if (verticalSelectable != null)
        {
            return verticalSelectable;
        }

        if (!IsListBoundary(currentSelectable, _tabSelectables, moveUp))
        {
            return null;
        }

        _lastBeforeBottomSelectable = currentSelectable;
        return ResolveBottomSelectable();
    }
    [HideFromIl2Cpp]

    private static ModMenuSelectable FindListDirectionalByIndex(ModMenuSelectable currentSelectable,
        List<ModMenuSelectable> selectables, bool moveUp)
    {
        if (!TryGetSelectableIndex(selectables, currentSelectable, out var currentIndex))
        {
            return null;
        }

        var step = moveUp ? -1 : 1;
        for (var index = currentIndex + step; index >= 0 && index < selectables.Count; index += step)
        {
            var candidate = selectables[index];
            if (!IsSelectableValid(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }
    [HideFromIl2Cpp]

    private static bool IsListBoundary(ModMenuSelectable currentSelectable, List<ModMenuSelectable> selectables, bool moveUp)
    {
        if (!TryGetSelectableIndex(selectables, currentSelectable, out var currentIndex))
        {
            return false;
        }

        var step = moveUp ? -1 : 1;
        for (var index = currentIndex + step; index >= 0 && index < selectables.Count; index += step)
        {
            if (IsSelectableValid(selectables[index]))
            {
                return false;
            }
        }

        return true;
    }
    [HideFromIl2Cpp]

    private static bool TryGetSelectableIndex(List<ModMenuSelectable> selectables, ModMenuSelectable selectable,
        out int index)
    {
        index = -1;
        if (selectables == null || selectable == null)
        {
            return false;
        }

        for (var selectableIndex = 0; selectableIndex < selectables.Count; selectableIndex++)
        {
            if (!ReferenceEquals(selectables[selectableIndex], selectable))
            {
                continue;
            }

            index = selectableIndex;
            return true;
        }

        return false;
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable FindDirectionalFromBottom(ModMenuSelectable currentSelectable, Vector2 normalizedDirection)
    {
        if (normalizedDirection.y > 0.5f)
        {
            return ResolveReturnFromBottomSelectable();
        }

        if (Mathf.Abs(normalizedDirection.x) > 0.5f)
        {
            return FindDirectionalCandidate(currentSelectable, _bottomSelectables,
                normalizedDirection.x > 0f ? Vector2.right : Vector2.left) ?? currentSelectable;
        }

        return null;
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable FindDirectionalCandidate(ModMenuSelectable currentSelectable, List<ModMenuSelectable> selectables,
        Vector2 direction)
    {
        if (currentSelectable == null || selectables == null || selectables.Count == 0)
        {
            return null;
        }

        if (!TryGetCenterInPanelSpace(currentSelectable.AnchorRect, out var currentCenter))
        {
            return null;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            return null;
        }

        var normalizedDirection = direction.normalized;
        var verticalDirection = Mathf.Abs(normalizedDirection.y) >= Mathf.Abs(normalizedDirection.x);
        ModMenuSelectable bestSelectable = null;
        var bestScore = float.MaxValue;

        foreach (var candidate in selectables.Where(candidate => IsSelectableValid(candidate) && !ReferenceEquals(candidate, currentSelectable)))
        {
            if (!TryGetCenterInPanelSpace(candidate.AnchorRect, out var candidateCenter))
            {
                continue;
            }

            var delta = candidateCenter - currentCenter;
            if (Vector2.Dot(delta, normalizedDirection) <= 0.01f)
            {
                continue;
            }

            var primaryDistance = verticalDirection ? Mathf.Abs(delta.y) : Mathf.Abs(delta.x);
            var lateralDistance = verticalDirection ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
            var score = (primaryDistance * 1000f) + (lateralDistance * 12f) + (delta.sqrMagnitude * 0.05f);
            if (score >= bestScore)
            {
                continue;
            }

            bestSelectable = candidate;
            bestScore = score;
        }

        return bestSelectable;
    }
    [HideFromIl2Cpp]

    private static bool IsSliderSelectable(ModMenuSelectable selectable)
    {
        if (selectable == null || selectable.SelectionObject == null)
        {
            return false;
        }

        return selectable.SelectionObject.GetComponent<Slider>() != null;
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable ResolveTabSelectable()
    {
        return GetPreferredOrFirstValidSelectable(_lastTabSelectable, _tabSelectables);
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable ResolveOptionSelectable()
    {
        return GetPreferredOrFirstValidSelectable(_lastOptionSelectable, _optionSelectables);
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable ResolveBottomSelectable()
    {
        return GetPreferredOrFirstValidSelectable(_lastBottomSelectable, _bottomSelectables);
    }
    [HideFromIl2Cpp]

    private ModMenuSelectable ResolveReturnFromBottomSelectable()
    {
        if (IsSelectableValid(_lastBeforeBottomSelectable))
        {
            return _lastBeforeBottomSelectable;
        }

        var optionSelectable = ResolveOptionSelectable();
        if (optionSelectable != null)
        {
            return optionSelectable;
        }

        var tabSelectable = ResolveTabSelectable();
        return tabSelectable != null ? tabSelectable : GetFirstValidSelectable(_bottomSelectables);
    }
    [HideFromIl2Cpp]

    private static ModMenuSelectable GetPreferredOrFirstValidSelectable(ModMenuSelectable preferredSelectable,
        List<ModMenuSelectable> selectables)
    {
        if (selectables == null || selectables.Count == 0)
        {
            return null;
        }

        if (!IsSelectableValid(preferredSelectable))
        {
            return GetFirstValidSelectable(selectables);
        }

        return selectables.Any(selectable => ReferenceEquals(selectable, preferredSelectable)) ? preferredSelectable : GetFirstValidSelectable(selectables);
    }
    [HideFromIl2Cpp]

    private static ModMenuSelectable GetFirstValidSelectable(List<ModMenuSelectable> selectables)
    {
        return selectables?.FirstOrDefault(IsSelectableValid);
    }
    [HideFromIl2Cpp]

    private static bool IsSelectableValid(ModMenuSelectable selectable)
    {
        return selectable != null && selectable.IsValid();
    }
    [HideFromIl2Cpp]

    private bool TryGetDirectionalInput(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (Time.unscaledTime < _nextDirectionalInputTime)
        {
            return false;
        }

        if (!TryGetRewiredDirectionalInput(out direction))
        {
            return false;
        }

        _nextDirectionalInputTime = Time.unscaledTime + DirectionalRepeatDelay;
        return true;
    }
    [HideFromIl2Cpp]

    private bool TryGetRewiredDirectionalInput(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (!TryResolveRewiredPlayer(out var player, out var verticalActionId, out var horizontalActionId))
        {
            return false;
        }

        var vertical = player.GetAxis(verticalActionId);
        var horizontal = player.GetAxis(horizontalActionId);
        var absoluteVertical = Mathf.Abs(vertical);
        var absoluteHorizontal = Mathf.Abs(horizontal);
        if (absoluteVertical < DirectionalAxisThreshold && absoluteHorizontal < DirectionalAxisThreshold)
        {
            return false;
        }

        if (absoluteVertical >= absoluteHorizontal)
        {
            direction = vertical >= 0f ? Vector2.up : Vector2.down;
            return true;
        }

        direction = horizontal >= 0f ? Vector2.right : Vector2.left;
        return true;
    }
    [HideFromIl2Cpp]

    private bool TryResolveRewiredPlayer(out Player player, out int verticalActionId, out int horizontalActionId)
    {
        player = null;
        verticalActionId = -1;
        horizontalActionId = -1;
        if (!ReInput.isReady)
        {
            return false;
        }

        if (_rewiredInputModule == null)
        {
            _rewiredInputModule = FindObjectOfType<RewiredStandaloneInputModule>();
        }

        if (_rewiredInputModule == null)
        {
            return false;
        }

        var useSystemPlayer = _rewiredInputModule.UseRewiredSystemPlayer;
        var resolvedVerticalActionId = _rewiredInputModule.VerticalActionId;
        var resolvedHorizontalActionId = _rewiredInputModule.HorizontalActionId;
        if (resolvedVerticalActionId < 0 || resolvedHorizontalActionId < 0)
        {
            return false;
        }

        var shouldRefreshPlayer = _rewiredPlayer == null || _rewiredUseSystemPlayer != useSystemPlayer ||
                                  _rewiredVerticalActionId != resolvedVerticalActionId ||
                                  _rewiredHorizontalActionId != resolvedHorizontalActionId;
        if (shouldRefreshPlayer)
        {
            _rewiredPlayer = useSystemPlayer ? ReInput.players.GetSystemPlayer() : ResolveFallbackPlayer();
            _rewiredUseSystemPlayer = useSystemPlayer;
            _rewiredVerticalActionId = resolvedVerticalActionId;
            _rewiredHorizontalActionId = resolvedHorizontalActionId;
        }

        if (_rewiredPlayer == null)
        {
            return false;
        }

        player = _rewiredPlayer;
        verticalActionId = _rewiredVerticalActionId;
        horizontalActionId = _rewiredHorizontalActionId;
        return true;
    }
    [HideFromIl2Cpp]

    private static Player ResolveFallbackPlayer()
    {
        return ReInput.players.playerCount <= 0 ? null : ReInput.players.GetPlayer(0);
    }
    [HideFromIl2Cpp]

    private bool WasMouseUsedThisFrame()
    {
        var mousePosition = (Vector2)Input.mousePosition;
        var mouseMoved = (mousePosition - _lastMousePosition).sqrMagnitude > MouseMoveThresholdSquared;
        _lastMousePosition = mousePosition;

        if (!Input.mousePresent)
        {
            return false;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            return true;
        }

        if (Input.mouseScrollDelta.sqrMagnitude > 0.01f)
        {
            return true;
        }

        if (!mouseMoved)
        {
            return false;
        }

        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        return Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
    }
    [HideFromIl2Cpp]

    private static GameObject GetCurrentSelectedObject()
    {
        return EventSystem.current?.currentSelectedGameObject;
    }
    [HideFromIl2Cpp]

    private bool TryFindSelectableByGameObject(GameObject gameObj, out ModMenuSelectable selectable)
    {
        if (gameObj == null)
        {
            selectable = null;
            return false;
        }

        var directSelectable = gameObj.GetComponent<ModMenuSelectable>() ?? gameObj.GetComponentInParent<ModMenuSelectable>();
        if (directSelectable != null)
        {
            if (_allSelectables.Any(registeredSelectable => ReferenceEquals(registeredSelectable, directSelectable)))
            {
                selectable = directSelectable;
                return true;
            }
        }

        var selectedTransform = gameObj.transform;
        foreach (var registeredSelectable in _allSelectables)
        {
            if (!IsSelectableValid(registeredSelectable))
            {
                continue;
            }

            var selectionObject = registeredSelectable.SelectionObject;
            var anchorRect = registeredSelectable.AnchorRect;
            if (selectionObject == null || anchorRect == null)
            {
                continue;
            }

            if (selectionObject == gameObj || anchorRect.gameObject == gameObj)
            {
                selectable = registeredSelectable;
                return true;
            }

            var selectionTransform = selectionObject.transform;
            var anchorTransform = anchorRect.transform;
            if (!selectedTransform.IsChildOf(selectionTransform) &&
                !selectedTransform.IsChildOf(anchorTransform) &&
                !selectionTransform.IsChildOf(selectedTransform) &&
                !anchorTransform.IsChildOf(selectedTransform))
            {
                continue;
            }

            selectable = registeredSelectable;
            return true;
        }

        selectable = null;
        return false;
    }
    [HideFromIl2Cpp]

    private bool TryFindSelectableBySelectionObject(GameObject selectionObject, out ModMenuSelectable selectable)
    {
        if (selectionObject == null)
        {
            selectable = null;
            return false;
        }

        foreach (var registeredSelectable in _allSelectables.Where(registeredSelectable => ReferenceEquals(registeredSelectable.SelectionObject, selectionObject)))
        {
            selectable = registeredSelectable;
            return true;
        }

        selectable = null;
        return false;
    }
    [HideFromIl2Cpp]

    private static bool TryHandleSliderDirectionalInput(ModMenuSelectable currentSelectable, Vector2 direction)
    {
        if (currentSelectable == null || Mathf.Abs(direction.x) < 0.5f || Mathf.Abs(direction.y) > 0.5f)
        {
            return false;
        }

        var slider = currentSelectable.SelectionObject?.GetComponent<Slider>();
        if (slider == null || !slider.interactable)
        {
            return false;
        }

        var step = slider.wholeNumbers ? 1f : SliderDirectionalStep;
        var delta = direction.x > 0f ? step : -step;
        var nextValue = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
        if (slider.wholeNumbers)
        {
            nextValue = Mathf.Round(nextValue);
        }

        if (Mathf.Abs(nextValue - slider.value) <= 0.0001f)
        {
            return true;
        }

        slider.value = nextValue;
        return true;
    }
    [HideFromIl2Cpp]

    private void ApplySelection(ModMenuSelectable selectable, bool ensureVisible)
    {
        if (!IsSelectableValid(selectable))
        {
            return;
        }

        var previousSelectable = ResolveCurrentSelectable();
        if (ensureVisible)
        {
            EnsureSelectableVisible(selectable);
        }

        var eventSystem = EventSystem.current;
        var changedSelection = eventSystem == null || eventSystem.currentSelectedGameObject != selectable.SelectionObject;
        if (eventSystem != null && changedSelection)
        {
            eventSystem.SetSelectedGameObject(selectable.SelectionObject);
        }

        var selectableComponent = selectable.SelectionObject.GetComponent<Selectable>();
        if (selectableComponent != null && selectableComponent.IsInteractable() && changedSelection)
        {
            selectableComponent.Select();
        }

        if (ensureVisible)
        {
            EnsureSelectableVisible(selectable);
        }

        TrackSelection(selectable, previousSelectable);
        _lastKnownSelectedObject = selectable.SelectionObject;
    }
    [HideFromIl2Cpp]

    private static void EnsureSelectableVisible(ModMenuSelectable selectable)
    {
        if (selectable == null || selectable.AnchorRect == null)
        {
            return;
        }

        var ownerScrollRect = selectable.AnchorRect.GetComponentInParent<ScrollRect>();
        if (ownerScrollRect == null)
        {
            selectable.OwnerPanel?.EnsureVisible(selectable.AnchorRect, ScrollIntoViewPadding);
            return;
        }

        var moved = EnsureVisibleInScrollRect(ownerScrollRect, selectable.AnchorRect, ScrollIntoViewPadding);
        if (!moved && selectable.OwnerPanel != null)
        {
            selectable.OwnerPanel.EnsureVisible(selectable.AnchorRect, ScrollIntoViewPadding);
        }
    }
    [HideFromIl2Cpp]

    private void TrackSelection(ModMenuSelectable selectedSelectable, ModMenuSelectable previousSelectable)
    {
        if (selectedSelectable == null)
        {
            return;
        }

        var listType = ResolveSelectableListType(selectedSelectable);
        switch (listType)
        {
            case ModMenuSelectableListType.Tab:
                _lastTabSelectable = selectedSelectable;
                _lastBeforeBottomSelectable = selectedSelectable;
                break;
            case ModMenuSelectableListType.Options:
                _lastOptionSelectable = selectedSelectable;
                _lastBeforeBottomSelectable = selectedSelectable;
                break;
            case ModMenuSelectableListType.Bottom:
                _lastBottomSelectable = selectedSelectable;
                if (!IsSelectableValid(previousSelectable))
                {
                    return;
                }

                if (ResolveSelectableListType(previousSelectable) == ModMenuSelectableListType.Bottom)
                {
                    return;
                }

                _lastBeforeBottomSelectable = previousSelectable;
                break;
            case ModMenuSelectableListType.Unknown:
            default:
                break;
        }
    }
    [HideFromIl2Cpp]

    private ModMenuSelectableListType ResolveSelectableListType(ModMenuSelectable selectable)
    {
        if (selectable == null)
        {
            return ModMenuSelectableListType.Unknown;
        }

        if (ContainsSelectable(_tabSelectables, selectable))
        {
            return ModMenuSelectableListType.Tab;
        }

        if (ContainsSelectable(_optionSelectables, selectable))
        {
            return ModMenuSelectableListType.Options;
        }

        return ContainsSelectable(_bottomSelectables, selectable) ? ModMenuSelectableListType.Bottom : ModMenuSelectableListType.Unknown;
    }
    [HideFromIl2Cpp]

    private static bool ContainsSelectable(List<ModMenuSelectable> selectables, ModMenuSelectable selectable)
    {
        if (selectables == null || selectable == null)
        {
            return false;
        }

        return selectables.Any(candidate => ReferenceEquals(candidate, selectable));
    }
    [HideFromIl2Cpp]
    private void UpdateForTarget(RectTransform targetRect)
    {
        if (_panelRect == null || _containerRect == null || _leftRect == null || _rightRect == null || targetRect == null)
        {
            SetVisible(false);
            return;
        }

        transform.SetAsLastSibling();
        _leftRect.SetAsLastSibling();
        _rightRect.SetAsLastSibling();
        PositionIndicators(targetRect);
        SetVisible(true);
    }
    [HideFromIl2Cpp]

    internal void SetVisible(bool visible)
    {
        if (_leftAnimation == null || _rightAnimation == null)
        {
            return;
        }

        if (_visible == visible)
        {
            return;
        }

        _visible = visible;
        _leftAnimation.gameObject.SetActive(visible);
        _rightAnimation.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _leftAnimation.Play(hideWhenDone: false);
        _rightAnimation.Play(hideWhenDone: false);
    }
    [HideFromIl2Cpp]

    private void PositionIndicators(RectTransform targetRect)
    {
        if (!TryGetTargetMetricsInContainer(targetRect, out var center, out var targetWidth))
        {
            return;
        }

        var halfGap = ResolveArrowHalfGap(targetWidth);
        PositionIndicatorsForCenter(center.x, center.y, halfGap);
    }
    [HideFromIl2Cpp]

    private void PositionIndicatorsForCenter(float centerX, float centerY, float halfGap)
    {
        if (_containerRect == null)
        {
            return;
        }

        var halfArrow = _arrowSize * 0.5f;
        var halfPanelHeight = _containerRect.rect.height * 0.5f;
        var clampedCenterY = Mathf.Clamp(centerY, -halfPanelHeight + halfArrow, halfPanelHeight - halfArrow);
        var leftX = centerX - halfGap;
        var rightX = centerX + halfGap;

        // Arrows are positioned in navigator-container local space so they are not clipped
        // by the option panel mask when the selected control is near panel edges.
        ConfigureArrowRect(_leftRect, leftX, clampedCenterY, new Vector3(1f, 1f, 1f));
        ConfigureArrowRect(_rightRect, rightX, clampedCenterY, new Vector3(-1f, 1f, 1f));
    }
    [HideFromIl2Cpp]

    private float ResolveArrowHalfGap(float targetWidth)
    {
        var halfArrow = _arrowSize * 0.5f;
        return targetWidth <= 0.01f ? halfArrow : Mathf.Max(halfArrow, (targetWidth * 0.5f) + halfArrow);
    }
    [HideFromIl2Cpp]

    private void ConfigureArrowRect(RectTransform rect, float x, float y, Vector3 scale)
    {
        rect.sizeDelta = new Vector2(_arrowSize, _arrowSize);
        rect.localPosition = new Vector3(x, y, 0f);
        rect.localScale = scale;
        rect.localRotation = Quaternion.identity;
    }
    [HideFromIl2Cpp]

    private bool TryGetTargetMetricsInContainer(RectTransform targetRect, out Vector2 center, out float width)
    {
        center = Vector2.zero;
        width = 0f;

        if (_containerRect == null || targetRect == null)
        {
            return false;
        }

        var centerWorld = targetRect.TransformPoint(targetRect.rect.center);
        var centerLocal = _containerRect.InverseTransformPoint(centerWorld);
        center = new Vector2(centerLocal.x, centerLocal.y);

        width = ResolveTargetWidth(targetRect);
        if (width > 0.01f)
        {
            return true;
        }

        targetRect.GetWorldCorners(_cornerBuffer);
        var minX = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        foreach (var t in _cornerBuffer)
        {
            var localCorner = _containerRect.InverseTransformPoint(t);
            minX = Mathf.Min(minX, localCorner.x);
            maxX = Mathf.Max(maxX, localCorner.x);
        }

        if (!float.IsInfinity(minX) && !float.IsInfinity(maxX))
        {
            width = Mathf.Abs(maxX - minX);
        }

        width = Mathf.Max(width, 0f);
        return true;
    }
    [HideFromIl2Cpp]

    private static float ResolveTargetWidth(RectTransform targetRect)
    {
        if (targetRect == null)
        {
            return 0f;
        }

        var rectWidth = Mathf.Abs(targetRect.rect.width);
        if (rectWidth > 0.01f)
        {
            return rectWidth;
        }

        var sizeDeltaWidth = Mathf.Abs(targetRect.sizeDelta.x);
        if (sizeDeltaWidth > 0.01f)
        {
            return sizeDeltaWidth;
        }

        var preferredWidth = Mathf.Abs(LayoutUtility.GetPreferredWidth(targetRect));
        if (preferredWidth > 0.01f)
        {
            return preferredWidth;
        }

        var minWidth = Mathf.Abs(LayoutUtility.GetMinWidth(targetRect));
        return minWidth > 0.01f ? minWidth : 0f;
    }
    [HideFromIl2Cpp]

    private bool TryGetCenterInPanelSpace(RectTransform targetRect, out Vector2 center)
    {
        center = Vector2.zero;
        if (_panelRect == null || targetRect == null)
        {
            return false;
        }

        var worldCenter = targetRect.TransformPoint(targetRect.rect.center);
        var panelPoint = _panelRect.InverseTransformPoint(worldCenter);
        center = new Vector2(panelPoint.x, panelPoint.y);
        return true;
    }
    [HideFromIl2Cpp]

    private static bool EnsureVisibleInScrollRect(ScrollRect scrollRect, RectTransform targetRect, float padding)
    {
        if (scrollRect == null || targetRect == null)
        {
            return false;
        }

        if (!scrollRect.vertical)
        {
            return false;
        }

        var viewport = scrollRect.viewport;
        var content = scrollRect.content;
        if (viewport == null || content == null || !content.gameObject.activeInHierarchy ||
            !targetRect.gameObject.activeInHierarchy)
        {
            return false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        var viewportMask = viewport.GetComponent<RectMask2D>();
        var maskRect = viewportMask != null ? viewportMask.rectTransform : viewport;
        if (maskRect == null)
        {
            return false;
        }

        if (!TryGetVerticalBoundsInSpace(content, targetRect, out var targetTop, out var targetBottom))
        {
            return false;
        }

        if (!TryGetVerticalBoundsInSpace(content, maskRect, out var viewportTop, out var viewportBottom))
        {
            return false;
        }

        var viewportHeight = Mathf.Abs(viewportTop - viewportBottom);
        if (viewportHeight <= 0.01f)
        {
            return false;
        }

        var maxScrollY = ResolveMaxScrollY(content, viewport);
        if (maxScrollY <= 0.01f)
        {
            return false;
        }

        // Convert both target and viewport into a top-origin axis. This keeps normalized
        // scroll calculations consistent even when rect pivots/anchors differ.
        var contentTop = content.rect.yMax;
        var targetTopFromTop = contentTop - targetTop;
        var targetBottomFromTop = contentTop - targetBottom;
        var viewportTopFromTop = contentTop - viewportTop;
        var viewportBottomFromTop = contentTop - viewportBottom;

        var needsScrollUp = targetTopFromTop < viewportTopFromTop + padding;
        var needsScrollDown = targetBottomFromTop > viewportBottomFromTop - padding;
        if (!needsScrollUp && !needsScrollDown)
        {
            return false;
        }

        float desiredTopFromTop;
        if (needsScrollUp)
        {
            desiredTopFromTop = targetTopFromTop - padding;
        }
        else
        {
            desiredTopFromTop = targetBottomFromTop + padding - viewportHeight;
        }

        desiredTopFromTop = Mathf.Clamp(desiredTopFromTop, 0f, maxScrollY);
        var targetNormalized = Mathf.Clamp01(1f - (desiredTopFromTop / maxScrollY));
        var previousNormalized = scrollRect.verticalNormalizedPosition;
        if (Mathf.Abs(previousNormalized - targetNormalized) <= 0.0005f)
        {
            return false;
        }

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
        scrollRect.verticalNormalizedPosition = targetNormalized;
        Canvas.ForceUpdateCanvases();
        return true;
    }
    [HideFromIl2Cpp]

    private static bool TryGetVerticalBoundsInSpace(RectTransform referenceRect, RectTransform targetRect, out float top,
        out float bottom)
    {
        top = 0f;
        bottom = 0f;
        if (referenceRect == null || targetRect == null)
        {
            return false;
        }

        var rect = targetRect.rect;
        var x = rect.center.x;
        var topWorld = targetRect.TransformPoint(new Vector3(x, rect.yMax, 0f));
        var bottomWorld = targetRect.TransformPoint(new Vector3(x, rect.yMin, 0f));

        var topLocal = referenceRect.InverseTransformPoint(topWorld);
        var bottomLocal = referenceRect.InverseTransformPoint(bottomWorld);
        top = Mathf.Max(topLocal.y, bottomLocal.y);
        bottom = Mathf.Min(topLocal.y, bottomLocal.y);
        return true;
    }
    [HideFromIl2Cpp]

    private static float ResolveMaxScrollY(RectTransform content, RectTransform viewport)
    {
        if (content == null || viewport == null)
        {
            return 0f;
        }

        var contentHeight = content.rect.height;
        var preferredHeight = Mathf.Max(LayoutUtility.GetPreferredHeight(content), LayoutUtility.GetMinHeight(content));
        contentHeight = Mathf.Max(contentHeight, preferredHeight, ResolveChildBoundsHeight(content));
        if (contentHeight > content.rect.height + 0.1f)
        {
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        }

        return Mathf.Max(0f, contentHeight - viewport.rect.height);
    }
    [HideFromIl2Cpp]

    private static float ResolveChildBoundsHeight(RectTransform parent)
    {
        if (parent == null)
        {
            return 0f;
        }

        var minY = float.PositiveInfinity;
        var maxY = float.NegativeInfinity;
        var childCount = parent.childCount;
        for (var childIndex = 0; childIndex < childCount; childIndex++)
        {
            var child = parent.GetChild(childIndex) as RectTransform;
            if (child == null || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            child.GetWorldCorners(_cornerBuffer);
            foreach (var t in _cornerBuffer)
            {
                var localPoint = parent.InverseTransformPoint(t);
                if (localPoint.y < minY)
                {
                    minY = localPoint.y;
                }

                if (localPoint.y > maxY)
                {
                    maxY = localPoint.y;
                }
            }
        }

        if (float.IsInfinity(minY) || float.IsInfinity(maxY))
        {
            return 0f;
        }

        return Mathf.Max(0f, maxY - minY);
    }
    [HideFromIl2Cpp]

    private void UpdateForSelectable(ModMenuSelectable selectable)
    {
        if (_mouseInputMode || selectable == null || selectable.AnchorRect == null)
        {
            SetVisible(false);
            return;
        }

        UpdateForTarget(selectable.AnchorRect);
    }
}
