using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

// 卡牌效果管理，把触发时点入队后逐个执行
public partial class EffectManager : MonoBehaviour
{
    private Queue<CardController> _effectQueue = new();
    private float _effectTimer = 0f;
    private float _effectInterval = 1f; // 每秒1个效果
    public bool IsProcessingEffect => _isProcessingEffect || _effectQueue.Count > 0;
    private bool _isProcessingEffect = false; // 正在处理效果
    private CardController _castingSpell; // 正在发动中的法术卡
    private readonly HashSet<CardController> queuedCards = new HashSet<CardController>();
    private readonly HashSet<CardController> processingCards = new HashSet<CardController>();
    private CardController _processingCard;

    private Dictionary<EffectType, UnityAction<CardController>> _effectActionDic = new();

    private Vector3 cameraOriginPos = new Vector3(0f, 10f, -2.5f);

    // 清空队列并注册效果类型和处理函数的对应关系
    public void Init()
    {
        _effectQueue.Clear();
        queuedCards.Clear();
        processingCards.Clear();
        _castingSpell = null;
        _processingCard = null;
        _isProcessingEffect = false;
        _effectActionDic = new()
        {
            { EffectType.None, null }, { EffectType.DealDamageToEnemy, DealDamageToEnemy }
            , { EffectType.Draw, Draw }, { EffectType.BuffAlly, BuffAlly }
            , { EffectType.BuffSelf, BuffSelf }, { EffectType.DamageAll, DamageAll }
            , { EffectType.BuffAlliesAll, BuffAlliesAll }, { EffectType.AddCostMax, AddCostMax }
            , { EffectType.HealAlly, HealAlly }, { EffectType.EnemyBackHand, EnemyBackHand }
            , { EffectType.DealDamageToRandomEnemy, DealDamageToRandomEnemy }
            , { EffectType.DamageAllEnemy, DamageAllEnemy }, { EffectType.BuffEnemy, BuffEnemy }
            , { EffectType.HeallRandomAllies, HeallRandomAllies }
            , { EffectType.SlienceEnemy, SlienceEnemy }, { EffectType.OtherBackHand, OtherBackHand }
            , { EffectType.DigMumber, DigMumber }, { EffectType.DestoryEnemy, DestoryEnemy }
            , { EffectType.SearchMumberCostUp, SearchMumberCostUp }, { EffectType.GetAll, GetAll }
            , { EffectType.SearchMumberTrigger, SearchMumberTrigger }
            , { EffectType.Henshin, Henshin }, { EffectType.Revive, Revive }
            , { EffectType.AddCost, AddCost }, { EffectType.BackHandAddCost, BackHandAddCost }
            , { EffectType.BuffLowHpAlly, BuffLowHpAlly }
            , { EffectType.AttackAgain, AttackAgain }
            , { EffectType.DropEnemyHand , DropEnemyHand}, { EffectType.DropAndDraw, DropAndDraw}
        };
    }

    void Update()
    {
        if (_effectQueue.Count > 0)
        {
            if (!_isProcessingEffect)
            {
                _effectTimer += Time.deltaTime;
                if (_effectTimer >= _effectInterval)
                {
                    _effectTimer = 0f;
                    TriggerNextEffect();
                }
            }
        }
        else
        {
            _effectTimer = _effectInterval;
        }
    }

    // 回合开始和结束时点
    public void TriggerStartEnd(TriggerType triggerType, int playerId)
    {
        if (triggerType != TriggerType.Start && triggerType != TriggerType.End) return;
        // 假设有全局管理器 GM.Ins.players
        PlayerController player = GM.Ins != null && GM.Ins.BM != null ? GM.Ins.BM.GetPlayer(playerId) : null;
        if (player == null || player.field == null || player.field.cards == null) return;
        foreach (var card in player.field.cards)
        {
            if (card != null && card.cardData != null && card.cardData.triggerType == triggerType)
            {
                if (card.isSlience) continue;
                EnqueueEffect(card);
            }
        }
    }

    // 单张卡的时点触发，类型对得上才入队
    public void TriggerCardEffect(TriggerType triggerType, CardController card)
    {
        if (card == null || card.cardData == null) return;
        if (card.cardData.triggerType != triggerType) return;
        if (card.isSlience) return;
        EnqueueEffect(card);
    }

    // 法术牌发动效果
    public void CastSpell(CardController card)
    {
        if (card == null || card == _castingSpell || queuedCards.Contains(card) || processingCards.Contains(card)) return;
        EnqueueEffect(card);
        _castingSpell = card;
    }

    // 把卡加入效果队列，重复的卡跳过
    private void EnqueueEffect(CardController card)
    {
        if (card == null || queuedCards.Contains(card) || processingCards.Contains(card)) return;
        queuedCards.Add(card);
        _effectQueue.Enqueue(card);
    }

