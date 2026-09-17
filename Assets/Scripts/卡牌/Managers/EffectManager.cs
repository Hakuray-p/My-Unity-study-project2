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
    private Queue<EffectRequest> _effectQueue = new();
    private float _effectTimer = 0f;
    private float _effectInterval = 1f; // 每秒1个效果
    public bool IsProcessingEffect => _isProcessingEffect || _effectQueue.Count > 0;
    private bool _isProcessingEffect = false; // 正在处理效果
    private CardController _castingSpell; // 正在发动中的法术卡
    private readonly HashSet<CardController> queuedCards = new HashSet<CardController>();
    private readonly HashSet<CardController> processingCards = new HashSet<CardController>();
    private CardController _processingCard;
    private List<CardEffect> _pendingEffects; // 这次触发要按顺序执行的效果
    private int _pendingIndex; // 正在跑第几个效果

    private Dictionary<EffectType, UnityAction<CardController, CardEffect>> _effectActionDic = new();

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
            , { EffectType.SearchMumberCostUp, SearchMumberCostUp }, { EffectType.HealPlayer, HealPlayer }
            , { EffectType.SearchMumberTrigger, SearchMumberTrigger }
            , { EffectType.Revive, Revive }
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
            if (card != null && card.cardData != null && card.cardData.HasEffect(triggerType))
            {
                if (card.isSlience) continue;
                EnqueueEffect(card, triggerType);
            }
        }
    }

    // 单张卡的时点触发，类型对得上才入队
    public void TriggerCardEffect(TriggerType triggerType, CardController card)
    {
        if (card == null || card.cardData == null) return;
        if (!card.cardData.HasEffect(triggerType)) return;
        if (card.isSlience) return;
        if (triggerType == TriggerType.Cast)
        {
            if (card.player.isMainPlayer && !GM.Ins.BM.Tutorial.Allows(TutorialAction.Ability, card)) return;
            if (!CanCastCard(card)) return; // 条件不满足就整个不发动，摇牌和音效都不播
            card.ableCast = false; // 主动效果每回合只能发动一次
            GM.Ins.BM.NoteAction();
        }

        EnqueueEffect(card, triggerType);
        if (triggerType == TriggerType.Cast && card.player.isMainPlayer)
            GM.Ins.BM.Tutorial.ActionAccepted(TutorialAction.Ability);
    }

    // 法术牌发动效果
    public void CastSpell(CardController card)
    {
        if (card == null || card == _castingSpell || queuedCards.Contains(card) || processingCards.Contains(card)) return;
        EnqueueEffect(card, TriggerType.None); // 法术发动时按顺序跑全部效果
        _castingSpell = card;
    }

    // 把卡加入效果队列，重复的卡跳过
    private void EnqueueEffect(CardController card, TriggerType triggerType)
    {
        if (card == null || queuedCards.Contains(card) || processingCards.Contains(card)) return;
        queuedCards.Add(card);
        _effectQueue.Enqueue(new EffectRequest { card = card, effects = CollectEffects(card.cardData, triggerType) });
    }

    // 取出这次触发要执行的效果，时点传 None 表示跑全部效果
    private static List<CardEffect> CollectEffects(CardData cardData, TriggerType triggerType)
    {
        var result = new List<CardEffect>();
        if (cardData == null || cardData.effects == null) return result;
        foreach (CardEffect effect in cardData.effects)
        {
            if (triggerType == TriggerType.None || effect.triggerType == triggerType) result.Add(effect);
        }

        return result;
    }

    // 取出队首请求，播完抖动动画后按顺序执行它的效果
    private void TriggerNextEffect()
    {
        if (_effectQueue.Count == 0) return;
        EffectRequest request = _effectQueue.Dequeue();
        CardController effectCard = request.card;
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
        _pendingEffects = request.effects;
        _pendingIndex = 0;
        GM.Ins.AM.PlayAudio(AudioType.Effect);
        Debug.Log($"触发{effectCard.cardData.name}的效果, 效果数{_pendingEffects.Count}");
        effectCard.transform.DOLocalRotate(new Vector3(0f, 0f, 20f), 0.3f)
            .SetLoops(2, LoopType.Yoyo).SetDelay(0.5f).OnComplete(() => RunNextEffect(effectCard));
        //effectCard.transform.DOLocalMoveZ(-1.2f, 0.3f).SetLoops(2, LoopType.Yoyo).SetDelay(0.5f);
        //effectCard.transform.DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo);
    }

    // 按顺序跑下一个效果，条件不满足就停掉后面的效果
    private void RunNextEffect(CardController effectCard)
    {
        if (effectCard.cardData == null || _pendingEffects == null || _pendingIndex >= _pendingEffects.Count)
        {
            EffectFinish();
            return;
        }

        CardEffect effect = _pendingEffects[_pendingIndex];
        _pendingIndex++;
        if (!CheckCondition(effectCard, effect))
        {
            Debug.Log($"效果{effect.effectType}的发动条件不满足，停止后续效果");
            EffectFinish();
            return;
        }

        if (!HasRequiredParameters(effect))
        {
            Debug.Log($"效果{effect.effectType}的参数不够，停止后续效果");
            EffectFinish();
            return;
        }

        if (_effectActionDic.TryGetValue(effect.effectType, out UnityAction<CardController, CardEffect> action) && action != null)
        {
            action.Invoke(effectCard, effect);
            return;
        }

        EffectFinish();
    }

    // 检查这个效果的参数个数够不够
    private bool HasRequiredParameters(CardEffect effect)
    {
        int count = effect.effectValue == null ? 0 : effect.effectValue.Length;
        switch (effect.effectType)
        {
            case EffectType.BuffSelf:
            case EffectType.BuffAlliesAll:
            case EffectType.DealDamageToRandomEnemy:
            case EffectType.HeallRandomAllies:
            case EffectType.BuffEnemy:
            case EffectType.BuffLowHpAlly:
            case EffectType.DropAndDraw:
                return count >= 2;
            case EffectType.SearchMumberCostUp:
            case EffectType.SearchMumberTrigger:
            case EffectType.BackHandAddCost:
            case EffectType.Draw:
            case EffectType.DamageAllEnemy:
            case EffectType.DamageAll:
            case EffectType.AddCost:
            case EffectType.AddCostMax:
            case EffectType.DropEnemyHand:
            case EffectType.DealDamageToEnemy:
            case EffectType.HealAlly:
            case EffectType.HealPlayer:
                return count >= 1;
            default:
                return true;
        }
    }


    #region 效果实现

    #region 无需选择目标的效果

    // 抽牌
    private void Draw(CardController effectCard, CardEffect effect)
    {
        PlayerController player = effectCard.player;
        GM.Ins.BM.DrawCard(player, effect.effectValue[0]);
        FinishStep();
    }

    // 治疗博士
    private void HealPlayer(CardController effectCard, CardEffect effect)
    {
        effectCard.player.Heal(effect.effectValue[0]);
        FinishStep();
    }

    // 给自己加攻防
    private void BuffSelf(CardController effectCard, CardEffect effect)
    {
        if (effectCard == null) return;
        if (effect.effectValue.Length <= 1) return;
        int addAtk = effect.effectValue[0];
        int addHp = effect.effectValue[1];

        BuffCard(effectCard, addAtk, addHp);
        FinishStep();
    }

    // 对敌方全场造成伤害
    private void DamageAllEnemy(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        int damage = effect.effectValue[0];
        PlayerController enemyPlayer = GM.Ins.BM.GetEnemyPlayer(effectCard.player.playerId); // 敌方玩家
        List<CardController> targetCards = new();
        targetCards.AddRange(enemyPlayer.field.cards);
        foreach (var target in targetCards)
        {
            target.TakeDamage(damage);
        }

        FinishStep();
    }

    // 对双方全场造成伤害
    private void DamageAll(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        int damage = effect.effectValue[0];
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
        FinishStep();
    }


    // 增加当前费用
    private void AddCost(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        PlayerController player = effectCard.player;
        player.cost += effect.effectValue[0];
        player.UpdateCostUI();
        FinishStep();
    }

    // 提高费用上限
    private void AddCostMax(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        PlayerController player = effectCard.player;
        if (player.costMax < GameConst.costMax)
        {
            player.costMax += effect.effectValue[0];
            player.UpdateCostUI();
        }
        FinishStep();
    }

    // 给全体友军加攻防
    private void BuffAlliesAll(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 1) return;
        int addAtk = effect.effectValue[0];
        int addHp = effect.effectValue[1];
        PlayerController player = effectCard.player;
        List<CardController> targetCards = new();
        targetCards.AddRange(player.field.cards);
        foreach (var target in targetCards)
        {
            BuffCard(target, addAtk, addHp);
        }

        FinishStep();
    }

    // 随机打若干个敌人
    private void DealDamageToRandomEnemy(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 1) return;
        int num = effect.effectValue[0];
        int damage = effect.effectValue[1];
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

        FinishStep();
    }

    // 随机治疗若干个友军
    private void HeallRandomAllies(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 1) return;
        int num = effect.effectValue[0];
        int heal = effect.effectValue[1];
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

        FinishStep();
    }
    // 让这张卡可以再攻击一次
    private void AttackAgain(CardController effectCard, CardEffect effect)
    {
        effectCard.ableAttack = true;
        FinishStep();
    }

    // 随机弃掉敌方若干张手牌
    private void DropEnemyHand(CardController effectCard, CardEffect effect)
    {
        if (effect.effectValue.Length <= 0) return;
        int num = effect.effectValue[0];
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
        FinishStep();
    }

    #endregion


    // 当前效果处理完，接着跑这张卡的下一个效果
    private void FinishStep()
    {
        _isProcessingEffect = false;
        RunNextEffect(_processingCard);
    }

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
            _castingSpell.cardState = CardState.Graveyard;
            _castingSpell.transform.parent = player.gravePos;
            _castingSpell.transform.DOLocalMove(Vector3.zero, 0.5f);
            _castingSpell.transform.localRotation = Quaternion.identity;
            //_castingSpell.gameObject.SetActive(false);
            _castingSpell = null;
        }

        ResetCamera();
        GM.Ins.BM.NoteAction();
    }

    // 把相机摆回战斗视角
    public void ResetCamera()
    {
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


    // 一次待执行的效果请求
    private struct EffectRequest
    {
        public CardController card; // 触发效果的卡
        public List<CardEffect> effects; // 要按顺序执行的效果
    }
}
