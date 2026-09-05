using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class HDAdventureBootstrap
{
    private static bool hookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => hookInstalled = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallHook()
    {
        if (hookInstalled) return;
        hookInstalled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntime() => EnsureRuntimeForScene(SceneManager.GetActiveScene());

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureRuntimeForScene(scene);

    private static void EnsureRuntimeForScene(Scene scene)
    {
        if (scene.name != "HD_2D_Day") return;
        if (Object.FindObjectOfType<HDAdventureRuntime>() == null)
            new GameObject("HD Adventure Runtime").AddComponent<HDAdventureRuntime>();
    }
}

public sealed class HDAdventureRuntime : MonoBehaviour
{
    private static HDAdventureRuntime instance;
    public static Camera GameplayCamera => instance != null ? instance.adventureCamera : null;
    public static bool IsMapOpen => false;

    private CampaignSession session;
    private Camera adventureCamera;
    private Transform player;
    private Quaternion cameraRotation;
    private Vector3 cameraOffset;
    private HDAdventureCameraFollow cameraFollow;
    private bool playerInitialized;
    private bool worldInitialized;
    private bool depthOfFieldDisabled;
    private HDAdventureBuildingOcclusion playerOcclusion;
    private Canvas canvas;
    private Text hudText;
    private string statusMessage = string.Empty;
    private float statusUntil;
    private WorldInteractionActor dialogueActor;
    private bool dialogueOpen;
    private bool shopOpen;
    private bool deckOpen;
    private bool eventOpen;
    private Vector2 shopScroll;
    private List<int> deckDraft = new List<int>();

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Time.timeScale = 1f;
        session = CampaignSession.Instance;
        ConfigureCamera();
        DisableAdventureDepthOfField();
        TryInitializeAdventure();
    }

    private void OnDestroy()
    {
        SavePlayer();
        if (instance == this) instance = null;
    }

    private void ConfigureCamera()
    {
        Camera[] cameras = FindObjectsOfType<Camera>(true);
        foreach (Camera candidate in cameras)
        {
            if (candidate != null && candidate.gameObject.name == "Main Camera") { adventureCamera = candidate; break; }
        }
        if (adventureCamera == null)
            foreach (Camera candidate in cameras)
                if (candidate != null && candidate.CompareTag("MainCamera")) { adventureCamera = candidate; break; }
        if (adventureCamera == null && cameras.Length > 0) adventureCamera = cameras[0];
        if (adventureCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            adventureCamera = cameraObject.AddComponent<Camera>();
            adventureCamera.transform.position = new Vector3(0f, 12f, -10f);
            adventureCamera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }
        adventureCamera.enabled = true;
        adventureCamera.tag = "MainCamera";
        foreach (Camera candidate in cameras)
        {
            if (candidate == null || candidate == adventureCamera) continue;
            candidate.enabled = false;
            AudioListener listener = candidate.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
        Animator animator = adventureCamera.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        var director = adventureCamera.GetComponent<UnityEngine.Playables.PlayableDirector>();
        if (director != null) { director.Stop(); director.enabled = false; }
    }

    private void DisableAdventureDepthOfField()
    {
        if (depthOfFieldDisabled) return;
        Volume[] volumes = FindObjectsOfType<Volume>(true);
        foreach (Volume volume in volumes)
        {
            if (volume == null || volume.sharedProfile == null) continue;
            if (!volume.sharedProfile.TryGet(out DepthOfField depthOfField)) continue;
            VolumeProfile runtimeProfile = Instantiate(volume.sharedProfile);
            if (runtimeProfile.TryGet(out DepthOfField runtimeDepthOfField))
                runtimeDepthOfField.active = false;
            volume.profile = runtimeProfile;
        }
        depthOfFieldDisabled = true;
    }

    private void CreatePlayer()
    {
        AmiyaCharacter character = FindObjectOfType<AmiyaCharacter>(true);
        if (character == null) return;
        character.gameObject.SetActive(true);
        character.gameplayCamera = adventureCamera;
        character.moveSpeed = 4f;
        CharacterController controller = character.GetComponent<CharacterController>();
        if (controller == null) controller = character.gameObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = Vector3.up * 0.9f;
        controller.skinWidth = 0.03f;
        controller.stepOffset = 0.25f;
        SpriteRenderer renderer = character.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.sortingOrder = 50;
        playerOcclusion = character.GetComponent<HDAdventureBuildingOcclusion>();
        if (playerOcclusion == null) playerOcclusion = character.gameObject.AddComponent<HDAdventureBuildingOcclusion>();
        playerOcclusion.Bind(adventureCamera, character.transform);
        player = character.transform;
    }

    private void TryInitializeAdventure()
    {
        if (session == null) session = CampaignSession.Instance;
        if (adventureCamera == null) ConfigureCamera();
        if (player == null) CreatePlayer();
        if (player == null) return;

        if (!playerInitialized)
        {
            RestorePlayer();
            ConfigureFollowOffset();
            ConfigureCameraFollow();
            playerInitialized = true;
        }

        if (!worldInitialized)
        {
            CreateUi();
            worldInitialized = true;
        }
    }

    private void RestorePlayer()
    {
        if (session.State.playerPosition.sqrMagnitude <= 0.25f) return;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.position = session.State.playerPosition;
        if (controller != null) controller.enabled = true;
        player.GetComponent<AmiyaCharacter>()?.SetSafePosition();
    }

    private void ConfigureFollowOffset()
    {
        cameraRotation = adventureCamera.transform.rotation;
        Vector3 cameraForward = cameraRotation * Vector3.forward;
        Vector3 focusPoint = player.position + Vector3.up * 0.9f;
        float cameraDistance = Vector3.Distance(focusPoint, adventureCamera.transform.position);
        cameraOffset = Vector3.up * 0.9f - cameraForward.normalized * cameraDistance;
    }

    private void ConfigureCameraFollow()
    {
        if (adventureCamera == null || player == null) return;
        cameraFollow = adventureCamera.GetComponent<HDAdventureCameraFollow>();
        if (cameraFollow == null) cameraFollow = adventureCamera.gameObject.AddComponent<HDAdventureCameraFollow>();
        cameraFollow.Bind(player, cameraOffset, cameraRotation);
    }

    private void ApplyCameraFollow()
    {
        if (player == null || adventureCamera == null) return;
        if (cameraFollow == null) ConfigureCameraFollow();
        if (cameraFollow != null) cameraFollow.Bind(player, cameraOffset, cameraRotation);
        adventureCamera.transform.SetPositionAndRotation(player.position + cameraOffset, cameraRotation);
    }

    private void CreateUi()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        GameObject canvasObject = new GameObject("HD Adventure UI");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        hudText = CreateText("Adventure HUD", canvas.transform, 22, Color.white);
        hudText.rectTransform.anchorMin = new Vector2(0f, 1f);
        hudText.rectTransform.anchorMax = new Vector2(0f, 1f);
        hudText.rectTransform.pivot = new Vector2(0f, 1f);
        hudText.rectTransform.anchoredPosition = new Vector2(28f, -24f);
        hudText.rectTransform.sizeDelta = new Vector2(850f, 150f);
    }

    private Text CreateText(string name, Transform parent, int size, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void Update()
    {
        TryInitializeAdventure();
        if (session == null || session.State == null || player == null) return;
        UpdateHud();
    }

    private void LateUpdate()
    {
        TryInitializeAdventure();
        if (player != null)
        {
            AmiyaCharacter character = player.GetComponent<AmiyaCharacter>();
            if (character != null && character.gameplayCamera != adventureCamera)
                character.gameplayCamera = adventureCamera;
        }
        ApplyCameraFollow();
    }

    private void OpenDialogue(WorldInteractionActor actor)
    {
        dialogueActor = actor;
        dialogueOpen = true;
        Time.timeScale = 0f;
    }

    private void ClosePanels()
    {
        dialogueOpen = shopOpen = deckOpen = eventOpen = false;
        Time.timeScale = 1f;
    }

    internal void Activate(WorldInteractionActor actor)
    {
        if (actor.type == WorldInteractionType.Match)
        {
            MatchData match = CampaignCatalog.GetMatch(actor.id);
            if (!session.HasLegalDeck)
            {
                ShowStatus("当前卡组不合法，请先完成卡组编辑");
                OpenDeck();
                return;
            }
            if (match != null && session.CanStartMatch(match))
            {
                SavePlayer();
                ClosePanels();
                SceneFlowService.StartMatch(match.matchId, player.position);
            }
            else ShowStatus(GetMatchLockReason(match));
        }
        else if (actor.type == WorldInteractionType.Shop) { dialogueOpen = false; shopOpen = true; }
        else { dialogueOpen = false; eventOpen = true; }
    }

    private string GetMatchLockReason(MatchData match)
    {
        if (match == null) return "赛事不存在";
        if (match.matchType == MatchType.Champion && session.GetLeaguePoints(match.cityId) < CampaignCatalog.GetCity(match.cityId).requiredPoints)
            return $"城市冠军需要 {CampaignCatalog.GetCity(match.cityId).requiredPoints} 积分";
        if (!string.IsNullOrEmpty(match.prerequisiteMatchId) && !session.IsMatchComplete(match.prerequisiteMatchId))
            return $"需要先完成：{CampaignCatalog.GetMatch(match.prerequisiteMatchId).displayName}";
        if (!session.HasLegalDeck) return "当前卡组不合法，请按 B 编辑卡组";
        return "当前无法开始赛事";
    }

    private void OpenDeck()
    {
        deckDraft = new List<int>(session.State.deckDraftCardIds);
        deckOpen = true;
        Time.timeScale = 0f;
    }

    public void OpenDeckEditor()
    {
        if (!dialogueOpen && !shopOpen && !eventOpen) OpenDeck();
    }

    private void UpdateHud()
    {
        CityData city = CampaignCatalog.GetCity(session.State.currentCityId);
        CitySaveData state = session.GetCityState(session.State.currentCityId);
        if (hudText != null)
            hudText.text = $"{city?.displayName ?? "第一城"}\n积分：{state.leaguePoints}/{city?.requiredPoints}    金币：{session.State.currency}\n收藏：{session.State.collectedCardIds.Count} 张    徽章：{session.State.badgeIds.Count}" + (Time.unscaledTime < statusUntil ? "\n" + statusMessage : string.Empty);
    }

    private void SavePlayer()
    {
        if (player == null || session == null || session.State == null) return;
        session.SetPlayerPosition(player.position);
        session.Save();
    }

    private void ShowStatus(string message)
    {
        statusMessage = message;
        statusUntil = Time.unscaledTime + 3f;
    }

    private void OnGUI()
    {
        if (session == null || session.State == null) return;
        if (dialogueOpen) DrawDialogue();
        if (shopOpen) DrawShop();
        if (deckOpen) DrawDeckEditor();
        if (eventOpen) DrawEvent();
    }

    private void DrawDialogue()
    {
        GUI.Box(new Rect(280f, Screen.height - 260f, Screen.width - 560f, 190f), dialogueActor.displayName);
        MatchData match = CampaignCatalog.GetMatch(dialogueActor.id);
        string text = match != null ? $"{match.displayName}\n对手：{match.opponentId}\n奖励：{match.goldReward} 金币 / {match.pointReward} 积分" : "欢迎来到第一城。这里的每一场比赛都会留下你的足迹。";
        GUI.Label(new Rect(310f, Screen.height - 225f, Screen.width - 620f, 70f), text);
        if (dialogueActor.type == WorldInteractionType.Match)
        {
            if (GUI.Button(new Rect(320f, Screen.height - 145f, 190f, 42f), "挑战")) Activate(dialogueActor);
            if (GUI.Button(new Rect(530f, Screen.height - 145f, 190f, 42f), "查看条件")) ShowStatus(GetMatchLockReason(match));
        }
        else if (dialogueActor.type == WorldInteractionType.Shop && GUI.Button(new Rect(320f, Screen.height - 145f, 190f, 42f), "购买")) { dialogueOpen = false; shopOpen = true; }
        else if (dialogueActor.type == WorldInteractionType.Event && GUI.Button(new Rect(320f, Screen.height - 145f, 190f, 42f), "查看事件")) { dialogueOpen = false; eventOpen = true; }
        if (GUI.Button(new Rect(Screen.width - 520f, Screen.height - 145f, 150f, 42f), "离开")) ClosePanels();
    }

    private void DrawShop()
    {
        GUI.Box(new Rect(160f, 90f, Screen.width - 320f, Screen.height - 180f), "第一城卡牌商店");
        GUI.Label(new Rect(190f, 125f, 600f, 30f), $"金币：{session.State.currency}    普通 30（5积分）  稀有 60（10积分）  限定 100（15积分）");
        IReadOnlyList<CardShopEntry> entries = CampaignCatalog.GetFirstCityShop();
        shopScroll = GUI.BeginScrollView(new Rect(190f, 165f, Screen.width - 380f, Screen.height - 290f), shopScroll, new Rect(0f, 0f, Screen.width - 420f, entries.Count * 40f));
        for (int i = 0; i < entries.Count; i++)
        {
            CardShopEntry entry = entries[i];
            string reason;
            bool canBuy = session.CanBuyCard(entry, out reason);
            GUI.Label(new Rect(0f, i * 40f, 500f, 32f), $"{entry.cardId}  {entry.rarity}  {entry.price}金币  {entry.archetype}");
            GUI.enabled = canBuy;
            if (GUI.Button(new Rect(520f, i * 40f, 130f, 30f), canBuy ? "购买" : reason)) session.BuyCard(entry, out reason);
            GUI.enabled = true;
        }
        GUI.EndScrollView();
        if (GUI.Button(new Rect(Screen.width - 360f, Screen.height - 105f, 150f, 42f), "关闭")) ClosePanels();
    }

    private void DrawDeckEditor()
    {
        GUI.Box(new Rect(230f, 100f, Screen.width - 460f, Screen.height - 200f), "卡组编辑器");
        GUI.Label(new Rect(260f, 140f, 700f, 30f), $"草稿：{deckDraft.Count}/20    {session.GetDeckValidationError(deckDraft)}");
        IReadOnlyList<CardShopEntry> cards = CampaignCatalog.GetFirstCityShop();
        for (int i = 0; i < cards.Count; i++)
        {
            int cardId = cards[i].cardId;
            int count = CountCard(deckDraft, cardId);
            float y = 190f + i * 28f;
            GUI.Label(new Rect(270f, y, 170f, 24f), $"{cardId}  x{count}");
            GUI.enabled = session.State.collectedCardIds.Contains(cardId) && count < 2;
            if (GUI.Button(new Rect(445f, y, 42f, 23f), "+")) deckDraft.Add(cardId);
            GUI.enabled = count > 0;
            if (GUI.Button(new Rect(492f, y, 42f, 23f), "-")) deckDraft.Remove(cardId);
            GUI.enabled = true;
        }
        if (GUI.Button(new Rect(270f, Screen.height - 155f, 180f, 42f), "保存草稿")) { session.SaveDeckDraft(deckDraft); ShowStatus("草稿已保存"); }
        if (GUI.Button(new Rect(470f, Screen.height - 155f, 180f, 42f), "恢复最近合法卡组")) deckDraft = session.GetBattleDeck();
        if (GUI.Button(new Rect(Screen.width - 470f, Screen.height - 155f, 150f, 42f), "关闭")) ClosePanels();
    }

    private void DrawEvent()
    {
        GUI.Box(new Rect(360f, Screen.height - 250f, Screen.width - 720f, 170f), "街角事件");
        GUI.Label(new Rect(390f, Screen.height - 210f, Screen.width - 780f, 65f), "信使告诉你：第一城的赛事积分会永久保留，失败只会让你回到城市入口。准备好后继续前进吧。");
        if (GUI.Button(new Rect(Screen.width - 520f, Screen.height - 130f, 150f, 40f), "离开")) ClosePanels();
    }

    private static int CountCard(IList<int> cards, int cardId)
    {
        int count = 0;
        if (cards == null) return count;
        foreach (int id in cards) if (id == cardId) count++;
        return count;
    }
}

public sealed class HDAdventureCameraFollow : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private Quaternion rotation;

    internal void Bind(Transform followTarget, Vector3 followOffset, Quaternion followRotation)
    {
        target = followTarget;
        offset = followOffset;
        rotation = followRotation;
    }

    internal void Apply()
    {
        if (target == null) return;
        transform.SetPositionAndRotation(target.position + offset, rotation);
    }

    private void LateUpdate() => Apply();

    private void OnPreCull() => Apply();
}

