using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 在主菜单载入后接入按钮视觉、默认焦点和退出功能。
public static class GameMenuRuntime
{
    // 注册主菜单场景载入回调。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 为主菜单按钮接入运行时功能。
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameMenu") return;

        Button startButton = SceneTool.Find<Button>("开始游戏");
        Button continueButton = SceneTool.Find<Button>("继续游戏");
        Button keyGuideButton = SceneTool.Find<Button>("按键说明");
        Button settingsButton = SceneTool.Find<Button>("设置");
        Button exitButton = SceneTool.Find<Button>("退出游戏");

        AddButtonView(startButton);
        AddButtonView(continueButton);
        AddButtonView(keyGuideButton);
        AddButtonView(settingsButton);
        AddButtonView(exitButton);
        exitButton.onClick.AddListener(ExitGame);

        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    // 给按钮添加选中视觉组件。
    private static void AddButtonView(Button button)
    {
        if (button.GetComponent<GameMenuButtonView>() == null)
            button.gameObject.AddComponent<GameMenuButtonView>();
    }

    // 退出游戏，在编辑器中停止当前运行。
    private static void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