    // 取出队首卡牌，播完抖动动画后执行它的效果
    private void TriggerNextEffect()
    {
        if (_effectQueue.Count == 0) return;
        CardController effectCard = _effectQueue.Dequeue();
        if (effectCard == null) { _isProcessingEffect = false; return; }
        queuedCards.Remove(effectCard);
        processingCards.Add(effectCard);
        _processingCard = effectCard;

        _isProcessingEffect = true;
        if (effectCard.cardData == null)
        {
            EffectFinish();
            return;
        }
        GM.Ins.AM.PlayAudio(AudioType.Effect);
        Debug.Log($"触发{effectCard.cardData.name}的效果, " +
                  $"触发时点{effectCard.cardData.triggerType}" +
                  $"效果类型{effectCard.cardData.effectType}");
        effectCard.transform.DOLocalRotate(new Vector3(0f, 0f, 20f), 0.3f)
            .SetLoops(2, LoopType.Yoyo).SetDelay(0.5f).OnComplete(() =>
            {
                if (effectCard.cardData == null)
                {
                    EffectFinish();
                    return;
                }
                if (!HasRequiredParameters(effectCard))
                {
                    EffectFinish();
                    return;
                }
                if (_effectActionDic.ContainsKey(effectCard.cardData.effectType))
                {
                    UnityAction<CardController> action = _effectActionDic[effectCard.cardData.effectType];
                    if (action != null) action.Invoke(effectCard);
                    else EffectFinish();
                }
                else EffectFinish();
            });
        //effectCard.transform.DOLocalMoveZ(-1.2f, 0.3f).SetLoops(2, LoopType.Yoyo).SetDelay(0.5f);
        //effectCard.transform.DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo);
    }

    // 检查这张卡的效果参数个数够不够
    private bool HasRequiredParameters(CardController effectCard)
    {
        if (effectCard == null || effectCard.cardData == null) return false;
        int count = effectCard.cardData.effectValue == null ? 0 : effectCard.cardData.effectValue.Length;
        switch (effectCard.cardData.effectType)
        {
            case EffectType.BuffSelf:
            case EffectType.BuffAlliesAll:
            case EffectType.DealDamageToRandomEnemy:
            case EffectType.HeallRandomAllies:
            case EffectType.DigMumber:
            case EffectType.SearchMumberCostUp:
            case EffectType.SearchMumberTrigger:
            case EffectType.BackHandAddCost:
            case EffectType.BuffLowHpAlly:
            case EffectType.DropAndDraw:
                return count >= 2;
            case EffectType.Draw:
            case EffectType.DamageAllEnemy:
            case EffectType.DamageAll:
            case EffectType.AddCost:
            case EffectType.AddCostMax:
            case EffectType.DropEnemyHand:
            case EffectType.DealDamageToEnemy:
            case EffectType.BuffEnemy:
            case EffectType.HealAlly:
            case EffectType.EnemyBackHand:
            case EffectType.OtherBackHand:
            case EffectType.DestoryEnemy:
            case EffectType.SlienceEnemy:
                return count >= 1;
            default:
                return true;
        }
    }


    #region 效果实现

    #region 无需选择目标的效果

    // 抽牌
    private void Draw(CardController effectCard)
    {
        PlayerController player = effectCard.player;
        GM.Ins.BM.DrawCard(player, effectCard.cardData.effectValue[0]);
        EffectFinish();
    }

    // 给自己加攻防
    private void BuffSelf(CardController effectCard)
    {
        if (effectCard == null) return;
        if (effectCard.cardData.effectValue.Length <= 1) return;
        int addAtk = effectCard.cardData.effectValue[0];
        int addHp = effectCard.cardData.effectValue[1];

        BuffCard(effectCard, addAtk, addHp);
        EffectFinish();
    }

    // 对敌方全场造成伤害
    private void DamageAllEnemy(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        int damage = effectCard.cardData.effectValue[0];
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId); // 敌方玩家
        List<CardController> targetCards = new();
        targetCards.AddRange(enemyPlayer.field.cards);
        foreach (var target in targetCards)
        {
            target.TakeDamage(damage);
        }

