
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 电脑玩家，每 1.5 秒做一个动作：召唤 / 施法 / 发动主动效果 / 攻击 / 结束回合
public class AIController : PlayerController
{
    private bool isStarted = false; // 本回合是否已经开始行动
    private float timer; // 行动间隔计时
    // 回合开始后延迟 1 秒才允许 AI 行动
    public override void TurnStart()
    {
        base.TurnStart();
        DOVirtual.DelayedCall(1f, () =>
        {
            ResumeTurn();
        });
    }

    // 续战只恢复行动，不重复抽牌或增加费用。
    public void ResumeTurn()
    {
        timer = 0f;
        isStarted = isInTurn;
    }

    // 交出回合时停止当前行动计时。
    public override void TurnEnd()
    {
        base.TurnEnd();
        isStarted = false;
    }

    // 每隔 1.5 秒推进一步 AI 行动
    private void Update()
    {
        if (isStarted && isInTurn && Time.timeScale > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= 1.5f)
            {
                timer = 0f;
                TickOneStep();
            }
        }
    }

    // 按优先级走一步：能召唤就召唤，能施法就施法，能发动主动效果就发动，能攻击就攻击，都不行就结束回合
    private void TickOneStep()
    {
        if (!GM.Ins.BM.IsSettled) return;
        if (GM.Ins.BM.Tutorial.IsGuiding)
        {
            GM.Ins.BM.Tutorial.TakeEnemyTurn(this);
            return;
        }
        foreach (var handCard in hands.handCards)
        {
            if (handCard.cardData.cardType == CardType.MUMBER)
            {
                if (GM.Ins.BM.CheckSummonCondition(handCard))
                {
                    GM.Ins.BM.SummonCard(handCard);
                    return;
                }
            }
            else if (handCard.cardData.cardType == CardType.SPELL)
            {
                // 施放法术
                if (GM.Ins.BM.CheckSpellCastCondition(handCard))
                {
                    GM.Ins.BM.CastSpell(handCard);
                    return;
                }
            }
        }

        foreach (var fieldCard in field.cards)
        {
            // 发动场上角色的主动效果
            if (GM.Ins.BM.EM.CanCastCard(fieldCard))
            {
                GM.Ins.BM.EM.TriggerCardEffect(TriggerType.Cast, fieldCard);
                return;
            }
        }

        foreach (var fieldCard in field.cards)
        {
            if (fieldCard.ableAttack)
            {
                // 随机攻击一个可攻击的目标
                PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(playerId);
                var attackableTargets = new List<CardController>();
                foreach (var enemyCard in enemyPlayer.field.cards)
                {
                    if (GM.Ins.BM.IsAttackableTarget(enemyCard))
                    {
                        attackableTargets.Add(enemyCard);
                    }
                }
                CardController targetCard = null;
                if (attackableTargets.Count > 0)
                {
                    int randIndex = UnityEngine.Random.Range(0, attackableTargets.Count);
                    targetCard = attackableTargets[randIndex];
                }

                if (targetCard != null)
                {
                    GM.Ins.BM.AttackCard(fieldCard, targetCard);
                    return;
                }
                else
                {
                    if (GM.Ins.BM.IsAttackablePlayer(enemyPlayer))
                    {
                        // 攻击对方玩家
                        GM.Ins.BM.AttackPlayer(fieldCard, enemyPlayer);
                        return;
                    }
                }
            }
        }

        // 结束回合
        GM.Ins.BM.OnClickTurnEnd(playerId);
        isStarted = false;
    }

    // 从候选目标里随机挑不重复的若干个
    public TargetPack RandomSelect(List<CardController> targetCards, int num)
    {
        // AI自动选择（随机且不重复）
        var autoTargetPack = new TargetPack();
        int selectCount = Math.Min(num, targetCards.Count);
        var tempList = new List<CardController>(targetCards);
        for (int i = 0; i < selectCount; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, tempList.Count);
            autoTargetPack.cards.Add(tempList[randIndex]);
            tempList.RemoveAt(randIndex);
        }
        return autoTargetPack;
    }
}
