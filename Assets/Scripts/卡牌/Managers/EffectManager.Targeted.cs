using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;


// 需要选择目标的卡牌效果实现
public partial class EffectManager
{
    #region 需要选择目标的效果实现

    // 选一个敌人造成伤害
    private void DealDamageToEnemy(CardController effectCard, CardEffect effect)
    {
        _isProcessingEffect = true;
        if (!(effect.effectValue.Length > 0)) return;
        int damage = effect.effectValue[0];
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

            FinishStep();
        });
    }

    // 选一个敌人加攻防
    private void BuffEnemy(CardController effectCard, CardEffect effect)
    {
        _isProcessingEffect = true;

        if (!(effect.effectValue.Length > 1)) return;
        int addAtk = effect.effectValue[0];
        int addHp = effect.effectValue[1];
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

            FinishStep();
        });
    }

    // 选一个敌人直接消灭
    private void DestoryEnemy(CardController effectCard, CardEffect effect)
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

            FinishStep();
        });
    }

    // 选一个敌人沉默
    private void SlienceEnemy(CardController effectCard, CardEffect effect)
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

            FinishStep();
        });
    }

    // 选一个友军治疗
    private void HealAlly(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        _isProcessingEffect = true;
        int heal = effect.effectValue[0];
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

            FinishStep();
        });
    }

    // 选一个敌人弹回它的手牌
    private void EnemyBackHand(CardController effectCard, CardEffect effect)
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

            FinishStep();
        });
    }

    // 选一张场上的牌（不含自己）弹回持有者手牌
    private void OtherBackHand(CardController effectCard, CardEffect effect)
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

            FinishStep();
        });
    }

    /// <summary>
    /// 从墓地选择干员返回手牌
    /// </summary>
    /// <param name="effectPack"></param>
    private void DigMumber(CardController effectCard, CardEffect effect)
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

            FinishStep();
        }, false);
    }

    /// <summary>
    /// 从墓地复活干员
    /// </summary>
    /// <param name="effectPack"></param>
    private void Revive(CardController effectCard, CardEffect effect)
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
                if (effectPlayer.field.IsFull) continue; // 场上满了就留在墓地
                target.Init(target.cardData, target.player);
                // 加入场上
                effectPlayer.graveCards.Remove(target);
                effectPlayer.field.AddCard(target);
                target.cardState = CardState.Field;
                target.cardDisplay.ShowBack(false);
            }

            FinishStep();
        }, false);
    }

    // 选一个友军加攻防
    private void BuffAlly(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 1) return;
        _isProcessingEffect = true;
        int addAtk = effect.effectValue[0];
        int addHp = effect.effectValue[1];
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

            FinishStep();
        });
    }

    // 从牌堆检索一张费用达标的干员加入手牌
    private void SearchMumberCostUp(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        _isProcessingEffect = true;
        int costUp = effect.effectValue[0];
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

        GM.Ins.BM.TM.SelectFormList(effectPlayer, targetCards, 1, (targetPack) => RevealSearchedCard(effectPlayer, targetPack));
    }

    // 从牌堆检索一张指定触发时点的干员加入手牌
    private void SearchMumberTrigger(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        _isProcessingEffect = true;
        TriggerType triggerType = (TriggerType)effect.effectValue[0];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家
        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.deckCards)
        {
            // 判定条件
            if (target.cardData.HasEffect(triggerType) &&
                target.cardData.cardType == CardType.MUMBER)
            {
                targetCards.Add(target);
                target.cardDisplay.ShowSpecial(true);
            }
        }

        GM.Ins.BM.TM.SelectFormList(effectPlayer, targetCards, 1, (targetPack) => RevealSearchedCard(effectPlayer, targetPack));
    }

    // 把检索到的卡亮给对方看，看完再收进手牌
    private void RevealSearchedCard(PlayerController player, TargetPack targetPack)
    {
        if (targetPack.cards.Count == 0)
        {
            FinishStep();
            return;
        }

        CardController target = targetPack.cards[0];
        player.deckCards.Remove(target);
        string title = player.isMainPlayer ? "我方检索到的卡牌" : "对手检索到的卡牌";
        GM.Ins.BM.TM.selectContainer.ShowReveal(target, title, () =>
        {
            player.hands.AddCard(target);
            target.cardState = CardState.Hand;
            target.cardDisplay.ShowBack(!player.isMainPlayer);
            FinishStep();
        });
    }


    // 选一张场上的牌弹回手牌并加费用
    private void BackHandAddCost(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        int addCost = effect.effectValue[0];
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

            FinishStep();
        });
    }

    // 选一个血量不高于 2 的友军加攻防
    private void BuffLowHpAlly(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 1) return;
        _isProcessingEffect = true;
        int addAtk = effect.effectValue[0];
        int addHp = effect.effectValue[1];
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

            FinishStep();
        });
    }

    // 选若干张手牌弃掉，再抽等量的牌
    private void DropAndDraw(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 1) return;
        _isProcessingEffect = true;
        int dropNum = effect.effectValue[0];
        int drawNum = effect.effectValue[1];
        PlayerController effectPlayer = effectCard.player; // 效果发动玩家

        List<CardController> targetCards = new();
        foreach (var target in effectPlayer.hands.handCards)
        {
            if (target != effectCard)
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

            FinishStep();
        });
    }

    #endregion
}
