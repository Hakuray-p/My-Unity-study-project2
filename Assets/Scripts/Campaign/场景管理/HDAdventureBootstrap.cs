using UnityEngine;
using UnityEngine.SceneManagement;

public static class HDAdventureBootstrap
{
    private static bool hookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        hookInstalled = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallHook()
    {
        if (hookInstalled) return;
        hookInstalled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntime()
    {
        EnsureSceneManager(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneManager(scene);
    }

    private static void EnsureSceneManager(Scene scene)
    {
        if (scene.name != "HD_2D_Day") return;
        if (Object.FindObjectOfType<HD2DSceneGM>() == null)
            new GameObject("HD2D Scene GM").AddComponent<HD2DSceneGM>();
    }
}