        EffectFinish();
    }

    // 对双方全场造成伤害
    private void DamageAll(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        int damage = effectCard.cardData.effectValue[0];
        List<CardController> targetCards = new();
        foreach (var player in GM.Ins.BM.players)
        {
            targetCards.AddRange(player.field.cards);
        }

        foreach (var target in targetCards)
        {
            target.TakeDamage(damage);
        }

        Debug.Log($"全场伤害{damage}");
        EffectFinish();
    }


    // 增加当前费用
    private void AddCost(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        PlayerController player = effectCard.player;
        player.cost += effectCard.cardData.effectValue[0];
        player.UpdateCostUI();
        EffectFinish();
    }

    // 提高费用上限
    private void AddCostMax(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        PlayerController player = effectCard.player;
        if (player.costMax < GameConst.costMax)
        {
            player.costMax += effectCard.cardData.effectValue[0];
            player.UpdateCostUI();
        }
        EffectFinish();
    }

    // 给全体友军加攻防
    private void BuffAlliesAll(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        int addAtk = effectCard.cardData.effectValue[0];
        int addHp = effectCard.cardData.effectValue[1];
        PlayerController player = effectCard.player;
        List<CardController> targetCards = new();
        targetCards.AddRange(player.field.cards);
        foreach (var target in targetCards)
        {
            BuffCard(target, addAtk, addHp);
        }

        EffectFinish();
    }

    // 随机打若干个敌人
    private void DealDamageToRandomEnemy(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        int num = effectCard.cardData.effectValue[0];
        int damage = effectCard.cardData.effectValue[1];
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId); // 敌方玩家
        num = Math.Min(num, enemyPlayer.field.cards.Count);
        var availableIndices = new List<int>();
        for (int i = 0; i < enemyPlayer.field.cards.Count; i++)
        {
            availableIndices.Add(i);
        }

        // 随机选择不重复的索引
        List<CardController> result = new();
        for (int i = 0; i < num; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            int selectedIndex = availableIndices[randomIndex];

            result.Add(enemyPlayer.field.cards[selectedIndex]);
            availableIndices.RemoveAt(randomIndex);
        }

        foreach (var target in result)
        {
            target.TakeDamage(damage);
        }

        EffectFinish();
    }

    // 随机治疗若干个友军
    private void HeallRandomAllies(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        int num = effectCard.cardData.effectValue[0];
        int heal = effectCard.cardData.effectValue[1];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        num = Math.Min(num, effectPlayer.field.cards.Count);
        var availableIndices = new List<int>();
        for (int i = 0; i < effectPlayer.field.cards.Count; i++)
        {
            availableIndices.Add(i);
        }

        // 随机选择不重复的索引
        List<CardController> result = new();
        for (int i = 0; i < num; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            int selectedIndex = availableIndices[randomIndex];

            result.Add(effectPlayer.field.cards[selectedIndex]);
            availableIndices.RemoveAt(randomIndex);
        }

        foreach (var target in result)
        {
            target.Heal(heal);
        }

        EffectFinish();
    }
    // 让这张卡可以再攻击一次
    private void AttackAgain(CardController effectCard)
    {
        effectCard.ableAttack = true;
        EffectFinish();
    }

    // 随机弃掉敌方若干张手牌
    private void DropEnemyHand(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        int num = effectCard.cardData.effectValue[0];
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId); // 敌方玩家
        // 随机选择一张敌方手牌，置入墓地
        num = Math.Min(num, enemyPlayer.hands.handCards.Count);
        var availableIndices = new List<int>();
        for (int i = 0; i < enemyPlayer.hands.handCards.Count; i++)
        {
            availableIndices.Add(i);
        }
        // 随机选择不重复的索引
        List<CardController> result = new();
        for (int i = 0; i < num; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            int selectedIndex = availableIndices[randomIndex];

            result.Add(enemyPlayer.hands.handCards[selectedIndex]);
            availableIndices.RemoveAt(randomIndex);
        }
        foreach (var target in result)
        {
            enemyPlayer.hands.RemoveCard(target);
            enemyPlayer.graveCards.Add(target);
            target.cardState = CardState.Graveyard;
            target.transform.parent = enemyPlayer.gravePos;
            target.transform.DOLocalMove(Vector3.zero, 0.5f);
            target.transform.localRotation = Quaternion.identity;
            target.cardDisplay.ShowBack(false);
        }
        EffectFinish();
    }

    #endregion


    // 效果收尾：清状态、把法术牌送墓地、相机复位
    private void EffectFinish()
    {
        _isProcessingEffect = false;
        if (_processingCard != null) processingCards.Remove(_processingCard);
        _processingCard = null;
        Debug.Log("效果处理完毕");
        if (_castingSpell != null) // 处理法术牌
        {
            PlayerController player = _castingSpell.player;
            player.graveCards.Add(_castingSpell);
            _castingSpell.transform.parent = player.gravePos;
            _castingSpell.transform.DOLocalMove(Vector3.zero, 0.5f);
            _castingSpell.transform.localRotation = Quaternion.identity;
            //_castingSpell.gameObject.SetActive(false);
            _castingSpell = null;
        }

        Camera.main.transform.DOMove(cameraOriginPos, 0.5f);
    }


    // 复用的通用效果
    private void BuffCard(CardController target, int addAtk, int addHp)
    {
        if (target.cardState != CardState.Field) return; // 只能buff场上的干员
        target.mumberAtk += addAtk;
        if (target.mumberAtk < 0)
        {
            target.mumberAtk = 0;
        }

        target.mumberHp += addHp;
        if (target.mumberHp <= 0)
        {
            GM.Ins.BM.MumberDied(target);
        }

        target.cardDisplay.UpdateDisplay();
    }

    #endregion

}
