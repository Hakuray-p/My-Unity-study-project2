
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using UnityEngine;

    public class AIController: PlayerController
    {
        private bool isStarted = false;
        private float timer;
        public override void TurnStart()
        {
            base.TurnStart();
            DOVirtual.DelayedCall(1f, () =>
            {
                isStarted = true;
            });
        }

        private void Update()
        {
            if (isStarted)
            {
                timer += Time.deltaTime;
                if (timer >= 1.5f)
                {
                    timer = 0f;
                    TickOneStep();
                }
            }
        }

        private void TickOneStep()
        {
            if(GM.Ins.BM.EM.IsProcessingEffect) return;
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
                if (fieldCard.ableAttack)
                {
                    // 随机攻击一个可攻击的目标
                    PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(playerId);
                    List<CardController> attackableTargets = new List<CardController>();
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

        public TargetPack RandomSelect(List<CardController> targetCards, int num)
        {
            // AI自动选择（随机且不重复）
            TargetPack autoTargetPack = new TargetPack();
            int selectCount = Math.Min(num, targetCards.Count);
            List<CardController> tempList = new List<CardController>(targetCards);
            for (int i = 0; i < selectCount; i++)
            {
                int randIndex = UnityEngine.Random.Range(0, tempList.Count);
                autoTargetPack.cards.Add(tempList[randIndex]);
                tempList.RemoveAt(randIndex);
            }
            return autoTargetPack;
        }
    }
