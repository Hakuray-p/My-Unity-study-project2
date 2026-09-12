using UnityEngine;
using UnityEngine.SceneManagement;

// HD 冒险场景的启动器，场景加载后自动补上 HD2DSceneGM
public static class HDAdventureBootstrap
{
    private static bool hookInstalled; // 是否已经挂过场景加载回调

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    // 关闭域重载时重置标记，避免回调重复挂
    private static void ResetState()
    {
        hookInstalled = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    // 挂上场景加载回调，只挂一次
    private static void InstallHook()
    {
        if (hookInstalled) return;
        hookInstalled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // 给当前场景补上场景管理器
    private static void EnsureRuntime()
    {
        EnsureSceneManager(SceneManager.GetActiveScene());
    }

    // 每次加载场景都补一次场景管理器
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneManager(scene);
    }

    // 只对 One_City_DAY 生效，场景里没有场景 GM 就补一个
    private static void EnsureSceneManager(Scene scene)
    {
        if (scene.name != "One_City_DAY") return;
        if (Object.FindObjectOfType<HD2DSceneGM>() == null)
            new GameObject("HD2D Scene GM").AddComponent<HD2DSceneGM>();
    }
}
