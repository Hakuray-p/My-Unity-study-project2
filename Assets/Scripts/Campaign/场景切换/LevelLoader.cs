using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 场景转场：播完遮罩动画再加载目标场景
public class LevelLoader : MonoBehaviour
{
    public Animator transition; // 黑幕动画，接 Crossfade 上的 Animator
    public Animator wipe; // 左右遮罩动画，接 Define Canvas 上的 Animator
    public float transitionTime = 2f; // 等动画播完的时间
    static bool playRevealOnStart; // 标记新场景进入时要播遮罩拉开

    // 进入新场景时把遮罩拉开
    void Start()
    {
        if (!playRevealOnStart)
        {
            return;
        }

        playRevealOnStart = false;
        wipe.Play("My Wipe Reveal");
    }

    // 播转场动画，动画播完再加载目标场景
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadLevel(sceneName));
    }

    // 遮罩合拢盖住画面，等动画播完再切场景，回主菜单不做遮罩
    IEnumerator LoadLevel(string sceneName)
    {
        transition.SetTrigger("Start");
        playRevealOnStart = sceneName != "GameMenu";
        if (playRevealOnStart)
        {
            wipe.Play("My Wipe Cover");
        }

        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneName);
    }
}