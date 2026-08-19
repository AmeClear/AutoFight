using System.Collections.Generic;
using GAS.Runtime;
using GameEvent;
using UnityEngine;

/// <summary>
/// Actor 上的武器持有器：管理武器实例、当前装备、开火许可与换弹。
/// <para>开火成功后可激活 GAS 技能；射线参数通过 <see cref="BuildFireContext"/> 提供给命中层。</para>
/// </summary>
[DisallowMultipleComponent]
public class WeaponHolder : MonoBehaviour
{
    [Header("初始武器")]
    [SerializeField] private WeaponDefinition[] startingWeapons;

    [Header("枪口")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Vector3 muzzleFallbackOffset = new Vector3(0f, 1.4f, 0.4f);

    [Header("GAS")]
    [SerializeField] private bool activateAbilityOnFire = true;

    private readonly List<WeaponInstance> _weapons = new List<WeaponInstance>();
    private AbilitySystemComponent _asc;
    private Actor _actor;
    private int _currentIndex = -1;
    private int _nextInstanceId = 1;
    private bool _fireHeld;

    public Actor Owner => _actor;
    public WeaponInstance CurrentWeapon =>
        _currentIndex >= 0 && _currentIndex < _weapons.Count ? _weapons[_currentIndex] : null;

    public IReadOnlyList<WeaponInstance> Weapons => _weapons;
    public Transform Muzzle => muzzle;
    public bool IsFireHeld => _fireHeld;

    private void Awake()
    {
        _actor = GetComponent<Actor>();
        _asc = GetComponent<AbilitySystemComponent>();
        EnsureMuzzle();
        CreateStartingWeapons();
    }

    private void Update()
    {
        TickCurrentWeapon(Time.deltaTime);

        if (_fireHeld && CurrentWeapon != null && CurrentWeapon.Definition.FireMode == WeaponFireMode.FullAuto)
            TryFire();
    }

    /// <summary>
    /// 按下/松开开火键。全自动武器在按住期间由 Update 连发。
    /// </summary>
    public void SetFireHeld(bool held)
    {
        _fireHeld = held;
    }

    /// <summary>
    /// 尝试开火：检查弹药与射速，成功则激活 GAS 技能并扣弹。
    /// </summary>
    public bool TryFire()
    {
        var weapon = CurrentWeapon;
        if (weapon == null)
            return false;

        var time = Time.time;
        if (!weapon.CanFire(time))
        {
            if (weapon.IsMagazineEmpty && weapon.Definition.AutoReloadOnEmpty)
                TryReload();
            return false;
        }

        if (activateAbilityOnFire && !string.IsNullOrEmpty(weapon.Definition.AbilityName))
        {
            if (_asc == null || !_asc.TryActivateAbility(weapon.Definition.AbilityName))
                return false;
        }

        weapon.ConsumeShot(time);
        var context = BuildFireContext();
        PublishFired(weapon, context);
        PublishAmmoChanged(weapon);

        if (weapon.IsMagazineEmpty && weapon.Definition.AutoReloadOnEmpty)
            TryReload();

        return true;
    }

    /// <summary>
    /// 尝试换弹。
    /// </summary>
    public bool TryReload()
    {
        var weapon = CurrentWeapon;
        if (weapon == null || !weapon.BeginReload())
            return false;

        EventBus.Publish(new WeaponReloadStartedEvent
        {
            ActorId = GetActorId(),
            Actor = _actor,
            Weapon = weapon,
            Duration = weapon.Definition.ReloadDuration
        });
        PublishAmmoChanged(weapon);
        return true;
    }

    /// <summary>
    /// 根据配置创建一把运行时武器并加入持有列表。
    /// </summary>
    public WeaponInstance AddWeapon(WeaponDefinition definition, bool equip = false)
    {
        if (definition == null)
            return null;

        var instance = new WeaponInstance(definition, _nextInstanceId++);
        _weapons.Add(instance);

        EventBus.Publish(new WeaponEquippedEvent
        {
            ActorId = GetActorId(),
            Actor = _actor,
            Weapon = instance
        });

        if (equip || CurrentWeapon == null)
            EquipAt(_weapons.Count - 1);

        return instance;
    }

    /// <summary>
    /// 装备指定下标的武器。
    /// </summary>
    public bool EquipAt(int index)
    {
        if (index < 0 || index >= _weapons.Count)
            return false;

        if (_currentIndex == index)
            return true;

        var previous = CurrentWeapon;
        previous?.CancelReload();
        _currentIndex = index;

        EventBus.Publish(new WeaponSwitchedEvent
        {
            ActorId = GetActorId(),
            Actor = _actor,
            PreviousWeapon = previous,
            CurrentWeapon = CurrentWeapon
        });
        PublishAmmoChanged(CurrentWeapon);
        return true;
    }

    /// <summary>
    /// 切换到下一把武器。
    /// </summary>
    public bool SwitchNext()
    {
        if (_weapons.Count <= 1)
            return false;

        return EquipAt((_currentIndex + 1) % _weapons.Count);
    }

    /// <summary>
    /// 构建当前武器的射线开火上下文。
    /// </summary>
    public WeaponFireContext BuildFireContext()
    {
        EnsureMuzzle();
        var weapon = CurrentWeapon;
        return new WeaponFireContext
        {
            Owner = _actor,
            Weapon = weapon,
            Origin = muzzle.position,
            Direction = muzzle.forward,
            Range = weapon != null ? weapon.Definition.Range : 0f,
            RayRadius = weapon != null ? weapon.Definition.RayRadius : 0f,
            HitMask = weapon != null ? weapon.Definition.HitMask : (LayerMask)~0
        };
    }

    private void TickCurrentWeapon(float deltaTime)
    {
        var weapon = CurrentWeapon;
        if (weapon == null)
            return;

        if (!weapon.Tick(deltaTime))
            return;

        EventBus.Publish(new WeaponReloadCompletedEvent
        {
            ActorId = GetActorId(),
            Actor = _actor,
            Weapon = weapon
        });
        PublishAmmoChanged(weapon);
    }

    private void CreateStartingWeapons()
    {
        if (startingWeapons == null)
            return;

        foreach (var definition in startingWeapons)
        {
            if (definition == null)
                continue;
            AddWeapon(definition, CurrentWeapon == null);
        }
    }

    private void EnsureMuzzle()
    {
        if (muzzle != null)
            return;

        var found = transform.Find("Muzzle");
        if (found != null)
        {
            muzzle = found;
            return;
        }

        var muzzleObject = new GameObject("Muzzle");
        muzzle = muzzleObject.transform;
        muzzle.SetParent(transform, false);
        muzzle.localPosition = muzzleFallbackOffset;
        muzzle.localRotation = Quaternion.identity;
    }

    private int GetActorId()
    {
        return _actor != null ? _actor.GetInstanceID() : GetInstanceID();
    }

    private void PublishFired(WeaponInstance weapon, WeaponFireContext context)
    {
        EventBus.Publish(new WeaponFiredEvent
        {
            ActorId = GetActorId(),
            Actor = _actor,
            Weapon = weapon,
            FireContext = context
        });
    }

    private void PublishAmmoChanged(WeaponInstance weapon)
    {
        if (weapon == null)
            return;

        EventBus.Publish(new WeaponAmmoChangedEvent
        {
            ActorId = GetActorId(),
            Actor = _actor,
            Weapon = weapon,
            Magazine = weapon.Magazine,
            Reserve = weapon.Reserve,
            IsReloading = weapon.IsReloading
        });
    }
}
