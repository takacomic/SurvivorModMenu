using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using Il2CppVampireSurvivors.Graphics;
using Il2CppVampireSurvivors.UI;
using SurvivorModMenu.ModMenu.Components;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SurvivorModMenu.ModMenu;

internal static class ModMenuController
{
    private enum NavigationPanelType
    {
        Unknown,
        Tab,
        Options,
        Bottom
    }

    private const string ModButtonName = "SurvivorModMenu_ModOptionsButton";
    private const string MenuRootName = "View - ModMenu";
    private const string LegacyMenuRootName = "SurvivorModMenu_ModMenu";
    private const string SafeAreaName = "Safe Area";
    private const string TopBackButtonName = "SurvivorModMenu_TopBackButton";
    private const string TopResetButtonName = "SurvivorModMenu_TopResetButton";
    private const string VanillaMenuNavigatorsName = "Navigators (Menu)";
    private const string CustomNavigatorsRootName = "Navigators";
    private const string LeftNavigatorName = "NavigatorLeft";
    private const string RightNavigatorName = "NavigatorRight";
    private const string OptionsButtonTypeName = "VampireSurvivors.UI.OptionsButton";
    private const string SelectableUiTypeName = "VampireSurvivors.UI.SelectableUI";
    private const string ModsButtonSpriteName = "button_c9_mouseover";
    private const string BackButtonSpriteName = "button_c8_normal";
    private const string ResetButtonSpriteName = "button_c5_mouseover";
    private const string OptionsPanelName = "Options Panel";
    private const string TopPanelName = "Bottom Panel";
    private const string TopButtonsRootName = "ModButtons";
    private const float ButtonSpritePixelsPerUnit = 0.3f;
    private const float ScanIntervalSeconds = 0.2f;
    private const float PanelEdgeInset = 10f;
    private const float ContentSidePadding = 60f;
    private const float ContentTopPadding = 140f;
    private const float ContentBottomPadding = 110f;
    private const float ContentLeftGapFromSeparator = 30f;
    private const float TabPanelSidePadding = 0f;
    private const float TabPanelInnerPadding = 8f;
    private const float TabPanelButtonVerticalGap = 20f;
    private const float TabPanelGap = 0f;
    private const float TabPanelWidthPercent = 0.2f;
    private const float TabPanelMinWidth = 92f;
    private const float TabPanelMaxWidth = 160f;
    private const float TabButtonWidthReduction = 40f;
    private const float TabButtonMinFontSize = 10f;
    private const int TabButtonMaxVisibleLines = 2;
    private const float ScrollbarWidth = 14f;
    private const float ScrollbarEdgePadding = 4f;
    private const float TopPanelHeight = 90f;
    private const float BottomPanelPositionX = 65f;
    private const float BottomPanelPositionY = -485f;
    private const float TopPanelContentPadding = 2f;
    private const float TopButtonSpacing = 12f;
    private const float TopButtonMinWidth = 220f;
    private const float TopButtonMinHeight = 60f;
    private const float NavigatorArrowSize = 72f;
    private const float NavigatorArrowSidePadding = 28f;
    private const int NavigatorArrowAnimationFps = 10;
    private const float NavigatorArrowObjectGap = 6f;
    private const float DirectionalRepeatDelay = 0.18f;
    private const float SliderDirectionalStep = 0.01f;
    private const float MouseMoveThresholdSquared = 36f;
    private const float ScrollIntoViewPadding = 8f;

    private static readonly Color Gray = new(0.34f, 0.36f, 0.40f, 1f);
    private static readonly Color LightGray = new(0.57f, 0.60f, 0.66f, 1f);
    private static readonly Color Blue = new(0.27f, 0.47f, 0.78f, 1f);
    private static readonly Color Green = new(0.24f, 0.68f, 0.38f, 1f);
    private static readonly Color Red = new(0.74f, 0.24f, 0.25f, 1f);
    private static readonly Color Dark = new(0.16f, 0.18f, 0.22f, 1f);
    private static readonly Color TrackbarDarkGray = new(0.23f, 0.23f, 0.23f, 0.9f);
    private static readonly string[] MainMenuSceneNames = { "main menu", "mainmenu" };
    private static readonly List<ModMenuSelectable> NavigationTargets = new();
    private static readonly List<ModMenuSelectable> TabNavigationTargets = new();
    private static readonly List<ModMenuSelectable> OptionNavigationTargets = new();
    private static readonly List<ModMenuSelectable> BottomNavigationTargets = new();
    private static readonly Dictionary<ModMenuSelectable, ModMenuMainOptionObject> OptionTargetOwners =
        new();
    private static readonly Dictionary<int, ModMenuMainOptionObject> MainOptionObjectsById = new();
    private static readonly List<ModMenuMainOptionObject> MainOptionObjects = new();
    private static readonly Vector3[] VisibilityCornerBuffer = new Vector3[4];

    private static Sprite _roundedSprite;
    private static Sprite _framePanelSprite;
    private static readonly Dictionary<string, Sprite> UiSpriteCache = new(StringComparer.Ordinal);

    private static Button _modButton;
    private static Button _optionsAnchorButton;
    private static GameObject _menuRoot;
    private static CanvasGroup _menuGroup;
    private static GameObject _panelRoot;
    private static GameObject _tabPanelRoot;
    private static GameObject _optionsPanelRoot;
    private static GameObject _topPanelRoot;
    private static GameObject _topButtonsRoot;
    private static GameObject _topButtonsContentRoot;
    private static ScrollRect _topButtonsScrollRect;
    private static GameObject _listViewRoot;
    private static GameObject _listRoot;
    private static ScrollRect _listScrollRect;
    private static Scrollbar _listScrollbar;
    private static ModMenuPanel _listNavigationPanel;
    private static GameObject _contentRoot;
    private static RectTransform _contentViewport;
    private static ScrollRect _contentScrollRect;
    private static Scrollbar _contentScrollbar;
    private static ModMenuPanel _contentNavigationPanel;
    private static GameObject _vanillaMenuNavigators;
    private static GameObject _customNavigatorsRoot;
    private static UISpriteAnimation _leftNavigatorAnimation;
    private static UISpriteAnimation _rightNavigatorAnimation;
    private static ModMenuNavigator _navigatorVisuals;
    private static Button _topBackButton;
    private static Button _topResetButton;
    private static GameObject _titleObject;
    private static GameObject _subtitleObject;
    private static float _nextScanTime;
    private static ModMenuTextStyle _textStyle;
    private static int _registryVersion = -1;
    private static readonly Dictionary<string, List<Action<ModMenuBuilder>>> ModOptionsById =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Button> EntryButtons = new();
    private static readonly Dictionary<int, float> LabelBaseY = new();
    private static GameObject _modPageRoot;
    private static bool _optionsWasActive;
    private static bool _modWasActive;
    private static string _selectedEntryId;
    private static GameObject _lastKnownSelectedObject;
    private static GameObject _lastMouseSelectedObject;
    private static Vector2 _lastMousePosition;
    private static bool _mouseInputMode;
    private static float _nextDirectionalInputTime;
    private static ModMenuSelectable _lastTabTarget;
    private static ModMenuSelectable _lastOptionTarget;
    private static ModMenuSelectable _lastBottomTarget;
    private static ModMenuSelectable _lastTargetBeforeBottom;

    internal static void Update()
    {
        if (!IsReady())
        {
            if (Time.unscaledTime >= _nextScanTime)
            {
                _nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
                TrySetup();
            }
        }

        UpdateCustomNavigation();
    }

    internal static void OnSceneWasLoaded(string sceneName)
    {
        if (!IsMainMenuScene(sceneName))
        {
            return;
        }

        ResetUiState();
        _nextScanTime = 0f;
        TrySetup();
    }

    private static bool IsReady()
    {
        return _modButton != null &&
               _menuRoot != null &&
               _panelRoot != null &&
               _topBackButton != null &&
               _topResetButton != null;
    }

    private static bool IsMainMenuScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        var normalizedName = sceneName.Trim().ToLowerInvariant();
        foreach (var expectedName in MainMenuSceneNames)
        {
            if (!normalizedName.Equals(expectedName, StringComparison.Ordinal))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static void TrySetup()
    {
        var existingButton = GameObject.Find(ModButtonName);
        if (existingButton != null)
        {
            _modButton = existingButton.GetComponent<Button>();
            RemoveOptionsButtonComponent(_modButton?.gameObject);
        }

        if (_menuRoot == null)
        {
            var existingMenu = GameObject.Find(MenuRootName);
            if (existingMenu == null)
            {
                existingMenu = GameObject.Find(LegacyMenuRootName);
            }

            if (existingMenu != null)
            {
                existingMenu.name = MenuRootName;
                _menuRoot = existingMenu;
                _menuGroup = existingMenu.GetComponent<CanvasGroup>();
            }
        }

        var optionsButton = FindButtonByLabel("OPTIONS", "Options");
        if (optionsButton == null)
        {
            return;
        }

        var canvas = optionsButton.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        _optionsAnchorButton = optionsButton;
        _textStyle = BuildTextStyle(optionsButton);
        EnsureMenuRootPlacement(canvas, optionsButton.transform);

        if (_menuRoot != null && _panelRoot == null)
        {
            CacheMenuParts();
        }

        if (_modButton == null)
        {
            _modButton = CreateMainMenuButton(optionsButton);
            if (_modButton == null)
            {
                return;
            }
        }

        if (_menuRoot == null)
        {
            BuildMenu(canvas, optionsButton.transform);
        }

        EnsureTopButtons();
    }

    private static Button CreateMainMenuButton(Button optionsButton)
    {
        if (optionsButton == null)
        {
            return null;
        }

        var parent = optionsButton.transform.parent;
        if (parent == null)
        {
            return null;
        }

        var modButton = CloneButtonFromTemplate(optionsButton, parent, ModButtonName) ?? CreateButton(parent, ModButtonName, "MODS", Blue, LightGray, Green, Dark);
        if (modButton == null)
        {
            return null;
        }

        RemoveOptionsButtonComponent(modButton.gameObject);
        SetButtonLabel(modButton, "MODS");
        ApplyButtonSprite(modButton, ModsButtonSpriteName);
        var optionsRect = optionsButton.GetComponent<RectTransform>();
        var modRect = modButton.GetComponent<RectTransform>();
        CopyRectTransform(optionsRect, modRect);

        var width = optionsRect != null && optionsRect.rect.width > 0.1f
            ? optionsRect.rect.width
            : optionsRect != null ? optionsRect.sizeDelta.x : 200f;
        modRect.anchoredPosition += new Vector2(width + 12f, 0f);

        EnsureIgnoreLayout(modButton.gameObject);
        AddClickListener(modButton, OpenMenu);

        return modButton;
    }

    private static void OpenMenu()
    {
        if (_menuRoot == null || _menuGroup == null)
        {
            return;
        }

        _mouseInputMode = false;
        _nextDirectionalInputTime = 0f;
        _lastMousePosition = Input.mousePosition;
        SetNavigatorVisibility(modMenuVisible: true);
        _menuRoot.transform.SetAsLastSibling();
        SetMenuVisible(true);
        CacheMenuButtonState();
        ApplyMenuButtonVisibility(false);
        ShowModList();
        SelectInitialNavigationTarget();
    }

    private static void CloseMenu()
    {
        if (_menuGroup == null)
        {
            return;
        }

        SetMenuVisible(false);
        SetTopButtonsVisible(false, false);
        SetScrollbarsVisible(false, false);
        ApplyMenuButtonVisibility(true);
        SetNavigatorVisibility(modMenuVisible: false);
        _mouseInputMode = false;
        _nextDirectionalInputTime = 0f;
        _lastMousePosition = Input.mousePosition;
    }

    private static void SetMenuVisible(bool visible)
    {
        _menuGroup.alpha = visible ? 1f : 0f;
        _menuGroup.interactable = visible;
        _menuGroup.blocksRaycasts = visible;
    }

    private static void SetNavigatorVisibility(bool modMenuVisible)
    {
        SetVanillaMenuNavigatorsActive(!modMenuVisible);
        if (_panelRoot == null)
        {
            return;
        }

        EnsureCustomNavigators();
        if (_customNavigatorsRoot == null)
        {
            return;
        }

        _customNavigatorsRoot.SetActive(modMenuVisible);
        _navigatorVisuals?.SetVisible(false);
    }

    private static void SetVanillaMenuNavigatorsActive(bool active)
    {
        if (_vanillaMenuNavigators == null)
        {
            _vanillaMenuNavigators = FindSceneObjectByName(VanillaMenuNavigatorsName);
        }

        if (_vanillaMenuNavigators == null)
        {
            return;
        }

        _vanillaMenuNavigators.SetActive(active);
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var activeObject = GameObject.Find(objectName);
        if (activeObject != null)
        {
            return activeObject;
        }

        foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform == null || transform.gameObject == null)
            {
                continue;
            }

            if (!string.Equals(transform.name, objectName, StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsSceneObject(transform))
            {
                continue;
            }

            return transform.gameObject;
        }

        return null;
    }

