using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorModMenu;

[RegisterTypeInIl2Cpp]
public sealed class ModMenuNavigationPanel : MonoBehaviour
{
    public ModMenuNavigationPanel(IntPtr ptr) : base(ptr)
    {
    }

    private static readonly Vector3[] CornerBuffer = new Vector3[4];

    private ScrollRect _scrollRect;
    private RectTransform _viewport;

    internal void Configure(ScrollRect scrollRect)
    {
        _scrollRect = scrollRect;
        _viewport = scrollRect != null ? scrollRect.viewport : null;
    }

    internal bool EnsureVisible(RectTransform targetRect, float padding)
    {
        if (_scrollRect == null || targetRect == null)
        {
            return false;
        }

        var content = _scrollRect.content;
        var viewport = _viewport != null ? _viewport : _scrollRect.viewport;
        if (content == null || viewport == null || !content.gameObject.activeInHierarchy ||
            !targetRect.gameObject.activeInHierarchy)
        {
            return false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        targetRect.GetWorldCorners(CornerBuffer);

        var targetTop = float.NegativeInfinity;
        var targetBottom = float.PositiveInfinity;
        for (var index = 0; index < CornerBuffer.Length; index++)
        {
            var localPoint = viewport.InverseTransformPoint(CornerBuffer[index]);
            if (localPoint.y > targetTop)
            {
                targetTop = localPoint.y;
            }

            if (localPoint.y < targetBottom)
            {
                targetBottom = localPoint.y;
            }
        }

        var viewportTop = viewport.rect.yMax - padding;
        var viewportBottom = viewport.rect.yMin + padding;

        var offset = 0f;
        if (targetTop > viewportTop)
        {
            offset = viewportTop - targetTop;
        }
        else if (targetBottom < viewportBottom)
        {
            offset = viewportBottom - targetBottom;
        }

        if (Mathf.Abs(offset) <= 0.01f)
        {
            return false;
        }

        _scrollRect.StopMovement();
        var contentPos = content.anchoredPosition;
        var nextY = contentPos.y + offset;

        var contentHeight = Mathf.Max(content.rect.height, LayoutUtility.GetPreferredHeight(content));
        if (contentHeight > content.rect.height + 0.1f)
        {
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        }

        var maxScroll = Mathf.Max(0f, contentHeight - viewport.rect.height);
        nextY = Mathf.Clamp(nextY, 0f, maxScroll);
        if (Mathf.Abs(nextY - contentPos.y) <= 0.01f)
        {
            return false;
        }

        contentPos.y = nextY;
        content.anchoredPosition = contentPos;

        Canvas.ForceUpdateCanvases();
        return true;
    }
}
