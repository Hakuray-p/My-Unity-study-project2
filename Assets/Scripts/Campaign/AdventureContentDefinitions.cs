using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldInteractionDefinition", menuName = "ArkCard/Campaign/World Interaction")]
public class WorldInteractionDefinitionAsset : ScriptableObject
{
    public WorldInteractionDefinition definition = new WorldInteractionDefinition();
}

[CreateAssetMenu(fileName = "DialogueDefinition", menuName = "ArkCard/Campaign/Dialogue")]
public class DialogueDefinitionAsset : ScriptableObject
{
    public DialogueDefinition definition = new DialogueDefinition();
}

[CreateAssetMenu(fileName = "ShopDefinition", menuName = "ArkCard/Campaign/Shop")]
public class ShopDefinitionAsset : ScriptableObject
{
    public ShopDefinition definition = new ShopDefinition();
}

[CreateAssetMenu(fileName = "MatchDefinition", menuName = "ArkCard/Campaign/Match")]
public class MatchDefinitionAsset : ScriptableObject
{
    public MatchData definition = new MatchData();
}
