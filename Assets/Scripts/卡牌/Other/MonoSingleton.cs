using System.Collections;
using System.Collections.Generic;
//using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Mono单例模板
/// </summary>
/// <typeparam name="T">单例类型</typeparam>
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance; // 单例实例
    public static T Ins
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
            }
            return instance;
        }
    }

    // 场景里出现重复单例时销毁自己
    private void Awake()
    {
        if (Ins != this)
        {
            Destroy(gameObject);
        }
    }

}
