using Il2CppTMPro;

namespace SurvivorModMenu.ModMenu;

/// <summary>
/// Captures a text template copied from the game's existing UI.
/// </summary>
internal struct ModMenuTextStyle
{
    /// <summary>True when TextMeshPro should be used instead of legacy UI Text.</summary>
    internal bool isTmp;

    /// <summary>Template TMP font for menu text.</summary>
    internal TMP_FontAsset tmpFont;

    /// <summary>Template legacy UI font used when TMP is unavailable.</summary>
    internal Font uiFont;

    /// <summary>Base font size used by generated labels/buttons.</summary>
    internal float fontSize;

    /// <summary>Template text color used by generated labels/buttons.</summary>
    internal Color color;
}
