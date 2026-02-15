using MelonLoader;

[assembly: MelonInfo(typeof(SurvivorModMenu.SurvivorModMenuPlugin), SurvivorModMenu.BuildInfo.Name, SurvivorModMenu.BuildInfo.Version, SurvivorModMenu.BuildInfo.Author, SurvivorModMenu.BuildInfo.Download)]
[assembly: MelonGame("poncle", "Vampire Survivors")]

namespace SurvivorModMenu;

internal static class BuildInfo
{
    internal const string Name = "SurvivorModMenu";
    internal const string Author = "Takacomic";
    internal const string Version = "1.0.0";
    internal const string Download = "https://github.com/takacomic";
}

public sealed class SurvivorModMenuPlugin : MelonPlugin
{
    public override void OnInitializeMelon()
    {
#if DEBUG
        DebugTestSettings.Register();
#endif
    }

    public override void OnUpdate()
    {
        ModMenuController.Update();
    }
}