    private static void EnsureCustomNavigators()
    {
        if (_menuRoot == null || _panelRoot == null)
        {
            return;
        }

        if (_customNavigatorsRoot == null)
        {
            var existingNavigators = _menuRoot.transform.Find(CustomNavigatorsRootName);
            if (existingNavigators == null)
            {
                existingNavigators = _panelRoot.transform.Find(CustomNavigatorsRootName);
            }

            if (existingNavigators != null)
            {
                _customNavigatorsRoot = existingNavigators.gameObject;
            }
        }

        if (_customNavigatorsRoot == null)
        {
            var navigatorsRect = ModMenuObjectFactory.CreateRect(CustomNavigatorsRootName, _menuRoot.transform);
            _customNavigatorsRoot = navigatorsRect.gameObject;
            _customNavigatorsRoot.SetActive(false);
        }

        SyncNavigatorContainerRect();

        _leftNavigatorAnimation = CreateNavigatorArrow(_customNavigatorsRoot.transform, LeftNavigatorName,
            isRightArrow: false);
        _rightNavigatorAnimation = CreateNavigatorArrow(_customNavigatorsRoot.transform, RightNavigatorName,
            isRightArrow: true);
        _navigatorVisuals = _customNavigatorsRoot.GetComponent<ModMenuNavigator>() ??
                            ModMenuObjectFactory.GetOrAddComponent<ModMenuNavigator>(_customNavigatorsRoot);
        _navigatorVisuals.Configure(_panelRoot.GetComponent<RectTransform>(), _leftNavigatorAnimation,
            _rightNavigatorAnimation, NavigatorArrowSize, NavigatorArrowObjectGap);
    }

    private static void SyncNavigatorContainerRect()
    {
        if (_customNavigatorsRoot == null || _panelRoot == null)
        {
            return;
        }

        var rootRect = _customNavigatorsRoot.GetComponent<RectTransform>() ??
                       ModMenuObjectFactory.GetOrAddComponent<RectTransform>(_customNavigatorsRoot);
        var panelRect = _panelRoot.GetComponent<RectTransform>();
        if (rootRect == null || panelRect == null)
        {
            return;
        }

        if (_customNavigatorsRoot.transform.parent != _menuRoot.transform)
        {
            _customNavigatorsRoot.transform.SetParent(_menuRoot.transform, false);
        }

        rootRect.anchorMin = panelRect.anchorMin;
        rootRect.anchorMax = panelRect.anchorMax;
        rootRect.pivot = panelRect.pivot;
        rootRect.sizeDelta = panelRect.sizeDelta;
        rootRect.anchoredPosition = panelRect.anchoredPosition;
        rootRect.localScale = Vector3.one;
        rootRect.localRotation = Quaternion.identity;
    }

