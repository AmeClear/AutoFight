using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ABSystem;
using UnityEngine;

public class UIInit : MonoBehaviour
{
    public Transform UIRoot;
    // Start is called before the first frame update
    private async void Start()
    {
        // 分组常量
        await ABGroup.PreloadAsync(ABGroup.Prefab);
        UIModule.Instance.Initialize(GetUIView);
    }

    private GameObject GetUIView(string name)
    {
        // 异步加载
        var prefab = ABLoader.Instance.LoadAssetSync<GameObject>(name);
        var obj = Instantiate(prefab, UIRoot);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = Vector3.zero;

        return obj;
    }
}
