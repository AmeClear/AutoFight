using UnityEngine;

/// <summary>
/// TPS 瞄准以游戏摄像机屏幕中心为准。UIAim 准星只是画在中心的 HUD，不参与射线换算。
/// </summary>
public static class CrosshairAim
{
    public static readonly Vector3 ViewportCenter = new Vector3(0.5f, 0.5f, 0f);

    private static RectTransform _pointCenter;
    private static Canvas _canvas;

    public static RectTransform PointCenter => _pointCenter;
    public static bool IsBound => _pointCenter != null;

    public static void Bind(RectTransform pointCenter, Canvas canvas)
    {
        _pointCenter = pointCenter;
        _canvas = canvas;
    }

    public static void Unbind(RectTransform pointCenter = null)
    {
        if (pointCenter != null && _pointCenter != pointCenter)
            return;

        _pointCenter = null;
        _canvas = null;
    }

    public static Vector2 GetScreenPoint(Camera worldCamera)
    {
        if (worldCamera != null)
            return worldCamera.ViewportToScreenPoint(ViewportCenter);

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    public static Vector3 GetViewportPoint(Camera worldCamera)
    {
        return ViewportCenter;
    }

    /// <summary>
    /// 游戏摄像机穿过屏幕正中（准星）的世界射线。
    /// </summary>
    public static Ray GetAimRay(Camera worldCamera)
    {
        if (worldCamera == null)
            return new Ray(Vector3.zero, Vector3.forward);

        return worldCamera.ViewportPointToRay(ViewportCenter);
    }
}
