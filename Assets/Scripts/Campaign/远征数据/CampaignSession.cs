using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 远征模式的存档与进度中心，跨场景常驻，其他脚本通过 CampaignSession.Instance 读写。
[DefaultExecutionOrder(-10000)]
public class CampaignSession : MonoBehaviour
{
    private const string SaveFileName = "campaign_save.json"; // 存档文件名，放在 persistentDataPath 下
    private const int CurrentSaveVersion = 9; // 当前存档版本号，读档时用它判断要不要升级
    private bool battleReactionPending; // 本次战后回应是否尚未播放
    private static CampaignSession instance; // 全局单例

    public static CampaignSession Instance => EnsureInstance(); // 单例入口，场景里没有就自动建
    public CampaignSaveData State { get; private set; } // 当前存档数据
    public BattleOutcome LastBattleOutcome { get; private set; } // 上一场战斗的结果
    public string LastResolvedMatchId { get; private set; } // 上一场结算过的比赛 id
    public bool LastBattleResolved { get; private set; } // 上一场战斗是否已经结算
    public bool HasPendingBattleReaction => battleReactionPending; // 本次战后回应是否尚未播放
    public event Action ProgressChanged; // 进度变化时广播，界面靠它刷新

    public bool HasLegalDeck => State != null && IsLegalDeck(State.lastValidDeckCardIds); // 手上有没有一套合法卡组
    public bool HasPendingBattle => State != null && State.pendingBattle != null &&
                                    !string.IsNullOrEmpty(State.pendingBattle.matchId); // 有没有没打完的比赛

    // 检查卡组是否合法，返回错误说明
    public string GetDeckValidationError(IList<int> deck)
    {
        if (deck == null || deck.Count != 30) return "卡组必须正好包含 30 张牌";
        var counts = new Dictionary<int, int>();
        foreach (int cardId in deck)
        {
            if (CampaignCatalog.HasCardDatabase && CampaignCatalog.GetCardData(cardId) == null)
                return $"卡牌 {cardId} 已经不在卡牌数据库里";
            if (!counts.ContainsKey(cardId)) counts[cardId] = 0;
            counts[cardId]++;
            if (counts[cardId] > 3) return $"卡牌 {cardId} 不能超过 3 张";
        }
        return string.Empty;
    }

    public bool IsLegalDeck(IList<int> deck) => string.IsNullOrEmpty(GetDeckValidationError(deck));

    // 存一份卡组草稿，草稿合法时同时更新正式卡组
    public bool SaveDeckDraft(IList<int> deck)
    {
        if (State == null) return false;
        State.deckDraftCardIds = deck == null ? new List<int>() : new List<int>(deck);
        if (IsLegalDeck(deck)) State.lastValidDeckCardIds = new List<int>(deck);
        State.sharedDeckCardIds = IsLegalDeck(State.lastValidDeckCardIds)
            ? new List<int>(State.lastValidDeckCardIds)
            : CampaignCatalog.GetDeck(0);
        Save();
        ProgressChanged?.Invoke();
        return IsLegalDeck(deck);
    }

    // 取进场战斗要用的卡组，没有合法卡组时退回默认卡组
    public List<int> GetBattleDeck()
    {
        if (State == null) return CampaignCatalog.GetDeck(0);
        return IsLegalDeck(State.lastValidDeckCardIds)
            ? new List<int>(State.lastValidDeckCardIds)
            : new List<int>(CampaignCatalog.GetDeck(0));
    }

    // 清掉数据库里已经不存在的卡，卡组因此失效时换回默认卡组
    public void PurgeMissingCards()
    {
        if (State == null || !CampaignCatalog.HasCardDatabase) return;

        State.collectedCardIds.RemoveAll(cardId => CampaignCatalog.GetCardData(cardId) == null);
        State.deckDraftCardIds.RemoveAll(cardId => CampaignCatalog.GetCardData(cardId) == null);
        State.lastValidDeckCardIds.RemoveAll(cardId => CampaignCatalog.GetCardData(cardId) == null);

        if (!IsLegalDeck(State.lastValidDeckCardIds))
            State.lastValidDeckCardIds = new List<int>(CampaignCatalog.GetDeck(0));
        if (!IsLegalDeck(State.deckDraftCardIds))
            State.deckDraftCardIds = new List<int>(State.lastValidDeckCardIds);

        foreach (int cardId in State.lastValidDeckCardIds)
        {
            if (!State.collectedCardIds.Contains(cardId)) State.collectedCardIds.Add(cardId);
        }

        State.sharedDeckCardIds = new List<int>(State.lastValidDeckCardIds);
        Save();
        ProgressChanged?.Invoke();
    }

