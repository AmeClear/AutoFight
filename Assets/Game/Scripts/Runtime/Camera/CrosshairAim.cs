using UnityEngine;

/// <summary>
/// 将 UIAim 的 PointCenter 提供给游戏摄像机与武器瞄准。
/// </summary>
public static class CrosshairAim
{
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

    /// <summary>
    /// PointCenter 的屏幕像素坐标。未绑定时返回屏幕中心。
    /// </summary>
    public static Vector2 GetScreenPoint()
    {
        if (_pointCenter == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        var eventCamera = ResolveCanvasCamera(_canvas);
        return RectTransformUtility.WorldToScreenPoint(eventCamera, _pointCenter.position);
    }

    /// <summary>
    /// PointCenter 在游戏摄像机视口中的位置（0-1，左下为原点）。
    /// </summary>
    public static Vector3 GetViewportPoint(Camera worldCamera)
    {
        if (worldCamera == null)
            return new Vector3(0.5f, 0.5f, 0f);

        return worldCamera.ScreenToViewportPoint(GetScreenPoint());
    }

    /// <summary>
    /// 从游戏摄像机穿过准星的世界射线。
    /// </summary>
    public static Ray GetAimRay(Camera worldCamera)
    {
        if (worldCamera == null)
            return new Ray(Vector3.zero, Vector3.forward);

        var screen = GetScreenPoint();
        return worldCamera.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
    }

    private static Camera ResolveCanvasCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        var root = canvas.rootCanvas;
        if (root != null && root.worldCamera != null)
            return root.worldCamera;

        return null;
    }
}
