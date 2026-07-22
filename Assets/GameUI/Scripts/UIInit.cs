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
        UIModule.Instance.PreLoadWindow<BarUI>();
        UIModule.Instance.PreLoadWindow<UISkill>();
        UIModule.Instance.PopUpWindow<BarUI>();
        UIModule.Instance.PopUpWindow<UISkill>();

    }

    private GameObject GetUIView(string name)
    {
        // 异步加载
        // 文件名（不带扩展名）
        var address = ABAddress.GetByFileName(name);
        var prefab = ABLoader.Instance.LoadAssetSync<GameObject>(address);
        var obj = Instantiate(prefab, UIRoot);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = Vector3.zero;

        return obj;
    }
}
