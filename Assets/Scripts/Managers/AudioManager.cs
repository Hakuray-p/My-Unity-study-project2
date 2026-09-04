using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip draw;
    public AudioClip shuffle;
    public AudioClip destroy;
    public AudioClip damage;
    public AudioClip heal;
    public AudioClip nextTurn;
    public AudioClip effect;
    public AudioClip summon;
    public void Init()
    {
        
    }

    public void PlayAudio(AudioClip audioClip)
    {
        if (audioClip == null)
        {
            return;
        }
        // 播放音效
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
    }

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
