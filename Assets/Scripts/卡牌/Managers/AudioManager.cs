using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 战斗音效播放，按类型播对应的音效
public class AudioManager : MonoBehaviour
{
    public AudioClip draw; // 抽卡音效
    public AudioClip shuffle; // 洗牌音效
    public AudioClip destroy; // 卡牌被消灭音效
    public AudioClip damage; // 受伤音效
    public AudioClip heal; // 治疗音效
    public AudioClip nextTurn; // 回合切换音效
    public AudioClip effect; // 效果触发音效
    public AudioClip summon; // 召唤音效
    // 预留的初始化入口
    public void Init()
    {

    }

    // 在相机位置播一段音效
    public void PlayAudio(AudioClip audioClip)
    {
        if (audioClip == null)
        {
            return;
        }
        // 播放音效
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
    }

    // 按音效类型播对应的音效
    public void PlayAudio(AudioType audioType)
    {
        switch (audioType)
        {
            case AudioType.DrawCard:
                AudioSource.PlayClipAtPoint(draw, Camera.main.transform.position);
                break;
            case AudioType.Shuffle:
                AudioSource.PlayClipAtPoint(shuffle, Camera.main.transform.position);
                break;
            case AudioType.Destroy:
                AudioSource.PlayClipAtPoint(destroy, Camera.main.transform.position);
                break;
            case AudioType.Damage:
                AudioSource.PlayClipAtPoint(damage, Camera.main.transform.position);
                break;
            case AudioType.Heal:
                AudioSource.PlayClipAtPoint(heal, Camera.main.transform.position);
                break;
            case AudioType.NextTurn:
                AudioSource.PlayClipAtPoint(nextTurn, Camera.main.transform.position);
                break;
            case AudioType.Effect:
                AudioSource.PlayClipAtPoint(effect, Camera.main.transform.position);
                break;
            case AudioType.Summon:
                AudioSource.PlayClipAtPoint(summon, Camera.main.transform.position);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(audioType), audioType, null);
        }
    }
}
