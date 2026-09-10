using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class CampaignSession : MonoBehaviour
{
    private const string SaveFileName = "campaign_save.json";
    private const int CurrentSaveVersion = 7;
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
        SaveCardHoloVariant(entry);
        Save();
        ProgressChanged?.Invoke();
        return true;
    }

    public bool TryGetCardHoloVariantSeed(int cardId, out int colorSeed)
    {
        colorSeed = 0;
        if (State == null || State.cardHoloVariants == null) return false;
        CardHoloVariantSaveData variant = State.cardHoloVariants.Find(item => item != null && item.cardId == cardId);
        if (variant == null) return false;
        colorSeed = variant.colorSeed;
        return true;
    }

    public bool IsEventActive(string cityId, string eventId)
    {
        return GetCityState(cityId).activeEventIds.Contains(eventId);
    }

    public bool IsEventResolved(string cityId, string eventId)
    {
        return GetCityState(cityId).resolvedEventIds.Contains(eventId);
    }

    public void StartEvent(CampaignEventData eventData)
    {
        CitySaveData cityState = GetCityState(eventData.cityId);
        if (cityState.resolvedEventIds.Contains(eventData.eventId)) return;
        if (cityState.activeEventIds.Contains(eventData.eventId)) return;
        cityState.activeEventIds.Add(eventData.eventId);
        Save();
        ProgressChanged?.Invoke();
    }

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

    public void BeginMatch(MatchData match, Vector3 returnPosition)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        State.currentCityId = match.cityId;
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

        return new BattleLaunchContext
        {
            matchId = match.matchId,
            cityId = match.cityId,
            returnScene = "HD_2D_Day",
            randomSeed = StableSeed(match.matchId),
            enemyDeckId = match.enemyDeckId,
            fieldRuleId = match.fieldRuleId,
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
            }
        }
        else
        {
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
        BeginMatch(match, returnPosition);
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


    private CampaignSaveData CreateDefaultState()
    {
        CampaignSaveData state = new CampaignSaveData
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

    private void EnsureCityStateShape(CitySaveData cityState)
    {
        if (cityState.completedMatchIds == null) cityState.completedMatchIds = new List<string>();
        if (cityState.claimedRewardIds == null) cityState.claimedRewardIds = new List<string>();
        if (cityState.completedQuestIds == null) cityState.completedQuestIds = new List<string>();
        if (cityState.activeEventIds == null) cityState.activeEventIds = new List<string>();
        if (cityState.resolvedEventIds == null) cityState.resolvedEventIds = new List<string>();
        if (cityState.collectedObjectIds == null) cityState.collectedObjectIds = new List<string>();
    }

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
