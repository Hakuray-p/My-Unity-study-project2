using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class EffectManager : MonoBehaviour
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

    private void EnqueueEffect(CardController card)
    {
        if (card == null || queuedCards.Contains(card) || processingCards.Contains(card)) return;
        queuedCards.Add(card);
        _effectQueue.Enqueue(card);
    }

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

    private void Draw(CardController effectCard)
    {
        PlayerController player = effectCard.player;
        GM.Ins.BM.DrawCard(player, effectCard.cardData.effectValue[0]);
        EffectFinish();
    }

    private void BuffSelf(CardController effectCard)
    {
        if (effectCard == null) return;
        if (effectCard.cardData.effectValue.Length <= 1) return;
        int addAtk = effectCard.cardData.effectValue[0];
        int addHp = effectCard.cardData.effectValue[1];

        BuffCard(effectCard, addAtk, addHp);
        EffectFinish();
    }

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


    private void AddCost(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        PlayerController player = effectCard.player;
        player.cost += effectCard.cardData.effectValue[0];
        player.UpdateCostUI();
        EffectFinish();
    }

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

    private void DealDamageToRandomEnemy(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        int num = effectCard.cardData.effectValue[0];
        int damage = effectCard.cardData.effectValue[1];
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId); // 敌方玩家
        num = Math.Min(num, enemyPlayer.field.cards.Count);
        List<int> availableIndices = new List<int>();
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

    private void HeallRandomAllies(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        int num = effectCard.cardData.effectValue[0];
        int heal = effectCard.cardData.effectValue[1];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        num = Math.Min(num, effectPlayer.field.cards.Count);
        List<int> availableIndices = new List<int>();
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
    private void AttackAgain(CardController effectCard)
    {
        effectCard.ableAttack = true;
        EffectFinish();
    }

    private void DropEnemyHand(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        int num = effectCard.cardData.effectValue[0];
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId); // 敌方玩家
        // 随机选择一张敌方手牌，置入墓地
        num = Math.Min(num, enemyPlayer.hands.handCards.Count);
        List<int> availableIndices = new List<int>();
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

    #region 需要选择目标的效果实现

    private void DealDamageToEnemy(CardController effectCard)
    {
        _isProcessingEffect = true;
        if (!(effectCard.cardData.effectValue.Length > 0)) return;
        int damage = effectCard.cardData.effectValue[0];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId); // 敌方玩家
        List<CardController> targetCards = new();
        foreach (var target in enemyPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                target.TakeDamage(damage);
            }

            EffectFinish();
        });
    }

    private void BuffEnemy(CardController effectCard)
    {
        _isProcessingEffect = true;

        if (!(effectCard.cardData.effectValue.Length > 1)) return;
        int addAtk = effectCard.cardData.effectValue[0];
        int addHp = effectCard.cardData.effectValue[1];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId);
        List<CardController> targetCards = new();
        foreach (var target in enemyPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        Debug.Log("BuffEnemy选择目标");
        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                BuffCard(target, addAtk, addHp);
            }

            EffectFinish();
        });
    }

    private void DestoryEnemy(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId);
        List<CardController> targetCards = new();
        foreach (var target in enemyPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        Debug.Log("BuffEnemy选择目标");
        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                GM.Ins.BM.MumberDied(target);
            }

            EffectFinish();
        });
    }

    private void SlienceEnemy(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId);
        List<CardController> targetCards = new();
        foreach (var target in enemyPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                target.isSlience = true;
            }

            EffectFinish();
        });
    }

    private void HealAlly(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        _isProcessingEffect = true;
        int heal = effectCard.cardData.effectValue[0];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                // 治疗
                target.Heal(heal);
            }

            EffectFinish();
        });
    }

    private void EnemyBackHand(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId);
        List<CardController> targetCards = new();
        foreach (var target in enemyPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                enemyPlayer.field.RemoveCard(target);
                enemyPlayer.hands.AddCard(target);
                target.cardState = CardState.Hand;
                target.Init(target.cardData, target.player); // 重置状态
                target.cardDisplay.ShowBack(!enemyPlayer.isMainPlayer);
            }

            EffectFinish();
        });
    }

    private void OtherBackHand(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId);
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.field.cards)
        {
            if (target == effectCard)
            {
                continue;
            }

            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        foreach (var target in enemyPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                PlayerController targetPlayer = target.player;
                targetPlayer.field.RemoveCard(target);
                targetPlayer.hands.AddCard(target);
                target.cardState = CardState.Hand;
                target.Init(target.cardData, target.player);
                target.cardDisplay.ShowBack(!targetPlayer.isMainPlayer);
            }

            EffectFinish();
        });
    }

    /// <summary>
    /// 从墓地选择干员返回手牌
    /// </summary>
    /// <param name="effectPack"></param>
    private void DigMumber(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        // 从墓地中选择
        foreach (var target in effectPlayer.graveCards)
        {
            if (target.cardData.cardType == CardType.MUMBER)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }

        GM.Ins.BM.TM.SelectFormList(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                target.Init(target.cardData, target.player);
                // 加入手牌
                effectPlayer.graveCards.Remove(target);
                effectPlayer.hands.AddCard(target);
                target.cardState = CardState.Hand;
                target.cardDisplay.ShowBack(!effectPlayer.isMainPlayer);
            }

            EffectFinish();
        }, false);
    }

    /// <summary>
    /// 从墓地复活干员
    /// </summary>
    /// <param name="effectPack"></param>
    private void Revive(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        // 从墓地中选择
        foreach (var target in effectPlayer.graveCards)
        {
            if (target.cardData.cardType == CardType.MUMBER)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }

        GM.Ins.BM.TM.SelectFormList(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                target.Init(target.cardData, target.player);
                // 加入场上
                effectPlayer.graveCards.Remove(target);
                effectPlayer.field.AddCard(target);
                target.cardState = CardState.Field;
                target.cardDisplay.ShowBack(false);
            }

            EffectFinish();
        },false);
    }

    private void BuffAlly(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        _isProcessingEffect = true;
        int addAtk = effectCard.cardData.effectValue[0];
        int addHp = effectCard.cardData.effectValue[1];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        Debug.Log("BuffEnemy选择目标");
        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                BuffCard(target, addAtk, addHp);
            }

            EffectFinish();
        });
    }

    private void SearchMumberCostUp(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        _isProcessingEffect = true;
        int costUp = effectCard.cardData.effectValue[0];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.deckCards)
        {
            // 判定条件
            if (target.cardData.cost >= costUp && target.cardData.cardType == CardType.MUMBER)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }

        GM.Ins.BM.TM.SelectFormList(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                effectPlayer.deckCards.Remove(target);
                effectPlayer.hands.AddCard(target);
                target.cardState = CardState.Hand;
                target.cardDisplay.ShowBack(!effectPlayer.isMainPlayer);
            }

            EffectFinish();
        });
    }

    private void SearchMumberTrigger(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        _isProcessingEffect = true;
        TriggerType triggerType = (TriggerType)effectCard.cardData.effectValue[0];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.deckCards)
        {
            // 判定条件
            if (target.cardData.triggerType == triggerType &&
                target.cardData.cardType == CardType.MUMBER)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }

        GM.Ins.BM.TM.SelectFormList(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                effectPlayer.deckCards.Remove(target);
                effectPlayer.hands.AddCard(target);
                target.cardState = CardState.Hand;
                target.cardDisplay.ShowBack(!effectPlayer.isMainPlayer);
            }

            EffectFinish();
        });
    }

    private void GetAll(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId);
        List<CardController> targetCards = new();
        foreach (var target in enemyPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                target.TakeDamage(5);
            }

            GM.Ins.BM.DrawCard(effectPlayer, 5); // 抽5张牌
            effectPlayer.Heal(5); // 回5点血
            EffectFinish();
        });
    }

    private void Henshin(CardController effectCard)
    {
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.field.cards)
        {
            // 判定条件
            if (target.cardData.index == 1007 && target.cardData.cardType == CardType.MUMBER)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                CardData cardData = GM.Ins.DM.cardListSO.GetData(1017); // 变身为近卫阿米娅
                effectPlayer.field.RemoveCard(target);
                target.Init(cardData, target.player);
                effectPlayer.field.AddCard(target);
                target.cardState = CardState.Field;
                target.cardDisplay.ShowBack(false);
            }

            EffectFinish();
        });
    }

    private void BackHandAddCost(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 0) return;
        int addCost = effectCard.cardData.effectValue[0];
        _isProcessingEffect = true;
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.field.cards)
        {
            targetCards.Add(target);
            target.cardDisplay.ShowSpecial(true);
        }

        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                PlayerController targetPlayer = target.player;
                targetPlayer.field.RemoveCard(target);
                targetPlayer.hands.AddCard(target);
                target.cardState = CardState.Hand;
                target.Init(target.cardData, target.player);
                target.cardDisplay.ShowBack(!targetPlayer.isMainPlayer);
                // 增加费用
                targetPlayer.cost += addCost;
                targetPlayer.UpdateCostUI();
            }

            EffectFinish();
        });
    }

    private void BuffLowHpAlly(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        _isProcessingEffect = true;
        int addAtk = effectCard.cardData.effectValue[0];
        int addHp = effectCard.cardData.effectValue[1];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.field.cards)
        {
            if (target.mumberHp <= 2)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }

        Debug.Log("BuffEnemy选择目标");
        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, 1, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                BuffCard(target, addAtk, addHp);
            }

            EffectFinish();
        });
    }

    private void DropAndDraw(CardController effectCard)
    {
        if (effectCard.cardData.effectValue.Length <= 1) return;
        _isProcessingEffect = true;
        int dropNum = effectCard.cardData.effectValue[0];
        int drawNum = effectCard.cardData.effectValue[1];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.hands.handCards)
        {
            if (target  !=  effectCard)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }
        
        GM.Ins.BM.TM.StartSelectFieldCards(effectPlayer, targetCards, dropNum, (targetPack) =>
        {
            foreach (var target in targetPack.cards)
            {
                effectPlayer.hands.RemoveCard(target);
                effectPlayer.graveCards.Add(target);
                target.cardState = CardState.Graveyard;
                target.transform.parent = effectPlayer.gravePos;
                target.transform.DOLocalMove(Vector3.zero, 0.5f);
                target.transform.localRotation = Quaternion.identity;
                target.cardDisplay.ShowBack(false);
            }
            
            GM.Ins.BM.DrawCard(effectPlayer, drawNum);

            EffectFinish();
        });
    }

    #endregion

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

    #region 发动条件

    public bool CheckCastCondition(CardController effectCard)
    {
        PlayerController player;
        switch (effectCard.cardData.effectCondition)
        {
            case ConditionType.None:
                break;
            case ConditionType.TwoMumber:
                player = effectCard.player;
                if (player.field.cards.Count != 2)
                {
                    return false;
                }
                break;
            case ConditionType.ThreeMoreHand:
                player = effectCard.player;
                if (player.hands.handCards.Count < 3)
                {
                    return false;
                }
                break;
            case ConditionType.HasAmiya:
                player = effectCard.player;
                foreach (var fieldCard in player.field.cards)
                {
                    if (fieldCard.cardData.index == 1007)
                    {
                        return true;
                    }
                }

                return false;
                break;

            case ConditionType.HasEnemy:
                player = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId);
                if (player.field.cards.Count < 1)
                {
                    return false;
                }

                break;
            case ConditionType.HasAlly:
                player = effectCard.player;
                if (player.field.cards.Count < 1)
                {
                    return false;
                }

                break;
            case ConditionType.HasDiedMumber:
                player = effectCard.player;
                foreach (var graveCard in player.graveCards)
                {
                    if (graveCard.cardData.cardType == CardType.MUMBER)
                    {
                        return true;
                    }
                }

                return false;
                break;
            default:
                break;
        }

        return true;
    }

    #endregion
}
