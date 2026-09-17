using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 保存说话者与预先制作的对话界面的明确对应关系。
[Serializable]
public class DialogueViewBinding
{
    public string speakerId; // 说话者标识
    public string displayName; // 显示的名字
    public Canvas canvas; // 场景中的静态画布
    public Animator animator; // 对话框开关动画
    public TMP_Text speakerName; // 中文名字文字
    public TMP_Text dialogueText; // 中文正文文字
    public GameObject continueArrow; // 继续阅读的按钮
}

// 播放对话文字并处理逐句推进，由 DialogueGM 协调城市场景的交互。
public class DialogueManager : MonoBehaviour, IPointerClickHandler
{
    public Text nameText; // Test 场景使用的旧版名字文字
    public Text dialogueText; // Test 场景使用的旧版正文文字
    public Animator animator; // Test 场景的开关动画
    public DialogueViewBinding[] views; // 城市场景预先绑定的角色界面

    private readonly Queue<string> sentences = new Queue<string>(); // Test 场景等待显示的句子
    private DialogueLine[] lines; // 当前城市对话段落
    private int lineIndex; // 下一句的位置
    private Action onFinished; // 完整读完段落后执行的行为
    private DialogueViewBinding activeView; // 当前显示或收起的界面
    private string currentSentence; // 正在显示的完整句子
    private bool isTyping; // 当前是否正在逐字显示
    private bool isOpen; // 当前是否接受继续输入
    private bool isSwitching; // 当前是否正在切换界面

    public bool IsBusy => isOpen || isSwitching; // 文字和界面是否仍在播放

