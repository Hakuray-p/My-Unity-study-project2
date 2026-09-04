using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;


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
    
    public void Init()
    {
        LaunchContext = CampaignSession.Instance.CreateBattleContext();
        if (LaunchContext != null) UnityEngine.Random.InitState(LaunchContext.randomSeed);
        if (EM != null) EM.Init();
        StartBattle();
    }

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
        if (players == null || players.Count == 0) return;
        battleResolved = false;
        if (_mainPlayer == null) _mainPlayer = players.Find(player => player != null && player.isMainPlayer);

        if (LaunchContext != null && LaunchContext.snapshot != null)
        {
            ApplySnapshot(LaunchContext.snapshot);
            curPlayer = GetPlayer(LaunchContext.snapshot.activePlayerId) ?? _mainPlayer;
            turn = LaunchContext.snapshot.turn;
            return;
        }

        PlayerController firstPlayer = UnityEngine.Random.Range(0, 2) == 0
            ? _mainPlayer
            : players.Find(player => player != null && player != _mainPlayer);

        // 初始化玩家状态
        foreach (var player in players)
        {
            if (player == null) continue;
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

    public BattleSnapshot ExportSnapshot()
    {
        BattleSnapshot snapshot = new BattleSnapshot
        {
            matchId = LaunchContext != null ? LaunchContext.matchId : CampaignSession.Instance.State.pendingMatchId,
            randomSeed = LaunchContext != null ? LaunchContext.randomSeed : 0,
            turn = turn,
            randomStateJson = JsonUtility.ToJson(UnityEngine.Random.state),
            activePlayerId = curPlayer != null ? curPlayer.playerId : (_mainPlayer != null ? _mainPlayer.playerId : 0)
        };

        foreach (PlayerController player in players)
        {
            if (player == null) continue;
            BattlePlayerSnapshot playerSnapshot = new BattlePlayerSnapshot
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
            if (player == null) continue;
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

    private void SaveCurrentSnapshot()
    {
        if (battleResolved || LaunchContext == null) return;
        CampaignSession.Instance.SaveBattleSnapshot(ExportSnapshot());
    }

    public bool CheckSummonCondition(CardController card)
    {
        if (card == null || card.player == null || card.player.field == null || card.cardData == null) return false;
        // 检查场上位置数量
        if (card.player.field.cards.Count >= 7) return false;
        // 检查费用满足
        if (card.player.cost < card.cardData.cost) return false;

        return true;
    }

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
            if(enemyCard != null && enemyCard.cardData != null && enemyCard.cardData.passiveType == PassiveType.Guard &&
               !enemyCard.isSlience)
            {
                return false; // 如果有其他守护则不能攻击
            }
        }
        return true;
    }

    public bool IsAttackablePlayer(PlayerController player)
    {
        if (player == null || player.field == null) return false;
        // 攻击玩家判定逻辑
        // 判定对方场上是否存在守护
        if (player.field.cards == null) return true;
        foreach (var enemyCard in player.field.cards)
        {
            if(enemyCard != null && enemyCard.cardData != null && enemyCard.cardData.passiveType == PassiveType.Guard &&
               !enemyCard.isSlience)
            {
                return false; // 如果有其他守护则不能攻击
            }
        }
        return true;
    }

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
                else if(target.cardState == CardState.Field)
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
    public bool CheckSpellCastCondition(CardController spell)
    {
        if (spell == null || spell.player == null || spell.cardData == null || EM == null) return false;
        // 检查费用满足
        if (spell.player.cost < spell.cardData.cost) return false;
        if (EM.CheckCastCondition(spell) == false) return false;
        return true;
    }

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
        if (curPlayer == null || players == null || players.Count < 2) return;
        if (GM.Ins == null || GM.Ins.BM == null) return;
        turnChangePending = true;
        GM.Ins.BM.EM.TriggerStartEnd(TriggerType.End, curPlayer.playerId);
        curPlayer.TurnEnd();
        string playrStr = curPlayer.isMainPlayer ? "我方" : "敌方";
        GM.Ins.UM.turnPanel.ShowTurnChange($"{playrStr}回合结束");
        GM.Ins.AM.PlayAudio(AudioType.NextTurn);

        DOVirtual.DelayedCall(2f, () =>
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
        });
    }

    public void OnClickTurnEnd(int playerId)
    {
        PlayerController player = GetPlayer(playerId);
        if (player != null && player.isInTurn)
        {
            TurnChange();
            SaveCurrentSnapshot();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveCurrentSnapshot();
    }

    private void OnApplicationQuit()
    {
        SaveCurrentSnapshot();
    }

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
