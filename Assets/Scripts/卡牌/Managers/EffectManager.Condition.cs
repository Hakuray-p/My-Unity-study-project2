using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;


// 卡牌效果的发动条件判定
public partial class EffectManager
{
    #region 发动条件

    // 按法术第一个效果的发动条件判断能不能打
    public bool CheckCastCondition(CardController effectCard)
    {
        if (effectCard == null || effectCard.cardData == null ||
            effectCard.cardData.effects == null || effectCard.cardData.effects.Count == 0) return true;
        return CheckCondition(effectCard, effectCard.cardData.effects[0]);
    }

    // 判断这张卡现在有没有能发动的主动效果，条件不满足时不发动的表现也不该有
    public bool CanCastCard(CardController effectCard)
    {
        if (effectCard == null || effectCard.cardData == null || effectCard.cardData.effects == null) return false;
        if (effectCard.isSlience || !effectCard.ableCast) return false;
        foreach (CardEffect effect in effectCard.cardData.effects)
        {
            if (effect.triggerType != TriggerType.Cast) continue;
            if (CheckCondition(effectCard, effect)) return true;
        }

        return false;
    }

    // 按效果自己的发动条件判断能不能发动
    private bool CheckCondition(CardController effectCard, CardEffect effect)
    {
        PlayerController player;
        switch (effect.effectCondition)
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
            default:
                break;
        }

        return true;
    }

    #endregion
}
