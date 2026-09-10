using System;
using System.Collections.Generic;
using UnityEngine;

public enum MatchType
{
    Practice = 0,
    Public = 1,
    Champion = 2
}

public enum CardRarity
{
    Common = 0,
    Rare = 1,
    Limited = 2
}

public enum WorldInteractionType
{
    Match = 0,
    Shop = 1,
    Event = 2
}

public enum BattleOutcome
{
    None = 0,
    PlayerWin = 1,
    PlayerLoss = 2
}

[Serializable]
public class CityData
{
    public string cityId;
    public string displayName;
    public string sceneName;
    public int requiredPoints;
    public string championMatchId;
    public List<string> availableMatchIds = new List<string>();
    public List<string> sideQuestIds = new List<string>();
    public List<int> cardRewardIds = new List<int>();
}

[Serializable]
public class MatchData
{
    public string matchId;
    public string cityId;
    public string displayName;
    public MatchType matchType;
    public string opponentId;
    public int pointReward;
    public int goldReward;
    public int enemyDeckId;
    public string fieldRuleId;
    public int unlockPoints;
    public string prerequisiteMatchId;
    public bool repeatable = true;
    public bool awardsBadge;
    public List<int> rewardCardIds = new List<int>();
}

[Serializable]
public class DialogueOptionData
{
    public string label;
    public string action;
}

[Serializable]
public class DialogueDefinition
{
    public string dialogueId;
    public string speakerName;
    [TextArea] public string text;
    public List<DialogueOptionData> options = new List<DialogueOptionData>();
}

[Serializable]
public class CampaignEventData
{
    public string eventId;
    public string cityId;
    public string displayName;
    [TextArea] public string startText;
    [TextArea] public string objectiveText;
    [TextArea] public string completeText;
    [TextArea] public string reviewText;
    public int goldReward;
    public int cardRewardId;
    public string cardRewardName;
}

[Serializable]
public class CardShopEntry
{
    public int cardId;
    public CardRarity rarity;
    public int price;
    public int unlockPoints;
    public string archetype;
}

[Serializable]
public class CardHoloVariantSaveData
{
    public int cardId;
    public int colorSeed;
}

[Serializable]
public class ShopDefinition
{
    public string shopId;
    public string displayName;
    public List<CardShopEntry> entries = new List<CardShopEntry>();
}

[Serializable]
public class WorldInteractionDefinition
{
    public string interactionId;
    public string displayName;
    public WorldInteractionType interactionType;
    public string matchId;
    public string shopId;
    public string dialogueId;
    public Vector3 position;
    public Color tint = Color.white;
}

[Serializable]
public class CitySaveData
{
    public string cityId;
    public int leaguePoints;
    public bool championDefeated;
    public List<string> completedMatchIds = new List<string>();
    public List<string> claimedRewardIds = new List<string>();
    public List<string> completedQuestIds = new List<string>();
    public List<string> activeEventIds = new List<string>();
    public List<string> resolvedEventIds = new List<string>();
    public List<string> collectedObjectIds = new List<string>();
}

[Serializable]
public class BattleCardSnapshot
{
    public int cardId;
    public CardState state;
    public int attack;
    public int health;
    public int healthMax;
    public bool ableAttack;
    public bool silenced;
}

[Serializable]
public class BattlePlayerSnapshot
{
    public int playerId;
    public int health;
    public int healthMax;
    public int cost;
    public int costMax;
    public bool isInTurn;
    public int fatigueLevel;
    public List<BattleCardSnapshot> cards = new List<BattleCardSnapshot>();
}

[Serializable]
public class BattleSnapshot
{
    public string matchId;
    public int randomSeed;
    public int turn;
    public int activePlayerId;
    public string randomStateJson;
    public List<BattlePlayerSnapshot> players = new List<BattlePlayerSnapshot>();
}

[Serializable]
public class BattleLaunchContext
{
    public string matchId;
    public string cityId;
    public string returnScene;
    public int randomSeed;
    public int enemyDeckId;
    public string fieldRuleId;
    public BattleSnapshot snapshot;
}

[Serializable]
public class CardUpgradeSaveData
{
    public int cardId;
    public int level;
}

[Serializable]
public class BattleResult
{
    public string matchId;
    public string cityId;
    public BattleOutcome outcome;
    public int leaguePoints;
    public int gold;
    public bool firstWin;
    public bool badgeAwarded;
    public List<int> rewardCardIds = new List<int>();
}

[Serializable]
public class CampaignSaveData
{
    public int version = 7;
    public string currentCityId;
    public Vector3 playerPosition;
    public List<string> unlockedCityIds = new List<string>();
    public List<CitySaveData> cityStates = new List<CitySaveData>();
    public List<int> collectedCardIds = new List<int>();
    public List<int> sharedDeckCardIds = new List<int>();
    public int currency;
    public List<string> badgeIds = new List<string>();
    public List<int> deckDraftCardIds = new List<int>();
    public List<int> lastValidDeckCardIds = new List<int>();
    public List<CardUpgradeSaveData> cardUpgrades = new List<CardUpgradeSaveData>();
    public List<CardHoloVariantSaveData> cardHoloVariants = new List<CardHoloVariantSaveData>();
    public BattleSnapshot pendingBattle;
    public string pendingMatchId;
}
