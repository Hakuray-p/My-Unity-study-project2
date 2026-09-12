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