    private static UISpriteAnimation CreateNavigatorArrow(Transform parent, string objectName, bool isRightArrow)
    {
        if (parent == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var arrowTransform = parent.Find(objectName);
        var arrowObject = arrowTransform != null
            ? arrowTransform.gameObject
            : ModMenuObjectFactory.CreateRect(objectName, parent).gameObject;
        var arrowRect = arrowObject.GetComponent<RectTransform>() ??
                        ModMenuObjectFactory.GetOrAddComponent<RectTransform>(arrowObject);
        if (arrowRect == null)
        {
            return null;
        }

        arrowRect.anchorMin = new Vector2(isRightArrow ? 1f : 0f, 0.5f);
        arrowRect.anchorMax = new Vector2(isRightArrow ? 1f : 0f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(isRightArrow ? -NavigatorArrowSidePadding : NavigatorArrowSidePadding, 0f);
        arrowRect.sizeDelta = new Vector2(NavigatorArrowSize, NavigatorArrowSize);
        arrowRect.localScale = isRightArrow ? new Vector3(-1f, 1f, 1f) : Vector3.one;
        arrowRect.localRotation = Quaternion.identity;

        var image = arrowObject.GetComponent<Image>() ?? ModMenuObjectFactory.GetOrAddComponent<Image>(arrowObject);
        if (image == null)
        {
            return null;
        }

        image.enabled = false;
        image.raycastTarget = false;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        var animation = arrowObject.GetComponent<UISpriteAnimation>() ??
                        ModMenuObjectFactory.GetOrAddComponent<UISpriteAnimation>(arrowObject);
        if (animation == null)
        {
            return null;
        }

        ConfigureNavigatorAnimationFrames(animation);
        return animation;
    }

    private static void ConfigureNavigatorAnimationFrames(UISpriteAnimation animation)
    {
        if (animation == null)
        {
            return;
        }

        animation.Clean();
        for (var index = 1; index <= 8; index++)
        {
            var sprite = SpriteManager.GetSprite($"arrow_0{index}");
            if (sprite == null)
            {
                continue;
            }

            animation.sprites.Add(sprite);
        }

        if (animation.sprites.Count <= 0)
        {
            return;
        }

        animation.SetFPS(NavigatorArrowAnimationFps);
        animation.RecalculateTriggerTime();
        animation.Play(hideWhenDone: false);
    }

    private static void UpdateCustomNavigation()
    {
        if (!IsMenuOpen())
        {
            return;
        }

        RefreshNavigationTargets();
        if (NavigationTargets.Count <= 0)
        {
            _navigatorVisuals?.SetVisible(false);
            return;
        }

        if (TryFindTargetByGameObject(GetCurrentSelectedObject(), out var selectedBySystem))
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
            if (TryFindTargetByGameObject(GetCurrentSelectedObject(), out var pointerTarget))
            {
                _lastMouseSelectedObject = pointerTarget.SelectionObject;
                _lastKnownSelectedObject = pointerTarget.SelectionObject;
            }

            _navigatorVisuals?.SetVisible(false);
            return;
        }

        if (TryGetDirectionalInput(out var direction))
        {
            _mouseInputMode = false;

            var currentTarget = ResolveCurrentNavigationTarget() ?? ResolveFallbackNavigationTarget();
            if (currentTarget == null)
            {
                _navigatorVisuals?.SetVisible(false);
                return;
            }

            if (TryHandleSliderDirectionalInput(currentTarget, direction))
            {
                ApplyNavigationSelection(currentTarget, ensureVisible: true);
                UpdateNavigatorForTarget(currentTarget);
                return;
            }

            var nextTarget = FindDirectionalTarget(currentTarget, direction) ?? currentTarget;
            ApplyNavigationSelection(nextTarget, ensureVisible: true);
            UpdateNavigatorForTarget(nextTarget);
            return;
        }

        if (_mouseInputMode)
        {
            return;
        }

        var selectedTarget = ResolveCurrentNavigationTarget() ?? ResolveFallbackNavigationTarget();
        if (selectedTarget == null)
        {
            _navigatorVisuals?.SetVisible(false);
            return;
        }

        ApplyNavigationSelection(selectedTarget, ensureVisible: true);
        UpdateNavigatorForTarget(selectedTarget);
    }

    private static bool IsMenuOpen()
    {
        if (_menuGroup == null)
        {
            return false;
        }

        if (!_menuGroup.interactable || !_menuGroup.blocksRaycasts)
        {
            return false;
        }

        return _menuGroup.alpha > 0.99f;
    }

    private static void SelectInitialNavigationTarget()
    {
        _mouseInputMode = false;
        RefreshNavigationTargets();

        var initialTarget = FindInitialNavigationTarget();
        if (initialTarget == null)
        {
            _navigatorVisuals?.SetVisible(false);
            return;
        }

        ApplyNavigationSelection(initialTarget, ensureVisible: true);
        UpdateNavigatorForTarget(initialTarget);
    }

    private static void RefreshNavigationTargets()
    {
        NavigationTargets.Clear();
        TabNavigationTargets.Clear();
        OptionNavigationTargets.Clear();
        BottomNavigationTargets.Clear();
        OptionTargetOwners.Clear();
        MainOptionObjectsById.Clear();
        MainOptionObjects.Clear();
        EnsureNavigationPanels();
        _listNavigationPanel?.BeginTargetRegistration();
        _contentNavigationPanel?.BeginTargetRegistration();
        AddTopButtonNavigationTargets();
        AddTabButtonNavigationTargets();
        AddOptionNavigationTargets();
        FinalizeNavigationTargets();
    }

    private static void EnsureNavigationPanels()
    {
        _listNavigationPanel = ConfigureNavigationPanel(_listViewRoot, _listScrollRect);
        _contentNavigationPanel = ConfigureNavigationPanel(_contentRoot, _contentScrollRect);
    }

    private static ModMenuPanel ConfigureNavigationPanel(GameObject panelRoot, ScrollRect scrollRect)
    {
        if (panelRoot == null || scrollRect == null)
        {
            return null;
        }

        var panel = panelRoot.GetComponent<ModMenuPanel>() ??
                    ModMenuObjectFactory.GetOrAddComponent<ModMenuPanel>(panelRoot);
        panel.Configure(scrollRect);
        return panel;
    }

    private static void AddTopButtonNavigationTargets()
    {
        if (_topBackButton != null && _topBackButton.gameObject.activeInHierarchy)
        {
            AddNavigationTarget(_topBackButton.gameObject, _topBackButton.GetComponent<RectTransform>(), null,
                isOptionObject: false);
        }

        if (_topResetButton == null || !_topResetButton.gameObject.activeInHierarchy)
        {
            return;
        }

        AddNavigationTarget(_topResetButton.gameObject, _topResetButton.GetComponent<RectTransform>(), null,
            isOptionObject: false);
    }

    private static void AddTabButtonNavigationTargets()
    {
        foreach (var button in EntryButtons.Values)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                continue;
            }

            AddNavigationTarget(button.gameObject, button.GetComponent<RectTransform>(), _listNavigationPanel,
                isOptionObject: false);
        }
    }

    private static void AddOptionNavigationTargets()
    {
        if (_panelRoot == null)
        {
            return;
        }

        foreach (var button in _panelRoot.GetComponentsInChildren<Button>(true))
        {
            if (!IsOptionNavigationButton(button))
            {
                continue;
            }

            var ownerPanel = ResolveNavigationOwnerPanel(button.transform);
            AddNavigationTarget(button.gameObject, button.GetComponent<RectTransform>(), ownerPanel, isOptionObject: true);
        }

        foreach (var inputField in _panelRoot.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (!IsOptionNavigationInputField(inputField))
            {
                continue;
            }

            var ownerPanel = ResolveNavigationOwnerPanel(inputField.transform);
            AddNavigationTarget(inputField.gameObject, inputField.GetComponent<RectTransform>(), ownerPanel,
                isOptionObject: true);
        }

        foreach (var slider in _panelRoot.GetComponentsInChildren<Slider>(true))
        {
            if (!IsOptionNavigationSlider(slider))
            {
                continue;
            }

            var ownerPanel = ResolveNavigationOwnerPanel(slider.transform);
            AddNavigationTarget(slider.gameObject, slider.handleRect, ownerPanel, isOptionObject: true);
        }
    }

    private static bool IsOptionNavigationButton(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (IsTopActionButton(button))
        {
            return false;
        }

        if (IsTabEntryButton(button))
        {
            return false;
        }

        return IsChildOfPanelRoot(button.transform);
    }

    private static bool IsOptionNavigationInputField(TMP_InputField inputField)
    {
        if (inputField == null || !inputField.gameObject.activeInHierarchy)
        {
            return false;
        }

        return IsChildOfPanelRoot(inputField.transform);
    }

    private static bool IsOptionNavigationSlider(Slider slider)
    {
        if (slider == null || slider.handleRect == null || !slider.gameObject.activeInHierarchy)
        {
            return false;
        }

        return IsChildOfPanelRoot(slider.transform);
    }

    private static bool IsTopActionButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        var buttonObject = button.gameObject;
        if (_topBackButton != null && _topBackButton.gameObject == buttonObject)
        {
            return true;
        }

        return _topResetButton != null && _topResetButton.gameObject == buttonObject;
    }

    private static bool IsTabEntryButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        foreach (var entryButton in EntryButtons.Values)
        {
            if (entryButton == null)
            {
                continue;
            }

            if (entryButton.gameObject == button.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsChildOfPanelRoot(Transform target)
    {
        if (_panelRoot == null || target == null)
        {
            return false;
        }

        return target.IsChildOf(_panelRoot.transform);
    }

    private static ModMenuPanel ResolveNavigationOwnerPanel(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return null;
        }

        if (_contentRoot != null && targetTransform.IsChildOf(_contentRoot.transform))
        {
            return _contentNavigationPanel;
        }

        if (_listViewRoot != null && targetTransform.IsChildOf(_listViewRoot.transform))
        {
            return _listNavigationPanel;
        }

        return null;
    }

    private static void AddNavigationTarget(GameObject selectionObject, RectTransform anchorRect,
        ModMenuPanel ownerPanel, bool isOptionObject)
    {
        if (selectionObject == null || anchorRect == null || !selectionObject.activeInHierarchy)
        {
            return;
        }

        if (!anchorRect.gameObject.activeInHierarchy)
        {
            return;
        }

        var targetComponent = selectionObject.GetComponent<ModMenuSelectable>() ??
                              ModMenuObjectFactory.GetOrAddComponent<ModMenuSelectable>(selectionObject);
        if (targetComponent == null)
        {
            return;
        }

        targetComponent.Configure(anchorRect, ownerPanel, isOptionObject);
        if (!targetComponent.IsValid())
        {
            return;
        }

        foreach (var existingTarget in NavigationTargets)
        {
            if (existingTarget == targetComponent)
            {
                return;
            }
        }

        NavigationTargets.Add(targetComponent);
        ownerPanel?.RegisterTarget(targetComponent);
        RegisterTargetByPanel(targetComponent);
        RegisterMainOptionObject(targetComponent);
    }

    private static void FinalizeNavigationTargets()
    {
        SortNavigationTargets(TabNavigationTargets, horizontalSort: false);
        SortNavigationTargets(BottomNavigationTargets, horizontalSort: true);
        UpdateMainOptionLocalPositions();
        SortOptionNavigationTargets();
    }

    private static void RegisterTargetByPanel(ModMenuSelectable target)
    {
        if (target == null)
        {
            return;
        }

        var panelType = ResolvePanelType(target);
        switch (panelType)
        {
            case NavigationPanelType.Tab:
                TabNavigationTargets.Add(target);
                break;
            case NavigationPanelType.Options:
                OptionNavigationTargets.Add(target);
                break;
            case NavigationPanelType.Bottom:
                BottomNavigationTargets.Add(target);
                break;
            default:
                break;
        }
    }

    private static void RegisterMainOptionObject(ModMenuSelectable target)
    {
        if (target == null || ResolvePanelType(target) != NavigationPanelType.Options)
        {
            return;
        }

        var optionRoot = ResolveMainOptionRoot(target.AnchorRect != null ? target.AnchorRect.transform : null);
        if (optionRoot == null)
        {
            optionRoot = ResolveMainOptionRoot(target.SelectionObject != null ? target.SelectionObject.transform : null);
        }

        if (optionRoot == null)
        {
            return;
        }

        var optionRect = optionRoot.TryCast<RectTransform>() ?? optionRoot.GetComponent<RectTransform>();
        if (optionRect == null)
        {
            return;
        }

        var optionId = optionRoot.gameObject.GetInstanceID();
        if (!MainOptionObjectsById.TryGetValue(optionId, out var mainOptionObject) || mainOptionObject == null)
        {
            mainOptionObject = optionRoot.gameObject.GetComponent<ModMenuMainOptionObject>() ??
                               ModMenuObjectFactory.GetOrAddComponent<ModMenuMainOptionObject>(optionRoot.gameObject);
            if (mainOptionObject == null)
            {
                return;
            }

            mainOptionObject.Configure(optionRect, _contentScrollRect != null ? _contentScrollRect.content : null);
            mainOptionObject.ClearSubTargets();
            MainOptionObjectsById[optionId] = mainOptionObject;
            MainOptionObjects.Add(mainOptionObject);
        }

        mainOptionObject.RegisterSubTarget(target);
        OptionTargetOwners[target] = mainOptionObject;
    }

    private static Transform ResolveMainOptionRoot(Transform targetTransform)
    {
        if (targetTransform == null || _modPageRoot == null)
        {
            return null;
        }

        var modPageTransform = _modPageRoot.transform;
        var current = targetTransform;
        while (current != null)
        {
            if (current.parent == modPageTransform)
            {
                return current;
            }

            if (!current.IsChildOf(modPageTransform))
            {
                return null;
            }

            current = current.parent;
        }

        return null;
    }

    private static void UpdateMainOptionLocalPositions()
    {
        var referenceRect = _contentScrollRect != null ? _contentScrollRect.content : null;
        foreach (var mainOptionObject in MainOptionObjects)
        {
            if (mainOptionObject == null)
            {
                continue;
            }

            mainOptionObject.UpdateLocalY(referenceRect);
        }
    }

    private static void SortOptionNavigationTargets()
    {
        OptionNavigationTargets.Sort(CompareOptionTargets);
    }

    private static int CompareOptionTargets(ModMenuSelectable left, ModMenuSelectable right)
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

        var leftRowY = GetOptionRowY(left);
        var rightRowY = GetOptionRowY(right);
        var rowComparison = rightRowY.CompareTo(leftRowY);
        if (rowComparison != 0)
        {
            return rowComparison;
        }

        if (TryGetPanelLocalCenter(left.AnchorRect, out var leftCenter) &&
            TryGetPanelLocalCenter(right.AnchorRect, out var rightCenter))
        {
            var yComparison = rightCenter.y.CompareTo(leftCenter.y);
            if (yComparison != 0)
            {
                return yComparison;
            }

            var xComparison = leftCenter.x.CompareTo(rightCenter.x);
            if (xComparison != 0)
            {
                return xComparison;
            }
        }

        return left.SelectionObject.GetInstanceID().CompareTo(right.SelectionObject.GetInstanceID());
    }

    private static float GetOptionRowY(ModMenuSelectable target)
    {
        if (target == null)
        {
            return float.NegativeInfinity;
        }

        if (OptionTargetOwners.TryGetValue(target, out var owner) && owner != null)
        {
            return owner.LocalY;
        }

        if (TryGetPanelLocalCenter(target.AnchorRect, out var center))
        {
            return center.y;
        }

        return float.NegativeInfinity;
    }

    private static void SortNavigationTargets(List<ModMenuSelectable> targets, bool horizontalSort)
    {
        if (targets == null || targets.Count <= 1)
        {
            return;
        }

        targets.Sort((left, right) => CompareByPanelPosition(left, right, horizontalSort));
    }

    private static int CompareByPanelPosition(ModMenuSelectable left,
        ModMenuSelectable right, bool horizontalSort)
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

        var leftHasCenter = TryGetPanelLocalCenter(left.AnchorRect, out var leftCenter);
        var rightHasCenter = TryGetPanelLocalCenter(right.AnchorRect, out var rightCenter);
        if (!leftHasCenter || !rightHasCenter)
        {
            return left.SelectionObject.GetInstanceID().CompareTo(right.SelectionObject.GetInstanceID());
        }

        if (horizontalSort)
        {
            var xComparison = leftCenter.x.CompareTo(rightCenter.x);
            if (xComparison != 0)
            {
                return xComparison;
            }

            return rightCenter.y.CompareTo(leftCenter.y);
        }

        var yComparison = rightCenter.y.CompareTo(leftCenter.y);
        if (yComparison != 0)
        {
            return yComparison;
        }

        return leftCenter.x.CompareTo(rightCenter.x);
    }

    private static GameObject GetCurrentSelectedObject()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return null;
        }

        return eventSystem.currentSelectedGameObject;
    }

    private static bool WasMouseUsedThisFrame()
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

    private static bool TryGetDirectionalInput(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (Time.unscaledTime < _nextDirectionalInputTime)
        {
            return false;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            direction = Vector2.up;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            direction = Vector2.down;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            direction = Vector2.left;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            direction = Vector2.right;
        }
        else
        {
            return false;
        }

        _nextDirectionalInputTime = Time.unscaledTime + DirectionalRepeatDelay;
        return true;
    }

    private static ModMenuSelectable ResolveCurrentNavigationTarget()
    {
        if (TryFindTargetByGameObject(GetCurrentSelectedObject(), out var currentTarget))
        {
            return currentTarget;
        }

        if (TryFindTargetBySelectionObject(_lastKnownSelectedObject, out var lastKnownTarget))
        {
            return lastKnownTarget;
        }

        return null;
    }

    private static ModMenuSelectable ResolveFallbackNavigationTarget()
    {
        if (TryFindTargetBySelectionObject(_lastMouseSelectedObject, out var mouseTarget))
        {
            return mouseTarget;
        }

        if (TryFindTargetBySelectionObject(_lastKnownSelectedObject, out var lastKnownTarget))
        {
            return lastKnownTarget;
        }

        return FindInitialNavigationTarget();
    }

    private static ModMenuSelectable FindInitialNavigationTarget()
    {
        var optionTarget = FindTopMostOptionTarget();
        if (optionTarget != null)
        {
            return optionTarget;
        }

        return FindBackButtonTarget();
    }

    private static ModMenuSelectable FindTopMostOptionTarget()
    {
        return GetFirstValidTarget(OptionNavigationTargets);
    }

    private static ModMenuSelectable FindBackButtonTarget()
    {
        if (TryFindTargetBySelectionObject(_topBackButton?.gameObject, out var backTarget))
        {
            return backTarget;
        }

        return NavigationTargets.Count > 0 ? NavigationTargets[0] : null;
    }

    private static ModMenuSelectable FindDirectionalTarget(ModMenuSelectable currentTarget, Vector2 direction)
    {
        if (currentTarget == null || direction.sqrMagnitude <= 0.001f)
        {
            return null;
        }

        var normalizedDirection = direction.normalized;
        var panelType = ResolvePanelType(currentTarget);
        switch (panelType)
        {
            case NavigationPanelType.Options:
                return FindDirectionalFromOptionsPanel(currentTarget, normalizedDirection);
            case NavigationPanelType.Tab:
                return FindDirectionalFromTabPanel(currentTarget, normalizedDirection);
            case NavigationPanelType.Bottom:
                return FindDirectionalFromBottomPanel(currentTarget, normalizedDirection);
            default:
                break;
        }

        if (!TryGetPanelLocalCenter(currentTarget.AnchorRect, out var currentCenter))
        {
            return null;
        }

        return FindBestDirectionalTarget(currentTarget, currentCenter, normalizedDirection, filter: null);
    }

    private static ModMenuSelectable FindDirectionalFromOptionsPanel(
        ModMenuSelectable currentTarget, Vector2 normalizedDirection)
    {
        if (normalizedDirection.x < -0.5f && !IsSliderNavigationTarget(currentTarget))
        {
            return ResolveTabTargetForCrossPanel();
        }

        if (Mathf.Abs(normalizedDirection.y) > Mathf.Abs(normalizedDirection.x))
        {
            var moveUp = normalizedDirection.y > 0f;
            var verticalTarget = FindAdjacentOptionVerticalTarget(currentTarget, moveUp);
            if (verticalTarget != null)
            {
                return verticalTarget;
            }

            if (!IsBoundaryTarget(currentTarget, OptionNavigationTargets, moveUp))
            {
                return null;
            }

            _lastTargetBeforeBottom = currentTarget;
            return ResolveBottomTargetForCrossPanel();
        }

        if (currentTarget.OwnerPanel == null)
        {
            return null;
        }

        var samePanelTarget = currentTarget.OwnerPanel.FindDirectionalTarget(currentTarget, normalizedDirection,
            optionOnly: true);
        if (samePanelTarget != null)
        {
            return samePanelTarget;
        }

        return FindAdjacentOptionVerticalTarget(currentTarget, normalizedDirection.y > 0f);
    }

    private static ModMenuSelectable FindDirectionalFromTabPanel(
        ModMenuSelectable currentTarget, Vector2 normalizedDirection)
    {
        if (normalizedDirection.x > 0.5f)
        {
            return ResolveOptionTargetForCrossPanel();
        }

        if (Mathf.Abs(normalizedDirection.y) <= Mathf.Abs(normalizedDirection.x))
        {
            return null;
        }

        var moveUp = normalizedDirection.y > 0f;
        var verticalTarget = FindVerticalTargetInList(currentTarget, TabNavigationTargets, moveUp);
        if (verticalTarget != null)
        {
            return verticalTarget;
        }

        if (!IsBoundaryTarget(currentTarget, TabNavigationTargets, moveUp))
        {
            return null;
        }

        _lastTargetBeforeBottom = currentTarget;
        return ResolveBottomTargetForCrossPanel();
    }

    private static ModMenuSelectable FindDirectionalFromBottomPanel(
        ModMenuSelectable currentTarget, Vector2 normalizedDirection)
    {
        if (normalizedDirection.y > 0.5f)
        {
            return ResolveReturnFromBottomTarget();
        }

        if (Mathf.Abs(normalizedDirection.x) > 0.5f)
        {
            return FindHorizontalTargetInList(currentTarget, BottomNavigationTargets, normalizedDirection.x > 0f);
        }

        return null;
    }

    private static ModMenuSelectable FindAdjacentOptionVerticalTarget(
        ModMenuSelectable currentTarget, bool moveUp)
    {
        if (currentTarget == null)
        {
            return null;
        }

        if (!TryGetPanelLocalCenter(currentTarget.AnchorRect, out var currentCenter))
        {
            return null;
        }

        ModMenuSelectable bestTarget = null;
        var bestRowDistance = float.MaxValue;
        var bestXDistance = float.MaxValue;
        var currentRowY = GetOptionRowY(currentTarget);

        foreach (var target in OptionNavigationTargets)
        {
            if (target == null || !target.IsValid() || ReferenceEquals(target, currentTarget))
            {
                continue;
            }

            var candidateRowY = GetOptionRowY(target);
            var rowDelta = candidateRowY - currentRowY;
            if (moveUp)
            {
                if (rowDelta <= 0.01f)
                {
                    continue;
                }
            }
            else if (rowDelta >= -0.01f)
            {
                continue;
            }

            if (!TryGetPanelLocalCenter(target.AnchorRect, out var candidateCenter))
            {
                continue;
            }

            var rowDistance = Mathf.Abs(rowDelta);
            var xDistance = Mathf.Abs(candidateCenter.x - currentCenter.x);
            var betterRow = rowDistance < bestRowDistance - 0.01f;
            var betterX = !betterRow && Mathf.Abs(rowDistance - bestRowDistance) <= 0.01f &&
                          xDistance < bestXDistance - 0.01f;
            if (!betterRow && !betterX)
            {
                continue;
            }

            bestTarget = target;
            bestRowDistance = rowDistance;
            bestXDistance = xDistance;
        }

        return bestTarget;
    }

    private static ModMenuSelectable FindVerticalTargetInList(
        ModMenuSelectable currentTarget, List<ModMenuSelectable> targets, bool moveUp)
    {
        if (currentTarget == null || targets == null || targets.Count == 0)
        {
            return null;
        }

        var currentIndex = FindTargetIndex(targets, currentTarget);
        if (currentIndex < 0)
        {
            return moveUp ? GetLastValidTarget(targets) : GetFirstValidTarget(targets);
        }

        var step = moveUp ? -1 : 1;
        for (var index = currentIndex + step; index >= 0 && index < targets.Count; index += step)
        {
            var candidate = targets[index];
            if (!IsSelectableTargetValid(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static ModMenuSelectable FindHorizontalTargetInList(
        ModMenuSelectable currentTarget, List<ModMenuSelectable> targets, bool moveRight)
    {
        if (currentTarget == null || targets == null || targets.Count == 0)
        {
            return null;
        }

        var currentIndex = FindTargetIndex(targets, currentTarget);
        if (currentIndex < 0)
        {
            return moveRight ? GetLastValidTarget(targets) : GetFirstValidTarget(targets);
        }

        var step = moveRight ? 1 : -1;
        for (var index = currentIndex + step; index >= 0 && index < targets.Count; index += step)
        {
            var candidate = targets[index];
            if (!IsSelectableTargetValid(candidate))
            {
                continue;
            }

            return candidate;
        }

        return currentTarget;
    }

    private static bool IsBoundaryTarget(ModMenuSelectable target,
        List<ModMenuSelectable> targets, bool moveUp)
    {
        if (target == null || targets == null || targets.Count == 0)
        {
            return false;
        }

        var boundaryTarget = moveUp ? GetFirstValidTarget(targets) : GetLastValidTarget(targets);
        return ReferenceEquals(boundaryTarget, target);
    }

    private static int FindTargetIndex(List<ModMenuSelectable> targets,
        ModMenuSelectable target)
    {
        if (targets == null || target == null)
        {
            return -1;
        }

        for (var index = 0; index < targets.Count; index++)
        {
            if (!ReferenceEquals(targets[index], target))
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static ModMenuSelectable ResolveTabTargetForCrossPanel()
    {
        return GetLastValidTarget(_lastTabTarget, TabNavigationTargets);
    }

    private static ModMenuSelectable ResolveOptionTargetForCrossPanel()
    {
        return GetLastValidTarget(_lastOptionTarget, OptionNavigationTargets);
    }

    private static ModMenuSelectable ResolveBottomTargetForCrossPanel()
    {
        return GetLastValidTarget(_lastBottomTarget, BottomNavigationTargets);
    }

    private static ModMenuSelectable ResolveReturnFromBottomTarget()
    {
        var optionTarget = ResolveOptionTargetForCrossPanel();
        if (optionTarget != null)
        {
            return optionTarget;
        }

        if (_lastTargetBeforeBottom != null && ResolvePanelType(_lastTargetBeforeBottom) != NavigationPanelType.Bottom &&
            _lastTargetBeforeBottom.IsValid())
        {
            return _lastTargetBeforeBottom;
        }

        var tabTarget = ResolveTabTargetForCrossPanel();
        if (tabTarget != null)
        {
            return tabTarget;
        }

        return GetFirstValidTarget(BottomNavigationTargets);
    }

    private static ModMenuSelectable GetFirstValidTarget(List<ModMenuSelectable> targets)
    {
        if (targets == null)
        {
            return null;
        }

        foreach (var target in targets)
        {
            if (!IsSelectableTargetValid(target))
            {
                continue;
            }

            return target;
        }

        return null;
    }

    private static ModMenuSelectable GetLastValidTarget(List<ModMenuSelectable> targets)
    {
        if (targets == null)
        {
            return null;
        }

        for (var index = targets.Count - 1; index >= 0; index--)
        {
            var target = targets[index];
            if (!IsSelectableTargetValid(target))
            {
                continue;
            }

            return target;
        }

        return null;
    }

    private static ModMenuSelectable GetLastValidTarget(ModMenuSelectable preferredTarget,
        List<ModMenuSelectable> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            return null;
        }

        if (IsSelectableTargetValid(preferredTarget))
        {
            foreach (var target in targets)
            {
                if (!ReferenceEquals(target, preferredTarget))
                {
                    continue;
                }

                return preferredTarget;
            }
        }

        return GetFirstValidTarget(targets);
    }

    private static bool IsSelectableTargetValid(ModMenuSelectable target)
    {
        return target != null && target.IsValid();
    }

    private static bool IsSliderNavigationTarget(ModMenuSelectable target)
    {
        if (target == null || target.SelectionObject == null)
        {
            return false;
        }

        return target.SelectionObject.GetComponent<Slider>() != null;
    }

    private static NavigationPanelType ResolvePanelType(ModMenuSelectable target)
    {
        if (target == null || target.SelectionObject == null)
        {
            return NavigationPanelType.Unknown;
        }

        if (IsTopActionNavigationTarget(target))
        {
            return NavigationPanelType.Bottom;
        }

        if (target.OwnerPanel == _listNavigationPanel)
        {
            return NavigationPanelType.Tab;
        }

        if (target.OwnerPanel == _contentNavigationPanel || target.IsOptionObject)
        {
            return NavigationPanelType.Options;
        }

        return NavigationPanelType.Unknown;
    }

    private static ModMenuSelectable FindBestDirectionalTarget(
        ModMenuSelectable currentTarget,
        Vector2 currentCenter,
        Vector2 normalizedDirection,
        Func<ModMenuSelectable, bool> filter)
    {
        var verticalDirection = Mathf.Abs(normalizedDirection.y) >= Mathf.Abs(normalizedDirection.x);
        ModMenuSelectable bestTarget = null;
        var bestScore = float.MaxValue;

        foreach (var target in NavigationTargets)
        {
            if (target == null || !target.IsValid() || ReferenceEquals(target, currentTarget))
            {
                continue;
            }

            if (filter != null && !filter(target))
            {
                continue;
            }

            if (!TryGetPanelLocalCenter(target.AnchorRect, out var targetCenter))
            {
                continue;
            }

            var delta = targetCenter - currentCenter;
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

            bestTarget = target;
            bestScore = score;
        }

        return bestTarget;
    }

    private static bool IsTopActionNavigationTarget(ModMenuSelectable target)
    {
        if (target == null || target.SelectionObject == null)
        {
            return false;
        }

        var selectionObject = target.SelectionObject;
        if (_topBackButton != null && selectionObject == _topBackButton.gameObject)
        {
            return true;
        }

        return _topResetButton != null && selectionObject == _topResetButton.gameObject;
    }

    private static bool TryHandleSliderDirectionalInput(ModMenuSelectable currentTarget, Vector2 direction)
    {
        if (currentTarget == null || Mathf.Abs(direction.x) < 0.5f || Mathf.Abs(direction.y) > 0.5f)
        {
            return false;
        }

        var slider = currentTarget.SelectionObject != null ? currentTarget.SelectionObject.GetComponent<Slider>() : null;
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

    private static bool TryFindTargetByGameObject(GameObject gameObject, out ModMenuSelectable target)
    {
        if (gameObject == null)
        {
            target = null;
            return false;
        }

        var directTarget = gameObject.GetComponent<ModMenuSelectable>() ??
                           gameObject.GetComponentInParent<ModMenuSelectable>();
        if (directTarget != null)
        {
            foreach (var navigationTarget in NavigationTargets)
            {
                if (navigationTarget != directTarget)
                {
                    continue;
                }

                target = directTarget;
                return true;
            }
        }

        var selectedTransform = gameObject.transform;
        foreach (var navigationTarget in NavigationTargets)
        {
            if (navigationTarget == null || !navigationTarget.IsValid())
            {
                continue;
            }

            var selectionObject = navigationTarget.SelectionObject;
            var anchorRect = navigationTarget.AnchorRect;
            if (selectionObject == null || anchorRect == null)
            {
                continue;
            }

            if (selectionObject == gameObject || anchorRect.gameObject == gameObject)
            {
                target = navigationTarget;
                return true;
            }

            var selectionTransform = selectionObject.transform;
            var anchorTransform = anchorRect.transform;
            if (selectedTransform.IsChildOf(selectionTransform) || selectedTransform.IsChildOf(anchorTransform))
            {
                target = navigationTarget;
                return true;
            }

            if (selectionTransform.IsChildOf(selectedTransform) || anchorTransform.IsChildOf(selectedTransform))
            {
                target = navigationTarget;
                return true;
            }
        }

        target = null;
        return false;
    }

    private static bool TryFindTargetBySelectionObject(GameObject selectionObject, out ModMenuSelectable target)
    {
        if (selectionObject != null)
        {
            foreach (var navigationTarget in NavigationTargets)
            {
                if (navigationTarget.SelectionObject != selectionObject)
                {
                    continue;
                }

                target = navigationTarget;
                return true;
            }
        }

        target = null;
        return false;
    }

    private static bool TryGetPanelLocalCenter(RectTransform rectTransform, out Vector2 center)
    {
        center = Vector2.zero;
        if (_panelRoot == null || rectTransform == null)
        {
            return false;
        }

        var panelRect = _panelRoot.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return false;
        }

        var worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
        var panelLocal = panelRect.InverseTransformPoint(worldCenter);
        center = new Vector2(panelLocal.x, panelLocal.y);
        return true;
    }

    private static void ApplyNavigationSelection(ModMenuSelectable target, bool ensureVisible)
    {
        if (target == null || !target.IsValid())
        {
            return;
        }

        var previousTarget = ResolveCurrentNavigationTarget();
        if (ensureVisible)
        {
            EnsureTargetVisible(target);
        }

        var eventSystem = EventSystem.current;
        var changedSelection = eventSystem == null || eventSystem.currentSelectedGameObject != target.SelectionObject;
        if (eventSystem != null && changedSelection)
        {
            eventSystem.SetSelectedGameObject(target.SelectionObject);
        }

        var selectable = target.SelectionObject.GetComponent<Selectable>();
        if (selectable != null && selectable.IsInteractable() && changedSelection)
        {
            selectable.Select();
        }

        if (ensureVisible)
        {
            EnsureTargetVisible(target);
        }

        TrackPanelSelectionState(target, previousTarget);
        _lastKnownSelectedObject = target.SelectionObject;
    }

    private static void TrackPanelSelectionState(ModMenuSelectable selectedTarget,
        ModMenuSelectable previousTarget)
    {
        if (selectedTarget == null)
        {
            return;
        }

        var selectedPanelType = ResolvePanelType(selectedTarget);
        switch (selectedPanelType)
        {
            case NavigationPanelType.Tab:
                _lastTabTarget = selectedTarget;
                _lastTargetBeforeBottom = selectedTarget;
                break;
            case NavigationPanelType.Options:
                _lastOptionTarget = selectedTarget;
                _lastTargetBeforeBottom = selectedTarget;
                break;
            case NavigationPanelType.Bottom:
                _lastBottomTarget = selectedTarget;
                if (previousTarget == null || !previousTarget.IsValid())
                {
                    return;
                }

                if (ResolvePanelType(previousTarget) == NavigationPanelType.Bottom)
                {
                    return;
                }

                _lastTargetBeforeBottom = previousTarget;
                break;
            default:
                break;
        }
    }

    private static void EnsureTargetVisible(ModMenuSelectable target)
    {
        if (target == null || target.AnchorRect == null)
        {
            return;
        }

        var scrolled = false;
        if (target.OwnerPanel != null)
        {
            scrolled = target.OwnerPanel.EnsureVisible(target.AnchorRect, ScrollIntoViewPadding);
        }

        if (scrolled)
        {
            return;
        }

        var ownerScrollRect = ResolveOwnerScrollRect(target.AnchorRect.transform);
        if (ownerScrollRect == null)
        {
            return;
        }

        EnsureVisibleInScrollRect(ownerScrollRect, target.AnchorRect, ScrollIntoViewPadding);
    }

    private static ScrollRect ResolveOwnerScrollRect(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return null;
        }

        if (_contentRoot != null && _contentScrollRect != null && targetTransform.IsChildOf(_contentRoot.transform))
        {
            return _contentScrollRect;
        }

        if (_listViewRoot != null && _listScrollRect != null && targetTransform.IsChildOf(_listViewRoot.transform))
        {
            return _listScrollRect;
        }

        return null;
    }

    private static bool EnsureVisibleInScrollRect(ScrollRect scrollRect, RectTransform targetRect, float padding)
    {
        if (scrollRect == null || targetRect == null)
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
        var referenceRect = viewportMask != null ? viewportMask.rectTransform : viewport;
        if (!TryGetVerticalBoundsInSpace(referenceRect, targetRect, out var targetTop, out var targetBottom))
        {
            return false;
        }

        if (!TryGetVerticalBoundsInSpace(referenceRect, referenceRect, out var viewportTopRaw, out var viewportBottomRaw))
        {
            return false;
        }

        var viewportTop = viewportTopRaw - padding;
        var viewportBottom = viewportBottomRaw + padding;
        var delta = 0f;
        if (targetTop > viewportTop)
        {
            delta = targetTop - viewportTop;
        }
        else if (targetBottom < viewportBottom)
        {
            delta = targetBottom - viewportBottom;
        }

        if (Mathf.Abs(delta) <= 0.01f)
        {
            return false;
        }

        var maxScrollY = ResolveMaxScrollY(content, viewport);
        if (maxScrollY <= 0.01f)
        {
            return false;
        }

        scrollRect.StopMovement();
        var contentPosition = content.anchoredPosition;
        var nextY = Mathf.Clamp(contentPosition.y - delta, 0f, maxScrollY);
        if (Mathf.Abs(nextY - contentPosition.y) <= 0.01f)
        {
            return false;
        }

        contentPosition.y = nextY;
        content.anchoredPosition = contentPosition;
        scrollRect.velocity = Vector2.zero;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - (nextY / maxScrollY));
        Canvas.ForceUpdateCanvases();
        return true;
    }

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

    private static bool TryGetVerticalBoundsInSpace(RectTransform referenceRect, RectTransform targetRect, out float top,
        out float bottom)
    {
        top = float.NegativeInfinity;
        bottom = float.PositiveInfinity;
        if (referenceRect == null || targetRect == null)
        {
            return false;
        }

        targetRect.GetWorldCorners(VisibilityCornerBuffer);
        for (var cornerIndex = 0; cornerIndex < VisibilityCornerBuffer.Length; cornerIndex++)
        {
            var localPoint = referenceRect.InverseTransformPoint(VisibilityCornerBuffer[cornerIndex]);
            if (localPoint.y > top)
            {
                top = localPoint.y;
            }

            if (localPoint.y < bottom)
            {
                bottom = localPoint.y;
            }
        }

        return !float.IsInfinity(top) && !float.IsInfinity(bottom);
    }

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
            var child = parent.GetChild(childIndex).TryCast<RectTransform>();
            if (child == null || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            child.GetWorldCorners(VisibilityCornerBuffer);
            for (var cornerIndex = 0; cornerIndex < VisibilityCornerBuffer.Length; cornerIndex++)
            {
                var localPoint = parent.InverseTransformPoint(VisibilityCornerBuffer[cornerIndex]);
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

    private static void UpdateNavigatorForTarget(ModMenuSelectable target)
    {
        if (_mouseInputMode || target == null || target.AnchorRect == null || _customNavigatorsRoot == null)
        {
            _navigatorVisuals?.SetVisible(false);
            return;
        }

        if (!_customNavigatorsRoot.activeInHierarchy)
        {
            _navigatorVisuals?.SetVisible(false);
            return;
        }

        _navigatorVisuals?.UpdateForTarget(target.AnchorRect);
    }

    private static void BuildMenu(Canvas canvas)
    {
        var rootParent = ResolveMenuRootParent(canvas, _optionsAnchorButton != null ? _optionsAnchorButton.transform : null);
        var rootRect = ModMenuObjectFactory.CreateRect(MenuRootName, rootParent);
        _menuRoot = rootRect.gameObject;
        _menuGroup = ModMenuObjectFactory.GetOrAddCanvasGroup(_menuRoot);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        SetMenuVisible(false);

        var backdropImage = ModMenuObjectFactory.CreateImage("BackDrop", _menuRoot.transform, out var backdropRect);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
        backdropImage.raycastTarget = true;

        var panelImage = ModMenuObjectFactory.CreateImage("Panel", backdropRect, out var panelRect);
        var panel = panelRect.gameObject;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        var canvasRect = canvas.GetComponent<RectTransform>();
        var canvasSize = canvasRect != null ? canvasRect.rect.size : new Vector2(1280f, 720f);
        var panelWidth = Mathf.Max(680f, Mathf.Min(980f, canvasSize.x * 0.78f));
        var panelHeight = Mathf.Max(460f, Mathf.Min(720f, canvasSize.y * 0.78f));
        panelHeight *= 1.5f;
        panelHeight = Mathf.Min(panelHeight, canvasSize.y * 0.95f);
        var topTrim = ResolveTopTrimAmount();
        panelHeight = Mathf.Max(360f, panelHeight);
        panelWidth = Mathf.Max(640f, panelWidth - (PanelEdgeInset * 8f));
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        panelRect.anchoredPosition = new Vector2(0f, -topTrim);

        ApplyPanelStyle(panelImage);
        _panelRoot = panel;
        EnsureCustomNavigators();
        SyncNavigatorContainerRect();
        var tabPanelWidth = ResolveTabPanelWidth(panelWidth);

        var tabPanelImage = ModMenuObjectFactory.CreateImage("TabPanel", panel.transform, out var tabPanelRect);
        var tabPanel = tabPanelRect.gameObject;
        ConfigureTabPanelRect(tabPanelRect, tabPanelWidth);
        ApplyPanelStyle(tabPanelImage);
        _tabPanelRoot = tabPanel;

        _listViewRoot = CreateScrollArea(tabPanel.transform, "ModList", out var listContentRect, out _, out _listScrollRect);
        var listRect = _listViewRoot.GetComponent<RectTransform>();
        ConfigureTabListRect(listRect);
        _listRoot = listContentRect.gameObject;
        ConfigureListLayout(listContentRect);

        var optionsPanelRect = ModMenuObjectFactory.CreateRect(OptionsPanelName, panel.transform);
        _optionsPanelRoot = optionsPanelRect.gameObject;
        ConfigureDetailContentRect(optionsPanelRect, tabPanelWidth);
        EnsureOptionsPanelHasNoImage();

        var title = CreateTextObject(panel.transform, "MOD OPTIONS", _textStyle, _textStyle.fontSize + 10f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        title.name = "MenuTitle";
        _titleObject = title;
        var titleRect = title.GetComponent<RectTransform>();
        ConfigureOptionsPanelTitleRect(titleRect, panelWidth, tabPanelWidth);

        var subtitle = CreateTextObject(title.transform, "SELECT A MOD", _textStyle, _textStyle.fontSize + 2f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        subtitle.name = "MenuSubtitle";
        _subtitleObject = subtitle;
        var subtitleRect = subtitle.GetComponent<RectTransform>();
        ConfigureOptionsPanelSubtitleRect(subtitleRect, titleRect);

        _contentRoot = CreateContentScrollArea(_optionsPanelRoot.transform, "ModContent", out _contentViewport,
            out _contentScrollRect);
        var contentRect = _contentRoot.GetComponent<RectTransform>();
        StretchToParent(contentRect);
        _contentRoot.SetActive(true);

        var topPanelRect = ModMenuObjectFactory.CreateRect(TopPanelName, panel.transform);
        _topPanelRoot = topPanelRect.gameObject;
        ConfigureTopPanelRect(topPanelRect, panelWidth, tabPanelWidth);

        _topButtonsRoot = CreateTopButtonsArea(_topPanelRoot.transform, out var topButtonsContentRect,
            out _topButtonsScrollRect);
        _topButtonsContentRoot = topButtonsContentRect.gameObject;

        var panelRectForScrollbars = panel.GetComponent<RectTransform>();
        _listScrollbar = null;
        _contentScrollbar = CreateVerticalScrollbar(panelRectForScrollbars, "ContentScrollbar");
        if (_contentScrollRect != null)
        {
            _contentScrollRect.verticalScrollbar = _contentScrollbar;
            _contentScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        SetScrollbarsVisible(false, false);
    }

    private static void BuildMenu(Canvas canvas, Transform optionsButtonTransform)
    {
        _optionsAnchorButton = _optionsAnchorButton ?? (optionsButtonTransform != null
            ? optionsButtonTransform.GetComponent<Button>()
            : null);
        BuildMenu(canvas);
    }

    private static void ShowModList()
    {
        if (_listViewRoot == null || _contentRoot == null)
        {
            return;
        }

        EnsureTopButtons();
        SyncTopButtonPositions();
        SetTopButtonsVisible(true, true);
        SetScrollbarsVisible(false, true);

        SetText(_titleObject, "MOD OPTIONS");
        _listViewRoot.SetActive(true);
        _contentRoot.SetActive(true);
        EnsureModListCurrent();
        OpenDefaultEntry();
    }

    private static void OpenModEntry(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId) || _contentRoot == null)
        {
            return;
        }

        var entry = ModMenuRegistry.FindEntry(entryId);
        if (entry == null)
        {
            return;
        }

        _selectedEntryId = entry.Id;
        UpdateEntryButtonStyles();
        SetText(_subtitleObject, entry.DisplayName);
        _listViewRoot.SetActive(true);
        _contentRoot.SetActive(true);
        SetScrollbarsVisible(false, true);
        BuildDynamicModPage(entry.Id);
    }

    private static void OnBackPressed()
    {
        CloseMenu();
    }

    private static void EnsureModListCurrent()
    {
        if (_listRoot == null)
        {
            return;
        }

        if (_registryVersion == ModMenuRegistry.Version && _listRoot.transform.childCount > 0)
        {
            return;
        }

        _registryVersion = ModMenuRegistry.Version;
        RebuildModOptionsCache();
        RefreshModList();
    }

    private static void RefreshModList()
    {
        if (_listRoot == null)
        {
            return;
        }

        ClearChildren(_listRoot.transform);
        EntryButtons.Clear();

        var entries = ModMenuRegistry.GetEntries();
        if (entries == null || entries.Count == 0)
        {
            CreateEmptyListLabel();
            _selectedEntryId = null;
            _contentRoot?.SetActive(false);
            ClearDynamicModPage();
            SetText(_subtitleObject, "NO MODS REGISTERED");
            return;
        }

        var sorted = new List<ModMenuEntry>(entries.Count);
        sorted.AddRange(entries);
        sorted.Sort(CompareEntries);

        foreach (var entry in sorted)
        {
            var button = CreateButton(_listRoot.transform, $"ModButton_{entry.Id}", entry.DisplayName,
                Gray, LightGray, Blue, Dark);
            if (button == null)
            {
                continue;
            }

            var layout = ModMenuObjectFactory.GetOrAddLayoutElement(button.gameObject);
            layout.ignoreLayout = false;
            layout.preferredHeight = 56f;
            layout.minHeight = 56f;

            var buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                buttonRect.localScale = Vector3.one;
            }

            ConfigureTabPanelButtonText(button);

            var entryId = entry.Id;
            EntryButtons[entryId] = button;
            AddClickListener(button, () => OpenModEntry(entryId));
        }

        var hasSelectedEntry = false;
        if (!string.IsNullOrWhiteSpace(_selectedEntryId))
        {
            if (sorted.Any(t => t.Id.Equals(_selectedEntryId, StringComparison.OrdinalIgnoreCase)))
            {
                hasSelectedEntry = true;
            }
        }

        if (!hasSelectedEntry)
        {
            _selectedEntryId = sorted[0].Id;
        }

        UpdateEntryButtonStyles();

        var listRect = _listRoot.GetComponent<RectTransform>();
        if (listRect == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        OpenDefaultEntry();
    }

    private static void OpenDefaultEntry()
    {
        if (string.IsNullOrWhiteSpace(_selectedEntryId))
        {
            SetText(_subtitleObject, "NO MODS REGISTERED");
            return;
        }

        OpenModEntry(_selectedEntryId);
    }

    private static void RebuildModOptionsCache()
    {
        ModOptionsById.Clear();
        var modOptions = ModMenuRegistry.GetModOptionsById();
        if (modOptions == null || modOptions.Count == 0)
        {
            return;
        }

        foreach (var pair in modOptions)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
            {
                continue;
            }

            var sectionBuilders = new List<Action<ModMenuBuilder>>(pair.Value.Count);
            foreach (var sectionBuilder in pair.Value)
            {
                if (sectionBuilder == null)
                {
                    continue;
                }

                sectionBuilders.Add(sectionBuilder);
            }

            ModOptionsById[pair.Key] = sectionBuilders;
        }
    }

    private static void BuildDynamicModPage(string entryId)
    {
        if (_contentViewport == null || string.IsNullOrWhiteSpace(entryId))
        {
            return;
        }

        DestroyDropdownOverlays();
        var pageRect = EnsureDynamicModPageRect();
        if (pageRect == null)
        {
            return;
        }

        ClearChildren(pageRect.transform);
        var builder = new ModMenuBuilder(pageRect, _textStyle, AddClickListener);
        if (!ModOptionsById.TryGetValue(entryId, out var sectionBuilders) || sectionBuilders == null ||
            sectionBuilders.Count == 0)
        {
            builder.AddLabel("No options registered.");
            ActivateDynamicModPage(pageRect);
            return;
        }

        foreach (var sectionBuilder in sectionBuilders)
        {
            if (sectionBuilder == null)
            {
                continue;
            }

            try
            {
                sectionBuilder(builder);
            }
            catch (Exception exception)
            {
                MelonLogger.Error($"[SurvivorModMenu] Failed to build options for '{entryId}': {exception}");
            }
        }

        ActivateDynamicModPage(pageRect);
    }

    private static RectTransform EnsureDynamicModPageRect()
    {
        if (_contentViewport == null)
        {
            return null;
        }

        if (_modPageRoot == null)
        {
            var pageRect = ModMenuObjectFactory.CreateRect("ModPage", _contentViewport);
            _modPageRoot = pageRect.gameObject;
            pageRect.anchorMin = new Vector2(0f, 1f);
            pageRect.anchorMax = new Vector2(1f, 1f);
            pageRect.pivot = new Vector2(0.5f, 1f);
            pageRect.anchoredPosition = Vector2.zero;
            pageRect.sizeDelta = Vector2.zero;
            return pageRect;
        }

        var existingRect = _modPageRoot.GetComponent<RectTransform>();
        if (existingRect == null)
        {
            return null;
        }

        if (_modPageRoot.transform.parent != _contentViewport)
        {
            _modPageRoot.transform.SetParent(_contentViewport, false);
        }

        existingRect.anchorMin = new Vector2(0f, 1f);
        existingRect.anchorMax = new Vector2(1f, 1f);
        existingRect.pivot = new Vector2(0.5f, 1f);
        existingRect.anchoredPosition = Vector2.zero;
        existingRect.sizeDelta = Vector2.zero;
        return existingRect;
    }

    private static void ActivateDynamicModPage(RectTransform pageRect)
    {
        if (_contentScrollRect == null || pageRect == null)
        {
            return;
        }

        _modPageRoot?.SetActive(true);
        _contentScrollRect.content = pageRect;
        LayoutRebuilder.ForceRebuildLayoutImmediate(pageRect);
        Canvas.ForceUpdateCanvases();
        _contentScrollRect.verticalNormalizedPosition = 1f;
        SetScrollbarsVisible(false, true);
    }

    private static void ClearDynamicModPage()
    {
        DestroyDropdownOverlays();
        if (_modPageRoot == null)
        {
            return;
        }

        ClearChildren(_modPageRoot.transform);
        _modPageRoot.SetActive(false);
        if (_contentScrollRect == null || _contentScrollRect.content == null)
        {
            return;
        }

        if (_contentScrollRect.content.gameObject == _modPageRoot)
        {
            _contentScrollRect.content = null;
        }
    }

    private static void DestroyDropdownOverlays()
    {
        if (_panelRoot == null)
        {
            return;
        }

        var panelTransform = _panelRoot.transform;
        var overlayObjects = new List<GameObject>();
        for (var childIndex = 0; childIndex < panelTransform.childCount; childIndex++)
        {
            var child = panelTransform.GetChild(childIndex);
            if (child == null)
            {
                continue;
            }

            var childName = child.name;
            if (string.IsNullOrWhiteSpace(childName) || !childName.StartsWith("DropdownOverlay_", StringComparison.Ordinal))
            {
                continue;
            }

            overlayObjects.Add(child.gameObject);
        }

        foreach (var overlay in overlayObjects)
        {
            if (overlay == null)
            {
                continue;
            }

            UnityEngine.Object.Destroy(overlay);
        }
    }

    private static void UpdateEntryButtonStyles()
    {
        foreach (var kvp in EntryButtons)
        {
            var entryId = kvp.Key;
            var button = kvp.Value;
            if (button == null)
            {
                continue;
            }

            var selected = !string.IsNullOrWhiteSpace(_selectedEntryId) &&
                           entryId.Equals(_selectedEntryId, StringComparison.OrdinalIgnoreCase);
            var normal = selected ? Blue : Gray;
            var pressed = selected ? Green : Blue;
            ApplyButtonStyle(button, normal, LightGray, pressed, Dark);
        }
    }

    private static int CompareEntries(ModMenuEntry left, ModMenuEntry right)
    {
        switch (left)
        {
            case null when right == null:
                return 0;
            case null:
                return 1;
        }

        if (right == null)
        {
            return -1;
        }

        var order = left.SortOrder.CompareTo(right.SortOrder);
        if (order != 0)
        {
            return order;
        }

        return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private static void CreateEmptyListLabel()
    {
        var rowRect = ModMenuObjectFactory.CreateRect("EmptyLabel", _listRoot.transform);
        var row = rowRect.gameObject;

        var layout = ModMenuObjectFactory.GetOrAddLayoutElement(row);
        layout.preferredHeight = 56f;
        layout.minHeight = 56f;

        var label = CreateTextObject(row.transform, "No mods registered.", _textStyle, _textStyle.fontSize + 2f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        StretchToParent(label.GetComponent<RectTransform>());

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
    }

    private static float ResolveTabPanelWidth(float panelWidth)
    {
        var width = panelWidth * TabPanelWidthPercent;
        return Mathf.Clamp(width, TabPanelMinWidth, TabPanelMaxWidth);
    }

    private static void ConfigureTabPanelRect(RectTransform rect, float tabPanelWidth)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(TabPanelSidePadding, 0f);
        rect.offsetMax = new Vector2(TabPanelSidePadding + tabPanelWidth, 0f);
    }

    private static void ConfigureTabListRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(TabPanelInnerPadding, TabPanelInnerPadding + TabPanelButtonVerticalGap);
        rect.offsetMax = new Vector2(-TabPanelInnerPadding, -(TabPanelInnerPadding + TabPanelButtonVerticalGap));
    }

    private static void ConfigureDetailContentRect(RectTransform rect, float tabPanelWidth)
    {
        if (rect == null)
        {
            return;
        }

        var leftOffset = ResolveDetailLeftOffset(tabPanelWidth);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(leftOffset, ContentBottomPadding);
        rect.offsetMax = new Vector2(-ContentSidePadding, -ContentTopPadding);
    }

    private static void ConfigureTopPanelRect(RectTransform rect, float panelWidth, float tabPanelWidth)
    {
        if (rect == null)
        {
            return;
        }

        var leftOffset = ResolveDetailLeftOffset(tabPanelWidth);
        var rightOffset = ContentSidePadding + ScrollbarWidth + ScrollbarEdgePadding;
        var width = Mathf.Max(120f, panelWidth - leftOffset - rightOffset);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(BottomPanelPositionX, BottomPanelPositionY);
        rect.sizeDelta = new Vector2(width, TopPanelHeight);
    }

    private static void ConfigureOptionsPanelTitleRect(RectTransform rect, float panelWidth, float tabPanelWidth)
    {
        if (rect == null)
        {
            return;
        }

        var detailLeftOffset = ResolveDetailLeftOffset(tabPanelWidth);
        var availableWidth = Mathf.Max(120f, panelWidth - detailLeftOffset - ContentSidePadding);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(10f, -28f);
        rect.sizeDelta = new Vector2(availableWidth, 60f);
    }

    private static void ConfigureOptionsPanelSubtitleRect(RectTransform rect, RectTransform titleRect)
    {
        if (rect == null)
        {
            return;
        }

        var titleWidth = titleRect != null ? Mathf.Max(120f, titleRect.sizeDelta.x) : 420f;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -44f);
        rect.sizeDelta = new Vector2(titleWidth, 40f);
    }

    private static float ResolveDetailLeftOffset(float tabPanelWidth)
    {
        return TabPanelSidePadding + tabPanelWidth + TabPanelGap + ContentLeftGapFromSeparator;
    }

    private static GameObject CreateScrollArea(Transform parent, string name, out RectTransform contentRect,
        out RectTransform viewportRect, out ScrollRect scrollRect)
    {
        scrollRect = ModMenuObjectFactory.CreateScrollRect(name, parent, out var rootRect);
        var root = rootRect.gameObject;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        var viewportImage = ModMenuObjectFactory.CreateImage("Viewport", rootRect, out viewportRect);
        var viewport = viewportRect.gameObject;
        StretchToParent(viewportRect);

        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;
        ModMenuObjectFactory.GetOrAddRectMask2D(viewport);

        contentRect = ModMenuObjectFactory.CreateRect("Content", viewportRect);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        return root;
    }

    private static GameObject CreateContentScrollArea(Transform parent, string name, out RectTransform viewportRect,
        out ScrollRect scrollRect)
    {
        scrollRect = ModMenuObjectFactory.CreateScrollRect(name, parent, out var rootRect);
        var root = rootRect.gameObject;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        var viewportImage = ModMenuObjectFactory.CreateImage("Viewport", rootRect, out viewportRect);
        var viewport = viewportRect.gameObject;
        StretchToParent(viewportRect);

        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;
        ModMenuObjectFactory.GetOrAddRectMask2D(viewport);

        scrollRect.viewport = viewportRect;
        scrollRect.content = null;

        return root;
    }

    private static GameObject CreateTopButtonsArea(Transform parent, out RectTransform contentRect,
        out ScrollRect scrollRect)
    {
        var root = CreateScrollArea(parent, TopButtonsRootName, out contentRect, out var viewportRect, out scrollRect);
        var rootRect = root.GetComponent<RectTransform>();
        StretchToParent(rootRect);

        scrollRect.horizontal = false;
        scrollRect.vertical = false;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 0f;
        scrollRect.verticalScrollbar = null;
        scrollRect.horizontalScrollbar = null;

        var contentLayout = contentRect.GetComponent<HorizontalLayoutGroup>() ??
                            ModMenuObjectFactory.GetOrAddHorizontalLayoutGroup(contentRect.gameObject);
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlWidth = false;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = TopButtonSpacing;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);

        var contentFitter = contentRect.GetComponent<ContentSizeFitter>() ??
                            ModMenuObjectFactory.GetOrAddContentSizeFitter(contentRect.gameObject);
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        if (viewportRect != null)
        {
            viewportRect.offsetMin = new Vector2(TopPanelContentPadding, TopPanelContentPadding);
            viewportRect.offsetMax = new Vector2(-TopPanelContentPadding, -TopPanelContentPadding);
        }

        return root;
    }

    private static Scrollbar CreateVerticalScrollbar(RectTransform parent, string name)
    {
        var scrollbar = ModMenuObjectFactory.CreateScrollbar(name, parent, out var scrollbarRect, out var trackImage);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-(ScrollbarWidth + ScrollbarEdgePadding), ContentBottomPadding);
        scrollbarRect.offsetMax = new Vector2(-ScrollbarEdgePadding, -ContentTopPadding);

        ApplyRoundedImage(trackImage);
        trackImage.color = TrackbarDarkGray;
        trackImage.raycastTarget = true;

        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var slidingAreaRect = ModMenuObjectFactory.CreateRect("Sliding Area", scrollbarRect);
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(2f, 2f);
        slidingAreaRect.offsetMax = new Vector2(-2f, -2f);

        var handleImage = ModMenuObjectFactory.CreateImage("Handle", slidingAreaRect, out var handleRect);
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        ApplyRoundedImage(handleImage);
        handleImage.color = new Color(0.35f, 0.64f, 0.95f, 0.95f);
        handleImage.raycastTarget = true;

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        scrollbar.size = 0.2f;
        scrollbar.value = 1f;

        return scrollbar;
    }

    private static void SetScrollbarsVisible(bool showList, bool showContent)
    {
        _listScrollbar?.gameObject.SetActive(showList);
        _contentScrollbar?.gameObject.SetActive(showContent);
    }

    private static void ConfigureListLayout(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        var layoutGroup = rect.GetComponent<VerticalLayoutGroup>() ?? ModMenuObjectFactory.GetOrAddVerticalLayoutGroup(rect.gameObject);
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 10f;
        var listInnerReduction = TabPanelInnerPadding * 2f;
        var sideInset = Mathf.Max(0f, (TabButtonWidthReduction - listInnerReduction) * 0.5f);
        var sideInsetInt = Mathf.RoundToInt(sideInset);
        layoutGroup.padding = new RectOffset(sideInsetInt, sideInsetInt, 0, 0);

        var fitter = rect.GetComponent<ContentSizeFitter>() ?? ModMenuObjectFactory.GetOrAddContentSizeFitter(rect.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static void CacheMenuParts()
    {
        if (_menuRoot == null)
        {
            return;
        }

        if (_menuGroup == null)
        {
            _menuGroup = _menuRoot.GetComponent<CanvasGroup>();
        }

        var panel = _menuRoot.transform.Find("Panel");
        if (panel == null)
        {
            panel = _menuRoot.transform.Find("BackDrop/Panel");
        }

        if (panel != null)
        {
            _panelRoot = panel.gameObject;
        }

        if (_panelRoot == null)
        {
            return;
        }

        var customNavigators = _menuRoot.transform.Find(CustomNavigatorsRootName);
        if (customNavigators == null)
        {
            customNavigators = _panelRoot.transform.Find(CustomNavigatorsRootName);
        }

        _customNavigatorsRoot = customNavigators != null ? customNavigators.gameObject : _customNavigatorsRoot;
        EnsureCustomNavigators();

        var tabPanel = _panelRoot.transform.Find("TabPanel");
        _tabPanelRoot = tabPanel != null ? tabPanel.gameObject : _tabPanelRoot;

        var optionsPanel = _panelRoot.transform.Find(OptionsPanelName);
        _optionsPanelRoot = optionsPanel != null ? optionsPanel.gameObject : _optionsPanelRoot;
        EnsureOptionsPanelHasNoImage();

        var topPanel = _panelRoot.transform.Find(TopPanelName);
        _topPanelRoot = topPanel != null ? topPanel.gameObject : _topPanelRoot;
        EnsureBottomPanelHasNoImage();

        var topButtons = _topPanelRoot != null ? _topPanelRoot.transform.Find(TopButtonsRootName) : null;
        _topButtonsRoot = topButtons != null ? topButtons.gameObject : _topButtonsRoot;
        var topButtonsContent = _topPanelRoot != null ? _topPanelRoot.transform.Find($"{TopButtonsRootName}/Viewport/Content") : null;
        _topButtonsContentRoot = topButtonsContent != null ? topButtonsContent.gameObject : _topButtonsContentRoot;
        _topButtonsScrollRect = _topButtonsRoot != null ? _topButtonsRoot.GetComponent<ScrollRect>() : _topButtonsScrollRect;

        var listParent = _tabPanelRoot != null ? _tabPanelRoot.transform : _panelRoot.transform;
        var listView = listParent.Find("ModList");
        _listViewRoot = listView != null ? listView.gameObject : _listViewRoot;
        _listScrollRect = _listViewRoot != null ? _listViewRoot.GetComponent<ScrollRect>() : _listScrollRect;

        var list = listParent.Find("ModList/Viewport/Content");
        _listRoot = list != null ? list.gameObject : _listRoot;
        if (_listRoot == null && _listViewRoot != null)
        {
            _listRoot = _listViewRoot;
        }

        var listScrollbar = _panelRoot.transform.Find("ListScrollbar");
        _listScrollbar = listScrollbar != null ? listScrollbar.GetComponent<Scrollbar>() : _listScrollbar;
        if (_listScrollRect != null && _listScrollbar != null)
        {
            _listScrollRect.verticalScrollbar = _listScrollbar;
            _listScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        var contentParent = _optionsPanelRoot != null ? _optionsPanelRoot.transform : _panelRoot.transform;
        var content = contentParent.Find("ModContent");
        _contentRoot = content != null ? content.gameObject : _contentRoot;
        _contentScrollRect = _contentRoot != null ? _contentRoot.GetComponent<ScrollRect>() : _contentScrollRect;

        var contentViewport = contentParent.Find("ModContent/Viewport");
        _contentViewport = contentViewport != null ? contentViewport.GetComponent<RectTransform>() : _contentViewport;
        if (_contentViewport == null && _contentRoot != null)
        {
            _contentViewport = _contentRoot.GetComponent<RectTransform>();
        }

        var modPage = contentParent.Find("ModContent/Viewport/ModPage");
        _modPageRoot = modPage != null ? modPage.gameObject : _modPageRoot;

        var contentScrollbar = _panelRoot.transform.Find("ContentScrollbar");
        _contentScrollbar = contentScrollbar != null ? contentScrollbar.GetComponent<Scrollbar>() : _contentScrollbar;
        if (_contentScrollRect != null && _contentScrollbar != null)
        {
            _contentScrollRect.verticalScrollbar = _contentScrollbar;
            _contentScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        var title = _panelRoot.transform.Find("MenuTitle");
        if (title == null)
        {
            title = _panelRoot.transform.Find($"{OptionsPanelName}/MenuTitle");
        }

        _titleObject = title != null ? title.gameObject : _titleObject;

        var subtitle = _panelRoot.transform.Find("MenuSubtitle");
        if (subtitle == null)
        {
            subtitle = _panelRoot.transform.Find($"{OptionsPanelName}/MenuTitle/MenuSubtitle");
        }

        _subtitleObject = subtitle != null ? subtitle.gameObject : _subtitleObject;

        var topBack = GameObject.Find(TopBackButtonName);
        if (topBack != null)
        {
            _topBackButton = topBack.GetComponent<Button>();
        }

        var topReset = GameObject.Find(TopResetButtonName);
        if (topReset != null)
        {
            _topResetButton = topReset.GetComponent<Button>();
        }
    }

    private static void SetText(GameObject root, string text)
    {
        if (root == null)
        {
            return;
        }

        var tmpText = root.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.SetText(text);
            return;
        }

        var uiText = root.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = text;
            return;
        }

        TrySetButtonLabel(root, text);
    }

    private static void EnsureOptionsPanelHasNoImage()
    {
        if (_optionsPanelRoot == null)
        {
            return;
        }

        var image = _optionsPanelRoot.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        DestroyComponentNow(image);
    }

    private static void EnsureBottomPanelHasNoImage()
    {
        if (_topPanelRoot == null)
        {
            return;
        }

        var image = _topPanelRoot.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        DestroyComponentNow(image);
    }

    private static void EnsureTopButtons()
    {
        if (_optionsAnchorButton == null)
        {
            return;
        }

        if (_topBackButton == null)
        {
            var existingBack = GameObject.Find(TopBackButtonName);
            if (existingBack != null)
            {
                _topBackButton = existingBack.GetComponent<Button>();
            }
        }

        if (_topResetButton == null)
        {
            var existingReset = GameObject.Find(TopResetButtonName);
            if (existingReset != null)
            {
                _topResetButton = existingReset.GetComponent<Button>();
            }
        }

        var parent = _topButtonsContentRoot != null ? _topButtonsContentRoot.transform : _panelRoot?.transform;
        if (parent == null)
        {
            return;
        }

        if (_topBackButton == null)
        {
            _topBackButton = CloneButtonFromTemplate(_optionsAnchorButton, parent, TopBackButtonName);
            if (_topBackButton == null)
            {
                _topBackButton = CreateButton(parent, TopBackButtonName, "BACK", Gray, LightGray, Blue, Dark);
            }

            ConfigureTopPanelButtonLayout(_topBackButton);
        }

        if (_topResetButton == null)
        {
            _topResetButton = CloneButtonFromTemplate(_optionsAnchorButton, parent, TopResetButtonName);
            if (_topResetButton == null)
            {
                _topResetButton = CreateButton(parent, TopResetButtonName, "RESET GAME", Red, LightGray, Blue, Dark);
            }

            ConfigureTopPanelButtonLayout(_topResetButton);
        }

        if (_topBackButton != null)
        {
            RemoveOptionsButtonComponent(_topBackButton.gameObject);
            ConfigureTopButton(_topBackButton, "BACK", OnBackPressed, 0f, BackButtonSpriteName);
        }

        if (_topResetButton != null)
        {
            RemoveOptionsButtonComponent(_topResetButton.gameObject);
            ConfigureTopButton(_topResetButton, "RESET GAME", ResetGame, 12f, ResetButtonSpriteName);
        }

        SyncTopButtonsInTopPanel();
    }

    private static void ConfigureTopButton(Button button, string label, Action onClick, float labelYOffset,
        string spriteName)
    {
        if (button == null)
        {
            return;
        }

        ResetButtonClick(button);
        SetButtonLabel(button, label);
        ApplyButtonSprite(button, spriteName);
        button.interactable = true;
        AdjustButtonLabelOffset(button, labelYOffset);
        AddClickListener(button, onClick);
    }

    private static void ConfigureTopPanelButtonLayout(Button button)
    {
        if (button == null)
        {
            return;
        }

        var usesTopPanelLayout = _topButtonsContentRoot != null;
        if (!usesTopPanelLayout)
        {
            EnsureIgnoreLayout(button.gameObject);
            return;
        }

        var layout = button.GetComponent<LayoutElement>() ?? ModMenuObjectFactory.GetOrAddLayoutElement(button.gameObject);
        layout.ignoreLayout = false;
        layout.preferredWidth = ResolveTopButtonWidth();
        layout.minWidth = ResolveTopButtonWidth();
        layout.preferredHeight = ResolveTopButtonHeight();
        layout.minHeight = ResolveTopButtonHeight();
    }

    private static float ResolveTopButtonWidth()
    {
        var anchorRect = _optionsAnchorButton != null ? _optionsAnchorButton.GetComponent<RectTransform>() : null;
        if (anchorRect == null)
        {
            return TopButtonMinWidth;
        }

        var width = anchorRect.rect.width > 0.1f ? anchorRect.rect.width : anchorRect.sizeDelta.x;
        if (width <= 0.1f)
        {
            return TopButtonMinWidth;
        }

        return Mathf.Max(TopButtonMinWidth, width);
    }

    private static float ResolveTopButtonHeight()
    {
        var anchorRect = _optionsAnchorButton != null ? _optionsAnchorButton.GetComponent<RectTransform>() : null;
        if (anchorRect == null)
        {
            return TopButtonMinHeight;
        }

        var height = anchorRect.rect.height > 0.1f ? anchorRect.rect.height : anchorRect.sizeDelta.y;
        if (height <= 0.1f)
        {
            return TopButtonMinHeight;
        }

        return Mathf.Max(TopButtonMinHeight, height);
    }

    private static void SyncTopButtonsInTopPanel()
    {
        if (_topButtonsContentRoot == null)
        {
            return;
        }

        var contentRect = _topButtonsContentRoot.GetComponent<RectTransform>();
        if (contentRect == null)
        {
            return;
        }

        ConfigureTopPanelButtonParent(_topBackButton, contentRect);
        ConfigureTopPanelButtonParent(_topResetButton, contentRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();
    }

    private static void ConfigureTopPanelButtonParent(Button button, RectTransform contentRect)
    {
        if (button == null || contentRect == null)
        {
            return;
        }

        var buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            return;
        }

        if (buttonRect.parent != contentRect)
        {
            buttonRect.SetParent(contentRect, false);
        }

        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(ResolveTopButtonWidth(), ResolveTopButtonHeight());
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity;
        ConfigureTopPanelButtonLayout(button);
    }

    private static void AdjustButtonLabelOffset(Button button, float yOffset)
    {
        if (button == null || Mathf.Abs(yOffset) < 0.1f)
        {
            return;
        }

        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            var rect = tmpText.rectTransform;
            var id = rect.GetInstanceID();
            if (!LabelBaseY.TryGetValue(id, out var baseY))
            {
                baseY = rect.anchoredPosition.y;
                LabelBaseY[id] = baseY;
            }

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, baseY + yOffset);
            return;
        }

        var uiText = button.GetComponentInChildren<Text>(true);
        if (uiText == null)
        {
            return;
        }

        var uiRect = uiText.rectTransform;
        var uiId = uiRect.GetInstanceID();
        if (!LabelBaseY.TryGetValue(uiId, out var uiBaseY))
        {
            uiBaseY = uiRect.anchoredPosition.y;
            LabelBaseY[uiId] = uiBaseY;
        }

        uiRect.anchoredPosition = new Vector2(uiRect.anchoredPosition.x, uiBaseY + yOffset);
    }

    private static void ResetGame()
    {
        try
        {
            ResetUiState();
            Time.timeScale = 1f;

            const int targetIndex = 0;
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            if (sceneCount <= 0)
            {
                var active = SceneManager.GetActiveScene();
                if (!active.IsValid())
                {
                    return;
                }

                SceneManager.LoadScene(active.buildIndex, LoadSceneMode.Single);
                return;
            }

            SceneManager.LoadScene(targetIndex, LoadSceneMode.Single);
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static void ResetUiState()
    {
        // Clear all static state so menu UI is rebuilt cleanly after a reset.
        SetVanillaMenuNavigatorsActive(active: true);
        DestroyUiObjects();
        _modButton = null;
        _optionsAnchorButton = null;
        _menuRoot = null;
        _menuGroup = null;
        _panelRoot = null;
        _tabPanelRoot = null;
        _optionsPanelRoot = null;
        _topPanelRoot = null;
        _topButtonsRoot = null;
        _topButtonsContentRoot = null;
        _topButtonsScrollRect = null;
        _listViewRoot = null;
        _listRoot = null;
        _listScrollRect = null;
        _listScrollbar = null;
        _listNavigationPanel = null;
        _contentRoot = null;
        _contentViewport = null;
        _contentScrollRect = null;
        _contentScrollbar = null;
        _contentNavigationPanel = null;
        _vanillaMenuNavigators = null;
        _customNavigatorsRoot = null;
        _leftNavigatorAnimation = null;
        _rightNavigatorAnimation = null;
        _navigatorVisuals = null;
        _topBackButton = null;
        _topResetButton = null;
        _titleObject = null;
        _subtitleObject = null;
        _registryVersion = -1;
        _modPageRoot = null;
        _optionsWasActive = false;
        _modWasActive = false;
        _selectedEntryId = null;
        _lastKnownSelectedObject = null;
        _lastMouseSelectedObject = null;
        _lastMousePosition = Vector2.zero;
        _mouseInputMode = false;
        _nextDirectionalInputTime = 0f;
        _nextScanTime = 0f;
        _lastTabTarget = null;
        _lastOptionTarget = null;
        _lastBottomTarget = null;
        _lastTargetBeforeBottom = null;
        ModOptionsById.Clear();
        EntryButtons.Clear();
        LabelBaseY.Clear();
        NavigationTargets.Clear();
        TabNavigationTargets.Clear();
        OptionNavigationTargets.Clear();
        BottomNavigationTargets.Clear();
        OptionTargetOwners.Clear();
        MainOptionObjectsById.Clear();
        MainOptionObjects.Clear();
    }

    private static void DestroyUiObjects()
    {
        if (_menuRoot != null)
        {
            UnityEngine.Object.Destroy(_menuRoot);
        }

        if (_modButton != null)
        {
            UnityEngine.Object.Destroy(_modButton.gameObject);
        }

        if (_topBackButton != null)
        {
            UnityEngine.Object.Destroy(_topBackButton.gameObject);
        }

        if (_topResetButton != null)
        {
            UnityEngine.Object.Destroy(_topResetButton.gameObject);
        }
    }

    private static void SyncTopButtonPositions()
    {
        if (_topButtonsContentRoot != null)
        {
            SyncTopButtonsInTopPanel();
            return;
        }

        var panelRect = _panelRoot?.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return;
        }

        var panelHalfSize = panelRect.rect.size * 0.5f;

        PositionButtonFromAnchor(_optionsAnchorButton?.GetComponent<RectTransform>(),
            _topResetButton?.GetComponent<RectTransform>(), panelRect, panelHalfSize);

        PositionButtonFromAnchor(_modButton?.GetComponent<RectTransform>(),
            _topBackButton?.GetComponent<RectTransform>(), panelRect, panelHalfSize);
    }

    private static void PositionButtonFromAnchor(RectTransform source, RectTransform target, RectTransform panelRect,
        Vector2 panelHalfSize)
    {
        if (source == null || target == null || panelRect == null)
        {
            return;
        }

        if (target.parent != panelRect)
        {
            target.SetParent(panelRect, false);
        }

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.zero;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;

        var sourceSize = source.rect.size;
        if (sourceSize.x <= 0.1f || sourceSize.y <= 0.1f)
        {
            sourceSize = source.sizeDelta;
        }

        if (sourceSize.x <= 0.1f)
        {
            sourceSize.x = 220f;
        }

        if (sourceSize.y <= 0.1f)
        {
            sourceSize.y = 60f;
        }

        target.sizeDelta = sourceSize;

        // Convert source center into the panel's local anchored coordinate space.
        var canvas = panelRect.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var sourceCenter = source.TransformPoint(source.rect.center);
        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, sourceCenter);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPoint, camera,
                out var localPoint))
        {
            target.anchoredPosition = source.anchoredPosition + panelHalfSize;
            return;
        }

        target.anchoredPosition = localPoint + panelHalfSize;
    }

    private static float ResolveTopTrimAmount()
    {
        var anchorRect = _modButton != null
            ? _modButton.GetComponent<RectTransform>()
            : _optionsAnchorButton?.GetComponent<RectTransform>();
        if (anchorRect == null)
        {
            return 36f;
        }

        var height = anchorRect.rect.height > 0.1f ? anchorRect.rect.height : anchorRect.sizeDelta.y;
        if (height <= 0.1f)
        {
            height = 60f;
        }

        return height * 0.6f;
    }

    private static void SetTopButtonsVisible(bool visible, bool showReset)
    {
        _topBackButton?.gameObject.SetActive(visible);

        _topResetButton?.gameObject.SetActive(visible && showReset);
    }

    private static void CacheMenuButtonState()
    {
        _optionsWasActive = _optionsAnchorButton != null && _optionsAnchorButton.gameObject.activeSelf;
        _modWasActive = _modButton != null && _modButton.gameObject.activeSelf;
    }

    private static void EnsureMenuRootPlacement(Canvas canvas, Transform optionsButtonTransform)
    {
        if (_menuRoot == null)
        {
            return;
        }

        var desiredParent = ResolveMenuRootParent(canvas, optionsButtonTransform);
        if (desiredParent != null && _menuRoot.transform.parent != desiredParent)
        {
            _menuRoot.transform.SetParent(desiredParent, false);
        }

        if (_menuRoot.name != MenuRootName)
        {
            _menuRoot.name = MenuRootName;
        }

        var rootRect = _menuRoot.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            return;
        }

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        SyncNavigatorContainerRect();
    }

    private static Transform ResolveMenuRootParent(Canvas canvas, Transform optionsButtonTransform)
    {
        var safeAreaAncestor = FindAncestorByName(optionsButtonTransform, SafeAreaName);
        if (safeAreaAncestor != null)
        {
            return safeAreaAncestor;
        }

        var sceneSafeArea = FindSceneObjectByName(SafeAreaName);
        if (sceneSafeArea != null)
        {
            return sceneSafeArea.transform;
        }

        return canvas != null ? canvas.transform : null;
    }

    private static Transform FindAncestorByName(Transform start, string objectName)
    {
        if (start == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var current = start;
        while (current != null)
        {
            if (string.Equals(current.name, objectName, StringComparison.Ordinal))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static void ApplyMenuButtonVisibility(bool showOriginal)
    {
        _optionsAnchorButton?.gameObject.SetActive(showOriginal && _optionsWasActive);

        _modButton?.gameObject.SetActive(showOriginal && _modWasActive);
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        if (target.transform.parent != source.transform.parent)
        {
            target.SetParent(source.transform.parent, false);
        }

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private static void EnsureIgnoreLayout(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        var layoutElement = target.GetComponent<LayoutElement>() ?? ModMenuObjectFactory.GetOrAddLayoutElement(target);
        layoutElement.ignoreLayout = true;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddClickListener(Button button, Action callback)
    {
        if (button == null || callback == null)
        {
            return;
        }

        var unityAction = DelegateSupport.ConvertDelegate<UnityAction>(callback);
        if (unityAction == null)
        {
            return;
        }

        button.onClick.AddListener(unityAction);
    }

    private static void ResetButtonClick(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick = new Button.ButtonClickedEvent();
    }

    private static void RemoveOptionsButtonComponent(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        foreach (var optionsButton in target.GetComponentsInChildren<OptionsButton>(true))
        {
            if (optionsButton == null)
            {
                continue;
            }

            DestroyComponentNow(optionsButton);
        }

        RemoveComponentsByTypeName(target, OptionsButtonTypeName);
        RemoveComponentsByTypeName(target, SelectableUiTypeName);
    }

    private static void RemoveComponentsByTypeName(GameObject target, string typeName)
    {
        if (target == null || string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        foreach (var component in target.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            var type = component.GetType();
            if (type == null || !string.Equals(type.FullName, typeName, StringComparison.Ordinal))
            {
                continue;
            }

            DestroyComponentNow(component);
        }
    }

    private static void DestroyComponentNow(Component component)
    {
        if (component == null)
        {
            return;
        }

        try
        {
            UnityEngine.Object.DestroyImmediate(component);
            return;
        }
        catch (Exception)
        {
            // ignored
        }

        UnityEngine.Object.Destroy(component);
    }

    private static Button CloneButtonFromTemplate(Button templateButton, Transform parent, string name)
    {
        if (templateButton == null || parent == null)
        {
            return null;
        }

        var clone = UnityEngine.Object.Instantiate(templateButton.gameObject, parent, false);
        if (clone == null)
        {
            return null;
        }

        clone.name = name;
        var button = clone.GetComponent<Button>();
        if (button == null)
        {
            return null;
        }

        ResetButtonClick(button);
        EnsureIgnoreLayout(clone);
        return button;
    }

    private static Button FindButtonByLabel(string label, string nameToken)
    {
        foreach (var button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!IsSceneObject(button) || button.gameObject == null)
            {
                continue;
            }

            if (button.gameObject.name == ModButtonName)
            {
                continue;
            }

            if (!button.gameObject.activeInHierarchy)
            {
                continue;
            }

            var buttonText = GetButtonText(button.gameObject);
            var textMatches = !string.IsNullOrEmpty(buttonText) &&
                              buttonText.Trim().Equals(label, StringComparison.OrdinalIgnoreCase);
            var nameMatches = !string.IsNullOrEmpty(nameToken) &&
                              button.name.Contains(nameToken, StringComparison.OrdinalIgnoreCase);
            if (!textMatches && !nameMatches)
            {
                continue;
            }

            return button;
        }

        return null;
    }

    private static bool IsSceneObject(Component component)
    {
        if (component == null || component.gameObject == null)
        {
            return false;
        }

        var scene = component.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private static string GetButtonText(GameObject root)
    {
        var tmpText = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            return tmpText.text;
        }

        var uiText = root.GetComponentInChildren<Text>(true);
        return uiText != null ? uiText.text : string.Empty;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color normal, Color highlighted,
        Color pressed, Color disabled)
    {
        var button = ModMenuObjectFactory.CreateButton(name, parent, out _, out var image);
        ApplyRoundedImage(image);

        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ApplyButtonStyle(button, normal, highlighted, pressed, disabled);
        SetButtonLabel(button, label);

        return button;
    }

    private static void ApplyButtonStyle(Button button, Color normal, Color highlighted, Color pressed, Color disabled)
    {
        if (button == null)
        {
            return;
        }

        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = highlighted;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.color = normal;
    }

    private static void ApplyRoundedImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
    }

    private static Sprite GetRoundedSprite()
    {
        if (_roundedSprite != null)
        {
            return _roundedSprite;
        }

        const int size = 32;
        const int radius = 7;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SurvivorModMenu_ControllerRoundedTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = 0f;
                var dy = 0f;

                if (x < radius)
                {
                    dx = radius - x;
                }
                else if (x > size - radius - 1)
                {
                    dx = x - (size - radius - 1);
                }

                if (y < radius)
                {
                    dy = radius - y;
                }
                else if (y > size - radius - 1)
                {
                    dy = y - (size - radius - 1);
                }

                var index = (y * size) + x;
                var outside = (dx * dx) + (dy * dy) > radius * radius;
                pixels[index] = outside ? new Color32(255, 255, 255, 0) : new Color32(255, 255, 255, 255);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        _roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        _roundedSprite.name = "SurvivorModMenu_ControllerRoundedSprite";

        return _roundedSprite;
    }

    private static Sprite GetPanelFrameSprite()
    {
        if (_framePanelSprite != null)
        {
            return _framePanelSprite;
        }

        try
        {
            _framePanelSprite = SpriteManager.GetSprite("frame5_c4");
        }
        catch (Exception)
        {
            _framePanelSprite = null;
        }

        return _framePanelSprite;
    }

    private static void ApplyButtonSprite(Button button, string spriteName)
    {
        if (button == null || string.IsNullOrWhiteSpace(spriteName))
        {
            return;
        }

        var sprite = GetUiSprite(spriteName);
        if (sprite == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = ButtonSpritePixelsPerUnit;
        SetImageMemberFloat(image, "multipliedPixelsPerUnit", ButtonSpritePixelsPerUnit);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void SetImageMemberFloat(Image image, string memberName, float value)
    {
        if (image == null || string.IsNullOrWhiteSpace(memberName))
        {
            return;
        }

        // Il2Cpp Unity builds can expose this member as either a property or a backing field.
        var type = image.GetType();

        var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.CanWrite && property.PropertyType == typeof(float))
        {
            property.SetValue(image, value, null);
            return;
        }

        var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(float))
        {
            return;
        }

        field.SetValue(image, value);
    }

    private static Sprite GetUiSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        if (UiSpriteCache.TryGetValue(spriteName, out var cached))
        {
            return cached;
        }

        Sprite sprite;
        try
        {
            sprite = SpriteManager.GetSprite(spriteName);
        }
        catch (Exception)
        {
            sprite = null;
        }

        UiSpriteCache[spriteName] = sprite;
        return sprite;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        if (TrySetButtonLabel(button.gameObject, label))
        {
            return;
        }

        var textObject = CreateTextObject(button.transform, label, _textStyle,
            _textStyle.fontSize, TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ConfigureTabPanelButtonText(Button button)
    {
        if (button == null)
        {
            return;
        }

        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            var maxSize = _textStyle.fontSize > 0f ? _textStyle.fontSize : tmpText.fontSize;
            var minSize = Mathf.Min(maxSize, Mathf.Max(TabButtonMinFontSize, maxSize * 0.45f));

            tmpText.enableWordWrapping = true;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMax = maxSize;
            tmpText.fontSizeMin = minSize;
            tmpText.maxVisibleLines = TabButtonMaxVisibleLines;
            tmpText.ForceMeshUpdate();
            return;
        }

        var uiText = button.GetComponentInChildren<Text>(true);
        if (uiText == null)
        {
            return;
        }

        var maxSizeInt = _textStyle.fontSize > 0f ? Mathf.RoundToInt(_textStyle.fontSize) : uiText.fontSize;
        var minSizeInt = Mathf.RoundToInt(Mathf.Min(maxSizeInt, Mathf.Max(TabButtonMinFontSize, maxSizeInt * 0.45f)));

        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMaxSize = Mathf.Max(minSizeInt, maxSizeInt);
        uiText.resizeTextMinSize = minSizeInt;
    }

    private static bool TrySetButtonLabel(GameObject root, string label)
    {
        var tmpText = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            tmpText.SetText(label);
            return true;
        }

        var uiText = root.GetComponentInChildren<Text>(true);
        if (uiText == null)
        {
            return false;
        }

        uiText.text = label;
        return true;
    }

    private static void ApplyPanelStyle(Image panelImage)
    {
        if (panelImage == null)
        {
            return;
        }

        var frameSprite = GetPanelFrameSprite();
        if (frameSprite != null)
        {
            panelImage.sprite = frameSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.preserveAspect = false;
            panelImage.color = Color.white;
            panelImage.raycastTarget = true;
            return;
        }

        ApplyRoundedImage(panelImage);
        panelImage.color = new Color(0.22f, 0.25f, 0.35f, 1f);
        panelImage.raycastTarget = true;
    }

    private static GameObject CreateTextObject(Transform parent, string text, ModMenuTextStyle template,
        float fontSize, TextAnchor uiAlignment, TextAlignmentOptions tmpAlignment)
    {
        if (template.isTmp && template.tmpFont != null)
        {
            var tmp = ModMenuObjectFactory.CreateTmpText("Text", parent, out _);
            var tmpTextObject = tmp.gameObject;
            tmp.font = template.tmpFont;
            tmp.fontSize = fontSize;
            tmp.color = template.color;
            tmp.alignment = tmpAlignment;
            tmp.enableWordWrapping = false;
            tmp.SetText(text);
            tmp.raycastTarget = false;
            return tmpTextObject;
        }

        var rect = ModMenuObjectFactory.CreateRect("Text", parent);
        var textObject = rect.gameObject;
        var uiText = textObject.AddComponent<Text>();
        uiText.font = template.uiFont;
        uiText.fontSize = Mathf.RoundToInt(fontSize);
        uiText.color = template.color;
        uiText.alignment = uiAlignment;
        uiText.text = text;
        uiText.raycastTarget = false;

        return textObject;
    }

    private static ModMenuTextStyle BuildTextStyle(Button templateButton)
    {
        var tmpText = templateButton?.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            return new ModMenuTextStyle
            {
                isTmp = true,
                tmpFont = tmpText.font,
                uiFont = null,
                fontSize = tmpText.fontSize,
                color = tmpText.color
            };
        }

        var uiText = templateButton?.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            return new ModMenuTextStyle
            {
                isTmp = false,
                tmpFont = null,
                uiFont = uiText.font,
                fontSize = uiText.fontSize,
                color = uiText.color
            };
        }

        return new ModMenuTextStyle
        {
            isTmp = false,
            tmpFont = null,
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"),
            fontSize = 24f,
            color = Color.white
        };
    }
}
