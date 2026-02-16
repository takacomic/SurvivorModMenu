using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorModMenu;

[RegisterTypeInIl2Cpp]
public sealed class ModMenuSelectableNavigationTarget : MonoBehaviour
{
    public ModMenuSelectableNavigationTarget(IntPtr ptr) : base(ptr)
    {
    }

    private RectTransform _anchorRect;
    private ModMenuNavigationPanel _ownerPanel;
    private bool _isOptionObject;

    internal RectTransform AnchorRect => _anchorRect;
    internal ModMenuNavigationPanel OwnerPanel => _ownerPanel;
    internal bool IsOptionObject => _isOptionObject;
    internal GameObject SelectionObject => gameObject;

    internal void Configure(RectTransform anchorRect, ModMenuNavigationPanel ownerPanel, bool isOptionObject)
    {
        _anchorRect = anchorRect;
        _ownerPanel = ownerPanel;
        _isOptionObject = isOptionObject;
    }

    internal bool IsValid()
    {
        if (SelectionObject == null || _anchorRect == null)
        {
            return false;
        }

        if (!SelectionObject.activeInHierarchy || !_anchorRect.gameObject.activeInHierarchy)
        {
            return false;
        }

        var selectable = SelectionObject.GetComponent<Selectable>();
        if (selectable == null)
        {
            return true;
        }

        return selectable.IsInteractable();
    }
}
