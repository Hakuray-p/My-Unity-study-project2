using UnityEngine.SceneManagement;

public static class SceneFlowService
{
    public static void StartNewGame()
    {
        CampaignSession.Instance.BeginNewGame();
        LoadAdventureScene();
    }

    public static void ContinueGame()
    {
        CampaignSession.Instance.LoadOrCreate();
        if (CampaignSession.Instance.HasPendingBattle)
            SceneManager.LoadScene("BattleScene");
        else
            LoadAdventureScene();
    }

    public static void LoadWorldMap()
    {
        LoadAdventureScene();
    }

    public static void LoadAdventureScene()
    {
        SceneManager.LoadScene("HD_2D_Day");
    }

    public static void EnterCity(string cityId)
    {
        CityData city = CampaignCatalog.GetCity(cityId);
        if (city == null || !CampaignSession.Instance.IsCityUnlocked(cityId)) return;
        CampaignSession.Instance.SelectCity(cityId);
        LoadAdventureScene();
    }

    public static void StartMatch(string matchId, UnityEngine.Vector3 returnPosition, string stageId = null, string cellId = null)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (!CampaignSession.Instance.CanStartMatch(match)) return;
        CampaignSession.Instance.BeginMatch(match, returnPosition, stageId, cellId);
        SceneManager.LoadScene("BattleScene");
    }

    public static void RetryLastMatch()
    {
        CampaignSession session = CampaignSession.Instance;
        if (session.TryRetryLastMatch(session.State.playerPosition))
        {
            SceneManager.LoadScene("BattleScene");
        }
    }

    public static void RestartCurrentStage()
    {
        CampaignSession.Instance.ResetCurrentStage();
        LoadAdventureScene();
    }

    public static void ResumePendingBattle()
    {
        if (CampaignSession.Instance.CreateBattleContext() != null)
        {
            SceneManager.LoadScene("BattleScene");
        }
    }

    public static void ReturnToCurrentCity()
    {
        LoadAdventureScene();
    }

    public static void ReturnToMenu()
    {
        SceneManager.LoadScene("GameMenu");
    }
}
