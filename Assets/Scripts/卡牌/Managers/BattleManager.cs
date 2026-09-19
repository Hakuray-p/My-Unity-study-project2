using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;


// 战斗流程管理，负责开局、出牌、攻击、回合切换和快照续战
public class BattleManager : MonoBehaviour
{
    public EffectManager EM;
    public DragManager DragManager;
    public TargetManager TM;

    public CardController cardPrefab;
    public List<PlayerController> players = new();
    private PlayerController curPlayer;
    public PlayerController GetMainPlayer => _mainPlayer;
    [SerializeField]
    private PlayerController _mainPlayer;

    public Transform spellPos;
    public BattleLaunchContext LaunchContext { get; private set; }
    private bool battleResolved;
    private int turn;
    private bool pauseOpen;
    private bool turnChangePending;

    public bool CanSafelyExit => !battleResolved && !turnChangePending &&
        (EM == null || !EM.IsProcessingEffect) && (TM == null || !TM.IsSelecting);

    // 读取战斗上下文并开战
    public void Init()
    {
        LaunchContext = CampaignSession.Instance.CreateBattleContext();
        if (LaunchContext != null) UnityEngine.Random.InitState(LaunchContext.randomSeed);
        if (EM != null) EM.Init();
        StartBattle();
    }

    // Esc 在安全点暂停 / 继续
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (pauseOpen)
        {
            pauseOpen = false;
            Time.timeScale = 1f;
            return;
        }

