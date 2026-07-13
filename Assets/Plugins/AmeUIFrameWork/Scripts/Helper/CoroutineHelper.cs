using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineHelper : MonoSingleton<CoroutineHelper>
{
    public void StartCoroutineMono(IEnumerator routine)
    {
        StartCoroutine(routine);
    }
    // 这个函数将会等待协程完成
   public void WaitForCoroutine(IEnumerator routine, Action callback)
    {
        // 创建一个新的协程来等待MyCoroutine完成
        StartCoroutine(WaitForOtherCoroutineToFinish(routine, callback));
    }
    // 这个协程将等待其他协程完成
    public IEnumerator WaitForOtherCoroutineToFinish(IEnumerator routine, Action callback)
    {
        // 等待给定的协程完成
        yield return StartCoroutine(routine);

        // 协程已完成，继续执行其他代码
        callback?.Invoke();
    }
}
