using UnityEngine;

/// <summary>
/// 一次开火的射线上下文，供后续 GAS Catcher / 弹道使用。
/// <para>
/// 摄像机射线穿过屏幕准星得到 <see cref="AimPoint"/>；
/// 枪口射线从 <see cref="Origin"/> 指向同一 <see cref="AimPoint"/>。
/// </para>
/// </summary>
public struct WeaponFireContext
{
    public Actor Owner;
    public WeaponInstance Weapon;
    public Vector3 Origin;
    public Vector3 Direction;
    public Vector3 AimPoint;
    public Vector3 CameraOrigin;
    public Vector3 CameraDirection;
    public Vector3 AimNormal;
    public bool HasAimHit;
    public bool HasCameraRay;
    public float Range;
    public float RayRadius;
    public LayerMask HitMask;
}
