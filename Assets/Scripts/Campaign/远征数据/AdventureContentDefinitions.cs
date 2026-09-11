using UnityEngine;

// 把交互点、对话、商店、比赛四类内容做成能在 Project 里右键创建的资源文件。
[CreateAssetMenu(fileName = "WorldInteractionDefinition", menuName = "RX-未知裂痕/Campaign/World Interaction")]
public class WorldInteractionDefinitionAsset : ScriptableObject
{
    public WorldInteractionDefinition definition = new WorldInteractionDefinition(); // 这个交互点的全部配置
}

// 一份对话内容的资源包装
[CreateAssetMenu(fileName = "DialogueDefinition", menuName = "RX-未知裂痕/Campaign/Dialogue")]
public class DialogueDefinitionAsset : ScriptableObject
{
    public DialogueDefinition definition = new DialogueDefinition(); // 这份对话的内容
}

// 一份商店货架的资源包装
[CreateAssetMenu(fileName = "ShopDefinition", menuName = "RX-未知裂痕/Campaign/Shop")]
public class ShopDefinitionAsset : ScriptableObject
{
    public ShopDefinition definition = new ShopDefinition(); // 这家商店的货架
}

// 一场比赛的资源包装
[CreateAssetMenu(fileName = "MatchDefinition", menuName = "RX-未知裂痕/Campaign/Match")]
public class MatchDefinitionAsset : ScriptableObject
{
    public MatchData definition = new MatchData(); // 这场比赛的规则和奖励
}