public sealed class HDAdventureBuildingOcclusion : MonoBehaviour
{
    [SerializeField] private float focusHeight = 0.9f;
    [SerializeField] private float refreshInterval = 0.1f;
    [SerializeField] private float rayPadding = 0.15f;

    private Camera gameplayCamera;
    private Transform target;
    private float nextRefreshTime;
    private Renderer[] sceneRenderers = new Renderer[0];
    private readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>();
    private readonly HashSet<Renderer> currentOccluders = new HashSet<Renderer>();
    private readonly RaycastHit[] raycastHits = new RaycastHit[64];

    internal void Bind(Camera camera, Transform followTarget)
    {
        gameplayCamera = camera;
        target = followTarget;
        RefreshSceneRenderers();
    }

    private void LateUpdate()
    {
        if (gameplayCamera == null) gameplayCamera = HDAdventureRuntime.GameplayCamera;
        if (target == null) target = transform;
        if (Time.unscaledTime >= nextRefreshTime)
        {
            RefreshSceneRenderers();
            nextRefreshTime = Time.unscaledTime + refreshInterval;
        }
        if (gameplayCamera != null && target != null) FindOccluders();
    }

    private void RefreshSceneRenderers()
    {
        sceneRenderers = FindObjectsOfType<Renderer>(true);
    }

