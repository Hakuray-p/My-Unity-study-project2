﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 卡组编辑场景的总管理器，负责草稿状态和卡库、构筑区两块网格的协调。
public sealed class DeckEditorSceneGM : MonoBehaviour
{
    private const int DeckSize = 30; // 卡组要求的张数

    private CampaignSession session; // 当前存档会话
    private List<int> draft; // 正在编辑的卡组草稿
    private DeckCardGrid deckGrid; // 构筑区网格
    private DeckCardGrid libraryGrid; // 卡库网格
    private DeckCardInspector inspector; // 左侧卡牌详情
    private TMP_Text hintText; // 底部提示文字

    // 注册场景加载回调，给卡组编辑场景补上管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded += CreateSceneManager;
    }

    // 加载的正是卡组编辑场景、且场景里还没有管理器时建一个
    private static void CreateSceneManager(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (loadedScene.name != "DeckEditorScene") return;
        if (FindObjectOfType<DeckEditorSceneGM>() == null)
            new GameObject("DeckEditorSceneGM").AddComponent<DeckEditorSceneGM>();
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
        FindButton("保存").onClick.AddListener(SaveDeck);
        FindButton("退出").onClick.AddListener(ReturnToCity);

        CreateHintText(deckRoot);
        RefreshAll();
    }

    // 左键点卡库的卡就在左边显示它的详情
    private void SelectFromLibrary(int index)
    {
        inspector.Show(CampaignCatalog.GetCardData(session.State.collectedCardIds[index]));
    }

    // 把卡库里的卡拖进构筑区就加进草稿，同名卡最多 3 张
    private void AddToDeck(int index)
    {
        if (draft.Count >= DeckSize) { ShowHint("卡组已经满了"); return; }
        int cardId = session.State.collectedCardIds[index];
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

    // 两块网格一起刷新
    private void RefreshAll()
    {
        deckGrid.Refresh(BuildSlots());
        libraryGrid.Refresh(session.State.collectedCardIds);
    }

    // 把草稿补成固定 30 格，空位写 0
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
        SceneFlowService.ReturnFromDeckEditor();
    }

    // 在屏幕底部建一条提示文字
    private void CreateHintText(RectTransform anchorRoot)
    {
        var hintObject = new GameObject("DeckEditorHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
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
        if (hintText != null) hintText.text = message;
    }

    // 按名字片段找场景里的节点
    private static GameObject FindSceneObject(string namePart)
    {
        foreach (Transform item in FindObjectsOfType<Transform>(true))
            if (item.gameObject.name.Contains(namePart)) return item.gameObject;
        return null;
    }

    // 按名字片段找场景里的按钮
    private static Button FindButton(string namePart)
    {
        foreach (Button button in FindObjectsOfType<Button>(true))
            if (button.gameObject.name.Contains(namePart)) return button;
        return null;
    }

    // 按名字片段找到节点下带网格布局的网格节点
    private static RectTransform FindGridRoot(string namePart)
    {
        return FindSceneObject(namePart).GetComponentInChildren<GridLayoutGroup>(true).GetComponent<RectTransform>();
    }
}
