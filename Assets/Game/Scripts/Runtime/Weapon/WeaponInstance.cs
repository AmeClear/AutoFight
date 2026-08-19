using UnityEngine;

/// <summary>
/// 单把武器的运行时实例：弹药、射速间隔、换弹进度。
/// <para>由 <see cref="WeaponDefinition"/> 创建，挂在 <see cref="WeaponHolder"/> 上使用。</para>
/// </summary>
public class WeaponInstance
{
    private float _nextFireTime;
    private float _reloadRemain;

    public WeaponDefinition Definition { get; }
    public int InstanceId { get; }
    public int Magazine { get; private set; }
    public int Reserve { get; private set; }
    public bool IsReloading => _reloadRemain > 0f;
    public float ReloadRemain => _reloadRemain;
    public bool IsMagazineEmpty => Magazine <= 0;
    public bool IsMagazineFull => Magazine >= Definition.MagazineSize;

    public WeaponInstance(WeaponDefinition definition, int instanceId)
    {
        Definition = definition;
        InstanceId = instanceId;
        Magazine = definition.MagazineSize;
        Reserve = definition.InfiniteAmmo ? 0 : definition.MaxReserveAmmo;
    }

    /// <summary>
    /// 当前是否满足开火间隔、弹药且未在换弹。
    /// </summary>
    public bool CanFire(float currentTime)
    {
        if (Definition == null)
            return false;

        if (IsReloading)
            return false;

        if (currentTime < _nextFireTime)
            return false;

        if (Definition.InfiniteAmmo)
            return true;

        return Magazine > 0;
    }

    /// <summary>
    /// 扣除一发并进入射速间隔。调用前须 <see cref="CanFire"/> 为 true。
    /// </summary>
    public void ConsumeShot(float currentTime)
    {
        if (!Definition.InfiniteAmmo)
            Magazine = Mathf.Max(0, Magazine - 1);

        _nextFireTime = currentTime + Definition.FireInterval;
    }

    /// <summary>
    /// 开始换弹。弹匣已满、无备用弹或正在换弹时失败。
    /// </summary>
    public bool BeginReload()
    {
        if (Definition.InfiniteAmmo)
            return false;

        if (IsReloading || IsMagazineFull || Reserve <= 0)
            return false;

        _reloadRemain = Definition.ReloadDuration;
        return true;
    }

    /// <summary>
    /// 推进换弹计时。完成后装填弹匣。
    /// </summary>
    /// <returns>本帧是否刚完成换弹。</returns>
    public bool Tick(float deltaTime)
    {
        if (!IsReloading)
            return false;

        _reloadRemain -= deltaTime;
        if (_reloadRemain > 0f)
            return false;

        _reloadRemain = 0f;
        FinishReload();
        return true;
    }

    /// <summary>
    /// 取消换弹，不装填。
    /// </summary>
    public void CancelReload()
    {
        _reloadRemain = 0f;
    }

    public void AddReserve(int amount)
    {
        if (Definition.InfiniteAmmo || amount <= 0)
            return;

        Reserve = Mathf.Min(Definition.MaxReserveAmmo, Reserve + amount);
    }

    private void FinishReload()
    {
        var need = Definition.MagazineSize - Magazine;
        var loaded = Mathf.Min(need, Reserve);
        Magazine += loaded;
        Reserve -= loaded;
    }
}
