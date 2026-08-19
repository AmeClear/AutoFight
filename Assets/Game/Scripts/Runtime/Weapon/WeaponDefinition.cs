using UnityEngine;

/// <summary>
/// 武器静态配置。运行时由 <see cref="WeaponInstance"/> 实例化，不直接参与开火结算。
/// </summary>
[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Weapon/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("标识")]
    [SerializeField] private string weaponId = "weapon_default";
    [SerializeField] private string displayName = "武器";

    [Header("开火")]
    [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.SemiAuto;
    [SerializeField] [Min(0.01f)] private float fireInterval = 0.2f;
    [SerializeField] private bool infiniteAmmo;
    [SerializeField] [Min(1)] private int magazineSize = 12;
    [SerializeField] [Min(0)] private int maxReserveAmmo = 60;
    [SerializeField] [Min(0.01f)] private float reloadDuration = 1.5f;
    [SerializeField] private bool autoReloadOnEmpty = true;

    [Header("命中参数（供射线 Catcher 读取）")]
    [SerializeField] [Min(0.1f)] private float range = 50f;
    [SerializeField] [Min(0f)] private float rayRadius;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("GAS")]
    [Tooltip("开火成功后激活的技能名。留空则只消耗弹药、不走 GAS。")]
    [SerializeField] private string abilityName = "GA_Fire";

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public WeaponFireMode FireMode => fireMode;
    public float FireInterval => fireInterval;
    public bool InfiniteAmmo => infiniteAmmo;
    public int MagazineSize => Mathf.Max(1, magazineSize);
    public int MaxReserveAmmo => Mathf.Max(0, maxReserveAmmo);
    public float ReloadDuration => Mathf.Max(0.01f, reloadDuration);
    public bool AutoReloadOnEmpty => autoReloadOnEmpty;
    public float Range => Mathf.Max(0.1f, range);
    public float RayRadius => Mathf.Max(0f, rayRadius);
    public LayerMask HitMask => hitMask;
    public string AbilityName => abilityName;
}