    // 数一下某张卡已经拥有几张
    private int CountOwnedCard(int cardId)
    {
        int count = 0;
        foreach (int id in State.collectedCardIds) if (id == cardId) count++;
        return count;
    }

    // 判断这张卡能不能买，不能买时用 reason 说明原因
    public bool CanBuyCard(CardShopEntry entry, out string reason)
    {
        reason = string.Empty;
        if (entry == null) { reason = "商品不存在"; return false; }
        if (CountOwnedCard(entry.cardId) >= 3) { reason = "同名卡最多 3 张"; return false; }
        if (State.currency < entry.price) { reason = "金币不足"; return false; }
        return true;
    }

    // 买下一张卡：扣金币、记进已拥有，并给 UR 卡分配光影变体种子
    public bool BuyCard(CardShopEntry entry, out string reason)
    {
        if (!CanBuyCard(entry, out reason)) return false;
        State.currency -= entry.price;
        State.collectedCardIds.Add(entry.cardId);
        SaveCardHoloVariant(entry);
        Save();
        ProgressChanged?.Invoke();
        return true;
    }

    // 取某张 UR 卡的光影变体种子，没记录过就返回 false
    public bool TryGetCardHoloVariantSeed(int cardId, out int colorSeed)
    {
        colorSeed = 0;
        if (State == null || State.cardHoloVariants == null) return false;
        CardHoloVariantSaveData variant = State.cardHoloVariants.Find(item => item != null && item.cardId == cardId);
        if (variant == null) return false;
        colorSeed = variant.colorSeed;
        return true;
    }

    // 某个事件在这座城里是不是已经触发、还没结算
    public bool IsEventActive(string cityId, string eventId)
    {
        return GetCityState(cityId).activeEventIds.Contains(eventId);
    }

    // 事件是不是已经完成
    public bool IsEventResolved(string cityId, string eventId)
    {
        return GetCityState(cityId).resolvedEventIds.Contains(eventId);
    }

    // 触发一个事件，记进这座城市的进行中事件
    public void StartEvent(CampaignEventData eventData)
    {
        CitySaveData cityState = GetCityState(eventData.cityId);
        if (cityState.resolvedEventIds.Contains(eventData.eventId)) return;
        if (cityState.activeEventIds.Contains(eventData.eventId)) return;
        cityState.activeEventIds.Add(eventData.eventId);
        Save();
        ProgressChanged?.Invoke();
    }

    // 结算一个事件，发金币和卡牌奖励
    public bool ResolveEvent(CampaignEventData eventData, out bool cardAdded)
    {
        cardAdded = false;
        CitySaveData cityState = GetCityState(eventData.cityId);
        if (cityState.resolvedEventIds.Contains(eventData.eventId) ||
            !cityState.activeEventIds.Contains(eventData.eventId)) return false;

        cityState.activeEventIds.Remove(eventData.eventId);
        cityState.resolvedEventIds.Add(eventData.eventId);
        State.currency += eventData.goldReward;
        if (eventData.cardRewardId > 0 && !State.collectedCardIds.Contains(eventData.cardRewardId))
        {
            State.collectedCardIds.Add(eventData.cardRewardId);
            cardAdded = true;
        }
        Save();
        ProgressChanged?.Invoke();
        return true;
    }

    // 取单例，场景里没有就新建一个常驻对象
    private static CampaignSession EnsureInstance()
    {
        if (instance != null) return instance;
        instance = FindObjectOfType<CampaignSession>();
        if (instance == null)
        {
            var root = new GameObject("CampaignSession");
            instance = root.AddComponent<CampaignSession>();
        }
        return instance;
    }

