using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 卡组管理场景的总管理器，负责草稿状态和卡库、构筑区两块网格的协调。
public sealed class DeckManagerSceneGM : MonoBehaviour
{
    private const int DeckSize = 30; // 卡组要求的张数

    private CampaignSession session; // 当前存档会话
    private List<int> draft; // 正在编辑的卡组草稿
    private List<int> library; // 卡库显示的卡，同名只留一张、已经全放进卡组的不显示
    private List<int?> libraryCounts; // 卡库每张卡还能放进卡组的张数
    private DeckCardGrid deckGrid; // 构筑区网格
    private DeckCardGrid libraryGrid; // 卡库网格
    private DeckCardInspector inspector; // 左侧卡牌详情
    private TMP_Text hintText; // 底部提示文字
    private string search = string.Empty; // 卡库的搜索词

    // 注册场景加载回调，给卡组管理场景补上管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded += CreateSceneManager;
    }

    // 加载的正是卡组管理场景、且场景里还没有管理器时建一个
    private static void CreateSceneManager(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (loadedScene.name != "DeckManagerScene") return;
        if (FindObjectOfType<DeckManagerSceneGM>() == null)
            new GameObject("DeckManagerSceneGM").AddComponent<DeckManagerSceneGM>();
    }

    // 读草稿，接上两块网格和保存、退出按钮
    private void Start()
    {
        Time.timeScale = 1f;
        session = CampaignSession.Instance;
        draft = new List<int>(session.State.deckDraftCardIds);

        RectTransform deckRoot = FindGridRoot("卡组构筑");
        deckGrid = new DeckCardGrid(deckRoot, SelectFromDeck, RemoveFromDeck, null, null);
        libraryGrid = new DeckCardGrid(FindGridRoot("已有卡牌"), SelectFromLibrary, null, deckRoot, AddToDeck);
        inspector = new DeckCardInspector();
        SceneTool.Find<Button>("保存").onClick.AddListener(SaveDeck);
        SceneTool.Find<Button>("退出").onClick.AddListener(ReturnToCity);
        SceneTool.Find<TMP_InputField>("InputField").onValueChanged.AddListener(ApplySearch);

        CreateHintText(deckRoot);
        RefreshAll();
    }

    // 左键点卡库的卡就在左边显示它的详情
    private void SelectFromLibrary(int index)
    {
        if (index >= library.Count) return;
        inspector.Show(CampaignCatalog.GetCardData(library[index]));
    }

    // 把卡库里的卡拖进构筑区就加进草稿，同名卡最多 3 张
    private void AddToDeck(int index)
    {
        if (index >= library.Count) return;
        if (draft.Count >= DeckSize) { ShowHint("卡组已经满了"); return; }
        int cardId = library[index];
        if (CountInDraft(cardId) >= 3) { ShowHint("同名卡最多 3 张"); return; }
        draft.Add(cardId);
        RefreshAll();
    }

    // 左键点构筑区的卡就在左边显示它的详情
    private void SelectFromDeck(int index)
    {
        if (index >= draft.Count) return;
        inspector.Show(CampaignCatalog.GetCardData(draft[index]));
    }

    // 点构筑区的卡就把它从草稿里拿掉，空格子不响应
    private void RemoveFromDeck(int index)
    {
        if (index >= draft.Count) return;
        draft.RemoveAt(index);
        RefreshAll();
    }

    // 记下搜索词并重刷卡库
    private void ApplySearch(string value)
    {
        search = value;
        RefreshAll();
    }

    // 卡名里包含搜索词就显示，没输入搜索词时全部显示
    private bool IsMatch(int cardId)
    {
        if (string.IsNullOrEmpty(search)) return true;
        return CampaignCatalog.GetCardData(cardId).name.Contains(search, System.StringComparison.OrdinalIgnoreCase);
    }

    // 重新整理卡库：同名只留一张，已经全放进卡组的直接不显示
    private void BuildLibrary()
    {
        var owned = new List<int>();
        var ownedCounts = new List<int>();
        foreach (int cardId in session.State.collectedCardIds)
        {
            int index = owned.IndexOf(cardId);
            if (index < 0)
            {
                owned.Add(cardId);
                ownedCounts.Add(1);
            }
            else ownedCounts[index]++;
        }

        library = new List<int>();
        libraryCounts = new List<int?>();
        for (int i = 0; i < owned.Count; i++)
        {
            if (!IsMatch(owned[i])) continue;
            int remains = ownedCounts[i] - CountInDraft(owned[i]);
            if (remains <= 0) continue;
            library.Add(owned[i]);
            libraryCounts.Add(remains);
        }
    }

    // 两块网格一起刷新
    private void RefreshAll()
    {
        BuildLibrary();
        deckGrid.Refresh(BuildSlots(), null);
        libraryGrid.Refresh(library, libraryCounts);
    }

    // 把草稿补成固定 30 格
    private List<int> BuildSlots()
    {
        var slots = new List<int>(draft);
        while (slots.Count < DeckSize) slots.Add(0);
        return slots;
    }

    // 数一下草稿里某张卡有几张
    private int CountInDraft(int cardId)
    {
        int count = 0;
        foreach (int id in draft) if (id == cardId) count++;
        return count;
    }

    // 保存草稿，不合法时把原因显示出来
    private void SaveDeck()
    {
        string error = session.GetDeckValidationError(draft);
        session.SaveDeckDraft(draft);
        ShowHint(string.IsNullOrEmpty(error) ? "卡组已保存" : error);
    }

    // 退出前留下草稿，回城市
    private void ReturnToCity()
    {
        session.SaveDeckDraft(draft);
        SceneFlowService.ReturnFromDeckManager();
    }

    // 在屏幕底部建一条提示文字
    private void CreateHintText(RectTransform anchorRoot)
    {
        var hintObject = new GameObject("DeckManagerHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(anchorRoot.GetComponentInParent<Canvas>().transform, false);
        hintText = hintObject.GetComponent<TMP_Text>();
        hintText.font = FindChineseFont();
        hintText.color = Color.red;
        hintText.fontSize = 26f;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.raycastTarget = false;
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 24f);
        hintRect.sizeDelta = new Vector2(900f, 40f);
    }

    // 从场景里的文字上取项目用的中文字体
    private static TMP_FontAsset FindChineseFont()
    {
        foreach (TMP_Text text in FindObjectsOfType<TMP_Text>(true))
            if (text.font.name == "SourceHanSansCN-Normal SDF") return text.font;
        return null;
    }

    // 更新底部提示
    private void ShowHint(string message)
    {
        hintText.text = message;
    }

    // 按名字片段找到节点下带网格布局的网格节点
    private static RectTransform FindGridRoot(string namePart)
    {
        Transform root = SceneTool.Find<Transform>(namePart);
        return root.GetComponentInChildren<GridLayoutGroup>(true).GetComponent<RectTransform>();
    }
}
