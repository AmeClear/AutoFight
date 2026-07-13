using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Threading.Tasks;


public class DelayManager : Singleton<DelayManager>
{
    private GameObject goDelayUtil;
    private TaskBehaviour m_Task;
    private Coroutine m_CoroutineWait;
    //内部类
    class TaskBehaviour : MonoBehaviour
    {
        void OnDestroy()
        {
            DelayManager.Instance.Dispose();
        }
    }

    private DelayManager() { }
    // private SimpleObjectPool<TaskCompletionSource<bool>> poolTask =  new SimpleObjectPool<TaskCompletionSource<bool>>(() => new TaskCompletionSource<bool>(false),initCount:15);
    public override void OnSingletonInit()
    {
        GameObject go = new GameObject("#DelayUtil#");
        GameObject.DontDestroyOnLoad(go);
        m_Task = go.AddComponent<TaskBehaviour>();
        goDelayUtil = go;
    }

    public Coroutine WaitTime(float time, UnityAction callback)
    {
        return m_Task.StartCoroutine(Coroutine(time, callback));
    }

    public Coroutine WaitNextFrame(UnityAction callback)
    {
        return m_Task.StartCoroutine(Coroutine(callback));
    }

    //取消等待
    public void CancelWait(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            m_Task.StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    IEnumerator Coroutine(float time, UnityAction callback)
    {
        if (time > 0.01)
        {

            yield return new WaitForSeconds(time);
        }

        if (callback != null)
        {
            callback();
        }
    }
    IEnumerator Coroutine(UnityAction callback)
    {
        yield return 0;
        if (callback != null)
        {
            callback();
        }
    }

    public async Task WaitTimeAsync(float time)
    {

        var tcs = new TaskCompletionSource<bool>();
        if (m_Task == null || time <= 0)
        {
            tcs.SetResult(true);
        }
        else
        {
            m_CoroutineWait = m_Task.StartCoroutine(Coroutine(time, () =>
            {
                tcs.SetResult(true);
                m_CoroutineWait = null;
            }));
        }

        await tcs.Task;
    }

    public void CancelWaitAsync()
    {
        if (goDelayUtil != null)
        {
            GameObject.DestroyImmediate(goDelayUtil);
            goDelayUtil = null;
        }
        if (m_CoroutineWait != null)
        {
            m_Task.StopCoroutine(m_CoroutineWait);                
            m_CoroutineWait = null;
        }
    }
}

