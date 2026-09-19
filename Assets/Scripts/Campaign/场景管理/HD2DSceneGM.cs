using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// HD 城市探索场景的总管理器，负责相机、UI、商店、卡组管理和世界交互的协调
public sealed class HD2DSceneGM : MonoBehaviour
{
    private static HD2DSceneGM instance; // 全局单例

    public static Camera GameplayCamera => instance != null ? instance.sceneCamera : null;

    private CampaignSession session; // 当前存档会话
    private Camera sceneCamera; // 场景主相机
    private HD2DCameraFollow cameraFollow; // 相机跟随组件
    private Quaternion cameraRotation; // 相机固定朝向
    private Vector3 cameraOffset; // 相机相对玩家的偏移
    private CharacterGM characterGM; // 角色管理器
    private DialogueGM dialogueGM; // 对话管理器
    private EventGM eventGM; // 事件管理器
    private PauseGM pauseGM; // 暂停管理器
    private bool playerInitialized; // 玩家是否已经初始化
    private bool worldInitialized; // 世界是否已经初始化
    private bool depthOfFieldDisabled; // 景深是否已经关掉
    private string statusMessage = string.Empty; // 当前提示文字
    private float statusUntil; // 提示显示到什么时候
    [SerializeField] private CardListSO cardListSO; // 卡牌数据库
    private ShopPanel shopPanel; // 商店面板
    [SerializeField] private CityGuideController cityGuide; // 场景中预置的主线指引
    [SerializeField] private CityHudView cityHud; // 场景中预置的积分、金币和交谈提示