    // 单例初始化：重复的实例销毁掉，自己常驻，然后读档
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadOrCreate();
    }

    // 读存档，文件不存在或读坏时按新档生成
    public void LoadOrCreate()
    {
        State = null;
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (File.Exists(path))
        {
            try
            {
                State = JsonUtility.FromJson<CampaignSaveData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Campaign save could not be read: {exception.Message}");
            }
        }

        if (State == null)
        {
            State = CreateDefaultState();
            Save();
            return;
        }

        int loadedVersion = State.version;
        EnsureStateShape();
        if (loadedVersion < 8)
        {
            State.practiceTutorialHandled = IsMatchComplete("first_light_practice");
            State.pendingPracticeTutorial = false;
            if (State.pendingBattle != null)
                foreach (BattlePlayerSnapshot player in State.pendingBattle.players)
                    foreach (BattleCardSnapshot card in player.cards) card.ableCast = true;
        }
        if (loadedVersion != CurrentSaveVersion)
        {
            State.version = CurrentSaveVersion;
            Save();
        }
    }

    // 重置存档，开一局新游戏
    public void BeginNewGame()
    {
        State = CreateDefaultState();
        LastBattleOutcome = BattleOutcome.None;
        LastResolvedMatchId = null;
        LastBattleResolved = false;
        battleReactionPending = false;
        Save();
        ProgressChanged?.Invoke();
    }

    // 把存档写到磁盘
    public void Save()
    {
        if (State == null) return;
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonUtility.ToJson(State, true));
    }

    public bool HasSave() => File.Exists(Path.Combine(Application.persistentDataPath, SaveFileName));

    public CityData GetCurrentCity() => State == null ? null : CampaignCatalog.GetCity(State.currentCityId);

    // 取某座城市的存档状态，没有就补一条
    public CitySaveData GetCityState(string cityId)
    {
        if (State == null) State = CreateDefaultState();
        if (State.cityStates == null) State.cityStates = new List<CitySaveData>();

        CitySaveData cityState = State.cityStates.Find(city => city != null && city.cityId == cityId);
        if (cityState == null)
        {
            cityState = new CitySaveData { cityId = cityId };
            State.cityStates.Add(cityState);
        }

        EnsureCityStateShape(cityState);
        return cityState;
    }

    // 城市是不是已经解锁
    public bool IsCityUnlocked(string cityId)
    {
        return State != null && State.unlockedCityIds != null && State.unlockedCityIds.Contains(cityId);
    }

    // 这场比赛是不是已经通关
    public bool IsMatchComplete(string matchId)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (match == null) return false;
        CitySaveData cityState = GetCityState(match.cityId);
        return cityState.completedMatchIds != null && cityState.completedMatchIds.Contains(matchId);
    }

    // 判断这场比赛现在能不能打。
    public bool CanStartMatch(MatchData match)
    {
        return GetMatchLockReason(match) == "";
    }

    // 给对话和场景入口提供一致的赛事解锁说明。
    public string GetMatchLockReason(MatchData match)
    {
        if (match == null) return "赛事不存在";
        if (HasPendingBattle) return "当前已有一场未结束的战斗";
        if (!IsCityUnlocked(match.cityId)) return "当前城市尚未解锁";
        if (match.matchType == MatchType.Champion && GetLeaguePoints(match.cityId) < CampaignCatalog.GetCity(match.cityId).requiredPoints)
            return $"城市冠军需要 {CampaignCatalog.GetCity(match.cityId).requiredPoints} 积分";
        if (!string.IsNullOrEmpty(match.prerequisiteMatchId) && !IsMatchComplete(match.prerequisiteMatchId))
            return $"需要先完成：{CampaignCatalog.GetMatch(match.prerequisiteMatchId).displayName}";
        if (!HasLegalDeck) return "当前卡组不合法，请按 B 编辑卡组";
        return "";
    }

    // 在对应 NPC 第一次回应本场胜负时消耗临时标记，不改变存档结构。
    public bool ConsumeBattleReaction(string matchId)
    {
        if (!battleReactionPending || LastResolvedMatchId != matchId) return false;
        battleReactionPending = false;
        return true;
    }

    // 记下要打的比赛和回城位置，切去战斗场景前调用
    public void BeginMatch(MatchData match, Vector3 returnPosition, bool replayTutorial = false)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        State.currentCityId = match.cityId;
        State.playerPosition = returnPosition;
        State.pendingMatchId = match.matchId;
        State.pendingBattle = null;
        State.pendingPracticeTutorial = match.matchId == "first_light_practice" &&
            (replayTutorial || !State.practiceTutorialHandled && !IsMatchComplete(match.matchId));
        LastBattleResolved = false;
        battleReactionPending = false;
        Save();
    }

    // 存一份战斗快照，中途退出也还能接着打
    public void SaveBattleSnapshot(BattleSnapshot snapshot)
    {
        if (State == null || snapshot == null) return;
        State.pendingBattle = snapshot;
        State.pendingMatchId = snapshot.matchId;
        Save();
    }

    // 按待打赛事组装战斗上下文
    public BattleLaunchContext CreateBattleContext()
    {
        if (State == null || string.IsNullOrEmpty(State.pendingMatchId)) return null;
        MatchData match = CampaignCatalog.GetMatch(State.pendingMatchId);
        if (match == null) return null;

        return new BattleLaunchContext
        {
            matchId = match.matchId,
            cityId = match.cityId,
            returnScene = "One_City_DAY",
            randomSeed = StableSeed(Guid.NewGuid().ToString("N")),
            enemyDeckId = match.enemyDeckId,
            snapshot = State.pendingBattle,
            isTutorial = State.pendingPracticeTutorial
        };
    }

    // 结算这场比赛的胜负，发奖励并清掉待打状态
    public BattleResult ResolveMatch(bool won)
    {
        MatchData match = State == null ? null : CampaignCatalog.GetMatch(State.pendingMatchId);
        if (match == null) return null;

        var result = new BattleResult
        {
            matchId = match.matchId,
            cityId = match.cityId,
            outcome = won ? BattleOutcome.PlayerWin : BattleOutcome.PlayerLoss
        };
        LastBattleOutcome = result.outcome;
        LastResolvedMatchId = match.matchId;
        LastBattleResolved = true;
        battleReactionPending = true;

        if (won)
        {
            CitySaveData city = GetCityState(match.cityId);
            bool firstWin = !city.completedMatchIds.Contains(match.matchId);
            result.firstWin = firstWin;
            if (match.matchType != MatchType.Champion || firstWin)
            {
                State.currency += match.goldReward;
                result.gold = match.goldReward;
            }
            if (firstWin)
            {
                city.completedMatchIds.Add(match.matchId);
                if (match.matchType != MatchType.Practice)
                {
                    city.leaguePoints += match.pointReward;
                    result.leaguePoints = match.pointReward;
                }
                foreach (int cardId in match.rewardCardIds)
                {
                    if (!State.collectedCardIds.Contains(cardId)) State.collectedCardIds.Add(cardId);
                    result.rewardCardIds.Add(cardId);
                }
            }

            if (match.matchType == MatchType.Champion && firstWin)
            {
                city.championDefeated = true;
            }
        }
        else
        {
            State.playerPosition = Vector3.zero;
        }

        State.pendingBattle = null;
        State.pendingMatchId = null;
        State.pendingPracticeTutorial = false;
        Save();
        ProgressChanged?.Invoke();
        return result;
    }

    public int GetLeaguePoints(string cityId) => GetCityState(cityId).leaguePoints;

    // 重开上一场失败的比赛
    public bool TryRetryLastMatch(Vector3 returnPosition)
    {
        if (string.IsNullOrEmpty(LastResolvedMatchId)) return false;
        MatchData match = CampaignCatalog.GetMatch(LastResolvedMatchId);
        if (match == null || !CanStartMatch(match)) return false;
        BeginMatch(match, returnPosition);
        return true;
    }

    // 保留待打状态，只做一次存盘
    public void KeepPendingBattle() => Save();

    // 记下玩家当前所在的位置
    public void SetPlayerPosition(Vector3 position)
    {
        if (State != null) State.playerPosition = position;
    }

    // 造一份初始存档
    private CampaignSaveData CreateDefaultState()
    {
        var state = new CampaignSaveData
        {
            version = CurrentSaveVersion,
            currentCityId = CampaignCatalog.Cities.Count > 0 ? CampaignCatalog.Cities[0].cityId : string.Empty,
            playerPosition = Vector3.zero,
            sharedDeckCardIds = CampaignCatalog.GetDeck(0),
            currency = 0
        };
        state.lastValidDeckCardIds = new List<int>(state.sharedDeckCardIds);
        state.deckDraftCardIds = new List<int>(state.sharedDeckCardIds);

        foreach (CityData city in CampaignCatalog.Cities)
            state.cityStates.Add(new CitySaveData { cityId = city.cityId });

        if (state.cityStates.Count > 0) state.unlockedCityIds.Add(state.cityStates[0].cityId);
        state.collectedCardIds.AddRange(state.sharedDeckCardIds);
        return state;
    }

    // 补齐旧存档缺的字段，清掉失效引用
    private void EnsureStateShape()
    {
        if (State.unlockedCityIds == null) State.unlockedCityIds = new List<string>();
        if (State.cityStates == null) State.cityStates = new List<CitySaveData>();
        if (State.collectedCardIds == null) State.collectedCardIds = new List<int>();
        if (State.sharedDeckCardIds == null) State.sharedDeckCardIds = new List<int>();
        if (State.deckDraftCardIds == null) State.deckDraftCardIds = new List<int>();
        if (State.lastValidDeckCardIds == null) State.lastValidDeckCardIds = new List<int>();
        if (State.lastValidDeckCardIds.Count == 0 && IsLegalDeck(State.sharedDeckCardIds))
            State.lastValidDeckCardIds = new List<int>(State.sharedDeckCardIds);
        if (State.lastValidDeckCardIds.Count == 0) State.lastValidDeckCardIds = CampaignCatalog.GetDeck(0);
        if (State.deckDraftCardIds.Count == 0) State.deckDraftCardIds = new List<int>(State.lastValidDeckCardIds);
        State.sharedDeckCardIds = new List<int>(State.lastValidDeckCardIds);
        if (State.cardHoloVariants == null) State.cardHoloVariants = new List<CardHoloVariantSaveData>();

        State.cityStates.RemoveAll(city => city == null || CampaignCatalog.GetCity(city.cityId) == null);
        State.unlockedCityIds.RemoveAll(cityId => CampaignCatalog.GetCity(cityId) == null);
        if (CampaignCatalog.GetCity(State.currentCityId) == null && CampaignCatalog.Cities.Count > 0)
            State.currentCityId = CampaignCatalog.Cities[0].cityId;
        foreach (CityData city in CampaignCatalog.Cities) GetCityState(city.cityId);
        if (CampaignCatalog.Cities.Count > 0 && State.unlockedCityIds.Count == 0)
            State.unlockedCityIds.Add(CampaignCatalog.Cities[0].cityId);
        if (!string.IsNullOrEmpty(State.pendingMatchId) && CampaignCatalog.GetMatch(State.pendingMatchId) == null)
        {
            State.pendingMatchId = null;
            State.pendingBattle = null;
        }

        State.version = CurrentSaveVersion;
    }

    // 补齐单座城市存档里缺的列表字段
    private void EnsureCityStateShape(CitySaveData cityState)
    {
        if (cityState.completedMatchIds == null) cityState.completedMatchIds = new List<string>();
        if (cityState.activeEventIds == null) cityState.activeEventIds = new List<string>();
        if (cityState.resolvedEventIds == null) cityState.resolvedEventIds = new List<string>();
    }

    // 给刚买到的 UR 卡分配一个随机的光影变体种子
    private void SaveCardHoloVariant(CardShopEntry entry)
    {
        if (entry.rarity != CardRarity.Limited) return;
        if (State.cardHoloVariants == null) State.cardHoloVariants = new List<CardHoloVariantSaveData>();
        State.cardHoloVariants.RemoveAll(item => item != null && item.cardId == entry.cardId);
        State.cardHoloVariants.Add(new CardHoloVariantSaveData
        {
            cardId = entry.cardId,
            colorSeed = StableSeed(Guid.NewGuid().ToString("N") + entry.cardId)
        });
    }

    // 把任意字符串压成一个稳定的整数，用作随机种子
    private static int StableSeed(string value)
    {
        unchecked
        {
            int hash = 17;
            if (value == null) return hash;
            for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
            return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
        }
    }
}
