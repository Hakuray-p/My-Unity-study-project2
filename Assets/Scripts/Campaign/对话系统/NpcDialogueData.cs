using System;
using UnityEngine;

// 保存一句对话的说话者和正文。
[Serializable]
public class DialogueLine
{
    public string speakerId; // 说话者标识
    [TextArea(2, 5)] public string text; // 当前句子的正文
}

// 保存一场挑战及其前后对话。
[Serializable]
public class NpcChallenge
{
    public string matchId; // 赛事总表中的比赛标识
    public DialogueLine[] beforeMatch; // 开始挑战前的对话
    public DialogueLine[] victory; // 本次胜利后的对话
    public DialogueLine[] defeat; // 本次失败后的对话
}

// 在 Inspector 中配置一个 NPC 的身份、剧情和有序挑战。
[CreateAssetMenu(menuName = "ArkCard/NPC 对话", fileName = "NpcDialogue")]
public class NpcDialogueData : ScriptableObject
{
    public string speakerId; // 角色的稳定标识
    public string displayName; // 对话中显示的角色名字
    public string cityId = "first_light"; // 角色所属城市
    public string storyEventId = "first_light_event_01"; // 推进角色剧情的事件
    public WorldInteractionType interactionType; // 角色提供的交互类型
    public string eventId; // 角色负责接取的事件
    public DialogueLine[] sceneChat; // 场景闲聊
    public DialogueLine[] interaction; // 按 E 交互
    public DialogueLine[] eventDialogue; // 角色专属事件剧情
    public DialogueLine[] normal; // 普通交谈
    public DialogueLine[] afterEvent; // 调查完成后的交谈
    public DialogueLine[] ending; // 本城故事收尾
    public DialogueLine[] storyVictory; // 角色剧情胜利结算
    public DialogueLine[] storyDefeat; // 角色剧情失败结算
    public NpcChallenge[] matches; // 按挑战顺序配置的赛事

    // 选择角色剧情使用的战后结算，没有新稿时回退到赛事文案。
    public DialogueLine[] GetBattleReaction(NpcChallenge challenge, BattleOutcome outcome)
    {
        if (outcome == BattleOutcome.PlayerWin && storyVictory != null && storyVictory.Length > 0)
            return storyVictory;
        if (outcome == BattleOutcome.PlayerLoss && storyDefeat != null && storyDefeat.Length > 0)
            return storyDefeat;
        return outcome == BattleOutcome.PlayerWin ? challenge.victory : challenge.defeat;
    }

    // 选择尚未完成的第一场挑战，全部完成后重打最后一场。
    public NpcChallenge GetChallenge(CampaignSession session)
    {
        foreach (NpcChallenge challenge in matches)
            if (!session.IsMatchComplete(challenge.matchId)) return challenge;
        return matches.Length == 0 ? null : matches[matches.Length - 1];
    }

    // 根据已经保存的事件和冠军进度选择当前剧情。
    public DialogueLine[] GetConversation(CampaignSession session)
    {
        bool resolved = session.IsEventResolved(cityId, storyEventId);
        if (resolved && session.GetCityState(cityId).championDefeated) return ending;
        return resolved ? afterEvent : GetNormalConversation();
    }

    // 选择闲聊按钮对应的场景闲聊内容。
    private DialogueLine[] GetNormalConversation()
    {
        return sceneChat == null || sceneChat.Length == 0 ? normal : sceneChat;
    }

    // 选择按 E 打开时对应的交互内容。
    public DialogueLine[] GetInteractionConversation(CampaignSession session)
    {
        return interaction == null || interaction.Length == 0 ? GetConversation(session) : interaction;
    }
}
