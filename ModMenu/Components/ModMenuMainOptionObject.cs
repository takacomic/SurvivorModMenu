
namespace SurvivorModMenu.ModMenu.Components;

[RegisterTypeInIl2Cpp]
public sealed class ModMenuMainOptionObject : MonoBehaviour
{
    public ModMenuMainOptionObject(IntPtr ptr) : base(ptr)
    {
    }

    private RectTransform _rootRect;
    private float _localY;
    private readonly List<ModMenuSelectable> _subTargets = new();

    internal float LocalY => _localY;
    internal RectTransform RootRect => _rootRect;
    internal List<ModMenuSelectable> SubTargets => _subTargets;

    internal void Configure(RectTransform rootRect, RectTransform referenceRect)
    {
        _rootRect = rootRect;
        UpdateLocalY(referenceRect);
    }

    internal void UpdateLocalY(RectTransform referenceRect)
    {
        if (_rootRect == null)
        {
            _localY = 0f;
            return;
        }

        if (referenceRect == null)
        {
            _localY = _rootRect.anchoredPosition.y;
            return;
        }

        var worldCenter = _rootRect.TransformPoint(_rootRect.rect.center);
        var localPoint = referenceRect.InverseTransformPoint(worldCenter);
        _localY = localPoint.y;
    }

    internal void ClearSubTargets()
    {
        _subTargets.Clear();
    }

    internal void RegisterSubTarget(ModMenuSelectable target)
    {
        if (target == null)
        {
            return;
        }

        foreach (var existingTarget in _subTargets)
        {
            if (!ReferenceEquals(existingTarget, target))
            {
                continue;
            }

            return;
        }

        _subTargets.Add(target);
    }
}
