
namespace SurvivorModMenu.ModMenu.Components;

/// <summary>
/// Metadata attached to every selectable object registered with mod menu navigation.
/// </summary>
[RegisterTypeInIl2Cpp]
public sealed class ModMenuSelectable : MonoBehaviour
{
    public ModMenuSelectable(IntPtr ptr) : base(ptr)
    {
    }

    [HideFromIl2Cpp]
    internal RectTransform AnchorRect { get; private set; }

    [HideFromIl2Cpp]
    internal ModMenuPanel OwnerPanel { get; private set; }

    [HideFromIl2Cpp]
    internal bool IsOptionObject { get; private set; }

    [HideFromIl2Cpp]
    internal GameObject SelectionObject => gameObject;

    [HideFromIl2Cpp]
    internal void Configure(RectTransform anchorRect, ModMenuPanel ownerPanel, bool isOptionObject)
    {
        AnchorRect = anchorRect;
        OwnerPanel = ownerPanel;
        IsOptionObject = isOptionObject;
    }

    [HideFromIl2Cpp]
    internal bool IsValid()
    {
        if (SelectionObject == null || AnchorRect == null)
        {
            return false;
        }

        if (!SelectionObject.activeInHierarchy || !AnchorRect.gameObject.activeInHierarchy)
        {
            return false;
        }

        var selectable = SelectionObject.GetComponent<Selectable>();
        return selectable == null || selectable.IsInteractable();
    }
}