    private void FindOccluders()
    {
        currentOccluders.Clear();
        Vector3 origin = gameplayCamera.transform.position;
        Vector3 targetPoint = target.position + Vector3.up * focusHeight;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.01f)
        {
            RestoreOccluders();
            return;
        }

        Ray ray = new Ray(origin, toTarget / distance);
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, Mathf.Max(0f, distance - rayPadding),
            ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++) AddOccluderFromCollider(raycastHits[i].collider);

        foreach (Renderer renderer in sceneRenderers)
        {
            if (!IsBuildingRenderer(renderer)) continue;
            if (renderer.bounds.IntersectRay(ray, out float hitDistance) && hitDistance < distance - rayPadding)
                currentOccluders.Add(renderer);
        }

        foreach (Renderer renderer in currentOccluders)
        {
            if (renderer == null || !renderer.enabled) continue;
            renderer.enabled = false;
            hiddenRenderers.Add(renderer);
        }
        RestoreOccludersNotInCurrentSet();
    }

    private void AddOccluderFromCollider(Collider collider)
    {
        if (collider == null) return;
        Renderer[] renderers = collider.GetComponentsInParent<Renderer>(true);
        foreach (Renderer renderer in renderers)
            if (IsBuildingRenderer(renderer)) currentOccluders.Add(renderer);
    }

    private void RestoreOccludersNotInCurrentSet()
    {
        List<Renderer> restored = new List<Renderer>();
        foreach (Renderer renderer in hiddenRenderers)
        {
            if (renderer == null || currentOccluders.Contains(renderer)) continue;
            renderer.enabled = true;
            restored.Add(renderer);
        }
        foreach (Renderer renderer in restored) hiddenRenderers.Remove(renderer);
    }

    private void RestoreOccluders()
    {
        foreach (Renderer renderer in hiddenRenderers)
            if (renderer != null) renderer.enabled = true;
        hiddenRenderers.Clear();
    }

    private bool IsBuildingRenderer(Renderer renderer)
    {
        if (renderer == null) return false;
        if (renderer is SpriteRenderer || renderer is ParticleSystemRenderer) return false;
        if (renderer.transform == target || renderer.transform.IsChildOf(target)) return false;
        if (renderer.GetComponentInParent<HDAdventureRuntime>() != null) return false;
        if (renderer.GetComponentInParent<WorldInteractionActor>() != null) return false;

        string objectName = renderer.transform.name;
        Transform parent = renderer.transform.parent;
        while (parent != null)
        {
            objectName += " " + parent.name;
            parent = parent.parent;
        }

        return objectName.Contains("EnvBdg") || objectName.Contains("EnvMdrMD_") ||
               objectName.Contains("SM_Wall") || objectName.Contains("SM_wall") ||
               objectName.Contains("Building") || objectName.Contains("House") ||
               objectName.Contains("Mansion") || objectName.Contains("DepartmentStore") ||
               objectName.Contains("Theater") || objectName.Contains("Pub");
    }
}