        if (CanSafelyExit)
        {
            pauseOpen = true;
            Time.timeScale = 0f;
        }
    }

    // 开始战斗
    public void StartBattle()
    {
        if (players.Count == 0) return;
        battleResolved = false;
        if (_mainPlayer == null) _mainPlayer = players.Find(player => player.isMainPlayer);

        if (LaunchContext != null && LaunchContext.snapshot != null)
        {
            ApplySnapshot(LaunchContext.snapshot);
            curPlayer = GetPlayer(LaunchContext.snapshot.activePlayerId) ?? _mainPlayer;
            turn = LaunchContext.snapshot.turn;
            return;
        }

        PlayerController firstPlayer = UnityEngine.Random.Range(0, 2) == 0
            ? _mainPlayer
            : players.Find(player => player != _mainPlayer);

        // 初始化玩家状态
        foreach (var player in players)
        {
            List<int> deck = player.isMainPlayer
                ? CampaignSession.Instance.GetBattleDeck()
                : CampaignCatalog.GetDeck(LaunchContext != null ? LaunchContext.enemyDeckId : player.deckId);
            player.Init(deck);
            // 抽初始手牌
            DrawCard(player, player == firstPlayer ? Mathf.Max(0, GameConst.initalHands - 1) : GameConst.initalHands);
        }

        // 随机决定先手，先手方第一回合仍然正常抽牌
        turn = 1;
        curPlayer = firstPlayer ?? _mainPlayer;
        if (curPlayer != null) curPlayer.TurnStart();
        SaveCurrentSnapshot();
    }

    // 把当前战况打包成快照
    public BattleSnapshot ExportSnapshot()
    {
        var snapshot = new BattleSnapshot
        {
            matchId = LaunchContext != null ? LaunchContext.matchId : CampaignSession.Instance.State.pendingMatchId,
            turn = turn,
            randomStateJson = JsonUtility.ToJson(UnityEngine.Random.state),
            activePlayerId = curPlayer != null ? curPlayer.playerId : (_mainPlayer != null ? _mainPlayer.playerId : 0)
        };

        foreach (PlayerController player in players)
        {
            var playerSnapshot = new BattlePlayerSnapshot
            {
                playerId = player.playerId,
                health = player.playerHealth,
                healthMax = player.playerHealthMax,
                cost = player.cost,
                costMax = player.costMax,
                isInTurn = player.isInTurn,
                fatigueLevel = player.fatigueLevel
            };

            AddCardSnapshots(playerSnapshot, player.deckCards);
            AddCardSnapshots(playerSnapshot, player.hands != null ? player.hands.handCards : null);
            AddCardSnapshots(playerSnapshot, player.field != null ? player.field.cards : null);
            AddCardSnapshots(playerSnapshot, player.graveCards);
            snapshot.players.Add(playerSnapshot);
        }

        return snapshot;
    }

    // 把一批卡牌追加进玩家快照
    private void AddCardSnapshots(BattlePlayerSnapshot playerSnapshot, IEnumerable<CardController> cards)
    {
        if (cards == null) return;
        foreach (CardController card in cards)
        {
            if (card == null || card.cardData == null) continue;
            playerSnapshot.cards.Add(new BattleCardSnapshot
            {
                cardId = card.cardData.index,
                state = card.cardState,
                attack = card.mumberAtk,
                health = card.mumberHp,
                healthMax = card.mumberHpMax,
                ableAttack = card.ableAttack,
                silenced = card.isSlience
            });
        }
    }

    // 用快照还原战况
    public void ApplySnapshot(BattleSnapshot snapshot)
    {
        if (snapshot == null) return;
        turn = snapshot.turn;
        if (!string.IsNullOrEmpty(snapshot.randomStateJson))
        {
            try { UnityEngine.Random.state = JsonUtility.FromJson<Random.State>(snapshot.randomStateJson); }
            catch (System.Exception exception) { Debug.LogWarning($"Battle random state could not be restored: {exception.Message}"); }
        }
        foreach (PlayerController player in players)
        {
            player.Init(null, false);
            BattlePlayerSnapshot playerSnapshot = snapshot.players.Find(item => item.playerId == player.playerId);
            if (playerSnapshot == null) continue;

            player.playerHealthMax = playerSnapshot.healthMax;
            player.playerHealth = playerSnapshot.health;
            player.cost = playerSnapshot.cost;
            player.costMax = playerSnapshot.costMax;
            player.isInTurn = playerSnapshot.isInTurn;
            player.fatigueLevel = playerSnapshot.fatigueLevel;
            player.UpdateHealthUI();
            player.UpdateCostUI();

            if (playerSnapshot.cards == null) continue;
            foreach (BattleCardSnapshot cardSnapshot in playerSnapshot.cards)
            {
                CardController card = player.CreateRuntimeCard(cardSnapshot.cardId, cardSnapshot.state);
                if (card == null) continue;
                card.mumberAtk = cardSnapshot.attack;
                card.mumberHp = cardSnapshot.health;
                card.mumberHpMax = cardSnapshot.healthMax;
                card.ableAttack = cardSnapshot.ableAttack;
                card.isSlience = cardSnapshot.silenced;
                if (card.cardDisplay != null)
                {
                    card.cardDisplay.ShowBack(cardSnapshot.state == CardState.Hand && !player.isMainPlayer);
                    card.cardDisplay.UpdateDisplay();
                }
            }
        }
    }

    // 把当前战况写进战役存档
    private void SaveCurrentSnapshot()
    {
        if (battleResolved || LaunchContext == null) return;
        CampaignSession.Instance.SaveBattleSnapshot(ExportSnapshot());
    }

    // 判断能不能召唤：场上没满且费用够
    public bool CheckSummonCondition(CardController card)
    {
        if (card == null || card.player == null || card.player.field == null || card.cardData == null) return false;
        // 检查场上位置数量
        if (card.player.field.IsFull) return false;
        // 检查费用满足
        if (card.player.cost < card.cardData.cost) return false;

        return true;
    }

    // 出牌：扣费、上场并触发登场效果
    public void SummonCard(CardController card)
    {
        if (card == null || card.player == null || card.player.hands == null || card.player.field == null) return;
        PlayerController player = card.player;
        Debug.Log($"{player.playerId}召唤{card.cardData.name}");
        player.hands.RemoveCard(card);
        player.field.AddCard(card);
        player.cost -= card.cardData.cost;
        player.UpdateCostUI();
        card.cardState = CardState.Field;
        if (card.cardDisplay != null) card.cardDisplay.ShowBack(false);
        card.ableAttack = card.cardData.passiveType == PassiveType.Rush;// 具有冲锋则可以攻击
        card.ableCast = true;// 本回合可以发动主动效果
        // 触发登场时点
        if (EM != null) EM.TriggerCardEffect(TriggerType.Enter, card);
        if (GM.Ins != null && GM.Ins.AM != null && card.cardData.enterAudio == null)
        {
            GM.Ins.AM.PlayAudio(AudioType.Summon);
        }
        else if (GM.Ins != null && GM.Ins.AM != null)
        {
            GM.Ins.AM.PlayAudio(card.cardData.enterAudio);
        }
        SaveCurrentSnapshot();
    }

    // 判断这张牌能不能被攻击，对方有守护时只能打守护
    public bool IsAttackableTarget(CardController target)
    {
        if (target == null || target.player == null || target.player.field == null || target.cardData == null) return false;
        if (target.cardState != CardState.Field) return false;
        // 攻击目标判定逻辑
        // 如果目标有守护则可以攻击
        if (target.cardData.passiveType == PassiveType.Guard &&
            !target.isSlience) return true;
        if (target.player.field.cards == null) return true;
        foreach (var enemyCard in target.player.field.cards)
        {
            if (enemyCard != null && enemyCard.cardData != null && enemyCard.cardData.passiveType == PassiveType.Guard &&
               !enemyCard.isSlience)
            {
                return false; // 如果有其他守护则不能攻击
            }
        }
        return true;
    }

    // 判断能不能直接打玩家，对方有守护就不行
    public bool IsAttackablePlayer(PlayerController player)
    {
        if (player == null || player.field == null) return false;
        // 攻击玩家判定逻辑
        // 判定对方场上是否存在守护
        if (player.field.cards == null) return true;
        foreach (var enemyCard in player.field.cards)
        {
            if (enemyCard != null && enemyCard.cardData != null && enemyCard.cardData.passiveType == PassiveType.Guard &&
               !enemyCard.isSlience)
            {
                return false; // 如果有其他守护则不能攻击
            }
        }
        return true;
    }

    // 卡牌互相攻击，结算伤害和击杀时点
    public void AttackCard(CardController attacker, CardController target)
    {
        if (attacker == null || target == null || attacker.cardData == null || target.cardData == null) return;
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(attacker.cardData.attackAudio);
        attacker.ableAttack = false;
        attacker.transform.DOMove(target.transform.position, 0.4f)
            .SetLoops(2, LoopType.Yoyo).SetEase(Ease.InExpo)
            .OnComplete(() =>
            {
                int atkValue = attacker.mumberAtk;
                int backAtkValue = target.mumberAtk;
                if (attacker.cardData.passiveType == PassiveType.Swingle && target.player != null && target.player.field != null)// 处理横扫效果
                {
                    List<CardController> neighborCards = target.player.field.GetNeighborCards(target);
                    foreach (var card in neighborCards)
                    {
                        card.TakeDamage(atkValue);
                    }
                }
                target.TakeDamage(atkValue);
                attacker.TakeDamage(backAtkValue);


                if (target.cardState == CardState.Graveyard)
                {
                    if (attacker.cardState == CardState.Field)
                    {
                        // 触发攻击者取胜时点
                        EM.TriggerCardEffect(TriggerType.BeatDown, attacker);
                    }
                }
                else if (target.cardState == CardState.Field)
                {
                    // 触发受击者受伤时点
                    if (attacker.cardState == CardState.Graveyard)
                    {
                        // 触发受击者取胜时点
                        EM.TriggerCardEffect(TriggerType.BeatDown, target);
                    }
                }
                SaveCurrentSnapshot();
            });
    }

    // 干员直接攻击玩家
    public void AttackPlayer(CardController attacker, PlayerController player)
    {
        if (attacker == null || player == null || attacker.cardData == null) return;
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(attacker.cardData.attackAudio);
        Debug.Log($"{attacker.cardData.name}直接攻击玩家{player.playerId}");
        attacker.ableAttack = false;
        attacker.transform.DOMove(player.iconPos.position, 0.4f)
            .SetLoops(2, LoopType.Yoyo).SetEase(Ease.InExpo)
            .OnComplete(() =>
            {
                player.TakeDamage(attacker.mumberAtk);
                if (player.playerHealth <= 0)
                {
                    // 触发游戏结束，显示胜负
                    CheckWin();
                }
                else
                {
                    SaveCurrentSnapshot();
                }
            });
    }

    // 干员阵亡，送进墓地并触发退场效果
    public void MumberDied(CardController mumber)
    {
        // 送入墓地，并触发退场效果
        EM.TriggerCardEffect(TriggerType.Died, mumber);
        PlayerController player = mumber.player;
        mumber.cardState = CardState.Graveyard;
        mumber.Init(mumber.cardData, mumber.player);
        player.field.RemoveCard(mumber);
        player.graveCards.Add(mumber);
        mumber.transform.parent = player.gravePos;
        mumber.transform.DOLocalMove(Vector3.zero, 0.5f);
        mumber.transform.localRotation = Quaternion.identity;
        mumber.cardDisplay.ShowBack(false);
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Destroy);
        SaveCurrentSnapshot();
    }

    /// <summary>
    /// 检查法术效果的发动条件
    /// </summary>
    /// <param name="spell"></param>
    /// <returns></returns>
    // 判断法术能不能发动：费用够且满足发动条件
    public bool CheckSpellCastCondition(CardController spell)
    {
        if (spell == null || spell.player == null || spell.cardData == null || EM == null) return false;
        // 检查费用满足
        if (spell.player.cost < spell.cardData.cost) return false;
        if (EM.CheckCastCondition(spell) == false) return false;
        return true;
    }

    // 打出法术：扣费、飞到法术位并交给效果管理器
    public void CastSpell(CardController spell)
    {
        if (spell == null || spell.player == null || spell.player.hands == null || spell.cardData == null || spellPos == null) return;
        PlayerController player = spell.player;
        Debug.Log($"{player.playerId}发动{spell.cardData.name}");
        player.cost -= spell.cardData.cost;
        player.UpdateCostUI();
        player.hands.RemoveCard(spell);
        spell.transform.parent = spellPos;
        if (spell.cardDisplay != null) spell.cardDisplay.ShowBack(false);
        SortingGroup sortingGroup = spell.GetComponent<SortingGroup>();
        if (sortingGroup != null) sortingGroup.sortingOrder = 100;
        spell.transform.DOLocalMove(Vector3.zero, 0.5f);
        spell.transform.DOLocalRotate(Vector3.zero, 0.5f);
        EM.CastSpell(spell);
        SaveCurrentSnapshot();
    }

    // 抽牌，牌堆空了就吃疲劳伤害
    public void DrawCard(PlayerController player, int num)
    {
        if (player == null || player.hands == null || player.deckCards == null) return;
        for (int i = 0; i < num; i++)
        {
            if (player.deckCards.Count == 0)
            {
                player.fatigueLevel++;
                player.TakeDamage(player.fatigueLevel);
                if (player.playerHealth <= 0) CheckWin();
                continue;
            }
            CardController card = player.deckCards[0];
            player.hands.AddCard(card);
            card.cardState = CardState.Hand;
            if (card.cardDisplay != null) card.cardDisplay.UpdateDisplay();
            player.deckCards.RemoveAt(0);
            if (card.cardDisplay != null) card.cardDisplay.ShowBack(!player.isMainPlayer);
            if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.DrawCard);
        }
    }

    // 结算胜负并显示结果
    private void CheckWin()
    {
        if (battleResolved) return;
        battleResolved = true;
        bool won = GetMainPlayer != null && GetMainPlayer.playerHealth > 0;
        BattleResult result = CampaignSession.Instance.ResolveMatch(won);
        if (GM.Ins != null && GM.Ins.UM != null)
        {
            if (result != null) GM.Ins.UM.ShowBattleResult(result);
            else GM.Ins.UM.ShowWin(won ? "我方获胜" : "对方获胜");
        }
    }

    // 按玩家编号取玩家
    public PlayerController GetPlayer(int playerId)
    {
        foreach (var player in players)
        {
            if (player.playerId == playerId)
            {
                return player;
            }
        }

        return null;
    }



    // 取对手玩家
    public PlayerController GetEnemyPlayer(int playerId)
    {
        foreach (var player in players)
        {
            if (player.playerId != playerId)
            {
                return player;
            }
        }
        return players != null && players.Count > 1 ? players[1] : null;
    }

    /// <summary>
    /// 回合切换
    /// </summary>
    private void TurnChange()
    {
        if (curPlayer == null || players.Count < 2) return;
        if (GM.Ins == null || GM.Ins.BM == null) return;
        turnChangePending = true;
        GM.Ins.BM.EM.TriggerStartEnd(TriggerType.End, curPlayer.playerId);
        curPlayer.TurnEnd();
        string playrStr = curPlayer.isMainPlayer ? "我方" : "敌方";
        GM.Ins.UM.turnPanel.ShowTurnChange($"{playrStr}回合结束");
        GM.Ins.AM.PlayAudio(AudioType.NextTurn);

        DOVirtual.DelayedCall(2f, () => DiscardExcessHand(curPlayer, SwitchTurn));
    }

    // 结束回合的一方把超出上限的手牌弃进墓地，弃完再切回合
    private void DiscardExcessHand(PlayerController player, UnityAction onFinish)
    {
        if (EM.IsProcessingEffect)
        {
            DOVirtual.DelayedCall(0.5f, () => DiscardExcessHand(player, onFinish));
            return;
        }

        int excess = player.hands.handCards.Count - GameConst.handMax;
        if (excess <= 0)
        {
            onFinish();
            return;
        }

        var handCards = new List<CardController>(player.hands.handCards);
        TM.SelectFormList(player, handCards, excess, targetPack =>
        {
            foreach (var card in targetPack.cards) player.DiscardHandCard(card);
            player.hands.RefreshCards();
            EM.ResetCamera();
            onFinish();
        }, false, $"手牌超过 {GameConst.handMax} 张，请弃掉 {excess} 张");
    }

    // 把回合交给另一方
    private void SwitchTurn()
    {
        if (curPlayer == players[0])
        {
            curPlayer = players[1];
        }
        else
        {
            curPlayer = players[0];
        }

        curPlayer.TurnStart();
        turn++;
        string playrStr = curPlayer.isMainPlayer ? "我方" : "敌方";
        GM.Ins.UM.turnPanel.ShowTurnChange($"{playrStr}回合开始");
        GM.Ins.BM.EM.TriggerStartEnd(TriggerType.Start, curPlayer.playerId);
        SaveCurrentSnapshot();
        turnChangePending = false;
    }

    // 点结束回合按钮时切回合
    public void OnClickTurnEnd(int playerId)
    {
        PlayerController player = GetPlayer(playerId);
        if (player != null && player.isInTurn)
        {
            TurnChange();
            SaveCurrentSnapshot();
        }
    }

    // 切到后台时存一次快照
    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveCurrentSnapshot();
    }

    // 退出游戏时存一次快照
    private void OnApplicationQuit()
    {
        SaveCurrentSnapshot();
    }

    // 暂停时画一个简易暂停框
    private void OnGUI()
    {
        if (!pauseOpen) return;
        GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 120f, 360f, 240f), "暂停");
        GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f - 78f, 300f, 42f), "当前处于安全点，可以保存战斗快照。");
        if (GUI.Button(new Rect(Screen.width * 0.5f - 140f, Screen.height * 0.5f - 25f, 280f, 42f), "保存并返回主菜单"))
        {
            SaveCurrentSnapshot();
            Time.timeScale = 1f;
            pauseOpen = false;
            SceneFlowService.ReturnToMenu();
        }
        if (GUI.Button(new Rect(Screen.width * 0.5f - 140f, Screen.height * 0.5f + 30f, 280f, 42f), "继续战斗"))
        {
            pauseOpen = false;
            Time.timeScale = 1f;
        }
    }
}
