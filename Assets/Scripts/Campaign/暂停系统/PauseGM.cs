using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PauseGM : MonoBehaviour
{
    private CharacterGM characterGM;
    private CampaignSession session;
    private Action savePlayerAction;
    private Action closePanelsAction;
    private Canvas pauseCanvas;
    private bool pauseOpen;
    private float timeScaleBeforePause = 1f;

    public bool IsOpen => pauseOpen;

    public void Initialize(CharacterGM characterManager, CampaignSession campaignSession,
        Action savePlayer, Action closePanels)
    {
        characterGM = characterManager;
        session = campaignSession;
        savePlayerAction = savePlayer;
        closePanelsAction = closePanels;
        BindPauseCanvas();
    }

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
            if (closePanelsAction != null) closePanelsAction();
            return;
        }
        Open();
    }

    public void Close()
    {
        pauseOpen = false;
        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
        Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
    }

    private void BindPauseCanvas()
    {
        pauseCanvas = null;
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas candidate in canvases)
        {
            if (candidate == null) continue;
            string canvasName = candidate.gameObject.name;
            if (canvasName.Contains("PauseCanvas") || canvasName.Contains("暂停Canvas") || canvasName.Contains("暂停菜单"))
            {
                pauseCanvas = candidate;
                break;
            }
        }

        if (pauseCanvas == null) return;
        pauseCanvas.transform.localScale = Vector3.one;
        if (pauseCanvas.GetComponent<GraphicRaycaster>() == null)
            pauseCanvas.gameObject.AddComponent<GraphicRaycaster>();
        pauseCanvas.gameObject.SetActive(false);
        Button[] buttons = pauseCanvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            string buttonName = button.gameObject.name;
            if (buttonName.Contains("Resume") || buttonName.Contains("继续"))
            {
                button.onClick.RemoveListener(Close);
                button.onClick.AddListener(Close);
            }
            else if (buttonName.Contains("MainMenu") || buttonName.Contains("返回主菜单"))
            {
                button.onClick.RemoveListener(ReturnToMainMenu);
                button.onClick.AddListener(ReturnToMainMenu);
            }
            else if (buttonName.Contains("Exit") || buttonName.Contains("退出游戏"))
            {
                button.onClick.RemoveListener(ExitGame);
                button.onClick.AddListener(ExitGame);
            }
            else if (buttonName.Contains("Escape") || buttonName.Contains("脱离卡死"))
            {
                button.onClick.RemoveListener(RecoverPlayer);
                button.onClick.AddListener(RecoverPlayer);
            }
        }
    }

    private void Open()
    {
        if (pauseCanvas == null) return;
        timeScaleBeforePause = Time.timeScale;
        pauseOpen = true;
        pauseCanvas.transform.localScale = Vector3.one;
        pauseCanvas.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void ReturnToMainMenu()
    {
        if (savePlayerAction != null) savePlayerAction();
        Close();
        SceneFlowService.ReturnToMenu();
    }

    private void ExitGame()
    {
        if (savePlayerAction != null) savePlayerAction();
        Close();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RecoverPlayer()
    {
        if (characterGM == null || characterGM.Player == null) return;
        CharacterController controller = characterGM.Player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        characterGM.Player.position = characterGM.PlayerInitialPosition;
        if (controller != null) controller.enabled = true;
        characterGM.Player.GetComponent<AmiyaCharacter>()?.SetSafePosition();
        if (savePlayerAction != null) savePlayerAction();
        Close();
    }
}
