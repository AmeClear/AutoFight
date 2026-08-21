using UnityEngine;

/// <summary>
/// 把 UIAim 的 PointCenter 注册到 <see cref="CrosshairAim"/>，供 TPS 摄像机与开火射线使用。
/// </summary>
[DisallowMultipleComponent]
public class UIAimBinder : MonoBehaviour
{
    private RectTransform _boundPoint;

    private void OnEnable()
    {
        Bind();
    }

    private void Start()
    {
        Bind();
    }

    private void OnDisable()
    {
        CrosshairAim.Unbind(_boundPoint);
        _boundPoint = null;
    }

    private void Bind()
    {
        var data = GetComponent<UIAimDataComponent>();
        if (data == null || data.PointCenterRectTransform == null)
            return;

        _boundPoint = data.PointCenterRectTransform;
        CrosshairAim.Bind(_boundPoint, GetComponent<Canvas>());
    }
}