    // 占住单例并初始化城市场景
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Time.timeScale = 1f;
        CampaignCatalog.SetCardDatabase(cardListSO);
        session = CampaignSession.Instance;
        session.PurgeMissingCards();
        shopPanel = new ShopPanel(session, ShowStatus, ClosePanels);
        ConfigureCamera();
        DisableDepthOfField();
        EnsureManagers();
        TryInitializeScene();

    }

    // 离开场景前存一次玩家位置
    private void OnDestroy()
    {
        SavePlayer();
        if (instance == this) instance = null;
    }

    // 每帧依次处理场景初始化、关商店、暂停和世界交互
    private void Update()
    {
        TryInitializeScene();
        if (TryCloseShop()) return;
        if (TickPause()) return;
        if (!IsGameplayReady()) return;
        if (TryOpenDeckManager()) return;
        UpdateHud();
        TickWorldInteractions();
    }

    // 商店打开时按 Esc 关掉，返回 true 表示这一帧被商店吃掉了
    private bool TryCloseShop()
    {
        if (!shopPanel.IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return false;
        ClosePanels();
        return true;
    }

    // 把面板打开状态交给暂停系统，返回 true 表示暂停菜单开着
    private bool TickPause()
    {
        bool panelOpen = (dialogueGM != null && dialogueGM.IsOpen) || shopPanel.IsOpen;
        if (pauseGM != null) pauseGM.Tick(panelOpen);
        return pauseGM != null && pauseGM.IsOpen;
    }

    // 玩家和存档都就绪才算能开始交互
    private bool IsGameplayReady()
    {
        return session != null && session.State != null && characterGM != null && characterGM.Player != null;
    }

    // 商店开着时不响应世界交互
    private void TickWorldInteractions()
    {
        if (shopPanel.IsOpen) return;
        if (eventGM != null) eventGM.Tick();
        if (dialogueGM != null) dialogueGM.Tick();
    }

    // 按 B 进卡组管理场景，商店或对话开着时不响应
    private bool TryOpenDeckManager()
    {
        if (!Input.GetKeyDown(KeyCode.B)) return false;
        if (shopPanel.IsOpen || (dialogueGM != null && dialogueGM.IsOpen)) return false;
        SceneFlowService.OpenDeckManager();
        return true;
    }

    // 每帧补一次场景初始化和相机跟随
    private void LateUpdate()
    {
        TryInitializeScene();
        if (characterGM != null && characterGM.Player != null)
        {
            PlayerCharacter character = characterGM.Player.GetComponent<PlayerCharacter>();
            if (character != null && character.gameplayCamera != sceneCamera)
                character.gameplayCamera = sceneCamera;
        }
        ApplyCameraFollow();
        cityGuide.SetVisible(worldInitialized && !dialogueGM.IsOpen && !shopPanel.IsOpen && !pauseGM.IsOpen && !SceneFlowService.IsLoading);
        bool canTalk = worldInitialized && !dialogueGM.IsOpen && !shopPanel.IsOpen && !pauseGM.IsOpen &&
            !SceneFlowService.IsLoading && Time.timeScale > 0f;
        cityHud.ShowInteraction(canTalk ? dialogueGM.NearbyActor : null);
    }

    // 场景对象上缺哪个管理器就补哪个
    private void EnsureManagers()
    {
        characterGM = gameObject.GetComponent<CharacterGM>();
        if (characterGM == null) characterGM = gameObject.AddComponent<CharacterGM>();
        dialogueGM = gameObject.GetComponent<DialogueGM>();
        if (dialogueGM == null) dialogueGM = gameObject.AddComponent<DialogueGM>();
        eventGM = gameObject.GetComponent<EventGM>();
        if (eventGM == null) eventGM = gameObject.AddComponent<EventGM>();
        pauseGM = gameObject.GetComponent<PauseGM>();
        if (pauseGM == null) pauseGM = gameObject.AddComponent<PauseGM>();
    }

    // 分两步初始化，先玩家后世界，每次 Update 都可以安全重入
    private void TryInitializeScene()
    {
        if (session == null) session = CampaignSession.Instance;
        if (sceneCamera == null) ConfigureCamera();
        if (characterGM == null || dialogueGM == null || eventGM == null || pauseGM == null) EnsureManagers();
        characterGM.Initialize(session, sceneCamera);
        if (!characterGM.IsReady) return;

        if (!playerInitialized)
        {
            characterGM.RestorePlayer();
            ConfigureFollowOffset();
            ConfigureCameraFollow();
            playerInitialized = true;
        }

        if (!worldInitialized)
        {
            EnsureEventSystem();
            characterGM.CreateWorldActors();
            eventGM.Initialize(session, characterGM, ShowStatus);
            dialogueGM.Initialize(characterGM, session, eventGM, StartMatch, OpenShopPanel, ShowStatus, ClosePanels);
            pauseGM.Initialize(characterGM, SavePlayer, ClosePanels);
            shopPanel.Initialize();
            if (shopPanel.TryRestore()) OpenShopPanel();
            worldInitialized = true;
            cityGuide.Initialize(session, characterGM, eventGM);
            dialogueGM.ResumeVictoryDialogue();
        }
    }

    // 找到场景主摄像机并调好参数
    private void ConfigureCamera()
    {
        Camera[] cameras = FindObjectsOfType<Camera>(true);
        foreach (Camera candidate in cameras)
        {
            if (candidate != null && candidate.gameObject.name == "Main Camera")
            {
                sceneCamera = candidate;
                break;
            }
        }
        if (sceneCamera == null)
            foreach (Camera candidate in cameras)
                if (candidate != null && candidate.CompareTag("MainCamera"))
                {
                    sceneCamera = candidate;
                    break;
                }
        if (sceneCamera == null && cameras.Length > 0) sceneCamera = cameras[0];
        if (sceneCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.transform.position = new Vector3(0f, 12f, -10f);
            sceneCamera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        sceneCamera.enabled = true;
        sceneCamera.tag = "MainCamera";
        foreach (Camera candidate in cameras)
        {
            if (candidate == sceneCamera) continue;
            if (candidate.gameObject.name == "ShopCardPreviewCamera")
            {
                candidate.enabled = true;
                AudioListener previewListener = candidate.GetComponent<AudioListener>();
                if (previewListener != null) previewListener.enabled = false;
                continue;
            }
            candidate.enabled = false;
            AudioListener listener = candidate.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
        Animator animator = sceneCamera.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        UnityEngine.Playables.PlayableDirector director = sceneCamera.GetComponent<UnityEngine.Playables.PlayableDirector>();
        if (director != null)
        {
            director.Stop();
            director.enabled = false;
        }
    }

    // 复制一份 Volume 配置并关掉景深，避免影响城市画面
    private void DisableDepthOfField()
    {
        if (depthOfFieldDisabled) return;
        Volume[] volumes = FindObjectsOfType<Volume>(true);
        foreach (Volume volume in volumes)
        {
            if (volume.sharedProfile == null) continue;
            if (!volume.sharedProfile.TryGet(out DepthOfField depthOfField)) continue;
            VolumeProfile runtimeProfile = Instantiate(volume.sharedProfile);
            if (runtimeProfile.TryGet(out DepthOfField runtimeDepthOfField))
                runtimeDepthOfField.active = false;
            volume.profile = runtimeProfile;
        }
        depthOfFieldDisabled = true;
    }

    // 按相机朝向算出跟随偏移
    private void ConfigureFollowOffset()
    {
        cameraRotation = sceneCamera.transform.rotation;
        Vector3 cameraForward = cameraRotation * Vector3.forward;
        cameraOffset = Vector3.up * 1.2f - cameraForward.normalized * 8f;
    }

    // 给摄像机挂上跟随组件
    private void ConfigureCameraFollow()
    {
        if (sceneCamera == null || characterGM == null || characterGM.Player == null) return;
        cameraFollow = sceneCamera.GetComponent<HD2DCameraFollow>();
        if (cameraFollow == null) cameraFollow = sceneCamera.gameObject.AddComponent<HD2DCameraFollow>();
        cameraFollow.Bind(characterGM.Player, cameraOffset, cameraRotation);
    }

    // 把相机贴到玩家身上
    private void ApplyCameraFollow()
    {
        if (characterGM == null || characterGM.Player == null || sceneCamera == null) return;
        if (cameraFollow == null) ConfigureCameraFollow();
        if (cameraFollow != null) cameraFollow.Bind(characterGM.Player, cameraOffset, cameraRotation);
        sceneCamera.transform.SetPositionAndRotation(characterGM.Player.position + cameraOffset, cameraRotation);
    }

    // 准备静态界面、商店和卡组操作所需的事件系统。
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    // 刷新静态 HUD 的积分、金币和临时提示。
    private void UpdateHud()
    {
        CityData city = CampaignCatalog.GetCity(session.State.currentCityId);
        CitySaveData state = session.GetCityState(session.State.currentCityId);
        string status = Time.unscaledTime < statusUntil ? statusMessage : string.Empty;
        cityHud.Refresh(state.leaguePoints, session.State.currency, status);
    }

    // 打开商店并暂停世界
    private void OpenShopPanel()
    {
        Time.timeScale = 0f;
        shopPanel.Open();
    }


    // 开一场比赛，已经有没打完的战斗时改为继续那场
    private void StartMatch(string matchId)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (match == null)
        {
            ShowStatus("赛事不存在");
            return;
        }
        if (session.HasPendingBattle)
        {
            Time.timeScale = 1f;
            ClosePanels();
            SceneFlowService.ResumePendingBattle();
            return;
        }
        if (!session.HasLegalDeck)
        {
            ShowStatus("当前卡组不合法，请先完成卡组管理");
            SceneFlowService.OpenDeckManager();
            return;
        }
        if (!session.CanStartMatch(match))
        {
            ShowStatus(GetMatchLockReason(match));
            return;
        }
        SavePlayer();
        ClosePanels();
        SceneFlowService.StartMatch(match.matchId, characterGM.Player.position);
    }

    // 赛事不能打的原因，能打就返回空。
    private string GetMatchLockReason(MatchData match)
    {
        return session.GetMatchLockReason(match);
    }

    // 关掉所有面板并把时间恢复过来
    private void ClosePanels()
    {
        if (dialogueGM != null) dialogueGM.Close();
        shopPanel.Close();
        Time.timeScale = 1f;
    }

    // 把玩家位置写进存档
    private void SavePlayer()
    {
        if (characterGM == null || characterGM.Player == null || session == null || session.State == null) return;
        session.SetPlayerPosition(characterGM.Player.position);
        session.Save();
    }

    // 在 HUD 上弹一条限时提示
    private void ShowStatus(string message)
    {
        statusMessage = message;
        statusUntil = Time.unscaledTime + 3f;
    }
}
