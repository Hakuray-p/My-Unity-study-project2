using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldInteractionDefinition", menuName = "RX-未知裂痕/Campaign/World Interaction")]
public class WorldInteractionDefinitionAsset : ScriptableObject
{
    public WorldInteractionDefinition definition = new WorldInteractionDefinition();
}

[CreateAssetMenu(fileName = "DialogueDefinition", menuName = "RX-未知裂痕/Campaign/Dialogue")]
public class DialogueDefinitionAsset : ScriptableObject
{
    public DialogueDefinition definition = new DialogueDefinition();
}

[CreateAssetMenu(fileName = "ShopDefinition", menuName = "RX-未知裂痕/Campaign/Shop")]
public class ShopDefinitionAsset : ScriptableObject
{
    public ShopDefinition definition = new ShopDefinition();
}

[CreateAssetMenu(fileName = "MatchDefinition", menuName = "RX-未知裂痕/Campaign/Match")]
public class MatchDefinitionAsset : ScriptableObject
{
    public MatchData definition = new MatchData();
}
