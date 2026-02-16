using Il2CppInterop.Runtime;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using SurvivorModMenu.ModMenu;
#if DEBUG
using SurvivorModMenu.Debugging;
#endif

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
    private static UnityAction<Scene, LoadSceneMode> _sceneLoadedHandler;

    public override void OnInitializeMelon()
    {
#if DEBUG
        DebugTestSettings.Register();
#endif
        _sceneLoadedHandler ??= DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(OnSceneLoaded);
        if (_sceneLoadedHandler == null)
        {
            return;
        }

        SceneManager.sceneLoaded += _sceneLoadedHandler;
    }

    public override void OnUpdate()
    {
        ModMenuController.Update();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        ModMenuController.OnSceneWasLoaded(scene.name);
    }
}
