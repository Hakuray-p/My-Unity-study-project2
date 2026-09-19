using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 把主菜单设置按钮接到城市暂停菜单的设置面板。
public static class GameMenuSettingsBridge
{
    // 注册主菜单场景载入回调。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 载入城市设置面板并接入主菜单。
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameMenu") return;

        GameObject mainPanel = GameObject.Find("Panel");
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject settingsPrefab = Resources.Load<GameObject>("UI/GameMenuSettingsPanel");
        GameObject settingsObject = Object.Instantiate(settingsPrefab, canvas.transform, false);
        settingsObject.name = "GameMenuSettingsPanel";
        PauseMenuView pauseMenu = settingsObject.GetComponent<PauseMenuView>();
        Button settingsButton = SceneTool.Find<Button>("设置");
        Button backButton = FindButton(settingsObject, "返回暂停");
        GameMenuSettingsBridgeView bridge = settingsObject.AddComponent<GameMenuSettingsBridgeView>();
        bridge.Initialize(pauseMenu, mainPanel, backButton);
        settingsButton.onClick.AddListener(bridge.OpenSettings);
    }

    // 在共用设置面板内部查找返回按钮。
    private static Button FindButton(GameObject root, string namePart)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
            if (button.gameObject.name.Contains(namePart)) return button;
        return null;
    }
}

// 管理主菜单设置页面和主菜单之间的切换。
public sealed class GameMenuSettingsBridgeView : MonoBehaviour
{
    private PauseMenuView pauseMenu; // 城市设置菜单视图
    private GameObject mainPanel; // 主菜单面板
    private Button backButton; // 设置页返回按钮

    // 保存设置菜单与主菜单引用。
    public void Initialize(PauseMenuView menuView, GameObject menuPanel, Button returnButton)
    {
        pauseMenu = menuView;
        mainPanel = menuPanel;
        backButton = returnButton;
        backButton.onClick.AddListener(CloseSettings);
    }

    // 打开城市暂停菜单使用的设置页面。
    public void OpenSettings()
    {
        mainPanel.SetActive(false);
        pauseMenu.ShowSettings();
    }

    // 关闭设置页面并恢复主菜单。
    private void CloseSettings()
    {
        pauseMenu.CloseSettings();
        mainPanel.SetActive(true);
    }

    // 使用 Esc 关闭设置页面。
    private void Update()
    {
        if (pauseMenu.SettingsOpen && Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
    }
}