public sealed class WorldInteractionActor : MonoBehaviour
{
    private HDAdventureRuntime runtime;
    internal string id;
    internal string displayName;
    internal WorldInteractionType type;
    public Sprite[] idleFrames;
    public float animationFPS = 8f;
    private SpriteRenderer spriteRenderer;
    private int frame;
    private float timer;

    internal void Bind(HDAdventureRuntime owner, string actorId, string label, WorldInteractionType actorType)
    {
        runtime = owner;
        id = actorId;
        displayName = label;
        type = actorType;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    internal void SetIdleFrames(Sprite[] frames, float fps)
    {
        idleFrames = frames;
        animationFPS = Mathf.Max(1f, fps);
        frame = 0;
        timer = 0f;
        if (spriteRenderer != null && idleFrames != null && idleFrames.Length > 0)
            spriteRenderer.sprite = idleFrames[0];
    }

    private void Update()
    {
        Camera camera = HDAdventureRuntime.GameplayCamera;
        if (camera != null)
        {
            Vector3 towardCamera = Vector3.ProjectOnPlane(camera.transform.position - transform.position, Vector3.up);
            if (towardCamera.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(towardCamera, Vector3.up);
        }
        if (spriteRenderer == null || idleFrames == null || idleFrames.Length == 0) return;
        timer += Time.deltaTime * animationFPS;
        if (timer >= 1f) { timer = 0f; frame = (frame + 1) % idleFrames.Length; }
        spriteRenderer.sprite = idleFrames[frame];
    }
}

