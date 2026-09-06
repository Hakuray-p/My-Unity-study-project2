using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class CampaignSession : MonoBehaviour
{
    private const string SaveFileName = "campaign_save.json";
    private const int CurrentSaveVersion = 4;
    private static CampaignSession instance;

    public static CampaignSession Instance => EnsureInstance();
    public CampaignSaveData State { get; private set; }
    public BattleOutcome LastBattleOutcome { get; private set; }
    public string LastResolvedMatchId { get; private set; }
    public bool LastBattleResolved { get; private set; }
    public event Action ProgressChanged;

    public bool HasLegalDeck => State != null && IsLegalDeck(State.lastValidDeckCardIds);
    public bool HasPendingBattle => State != null && State.pendingBattle != null &&
                                    !string.IsNullOrEmpty(State.pendingBattle.matchId);

    public string GetDeckValidationError(IList<int> deck)
    {
        if (deck == null || deck.Count != 20) return "卡组必须正好包含 20 张牌";
        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (int cardId in deck)
        {
            if (!counts.ContainsKey(cardId)) counts[cardId] = 0;
            counts[cardId]++;
            if (counts[cardId] > 2) return $"卡牌 {cardId} 不能超过 2 张";
        }
        return string.Empty;
    }

    public bool IsLegalDeck(IList<int> deck) => string.IsNullOrEmpty(GetDeckValidationError(deck));

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

    public List<int> GetBattleDeck()
    {
        if (State == null) return CampaignCatalog.GetDeck(0);
        return IsLegalDeck(State.lastValidDeckCardIds)
            ? new List<int>(State.lastValidDeckCardIds)
            : new List<int>(CampaignCatalog.GetDeck(0));
    }

    public bool HasBadge(string badgeId) => State != null && State.badgeIds != null && State.badgeIds.Contains(badgeId);

    public bool CanBuyCard(CardShopEntry entry, out string reason)
    {
        reason = string.Empty;
        if (entry == null) { reason = "商品不存在"; return false; }
        if (GetLeaguePoints(State.currentCityId) < entry.unlockPoints) { reason = $"需要 {entry.unlockPoints} 积分"; return false; }
        if (State.collectedCardIds.Contains(entry.cardId)) { reason = "已经拥有这张卡"; return false; }
        if (State.currency < entry.price) { reason = "金币不足"; return false; }
        return true;
    }

    public bool BuyCard(CardShopEntry entry, out string reason)
    {
        if (!CanBuyCard(entry, out reason)) return false;
        State.currency -= entry.price;
        State.collectedCardIds.Add(entry.cardId);
        Save();
        ProgressChanged?.Invoke();
        return true;
    }

    private static CampaignSession EnsureInstance()
    {
        if (instance != null) return instance;
        instance = FindObjectOfType<CampaignSession>();
        if (instance == null)
        {
            GameObject root = new GameObject("CampaignSession");
            instance = root.AddComponent<CampaignSession>();
        }
        return instance;
    }

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
        if (loadedVersion != CurrentSaveVersion)
        {
            State.version = CurrentSaveVersion;
            Save();
        }
    }

    public void BeginNewGame()
    {
        State = CreateDefaultState();
        LastBattleOutcome = BattleOutcome.None;
        LastResolvedMatchId = null;
        LastBattleResolved = false;
        Save();
        ProgressChanged?.Invoke();
    }

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

    public bool IsCityUnlocked(string cityId)
    {
        return State != null && State.unlockedCityIds != null && State.unlockedCityIds.Contains(cityId);
    }

    public void SelectCity(string cityId)
    {
        CityData city = CampaignCatalog.GetCity(cityId);
        if (city == null || !IsCityUnlocked(cityId) || HasPendingBattle) return;

        State.currentCityId = cityId;
        CitySaveData cityState = GetCityState(cityId);
        StageData stage = StageCatalog.GetStageForCity(cityId);
        if (cityState.stageRun == null) cityState.stageRun = StageCatalog.CreateRun(stage);

        State.currentStageId = stage != null ? stage.stageId : null;
        State.currentStageRun = cityState.stageRun;
        State.currentAnchorId = cityState.stageRun != null && cityState.stageRun.active
            ? cityState.stageRun.currentCellId
            : "city_entrance";
        State.playerPosition = Vector3.zero;
        Save();
        ProgressChanged?.Invoke();
    }

    public StageData GetCurrentStage()
    {
        if (State == null) return null;
        return StageCatalog.GetStage(State.currentStageId) ?? StageCatalog.GetStageForCity(State.currentCityId);
    }

    public StageRunSaveData GetCurrentStageRun()
    {
        if (State == null) return null;
        StageData stage = GetCurrentStage();
        if (stage == null) return null;

        CitySaveData cityState = GetCityState(stage.cityId);
        if (cityState.stageRun == null || cityState.stageRun.stageId != stage.stageId)
            cityState.stageRun = StageCatalog.CreateRun(stage);

        State.currentCityId = stage.cityId;
        State.currentStageId = stage.stageId;
        State.currentStageRun = cityState.stageRun;
        return cityState.stageRun;
    }

    public StageRunSaveData GetOrCreateStageRun(string stageId)
    {
        StageData stage = StageCatalog.GetStage(stageId);
        if (stage == null || !IsCityUnlocked(stage.cityId)) return null;

        State.currentCityId = stage.cityId;
        State.currentStageId = stage.stageId;
        CitySaveData cityState = GetCityState(stage.cityId);
        if (cityState.stageRun == null || cityState.stageRun.stageId != stage.stageId)
            cityState.stageRun = StageCatalog.CreateRun(stage);
        State.currentStageRun = cityState.stageRun;
        return cityState.stageRun;
    }

    public bool BeginStage(string cityId, string stageId)
    {
        StageData stage = StageCatalog.GetStage(stageId);
        if (stage == null || stage.cityId != cityId || !IsCityUnlocked(cityId)) return false;

        CitySaveData cityState = GetCityState(cityId);
        if (cityState.stageRun == null || cityState.stageRun.stageId != stageId)
            cityState.stageRun = StageCatalog.CreateRun(stage);

        State.currentCityId = cityId;
        State.currentStageId = stageId;
        State.currentStageRun = cityState.stageRun;
        cityState.stageRun.active = true;
        State.currentAnchorId = cityState.stageRun.currentCellId;
        Save();
        ProgressChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Restarts the complete route while preserving permanent campaign progress.
    /// </summary>
    public void ResetCurrentStage()
    {
        StageData stage = GetCurrentStage();
        if (stage == null) return;

        CitySaveData cityState = GetCityState(stage.cityId);
        cityState.stageRun = StageCatalog.CreateRun(stage);
        cityState.stageRun.active = true;
        State.currentStageRun = cityState.stageRun;
        State.currentStageId = stage.stageId;
        State.currentAnchorId = cityState.stageRun.currentCellId;
        State.pendingBattle = null;
        State.pendingMatchId = null;
        Save();
        ProgressChanged?.Invoke();
    }

    public bool CanEnterStageCell(string stageId, string cellId)
    {
        StageData stage = StageCatalog.GetStage(stageId);
        if (stage == null || !IsCityUnlocked(stage.cityId)) return false;

        StageRunSaveData run = GetOrCreateStageRun(stageId);
        if (run == null || !run.active) return false;

        StageCellData from = stage.GetCell(run.currentCellId);
        StageCellData target = stage.GetCell(cellId);
        if (from == null || target == null || target.cellId == from.cellId || !stage.AreAdjacent(from, target)) return false;
        MatchData targetMatch = !string.IsNullOrEmpty(target.encounterId)
            ? CampaignCatalog.GetMatch(target.encounterId)
            : null;
        if (targetMatch == null || targetMatch.matchType != MatchType.Practice)
        {
            if (IsStageNodeComplete(stageId, target.cellId)) return false;
        }

        if (target.nodeType == StageNodeType.Champion)
        {
            CityData city = CampaignCatalog.GetCity(stage.cityId);
            if (city == null || GetLeaguePoints(stage.cityId) < city.requiredPoints) return false;
        }

        if (!string.IsNullOrEmpty(target.encounterId))
        {
            if (targetMatch != null && !CanStartMatch(targetMatch)) return false;
        }

        return true;
    }

    public bool TryMoveCurrentStage(string cellId)
    {
        StageData stage = GetCurrentStage();
        StageRunSaveData run = stage == null ? null : GetOrCreateStageRun(stage.stageId);
        if (stage == null || run == null || !CanEnterStageCell(stage.stageId, cellId)) return false;

        StageCellData target = stage.GetCell(cellId);
        run.previousCellId = run.currentCellId;
        run.currentCellId = target.cellId;
        if (!run.visitedCellIds.Contains(target.cellId)) run.visitedCellIds.Add(target.cellId);
        State.currentAnchorId = target.cellId;
        Save();
        ProgressChanged?.Invoke();
        return true;
    }

    public bool IsStageNodeComplete(string stageId, string cellId)
    {
        StageData stage = StageCatalog.GetStage(stageId);
        StageCellData cell = stage?.GetCell(cellId);
        if (cell == null) return false;

        StageRunSaveData run = State != null && State.currentStageRun != null && State.currentStageRun.stageId == stageId
            ? State.currentStageRun
            : GetCityState(stage.cityId).stageRun;
        if (run != null && run.completedNodeIds != null && run.completedNodeIds.Contains(cellId)) return true;
        if (string.IsNullOrEmpty(cell.encounterId)) return false;

        MatchData match = CampaignCatalog.GetMatch(cell.encounterId);
        // Practice nodes can be replayed and therefore never become permanently
        // complete route nodes.
        return match != null && match.matchType != MatchType.Practice && IsMatchComplete(match.matchId);
    }

    public void MarkStageNodeComplete(string stageId, string cellId)
    {
        StageRunSaveData run = GetOrCreateStageRun(stageId);
        if (run == null || string.IsNullOrEmpty(cellId)) return;
        if (run.completedNodeIds == null) run.completedNodeIds = new List<string>();
        if (!run.completedNodeIds.Contains(cellId)) run.completedNodeIds.Add(cellId);
    }

    public bool IsMatchComplete(string matchId)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (match == null) return false;
        CitySaveData cityState = GetCityState(match.cityId);
        return cityState.completedMatchIds != null && cityState.completedMatchIds.Contains(matchId);
    }

    public bool CanStartMatch(MatchData match)
    {
        if (match == null || !IsCityUnlocked(match.cityId)) return false;
        if (HasPendingBattle) return false;
        if (!HasLegalDeck) return false;
        if (match.matchType == MatchType.Champion)
        {
            CityData city = CampaignCatalog.GetCity(match.cityId);
            if (city == null || GetCityState(match.cityId).leaguePoints < city.requiredPoints) return false;
        }
        if (!string.IsNullOrEmpty(match.prerequisiteMatchId) && !IsMatchComplete(match.prerequisiteMatchId)) return false;
        return true;
    }

    public void BeginMatch(MatchData match, Vector3 returnPosition, string stageId = null, string cellId = null)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        State.currentCityId = match.cityId;
        State.currentStageId = string.IsNullOrEmpty(stageId)
            ? StageCatalog.GetStageForCity(match.cityId)?.stageId
            : stageId;

        StageRunSaveData run = GetOrCreateStageRun(State.currentStageId);
        if (run != null)
        {
            run.active = true;
            if (!string.IsNullOrEmpty(cellId) && run.currentCellId != cellId)
            {
                run.previousCellId = run.currentCellId;
                run.currentCellId = cellId;
            }
        }

        State.currentAnchorId = string.IsNullOrEmpty(cellId) ? match.matchId : cellId;
        State.playerPosition = returnPosition;
        State.pendingMatchId = match.matchId;
        State.pendingBattle = null;
        LastBattleResolved = false;
        Save();
    }

    public void SaveBattleSnapshot(BattleSnapshot snapshot)
    {
        if (State == null || snapshot == null) return;
        State.pendingBattle = snapshot;
        State.pendingMatchId = snapshot.matchId;
        Save();
    }

    public BattleLaunchContext CreateBattleContext()
    {
        if (State == null || string.IsNullOrEmpty(State.pendingMatchId)) return null;
        MatchData match = CampaignCatalog.GetMatch(State.pendingMatchId);
        if (match == null) return null;

        StageRunSaveData run = GetOrCreateStageRun(State.currentStageId);
        return new BattleLaunchContext
        {
            matchId = match.matchId,
            cityId = match.cityId,
            returnScene = "HD_2D_Day",
            returnAnchorId = State.currentAnchorId,
            randomSeed = StableSeed(match.matchId),
            enemyDeckId = match.enemyDeckId,
            fieldRuleId = match.fieldRuleId,
            stageId = State.currentStageId,
            cellId = run != null ? run.currentCellId : null,
            snapshot = State.pendingBattle
        };
    }

    public BattleResult ResolveMatch(bool won)
    {
        MatchData match = State == null ? null : CampaignCatalog.GetMatch(State.pendingMatchId);
        if (match == null) return null;

        BattleResult result = new BattleResult
        {
            matchId = match.matchId,
            cityId = match.cityId,
            outcome = won ? BattleOutcome.PlayerWin : BattleOutcome.PlayerLoss
        };
        LastBattleOutcome = result.outcome;
        LastResolvedMatchId = match.matchId;
        LastBattleResolved = true;

        if (won)
        {
            StageRunSaveData run = GetOrCreateStageRun(State.currentStageId);
            string cellId = run != null ? run.currentCellId : null;
            if (run != null && !string.IsNullOrEmpty(cellId) && match.matchType != MatchType.Practice)
                MarkStageNodeComplete(run.stageId, cellId);

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
                string badgeId = match.cityId + "_badge";
                if (match.awardsBadge && !HasBadge(badgeId))
                {
                    State.badgeIds.Add(badgeId);
                    result.badgeAwarded = true;
                }
                UnlockNextCity(match.cityId);
            }
        }
        else
        {
            // A failed match resets the current route run. Permanent city
            // progress, collected cards, and the shared deck are untouched.
            // This keeps the retry flow honest: the player must walk the route
            // again instead of being teleported back onto the failed node.
            ResetCurrentStage();
            State.currentAnchorId = "city_entrance";
            State.playerPosition = Vector3.zero;
        }

        State.pendingBattle = null;
        State.pendingMatchId = null;
        Save();
        ProgressChanged?.Invoke();
        return result;
    }

    public int GetLeaguePoints(string cityId) => GetCityState(cityId).leaguePoints;

    public bool TryRetryLastMatch(Vector3 returnPosition)
    {
        if (string.IsNullOrEmpty(LastResolvedMatchId)) return false;
        MatchData match = CampaignCatalog.GetMatch(LastResolvedMatchId);
        if (match == null || !CanStartMatch(match)) return false;

        StageData stage = StageCatalog.GetStage(State.currentStageId);
        StageRunSaveData run = stage != null ? GetOrCreateStageRun(stage.stageId) : null;
        StageCellData target = FindMatchCell(stage, match.matchId);
        if (run != null && target != null && run.currentCellId != target.cellId)
        {
            run.previousCellId = run.currentCellId;
            run.currentCellId = target.cellId;
            State.currentAnchorId = target.cellId;
        }

        BeginMatch(match, returnPosition, stage != null ? stage.stageId : null, target != null ? target.cellId : null);
        return true;
    }

    public void KeepPendingBattle() => Save();

    public void SetPlayerPosition(Vector3 position)
    {
        if (State != null) State.playerPosition = position;
    }

    public int GetCardUpgradeLevel(int cardId)
    {
        if (State == null || State.cardUpgrades == null) return 0;
        CardUpgradeSaveData upgrade = State.cardUpgrades.Find(item => item != null && item.cardId == cardId);
        return upgrade != null ? upgrade.level : 0;
    }

    private StageCellData FindMatchCell(StageData stage, string matchId)
    {
        if (stage == null || string.IsNullOrEmpty(matchId)) return null;
        return stage.cells.Find(cell => cell != null && cell.encounterId == matchId);
    }

    private void UnlockNextCity(string cityId)
    {
        CityData city = CampaignCatalog.GetCity(cityId);
        if (city == null || State.unlockedCityIds == null) return;

        foreach (CityData candidate in CampaignCatalog.Cities)
        {
            if (candidate.order == city.order + 1 && !State.unlockedCityIds.Contains(candidate.cityId))
            {
                State.unlockedCityIds.Add(candidate.cityId);
                break;
            }
        }
    }

    private CampaignSaveData CreateDefaultState()
    {
        CampaignSaveData state = new CampaignSaveData
        {
            version = CurrentSaveVersion,
            currentCityId = CampaignCatalog.Cities.Count > 0 ? CampaignCatalog.Cities[0].cityId : string.Empty,
            currentAnchorId = "city_entrance",
            currentStageId = null,
            playerPosition = Vector3.zero,
            sharedDeckCardIds = CampaignCatalog.GetDeck(0),
            currency = 0,
            currentStageRun = null
        };
        state.lastValidDeckCardIds = new List<int>(state.sharedDeckCardIds);
        state.deckDraftCardIds = new List<int>(state.sharedDeckCardIds);

        foreach (CityData city in CampaignCatalog.Cities)
        {
            StageData stage = StageCatalog.GetStageForCity(city.cityId);
            CitySaveData cityState = new CitySaveData
            {
                cityId = city.cityId,
                stageRun = StageCatalog.CreateRun(stage)
            };
            state.cityStates.Add(cityState);
        }

        CitySaveData firstCity = state.cityStates.Count > 0 ? state.cityStates[0] : null;
        state.currentStageId = firstCity?.stageRun?.stageId;
        state.currentStageRun = firstCity?.stageRun;
        if (firstCity != null) state.unlockedCityIds.Add(firstCity.cityId);
        state.collectedCardIds.AddRange(state.sharedDeckCardIds);
        return state;
    }

    private void EnsureStateShape()
    {
        if (State.unlockedCityIds == null) State.unlockedCityIds = new List<string>();
        if (State.cityStates == null) State.cityStates = new List<CitySaveData>();
        if (State.collectedCardIds == null) State.collectedCardIds = new List<int>();
        if (State.sharedDeckCardIds == null) State.sharedDeckCardIds = new List<int>();
        if (State.badgeIds == null) State.badgeIds = new List<string>();
        if (State.deckDraftCardIds == null) State.deckDraftCardIds = new List<int>();
        if (State.lastValidDeckCardIds == null) State.lastValidDeckCardIds = new List<int>();
        if (State.lastValidDeckCardIds.Count == 0 && IsLegalDeck(State.sharedDeckCardIds))
            State.lastValidDeckCardIds = new List<int>(State.sharedDeckCardIds);
        if (State.lastValidDeckCardIds.Count == 0) State.lastValidDeckCardIds = CampaignCatalog.GetDeck(0);
        if (State.deckDraftCardIds.Count == 0) State.deckDraftCardIds = new List<int>(State.lastValidDeckCardIds);
        State.sharedDeckCardIds = new List<int>(State.lastValidDeckCardIds);
        if (State.cardUpgrades == null) State.cardUpgrades = new List<CardUpgradeSaveData>();

        StageRunSaveData legacyRun = State.currentStageRun;
        if (string.IsNullOrEmpty(State.currentCityId) && CampaignCatalog.Cities.Count > 0)
            State.currentCityId = CampaignCatalog.Cities[0].cityId;

        foreach (CityData city in CampaignCatalog.Cities)
        {
            CitySaveData cityState = GetCityState(city.cityId);
            StageData stage = StageCatalog.GetStageForCity(city.cityId);
            if (cityState.stageRun == null || cityState.stageRun.stageId != stage?.stageId)
            {
                if (legacyRun != null && city.cityId == State.currentCityId && legacyRun.stageId == stage?.stageId)
                    cityState.stageRun = legacyRun;
                else
                    cityState.stageRun = StageCatalog.CreateRun(stage);
            }
        }

        if (CampaignCatalog.Cities.Count > 0 && State.unlockedCityIds.Count == 0)
            State.unlockedCityIds.Add(CampaignCatalog.Cities[0].cityId);

        CitySaveData currentCity = GetCityState(State.currentCityId);
        StageData currentStage = StageCatalog.GetStageForCity(State.currentCityId);
        if (currentStage != null)
        {
            State.currentStageId = currentStage.stageId;
            State.currentStageRun = currentCity.stageRun;
            if (State.currentStageRun == null) State.currentStageRun = StageCatalog.CreateRun(currentStage);
        }

        State.version = CurrentSaveVersion;
    }

    private void EnsureCityStateShape(CitySaveData cityState)
    {
        if (cityState.completedMatchIds == null) cityState.completedMatchIds = new List<string>();
        if (cityState.claimedRewardIds == null) cityState.claimedRewardIds = new List<string>();
        if (cityState.completedQuestIds == null) cityState.completedQuestIds = new List<string>();
        if (cityState.resolvedEventIds == null) cityState.resolvedEventIds = new List<string>();
        if (cityState.collectedObjectIds == null) cityState.collectedObjectIds = new List<string>();
    }

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
