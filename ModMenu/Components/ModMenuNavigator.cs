using Il2CppVampireSurvivors.UI;

namespace SurvivorModMenu.ModMenu.Components;

[RegisterTypeInIl2Cpp]
public sealed class ModMenuNavigator : MonoBehaviour
{
    private static readonly Vector3[] CornerBuffer = new Vector3[4];

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

    internal void Configure(RectTransform panelRect, UISpriteAnimation leftAnimation, UISpriteAnimation rightAnimation,
        float arrowSize, float objectGap)
    {
        _panelRect = panelRect;
        _containerRect = GetComponent<RectTransform>();
        _leftAnimation = leftAnimation;
        _rightAnimation = rightAnimation;
        _leftRect = leftAnimation != null ? leftAnimation.GetComponent<RectTransform>() : null;
        _rightRect = rightAnimation != null ? rightAnimation.GetComponent<RectTransform>() : null;
        _arrowSize = arrowSize;
        _objectGap = objectGap;
    }

    internal void UpdateForTarget(RectTransform targetRect)
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

    private void PositionIndicators(RectTransform targetRect)
    {
        if (!TryGetTargetBoundsInContainer(targetRect, out _, out var minX, out var maxX, out var minY,
                out var maxY))
        {
            return;
        }

        PositionIndicatorsForBounds(minX, maxX, (minY + maxY) * 0.5f);
    }

    private void PositionIndicatorsForBounds(float minX, float maxX, float centerY)
    {
        if (_containerRect == null)
        {
            return;
        }

        var halfArrow = _arrowSize * 0.5f;
        var halfPanelWidth = _containerRect.rect.width * 0.5f;
        var halfPanelHeight = _containerRect.rect.height * 0.5f;
        var clampedCenterY = Mathf.Clamp(centerY, -halfPanelHeight + halfArrow, halfPanelHeight - halfArrow);
        var leftX = Mathf.Clamp(minX - halfArrow - _objectGap, -halfPanelWidth + halfArrow, halfPanelWidth - halfArrow);
        var rightX = Mathf.Clamp(maxX + halfArrow + _objectGap, -halfPanelWidth + halfArrow, halfPanelWidth - halfArrow);

        ConfigureArrowRect(_leftRect, leftX, clampedCenterY, new Vector3(1f, 1f, 1f));
        ConfigureArrowRect(_rightRect, rightX, clampedCenterY, new Vector3(-1f, 1f, 1f));
    }

    private void ConfigureArrowRect(RectTransform rect, float x, float y, Vector3 scale)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(_arrowSize, _arrowSize);
        rect.anchoredPosition = new Vector2(x, y);
        rect.localScale = scale;
        rect.localRotation = Quaternion.identity;
    }

    private bool TryGetTargetBoundsInContainer(RectTransform targetRect, out Vector2 center, out float minX, out float maxX,
        out float minY, out float maxY)
    {
        center = Vector2.zero;
        minX = 0f;
        maxX = 0f;
        minY = 0f;
        maxY = 0f;

        if (_containerRect == null || targetRect == null)
        {
            return false;
        }

        targetRect.GetWorldCorners(CornerBuffer);

        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minY = float.PositiveInfinity;
        maxY = float.NegativeInfinity;

        for (var cornerIndex = 0; cornerIndex < CornerBuffer.Length; cornerIndex++)
        {
            var localCorner = _containerRect.InverseTransformPoint(CornerBuffer[cornerIndex]);
            if (localCorner.x < minX)
            {
                minX = localCorner.x;
            }

            if (localCorner.x > maxX)
            {
                maxX = localCorner.x;
            }

            if (localCorner.y < minY)
            {
                minY = localCorner.y;
            }

            if (localCorner.y > maxY)
            {
                maxY = localCorner.y;
            }
        }

        if (float.IsInfinity(minX) || float.IsInfinity(maxX) || float.IsInfinity(minY) || float.IsInfinity(maxY))
        {
            return false;
        }

        var width = maxX - minX;
        if (width <= 0.01f)
        {
            var centerWorld = targetRect.TransformPoint(targetRect.rect.center);
            var centerLocal = _containerRect.InverseTransformPoint(centerWorld);
            minX = centerLocal.x - (_arrowSize * 0.25f);
            maxX = centerLocal.x + (_arrowSize * 0.25f);
        }

        var height = maxY - minY;
        if (height <= 0.01f)
        {
            var centerWorld = targetRect.TransformPoint(targetRect.rect.center);
            var centerLocal = _containerRect.InverseTransformPoint(centerWorld);
            minY = centerLocal.y - (_arrowSize * 0.25f);
            maxY = centerLocal.y + (_arrowSize * 0.25f);
        }

        center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        return true;
    }
}
