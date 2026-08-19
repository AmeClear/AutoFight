using UnityEngine;

/// <summary>
/// 一次开火的射线上下文，供后续 GAS Catcher / 弹道使用。
/// </summary>
public struct WeaponFireContext
{
    public Actor Owner;
    public WeaponInstance Weapon;
    public Vector3 Origin;
    public Vector3 Direction;
    public float Range;
    public float RayRadius;
    public LayerMask HitMask;
}