    // 点击当前对话框时继续阅读，功能按钮保留各自的点击行为。
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || activeView == null) return;
        Transform target = eventData.pointerPressRaycast.gameObject.transform; // 本次点击的界面对象
        if (target.IsChildOf(activeView.canvas.transform)) DisplayNextSentence();
    }

    // 切换到当前角色的 TMP 对话界面。
    public DialogueViewBinding GetView(string speakerId)
    {
        return Array.Find(views, view => view.speakerId == speakerId);
    }

    // 开始一个城市对话段落，取消上一次未完成的行为。
    public void PlayLines(DialogueLine[] dialogueLines, Action finished)
    {
        StopDialogue();
        lines = dialogueLines;
        lineIndex = 0;
        onFinished = finished;
        isOpen = true;
        DisplayNextSentence();
    }

    // 打开对话并立即播放第一句话。
    public void StartDialogue(Dialogue dialogue)
    {
        StopDialogue();
        isOpen = true;
        animator.SetBool("IsOpen", true);
        nameText.text = dialogue.name;
        foreach (string sentence in dialogue.sentences) sentences.Enqueue(sentence);
        DisplayNextSentence();
    }

    // 补全当前文字或显示下一句话，句子结束后关闭对话。
    public void DisplayNextSentence()
    {
        if (!isOpen || isSwitching) return;
        if (isTyping)
        {
            StopAllCoroutines();
            if (activeView != null) activeView.dialogueText.maxVisibleCharacters = int.MaxValue;
            else dialogueText.text = currentSentence;
            isTyping = false;
            return;
        }
        if (lines != null)
        {
            if (lineIndex == lines.Length)
            {
                Action finished = onFinished;
                lines = null;
                onFinished = null;
                isOpen = false;
                activeView.continueArrow.SetActive(false);
                finished();
                return;
            }
            StartCoroutine(ShowLine(lines[lineIndex++]));
            return;
        }
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }
        currentSentence = sentences.Dequeue();
        StartCoroutine(TypeSentence(currentSentence));
    }

    // 切换到本句说话者，再播放这句正文。
    private IEnumerator ShowLine(DialogueLine line)
    {
        isSwitching = true;
        yield return ShowView(GetView(line.speakerId), line.text, true);
        isSwitching = false;
        currentSentence = line.text;
        yield return TypeSentence(currentSentence);
    }

    // 先收起上一位说话者，再打开下一位，避免两个完整对话框重叠。
    private IEnumerator ShowView(DialogueViewBinding view, string text, bool canContinue)
    {
        if (activeView != null && activeView != view && activeView.canvas.gameObject.activeSelf)
        {
            activeView.continueArrow.SetActive(false);
            activeView.animator.SetBool("IsOpen", false);
            yield return WaitForState(activeView.animator, "Dialogue_Close");
            activeView.canvas.gameObject.SetActive(false);
        }
        bool opening = activeView != view || !view.canvas.gameObject.activeSelf;
        activeView = view;
        view.speakerName.text = view.displayName;
        view.dialogueText.text = text;
        view.dialogueText.maxVisibleCharacters = canContinue ? 0 : int.MaxValue;
        view.continueArrow.SetActive(canContinue);
        view.canvas.gameObject.SetActive(true);
        if (opening)
        {
            view.animator.Rebind();
            view.animator.Update(0f);
        }
        view.animator.SetBool("IsOpen", true);
        if (opening || view.animator.IsInTransition(0) || !view.animator.GetCurrentAnimatorStateInfo(0).IsName("DialogueBox_Open"))
            yield return WaitForState(view.animator, "DialogueBox_Open");
    }

    // 等待动画状态及其过渡播放完成。
    private static IEnumerator WaitForState(Animator target, string state)
    {
        yield return null;
        while (target.IsInTransition(0) || !target.GetCurrentAnimatorStateInfo(0).IsName(state))
            yield return null;
    }

    // 回到交谈对象的选项界面，不再次播放完整段落。
    public void ShowOptions(string speakerId, string text, Action shown)
    {
        StopDialogue();
        StartCoroutine(ShowOptionsView(speakerId, text, shown));
    }

    // 等待界面切换完成后显示业务选项。
    private IEnumerator ShowOptionsView(string speakerId, string text, Action shown)
    {
        isSwitching = true;
        yield return ShowView(GetView(speakerId), text, false);
        isSwitching = false;
        shown();
    }

    // 在保持 TMP 排版的情况下逐字显示，兼容 Test 场景的旧版文字。
    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        if (activeView != null)
        {
            TMP_Text text = activeView.dialogueText;
            text.text = sentence;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            int characterCount = text.textInfo.characterCount;
            for (int visibleCharacters = 1; visibleCharacters <= characterCount; visibleCharacters++)
            {
                text.maxVisibleCharacters = visibleCharacters;
                yield return null;
            }
        }
        else
        {
            dialogueText.text = "";
            foreach (char letter in sentence)
            {
                dialogueText.text += letter;
                yield return null;
            }
        }
        isTyping = false;
    }

    // 中止播放并清空待播内容，避免离开或切换对话后继续写字。
    public void StopDialogue()
    {
        StopAllCoroutines();
        sentences.Clear();
        lines = null;
        onFinished = null;
        isTyping = false;
        isOpen = false;
        isSwitching = false;
    }

    // 结束播放并通知城市场景关闭当前角色的界面。
    private void EndDialogue()
    {
        StopDialogue();
        animator.SetBool("IsOpen", false);
    }

    // 播放当前对话框的收起动画，未配置动画时直接隐藏。
    public void CloseView()
    {
        StopDialogue();
        if (activeView == null) return;
        activeView.continueArrow.SetActive(false);
        activeView.animator.SetBool("IsOpen", false);
        StartCoroutine(HideAfterClose());
    }

    // 等待关闭状态的过渡完成后隐藏画布。
    private IEnumerator HideAfterClose()
    {
        yield return WaitForState(activeView.animator, "Dialogue_Close");
        activeView.canvas.gameObject.SetActive(false);
        activeView = null;
    }

    // 组件停用时同步停止对话播放。
    private void OnDisable()
    {
        StopDialogue();
        if (activeView != null) activeView.canvas.gameObject.SetActive(false);
        activeView = null;
        if (animator != null) animator.SetBool("IsOpen", false);
    }
}
