
namespace SurvivorModMenu.ModMenu.Components;

/// <summary>
/// Tracks the logical option row that owns one or more navigation targets.
/// </summary>
[RegisterTypeInIl2Cpp]
public sealed class ModMenuMainOptionObject : MonoBehaviour
{
    public ModMenuMainOptionObject(IntPtr ptr) : base(ptr)
    {
    }

    private RectTransform _rootRect;
    private readonly List<ModMenuSelectable> _subTargets = new();

    [HideFromIl2Cpp]
    internal float LocalY { get; private set; }

    [HideFromIl2Cpp]
    internal void Configure(RectTransform rootRect, RectTransform referenceRect)
    {
        _rootRect = rootRect;
        UpdateLocalY(referenceRect);
    }

    [HideFromIl2Cpp]
    internal void UpdateLocalY(RectTransform referenceRect)
    {
        if (_rootRect == null)
        {
            LocalY = 0f;
            return;
        }

        if (referenceRect == null)
        {
            LocalY = _rootRect.anchoredPosition.y;
            return;
        }

        var worldCenter = _rootRect.TransformPoint(_rootRect.rect.center);
        var localPoint = referenceRect.InverseTransformPoint(worldCenter);
        LocalY = localPoint.y;
    }

    [HideFromIl2Cpp]
    internal void ClearSubTargets()
    {
        _subTargets.Clear();
    }

    [HideFromIl2Cpp]
    internal void RegisterSubTarget(ModMenuSelectable target)
    {
        if (target == null)
        {
            return;
        }

        if (_subTargets.Any(existingTarget => ReferenceEquals(existingTarget, target)))
        {
            return;
        }

        _subTargets.Add(target);
    }
}
