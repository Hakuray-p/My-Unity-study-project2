using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    public int playerId;
    public int deckId;

    public int playerHealth;
    public int playerHealthMax = 30;

    public int cost;
    public int costMax;

    public DeckData deckData;

    public Transform deckPos;
    public Transform iconPos;
    public Transform gravePos;

    public List<CardController> deckCards = new(); // 卡组
    public FieldController field;
    public List<CardController> graveCards = new(); // 墓地
    public HandContainer hands;

    [Header("UI")] 
    public TMP_Text costText;
    public TMP_Text healthText;


    public bool isMainPlayer;
    public bool isInTurn;
    public int fatigueLevel;

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
            foreach (var cardId in deckData.cardDataList)
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

    private void DestroyCards(IEnumerable<CardController> cards)
    {
        if (cards == null) return;
        foreach (CardController card in cards)
        {
            if (card != null) Destroy(card.gameObject);
        }
    }

    public CardController CreateRuntimeCard(int cardId, CardState state)
    {
        if (GM.Ins == null || GM.Ins.DM == null || GM.Ins.DM.cardListSO == null ||
            GM.Ins.BM == null || GM.Ins.BM.cardPrefab == null)
        {
            return null;
        }

        CardData cardData = GM.Ins.DM.cardListSO.GetData(cardId);
        if (cardData == null) return null;

        CardController card = Instantiate(GM.Ins.BM.cardPrefab).GetComponent<CardController>();
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

    public void shuffleDeck()
    {
        for (int i = 0; i < deckCards.Count; i++)
        {
            int rad = Random.Range(0, deckCards.Count);
            var temp = deckCards[i];
            deckCards[i] = deckCards[rad];
            deckCards[rad] = temp;
        }

        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Shuffle);
    }

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
        foreach (var card in field.cards)
        {
            if (card.mumberAtk != 0)
            {
                card.ableAttack = true;
            }
        }
    }

    public virtual void TurnEnd()
    {
        isInTurn = false;
    }

    public void UpdateCostUI()
    {
        if (costText == null) return;
        costText.text = $"{cost}/{costMax}";
        costText.transform.DOScale(Vector3.one * 1.5f, 0.3f).SetLoops(2, LoopType.Yoyo);
    }

    public void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = playerHealth.ToString();
    }

    public void TakeDamage(int num)
    {
        playerHealth -= num;
        UpdateHealthUI();
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Damage);
    }

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
