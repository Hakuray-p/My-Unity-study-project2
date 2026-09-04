using System.Collections.Generic;

public static class StageCatalog
{
    private static readonly List<StageData> stages = new List<StageData>
    {
        CreateStage(
            "first_light_route",
            "first_light",
            "First Light - League Route",
            new[]
            {
                Cell("start", 0, 2, StageNodeType.Start, null),
                Cell("practice", 1, 2, StageNodeType.Battle, "first_light_practice"),
                Cell("crossroads", 2, 2, StageNodeType.Event, "first_light_event_01"),
                Cell("riverside", 3, 2, StageNodeType.Battle, "first_light_public_01"),
                Cell("market", 3, 1, StageNodeType.Shop, "first_light_shop"),
                Cell("lantern", 4, 2, StageNodeType.Battle, "first_light_public_02"),
                Cell("champion", 5, 2, StageNodeType.Champion, "first_light_champion"),
                Cell("exit", 6, 2, StageNodeType.Exit, null)
            }),
        CreateStage(
            "glass_harbor_route",
            "glass_harbor",
            "Glass Harbor - League Route",
            new[]
            {
                Cell("start", 0, 2, StageNodeType.Start, null),
                Cell("dockside", 1, 2, StageNodeType.Battle, "glass_harbor_public_01"),
                Cell("market", 2, 2, StageNodeType.Shop, "glass_harbor_shop"),
                Cell("moon_market", 3, 2, StageNodeType.Battle, "glass_harbor_public_02"),
                Cell("tide_event", 4, 2, StageNodeType.Event, "glass_harbor_event_01"),
                Cell("champion", 5, 2, StageNodeType.Champion, "glass_harbor_champion"),
                Cell("exit", 6, 2, StageNodeType.Exit, null)
            })
    };

    public static IReadOnlyList<StageData> Stages => stages;

    public static StageData GetStage(string stageId)
    {
        return stages.Find(stage => stage.stageId == stageId);
    }

    public static StageData GetStageForCity(string cityId)
    {
        return stages.Find(stage => stage.cityId == cityId);
    }

    public static StageRunSaveData CreateRun(StageData stage)
    {
        StageCellData start = stage != null ? stage.GetCell(stage.startCellId) : null;
        StageRunSaveData run = new StageRunSaveData
        {
            stageId = stage != null ? stage.stageId : string.Empty,
            randomSeed = StableSeed(stage != null ? stage.stageId : string.Empty),
            currentCellId = start != null ? start.cellId : string.Empty,
            // A route is prepared at save creation, but only becomes playable after
            // the player reaches the city entrance and explicitly opens it.
            active = false
        };
        if (start != null) run.visitedCellIds.Add(start.cellId);
        return run;
    }

    private static StageData CreateStage(string stageId, string cityId, string displayName, StageCellData[] cells)
    {
        return new StageData
        {
            stageId = stageId,
            cityId = cityId,
            displayName = displayName,
            gridWidth = 7,
            gridHeight = 5,
            startCellId = "start",
            exitCellId = "exit",
            cells = new List<StageCellData>(cells)
        };
    }

    private static StageCellData Cell(string id, int x, int y, StageNodeType nodeType, string encounterId)
    {
        return new StageCellData
        {
            cellId = id,
            x = x,
            y = y,
            walkable = true,
            nodeType = nodeType,
            encounterId = encounterId
        };
    }

    private static int StableSeed(string value)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
            return hash == int.MinValue ? int.MaxValue : System.Math.Abs(hash);
        }
    }
}
