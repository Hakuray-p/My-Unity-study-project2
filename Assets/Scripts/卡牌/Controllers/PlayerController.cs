using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

// 战斗中的一方玩家，管理牌堆、手牌、场上、墓地和费用生命
public class PlayerController : MonoBehaviour
{
    public int playerId; // 玩家编号
    public int deckId; // 使用的卡组编号

    public int playerHealth; // 当前生命值
    public int playerHealthMax = 30; // 生命值上限

    public int cost; // 当前部署费用
    public int costMax; // 部署费用上限

    public DeckData deckData; // 这套牌的数据

    public Transform deckPos; // 牌堆的挂载点
    public Transform iconPos; // 头像挂载点
    public Transform gravePos; // 墓地的挂载点

    public List<CardController> deckCards = new(); // 卡组
    public FieldController field; // 战场区域
    public List<CardController> graveCards = new(); // 墓地
    public HandContainer hands; // 手牌区域

    [Header("UI")]
    public TMP_Text costText; // 费用显示
    public TMP_Text healthText; // 生命值显示


    public bool isMainPlayer; // 是不是玩家本人
    public bool isInTurn; // 是不是轮到这一方
    public int fatigueLevel; // 疲劳等级

    // 初始化这一方：准备牌堆、洗牌，并设好初始费用和生命
    public void Init(IList<int> cardIds = null, bool populateDeck = true)
    {
        if (deckCards == null) deckCards = new List<CardController>();
        if (graveCards == null) graveCards = new List<CardController>();
        ClearRuntimeCards();

        if (cardIds == null)
        {
            if (isMainPlayer)
            {
                cardIds = CampaignSession.Instance.GetBattleDeck();
            }
            else
            {
                cardIds = CampaignCatalog.GetDeck(deckId);
            }
        }

        deckData = new DeckData();
        if (cardIds != null)
        {
            deckData.cardDataList.AddRange(cardIds);
        }

        if (populateDeck)
        {
            foreach (int cardId in deckData.cardDataList)
            {
                CreateRuntimeCard(cardId, CardState.Deck);
            }

            shuffleDeck();
        }

        // 部署费用
        costMax = 1;
        cost = 1;

        UpdateCostUI();

        playerHealthMax = GameConst.initalHealth;
        playerHealth = GameConst.initalHealth;
        fatigueLevel = 0;
        UpdateHealthUI();
    }

    // 清掉这一方所有运行时的卡牌实例
    public void ClearRuntimeCards()
    {
        DestroyCards(deckCards);
        if (hands != null) DestroyCards(hands.handCards);
        if (field != null) DestroyCards(field.cards);
        DestroyCards(graveCards);
        if (deckCards != null) deckCards.Clear();
        if (hands != null && hands.handCards != null) hands.handCards.Clear();
        if (field != null && field.cards != null) field.cards.Clear();
        if (graveCards != null) graveCards.Clear();
    }

    // 销毁一批卡牌实例
    private void DestroyCards(IEnumerable<CardController> cards)
    {
        if (cards == null) return;
        foreach (CardController card in cards)
        {
            if (card != null) Destroy(card.gameObject);
        }
    }

    // 按卡牌 id 造一张运行时实例，并按目标区域挂好
    public CardController CreateRuntimeCard(int cardId, CardState state)
    {
        if (GM.Ins == null || GM.Ins.DM == null || GM.Ins.DM.cardListSO == null ||
            GM.Ins.BM == null || GM.Ins.BM.cardPrefab == null)
        {
            return null;
        }

        CardData cardData = GM.Ins.DM.cardListSO.GetData(cardId);
        if (cardData == null) return null;

        CardController card = Instantiate(GM.Ins.BM.cardPrefab);
        card.Init(cardData, this);
        card.cardState = state;
        switch (state)
        {
            case CardState.Deck:
                card.transform.SetParent(deckPos, false);
                deckCards.Add(card);
                break;
            case CardState.Hand:
                if (hands != null) hands.AddCard(card);
                break;
            case CardState.Field:
                if (field != null) field.AddCard(card);
                break;
            case CardState.Graveyard:
                card.transform.SetParent(gravePos, false);
                graveCards.Add(card);
                break;
        }

        return card;
    }

    // 洗牌，顺便播洗牌音效
    public void shuffleDeck()
    {
        for (int i = 0; i < deckCards.Count; i++)
        {
            int rad = Random.Range(0, deckCards.Count);
            CardController temp = deckCards[i];
            deckCards[i] = deckCards[rad];
            deckCards[rad] = temp;
        }

        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Shuffle);
    }

    // 回合开始：涨费用、抽牌、重置场上的攻击次数
    public virtual void TurnStart()
    {
        // 回合开始时逻辑
        if (costMax < GameConst.costMax)
        {
            costMax += 1;
        }

        cost = costMax;
        UpdateCostUI();

        isInTurn = true;
        // 回合开始抽1
        if (GM.Ins != null && GM.Ins.BM != null) GM.Ins.BM.DrawCard(this, GameConst.turnDraw);
        // 场上所有干员重置攻击
        if (field == null || field.cards == null) return;
        foreach (CardController card in field.cards)
        {
            if (card.mumberAtk != 0)
            {
                card.ableAttack = true;
            }

            card.ableCast = true;
        }
    }

    // 回合结束，交出手牌权
    public virtual void TurnEnd()
    {
        isInTurn = false;
    }

    // 把手牌里的这张卡送进墓地
    public void DiscardHandCard(CardController card)
    {
        if (card == null) return;
        hands.RemoveCard(card);
        graveCards.Add(card);
        card.cardState = CardState.Graveyard;
        card.transform.parent = gravePos;
        card.transform.DOLocalMove(Vector3.zero, 0.5f);
        card.transform.localRotation = Quaternion.identity;
        card.cardDisplay.ShowBack(false);
    }

    // 刷新费用显示
    public void UpdateCostUI()
    {
        if (costText == null) return;
        costText.text = $"{cost}/{costMax}";
        costText.transform.DOScale(Vector3.one * 1.5f, 0.3f).SetLoops(2, LoopType.Yoyo);
    }

    // 刷新生命值显示
    public void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = playerHealth.ToString();
    }

    // 玩家受伤，扣血并刷新界面
    public void TakeDamage(int num)
    {
        playerHealth -= num;
        UpdateHealthUI();
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Damage);
    }

    // 玩家回血，不超过上限
    public void Heal(int num)
    {
        playerHealth += num;
        if (playerHealth > playerHealthMax)
        {
            playerHealth = playerHealthMax;
        }
        UpdateHealthUI();
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Heal);
    }
}
