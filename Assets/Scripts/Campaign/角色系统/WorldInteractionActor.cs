using UnityEngine;

// 世界里可以互动的 NPC，动画由已有的 HDNpcCharacter 负责。
public sealed class WorldInteractionActor : MonoBehaviour
{
    public NpcDialogueData dialogue; // 在场景中配置的角色身份、对话和赛事
}
