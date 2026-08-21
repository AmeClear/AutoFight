using System;
using System.Collections.Generic;
using GAS.Runtime;
using GameEvent;
using UnityEngine;

/// <summary>
/// Actor 上的武器持有器：管理武器实例、当前装备、开火许可与换弹。
/// <para>开火成功后可激活 GAS 技能；射线参数通过 <see cref="BuildFireContext"/> 提供给命中层。</para>
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class WeaponHolder : MonoBehaviour, IAbilityRayProvider
{
    [Header("初始武器")]
    [SerializeField] private WeaponDefinition[] startingWeapons;

    [Header("枪口")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Vector3 muzzleFallbackOffset = new Vector3(0f, 1.4f, 0.4f);

    [Header("瞄准")]
    [SerializeField] [Tooltip("TPS 下从屏幕准星打出摄像机射线，再让枪口指向该落点。关闭则沿枪口 forward。")]
    private bool aimFromCrosshair = true;
    [SerializeField] [Tooltip("准星射线使用的摄像机。留空则使用 Camera.main。")]
    private Camera aimCamera;
    [SerializeField] [Tooltip("准星射线检测的层级。应包含环境和可命中目标，否则准星会穿墙。")]
    private LayerMask aimMask = ~0;

    [Header("GAS")]
    [SerializeField] private bool activateAbilityOnFire = true;

    private static readonly RaycastHit[] AimHits = new RaycastHit[16];

    private readonly List<WeaponInstance> _weapons = new List<WeaponInstance>();
    private AbilitySystemComponent _asc;
    private Actor _actor;
    private int _currentIndex = -1;
    private int _nextInstanceId = 1;
    private bool _fireHeld;
    private bool _hasPreparedFireContext;
    private WeaponFireContext _preparedFireContext;
    private readonly WeaponRecoil _recoil = new WeaponRecoil();

    public Actor Owner => _actor;
    public WeaponInstance CurrentWeapon =>
        _currentIndex >= 0 && _currentIndex < _weapons.Count ? _weapons[_currentIndex] : null;

    public IReadOnlyList<WeaponInstance> Weapons => _weapons;
    public Transform Muzzle => muzzle;
    public bool IsFireHeld => _fireHeld;
    public WeaponRecoil Recoil => _recoil;

    private void Awake()
    {
        _actor = GetComponent<Actor>();
        _asc = GetComponent<AbilitySystemComponent>();
        _recoil.Bind(GetComponent<MoveComponent>());
        EnsureMuzzle();
        CreateStartingWeapons();
    }

    private void Update()
    {
        TickCurrentWeapon(Time.deltaTime);

        var now = Time.time;
        var recoilMoved = _recoil.Tick(
            CurrentWeapon != null ? CurrentWeapon.Definition : null,
            Time.deltaTime,
            now,
            _fireHeld);

        if (_fireHeld && CurrentWeapon != null && CurrentWeapon.Definition.FireMode == WeaponFireMode.FullAuto)
            TryFire();
        else if (recoilMoved)
            PushRecoilView();
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

        var context = BuildFireContext();
        _preparedFireContext = context;
        _hasPreparedFireContext = true;

        if (activateAbilityOnFire && !string.IsNullOrEmpty(weapon.Definition.AbilityName))
        {
            GrantWeaponAbility(weapon.Definition);
            if (_asc == null || !_asc.TryActivateAbility(weapon.Definition.AbilityName))
                return false;
        }

        weapon.ConsumeShot(time);
        if (_recoil.Kick(weapon.Definition, IsAiming(), time))
            PushRecoilView();
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

        _recoil.ResetBurst();

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

        GrantWeaponAbility(definition);
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
        _recoil.ResetBurst();

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
    /// 供 <see cref="CatchRay3D"/> 读取本次开火射线。优先返回刚准备好的上下文。
    /// </summary>
    public bool TryGetAbilityRay(out AbilityRayQuery ray)
    {
        var context = _hasPreparedFireContext ? _preparedFireContext : BuildFireContext();
        if (context.Direction.sqrMagnitude < 0.0001f)
        {
            ray = default;
            return false;
        }

        var muzzleToAim = context.AimPoint - context.Origin;
        var muzzleRange = muzzleToAim.sqrMagnitude > 0.0001f
            ? muzzleToAim.magnitude
            : context.Range;
        var cameraRange = context.HasCameraRay
            ? Vector3.Distance(context.CameraOrigin, context.AimPoint)
            : 0f;

        ray = new AbilityRayQuery
        {
            Origin = context.Origin,
            Direction = context.Direction,
            CameraOrigin = context.CameraOrigin,
            CameraDirection = context.CameraDirection,
            AimPoint = context.AimPoint,
            Range = (muzzleRange > 0.01f ? muzzleRange : context.Range) + 0.05f,
            CameraRange = cameraRange > 0.01f ? cameraRange : context.Range,
            Radius = context.RayRadius,
            Mask = context.HitMask,
            HasCameraRay = context.HasCameraRay
        };
        return true;
    }

    /// <summary>
    /// 构建当前武器的射线开火上下文。
    /// <para>
    /// TPS：摄像机穿过屏幕正中准星得到目标点；枪口射线指向同一目标点。
    /// 命中沿枪口到目标点检测。
    /// </para>
    /// </summary>
    public WeaponFireContext BuildFireContext()
    {
        EnsureMuzzle();
        var weapon = CurrentWeapon;
        var range = weapon != null ? weapon.Definition.Range : 0f;
        var origin = muzzle.position;
        ResolveAim(origin, range, out var direction, out var aimPoint, out var cameraRay, out var aimNormal,
            out var hasAimHit, out var hasCameraRay);

        return new WeaponFireContext
        {
            Owner = _actor,
            Weapon = weapon,
            Origin = origin,
            Direction = direction,
            AimPoint = aimPoint,
            CameraOrigin = cameraRay.origin,
            CameraDirection = cameraRay.direction,
            AimNormal = aimNormal,
            HasAimHit = hasAimHit,
            HasCameraRay = hasCameraRay,
            Range = range,
            RayRadius = weapon != null ? weapon.Definition.RayRadius : 0f,
            HitMask = weapon != null ? (LayerMask)(weapon.Definition.HitMask | aimMask) : aimMask
        };
    }

    private void ResolveAim(Vector3 muzzleOrigin, float range, out Vector3 direction, out Vector3 aimPoint,
        out Ray cameraRay, out Vector3 aimNormal, out bool hasAimHit, out bool hasCameraRay)
    {
        cameraRay = new Ray(muzzleOrigin, muzzle.forward);
        aimNormal = Vector3.up;
        hasAimHit = false;
        hasCameraRay = false;

        if (!aimFromCrosshair)
        {
            direction = muzzle.forward;
            aimPoint = muzzleOrigin + direction * range;
            return;
        }

        var cam = ResolveAimCamera();
        if (cam == null)
        {
            direction = muzzle.forward;
            aimPoint = muzzleOrigin + direction * range;
            return;
        }

        cameraRay = CrosshairAim.GetAimRay(cam);
        hasCameraRay = true;
        aimPoint = cameraRay.origin + cameraRay.direction * range;

        var hitCount = Physics.RaycastNonAlloc(cameraRay, AimHits, range, aimMask, QueryTriggerInteraction.Ignore);
        var bestDistance = float.MaxValue;
        for (var i = 0; i < hitCount; i++)
        {
            var hit = AimHits[i];
            if (hit.collider == null || IsOwnedCollider(hit.collider))
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            aimPoint = hit.point;
            aimNormal = hit.normal;
            hasAimHit = true;
        }

        var toAim = aimPoint - muzzleOrigin;
        if (toAim.sqrMagnitude < 0.0001f || Vector3.Dot(toAim, cameraRay.direction) <= 0f)
            direction = cameraRay.direction;
        else
            direction = toAim.normalized;
    }

    private Camera ResolveAimCamera()
    {
        if (aimCamera != null)
            return aimCamera;

        var mode = Camera.main != null ? Camera.main.GetComponent<CameraModeController>() : null;
        if (mode != null && mode.ActiveCamera != null)
            return mode.ActiveCamera;

        return Camera.main;
    }

    private bool IsAiming()
    {
        var cam = ResolveAimCamera();
        var mode = cam != null ? cam.GetComponent<CameraModeController>() : null;
        return mode != null && mode.IsAiming;
    }

    private void PushRecoilView()
    {
        var move = GetComponent<MoveComponent>();
        var cam = ResolveAimCamera();
        var mode = cam != null ? cam.GetComponent<CameraModeController>() : null;
        if (move == null || mode?.ActiveController == null)
            return;

        mode.ActiveController.SetViewRotation(move.ViewYaw, move.ViewPitch);
        mode.ActiveController.ApplyViewNow();
    }

    private bool IsOwnedCollider(Collider col)
    {
        if (_actor == null)
            return col.transform.IsChildOf(transform);

        return col.transform == _actor.transform || col.transform.IsChildOf(_actor.transform);
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

    private void GrantWeaponAbility(WeaponDefinition definition)
    {
        if (_asc == null || definition == null)
            return;

        var asset = definition.AbilityAsset;
        if (asset == null)
            return;

        var abilityName = definition.AbilityName;
        if (string.IsNullOrEmpty(abilityName) || _asc.AbilityContainer.HasAbility(abilityName))
            return;

        var abilityType = asset.AbilityType();
        if (abilityType == null)
            return;

        if (Activator.CreateInstance(abilityType, asset) is AbstractAbility ability)
            _asc.GrantAbility(ability);
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
