using GameEvent;
using UnityEngine;

/// <summary>
/// 武器实例装备完成。
/// </summary>
public class WeaponEquippedEvent : IGameEvent
{
    public int ActorId;
    public Actor Actor;
    public WeaponInstance Weapon;
}

/// <summary>
/// 当前武器切换。
/// </summary>
public class WeaponSwitchedEvent : IGameEvent
{
    public int ActorId;
    public Actor Actor;
    public WeaponInstance PreviousWeapon;
    public WeaponInstance CurrentWeapon;
}

/// <summary>
/// 武器开火成功（弹药已扣除）。
/// </summary>
public class WeaponFiredEvent : IGameEvent
{
    public int ActorId;
    public Actor Actor;
    public WeaponInstance Weapon;
    public WeaponFireContext FireContext;
}

/// <summary>
/// 武器弹药或换弹状态变化。
/// </summary>
public class WeaponAmmoChangedEvent : IGameEvent
{
    public int ActorId;
    public Actor Actor;
    public WeaponInstance Weapon;
    public int Magazine;
    public int Reserve;
    public bool IsReloading;
}

/// <summary>
/// 武器开始换弹。
/// </summary>
public class WeaponReloadStartedEvent : IGameEvent
{
    public int ActorId;
    public Actor Actor;
    public WeaponInstance Weapon;
    public float Duration;
}

/// <summary>
/// 武器换弹完成。
/// </summary>
public class WeaponReloadCompletedEvent : IGameEvent
{
    public int ActorId;
    public Actor Actor;
    public WeaponInstance Weapon;
}
