
namespace SurvivorModMenu.ModMenu.Components;

/// <summary>
/// Navigation helper bound to a scrollable panel. It knows which targets belong to the panel
/// and can move the panel viewport to keep a selected target visible.
/// </summary>
[RegisterTypeInIl2Cpp]
public sealed class ModMenuPanel : MonoBehaviour
{
    public ModMenuPanel(IntPtr ptr) : base(ptr)
    {
    }

    private static readonly Vector3[] _cornerBuffer = new Vector3[4];
    private readonly List<ModMenuSelectable> _targets = new();

    private ScrollRect _scrollRect;
    private RectTransform _panelRect;
    private RectTransform _viewport;
    private RectMask2D _viewportMask;

    [HideFromIl2Cpp]
    internal void Configure(ScrollRect scrollRect)
    {
        _scrollRect = scrollRect;
        _panelRect = GetComponent<RectTransform>();
        _viewport = scrollRect?.viewport;
        _viewportMask = _viewport?.GetComponent<RectMask2D>();
    }

    [HideFromIl2Cpp]
    internal void BeginTargetRegistration()
    {
        _targets.Clear();
    }

    [HideFromIl2Cpp]
    internal void RegisterTarget(ModMenuSelectable target)
    {
        if (target == null || !target.IsValid())
        {
            return;
        }

        if (target.OwnerPanel != this)
        {
            return;
        }

        if (_targets.Any(existingTarget => ReferenceEquals(existingTarget, target)))
        {
            return;
        }

        _targets.Add(target);
    }

    [HideFromIl2Cpp]
    internal ModMenuSelectable FindTopMostTarget(bool optionOnly)
    {
        ModMenuSelectable bestTarget = null;
        var bestY = float.NegativeInfinity;
        var bestX = float.PositiveInfinity;

        foreach (var target in _targets.Where(target => IsEligibleTarget(target, currentTarget: null, optionOnly)))
        {
            if (!TryGetCenterInPanelSpace(target.AnchorRect, out var center))
            {
                continue;
            }

            var isBetter = center.y > bestY + 0.01f;
            if (!isBetter && Mathf.Abs(center.y - bestY) <= 0.01f)
            {
                isBetter = center.x < bestX;
            }

            if (!isBetter)
            {
                continue;
            }

            bestTarget = target;
            bestY = center.y;
            bestX = center.x;
        }

        return bestTarget;
    }

    [HideFromIl2Cpp]
    internal ModMenuSelectable FindDirectionalTarget(
        ModMenuSelectable currentTarget,
        Vector2 direction,
        bool optionOnly)
    {
        if (currentTarget == null || !currentTarget.IsValid())
        {
            return null;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            return null;
        }

        if (!TryGetCenterInPanelSpace(currentTarget.AnchorRect, out var currentCenter))
        {
            return null;
        }

        var normalizedDirection = direction.normalized;
        var verticalDirection = Mathf.Abs(normalizedDirection.y) >= Mathf.Abs(normalizedDirection.x);
        ModMenuSelectable bestTarget = null;
        var bestScore = float.MaxValue;

        foreach (var target in _targets.Where(target => IsEligibleTarget(target, currentTarget, optionOnly)))
        {
            if (!TryGetCenterInPanelSpace(target.AnchorRect, out var targetCenter))
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
            var score = (primaryDistance * 1000f) + (lateralDistance * 12f) +
                        (delta.sqrMagnitude * 0.05f);
            if (score >= bestScore)
            {
                continue;
            }

            bestTarget = target;
            bestScore = score;
        }

        return bestTarget;
    }

    [HideFromIl2Cpp]
    internal ModMenuSelectable FindAdjacentVerticalTarget(
        ModMenuSelectable currentTarget,
        bool moveUp,
        bool optionOnly)
    {
        if (currentTarget == null || !currentTarget.IsValid())
        {
            return null;
        }

        if (!TryGetCenterInPanelSpace(currentTarget.AnchorRect, out var currentCenter))
        {
            return null;
        }

        ModMenuSelectable bestTarget = null;
        var bestPrimary = float.MaxValue;
        var bestSecondary = float.MaxValue;

        foreach (var target in _targets.Where(target => IsEligibleTarget(target, currentTarget, optionOnly)))
        {
            if (!TryGetCenterInPanelSpace(target.AnchorRect, out var targetCenter))
            {
                continue;
            }

            var deltaY = targetCenter.y - currentCenter.y;
            switch (moveUp)
            {
                case true when deltaY <= 0.01f:
                case false when deltaY >= -0.01f:
                    continue;
            }

            var primaryDistance = Mathf.Abs(deltaY);
            var secondaryDistance = Mathf.Abs(targetCenter.x - currentCenter.x);
            var isBetter = primaryDistance < bestPrimary - 0.01f;
            if (!isBetter && Mathf.Abs(primaryDistance - bestPrimary) <= 0.01f)
            {
                isBetter = secondaryDistance < bestSecondary - 0.01f;
            }

            if (!isBetter)
            {
                continue;
            }

            bestTarget = target;
            bestPrimary = primaryDistance;
            bestSecondary = secondaryDistance;
        }

        return bestTarget;
    }

    /// <summary>
    /// Scrolls this panel so the target rect is fully visible inside the masked viewport.
    /// </summary>
    [HideFromIl2Cpp]
    internal bool EnsureVisible(RectTransform targetRect, float padding)
    {
        if (_scrollRect == null || targetRect == null)
        {
            return false;
        }

        var content = _scrollRect.content;
        var viewport = _viewport ?? _scrollRect.viewport;
        if (content == null || viewport == null || !content.gameObject.activeInHierarchy ||
            !targetRect.gameObject.activeInHierarchy)
        {
            return false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        var maskRect = _viewportMask != null ? _viewportMask.rectTransform : viewport;
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

        // Convert world-space values into a top-origin scroll axis so clamping is stable
        // regardless of pivot/anchor setup on the content rect.
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
        if (Mathf.Abs(_scrollRect.verticalNormalizedPosition - targetNormalized) <= 0.0005f)
        {
            return false;
        }

        _scrollRect.StopMovement();
        _scrollRect.velocity = Vector2.zero;
        _scrollRect.verticalNormalizedPosition = targetNormalized;
        Canvas.ForceUpdateCanvases();
        return true;
    }

    [HideFromIl2Cpp]
    private static bool IsEligibleTarget(ModMenuSelectable target,
        ModMenuSelectable currentTarget, bool optionOnly)
    {
        if (target == null || !target.IsValid())
        {
            return false;
        }

        if (ReferenceEquals(target, currentTarget))
        {
            return false;
        }

        return !optionOnly || target.IsOptionObject;
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

}
