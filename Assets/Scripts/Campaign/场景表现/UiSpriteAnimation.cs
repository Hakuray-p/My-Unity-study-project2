using UnityEngine;
using UnityEngine.UI;

// 播放界面图片预先配置的逐帧动画。
public sealed class UiSpriteAnimation : MonoBehaviour
{
    [SerializeField] private Image image; // 显示动画的图片
    [SerializeField] private Sprite[] frames; // 按播放顺序排列的动画帧
    [SerializeField] private float frameRate = 8f; // 每秒播放的帧数
    private float elapsed; // 本次显示后的播放时间
    private int frameIndex; // 当前显示的帧

    // 界面重新显示时从第一帧开始播放。
    private void OnEnable()
    {
        elapsed = 0f;
        frameIndex = 0;
        image.sprite = frames[0];
    }

    // 按界面时间循环播放图片。
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        int nextFrame = Mathf.FloorToInt(elapsed * frameRate) % frames.Length;
        if (nextFrame == frameIndex) return;
        frameIndex = nextFrame;
        image.sprite = frames[frameIndex];
    }
}
