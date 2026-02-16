using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorModMenu;

[RegisterTypeInIl2Cpp]
public sealed class ModMenuPanel : MonoBehaviour
{
    private const float EnsureTraceIntervalSeconds = 1f;

    public ModMenuPanel(IntPtr ptr) : base(ptr)
    {
    }

    private static readonly Vector3[] CornerBuffer = new Vector3[4];
    private readonly List<ModMenuSelectable> _targets = new();

    private ScrollRect _scrollRect;
    private RectTransform _panelRect;
    private RectTransform _viewport;
    private RectMask2D _viewportMask;
    private float _nextEnsureTraceTime;

    internal void Configure(ScrollRect scrollRect)
    {
        _scrollRect = scrollRect;
        _panelRect = GetComponent<RectTransform>();
        _viewport = scrollRect?.viewport;
        _viewportMask = _viewport != null ? _viewport.GetComponent<RectMask2D>() : null;
    }

    internal void BeginTargetRegistration()
    {
        _targets.Clear();
    }

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

        foreach (var existingTarget in _targets)
        {
            if (!ReferenceEquals(existingTarget, target))
            {
                continue;
            }

            return;
        }

        _targets.Add(target);
    }

    internal bool ContainsTarget(ModMenuSelectable target)
    {
        if (target == null)
        {
            return false;
        }

        foreach (var existingTarget in _targets)
        {
            if (!ReferenceEquals(existingTarget, target))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    internal ModMenuSelectable FindTopMostTarget(bool optionOnly)
    {
        ModMenuSelectable bestTarget = null;
        var bestY = float.NegativeInfinity;
        var bestX = float.PositiveInfinity;

        foreach (var target in _targets)
        {
            if (!IsEligibleTarget(target, currentTarget: null, optionOnly))
            {
                continue;
            }

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

        foreach (var target in _targets)
        {
            if (!IsEligibleTarget(target, currentTarget, optionOnly))
            {
                continue;
            }

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

        foreach (var target in _targets)
        {
            if (!IsEligibleTarget(target, currentTarget, optionOnly))
            {
                continue;
            }

            if (!TryGetCenterInPanelSpace(target.AnchorRect, out var targetCenter))
            {
                continue;
            }

            var deltaY = targetCenter.y - currentCenter.y;
            if (moveUp && deltaY <= 0.01f)
            {
                continue;
            }

            if (!moveUp && deltaY >= -0.01f)
            {
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
            TraceEnsureVisible(targetRect, moved: false, "missing-content-or-target");
            return false;
        }

        var movedAny = false;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (!TryResolveScrollDelta(content, viewport, targetRect, padding, out var deltaY))
            {
                break;
            }

            if (Mathf.Abs(deltaY) <= 0.01f)
            {
                break;
            }

            if (!ApplyScrollDelta(content, viewport, deltaY))
            {
                break;
            }

            movedAny = true;
        }

        TraceEnsureVisible(targetRect, movedAny, movedAny ? "moved" : "no-op");
        return movedAny;
    }

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

        if (optionOnly && !target.IsOptionObject)
        {
            return false;
        }

        return true;
    }

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

    private bool TryResolveScrollDelta(RectTransform content, RectTransform viewport, RectTransform targetRect,
        float padding, out float deltaY)
    {
        deltaY = 0f;
        if (content == null || viewport == null || targetRect == null)
        {
            return false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        var maskRect = _viewportMask != null ? _viewportMask.rectTransform : viewport;
        if (TryResolveScrollDeltaInSpace(maskRect, maskRect, targetRect, padding, out deltaY))
        {
            if (Mathf.Abs(deltaY) > 0.01f)
            {
                return true;
            }
        }

        if (_panelRect != null && TryResolveScrollDeltaInSpace(_panelRect, viewport, targetRect, padding, out deltaY))
        {
            if (Mathf.Abs(deltaY) > 0.01f)
            {
                return true;
            }
        }

        deltaY = 0f;
        return true;
    }

    private static bool TryResolveScrollDeltaInSpace(RectTransform referenceRect, RectTransform viewportRect,
        RectTransform targetRect, float padding, out float deltaY)
    {
        deltaY = 0f;
        if (referenceRect == null || viewportRect == null || targetRect == null)
        {
            return false;
        }

        if (!TryGetVerticalBoundsInSpace(referenceRect, targetRect, out var targetTop, out var targetBottom))
        {
            return false;
        }

        if (!TryGetVerticalBoundsInSpace(referenceRect, viewportRect, out var viewportTopRaw, out var viewportBottomRaw))
        {
            return false;
        }

        var viewportTop = viewportTopRaw - padding;
        var viewportBottom = viewportBottomRaw + padding;
        if (targetTop > viewportTop)
        {
            deltaY = targetTop - viewportTop;
            return true;
        }

        if (targetBottom < viewportBottom)
        {
            deltaY = targetBottom - viewportBottom;
            return true;
        }

        return true;
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

        targetRect.GetWorldCorners(CornerBuffer);
        for (var index = 0; index < CornerBuffer.Length; index++)
        {
            var localPoint = referenceRect.InverseTransformPoint(CornerBuffer[index]);
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

    private bool ApplyScrollDelta(RectTransform content, RectTransform viewport, float deltaY)
    {
        var maxScrollY = ResolveMaxScrollY(content, viewport);
        if (maxScrollY <= 0.01f)
        {
            return false;
        }

        _scrollRect.StopMovement();

        var anchoredPosition = content.anchoredPosition;
        var nextY = Mathf.Clamp(anchoredPosition.y - deltaY, 0f, maxScrollY);
        if (Mathf.Abs(nextY - anchoredPosition.y) <= 0.01f)
        {
            return false;
        }

        anchoredPosition.y = nextY;
        content.anchoredPosition = anchoredPosition;
        _scrollRect.velocity = Vector2.zero;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - (nextY / maxScrollY));
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

            child.GetWorldCorners(CornerBuffer);
            for (var cornerIndex = 0; cornerIndex < CornerBuffer.Length; cornerIndex++)
            {
                var localPoint = parent.InverseTransformPoint(CornerBuffer[cornerIndex]);
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

    private void TraceEnsureVisible(RectTransform targetRect, bool moved, string state)
    {
#if DEBUG
        if (Time.unscaledTime < _nextEnsureTraceTime)
        {
            return;
        }

        _nextEnsureTraceTime = Time.unscaledTime + EnsureTraceIntervalSeconds;
        if (_scrollRect == null || _scrollRect.content == null || _viewport == null || targetRect == null)
        {
            MelonLogger.Msg($"[SurvivorModMenu][PanelEnsure] {state} missing refs moved={moved}");
            return;
        }

        var contentY = _scrollRect.content.anchoredPosition.y;
        var contentHeight = _scrollRect.content.rect.height;
        var viewportHeight = _viewport.rect.height;
        MelonLogger.Msg(
            $"[SurvivorModMenu][PanelEnsure] {state} moved={moved} target={targetRect.name} contentY={contentY:F1} contentH={contentHeight:F1} viewportH={viewportHeight:F1}");
#endif
    }
}
