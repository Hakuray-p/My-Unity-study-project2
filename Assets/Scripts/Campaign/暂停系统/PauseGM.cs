using System;
using UnityEngine;
using UnityEngine.UI;

// 暂停系统：Esc 开关暂停菜单，并提供继续游戏、返回主菜单、退出游戏、脱离卡死。
// 面板和按钮都是按名字找的，场景里改名要同步这里。
public sealed class PauseGM : MonoBehaviour
{
    private CharacterGM characterGM; // 角色管理器，脱离卡死时要靠它拿到玩家
    private CampaignSession session; // 当前存档会话
    private Action savePlayerAction; // 保存玩家位置的回调，由场景 GM 提供
    private Action closePanelsAction; // 关闭其它面板的回调，由场景 GM 提供
    private Canvas pauseCanvas; // 暂停菜单画布
    private bool pauseOpen; // 暂停菜单当前是否打开
    private float timeScaleBeforePause = 1f; // 暂停前的时间流速，恢复时用

    public bool IsOpen => pauseOpen; // 暂停菜单是否打开

    public void Initialize(CharacterGM characterManager, CampaignSession campaignSession,
        Action savePlayer, Action closePanels)
    {
        characterGM = characterManager;
        session = campaignSession;
        savePlayerAction = savePlayer;
        closePanelsAction = closePanels;
        BindPauseCanvas();
    }

    // 每帧处理 Esc：有面板开着就先关面板，都没有才弹暂停菜单
    public void Tick(bool panelOpen)
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (pauseOpen)
        {
            Close();
            return;
        }
        if (panelOpen)
        {
            closePanelsAction();
            return;
        }
        Open();
    }

    // 关掉暂停菜单并恢复时间
    public void Close()
    {
        pauseOpen = false;
        pauseCanvas.gameObject.SetActive(false);
        Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
    }

    // 找到暂停画布并给按钮接上点击
    private void BindPauseCanvas()
    {
        pauseCanvas = null;
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas candidate in canvases)
        {
            string canvasName = candidate.gameObject.name;
            if (canvasName.Contains("PauseCanvas"))
            {
                pauseCanvas = candidate;
                break;
            }
        }

        pauseCanvas.transform.localScale = Vector3.one;
        if (pauseCanvas.GetComponent<GraphicRaycaster>() == null)
            pauseCanvas.gameObject.AddComponent<GraphicRaycaster>();
        pauseCanvas.gameObject.SetActive(false);
        Button[] buttons = pauseCanvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            string buttonName = button.gameObject.name;
            if (buttonName.Contains("继续游戏")) button.onClick.AddListener(Close);
            else if (buttonName.Contains("返回主菜单")) button.onClick.AddListener(ReturnToMainMenu);
            else if (buttonName.Contains("退出游戏")) button.onClick.AddListener(ExitGame);
            else if (buttonName.Contains("脱离卡死")) button.onClick.AddListener(RecoverPlayer);
        }
    }

    // 打开暂停菜单，先记下当前时间流速再置 0
    private void Open()
    {
        timeScaleBeforePause = Time.timeScale;
        pauseOpen = true;
        pauseCanvas.transform.localScale = Vector3.one;
        pauseCanvas.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    // 返回主菜单前先存档并关掉面板
    private void ReturnToMainMenu()
    {
        savePlayerAction();
        Close();
        SceneFlowService.ReturnToMenu();
    }

    // 退出游戏前先存档，编辑器里只停止 Play
    private void ExitGame()
    {
        savePlayerAction();
        Close();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 脱离卡死：先关掉角色控制器再挪位置，最后再打开，
    // 先关掉 CharacterController 再挪玩家，否则会被判定成穿墙弹回
    private void RecoverPlayer()
    {
        CharacterController controller = characterGM.Player.GetComponent<CharacterController>();
        controller.enabled = false;
        characterGM.Player.position = characterGM.PlayerInitialPosition;
        controller.enabled = true;
        characterGM.Player.GetComponent<AmiyaCharacter>()?.SetSafePosition();
        savePlayerAction();
        Close();
    }
}
