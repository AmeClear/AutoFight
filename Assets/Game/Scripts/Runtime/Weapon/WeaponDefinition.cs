using UnityEngine;
using Sirenix.OdinInspector;
using GAS.Runtime;

/// <summary>
/// 武器静态配置。运行时由 <see cref="WeaponInstance"/> 实例化，不直接参与开火结算。
/// </summary>
[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Weapon/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [TabGroup("基本", "标识")]
    [BoxGroup("基本/标识/编号"), LabelText("武器 ID")]
    [Tooltip("武器的唯一标识，用于存档、装备查找与事件区分。建议使用稳定英文键，例如 rifle_01。")]
    [SerializeField] private string weaponId = "weapon_default";

    [BoxGroup("基本/标识/编号"), LabelText("显示名称")]
    [Tooltip("界面上展示的武器名称，可使用中文。")]
    [SerializeField] private string displayName = "武器";

    [TabGroup("基本", "开火")]
    [BoxGroup("基本/开火/模式"), LabelText("开火模式")]
    [Tooltip("SemiAuto：每次按下开火一次。FullAuto：按住期间按射速连发。")]
    [EnumToggleButtons]
    [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.SemiAuto;

    [BoxGroup("基本/开火/射速"), LabelText("开火间隔"), SuffixLabel("秒", true)]
    [Tooltip("两次有效开火之间的最短间隔。数值越小射速越快。")]
    [MinValue(0.01f)]
    [SerializeField] private float fireInterval = 0.2f;

    [BoxGroup("基本/开火/弹药"), LabelText("无限弹药"), ToggleLeft]
    [Tooltip("开启后不消耗弹匣与备用弹，换弹逻辑也不会生效。")]
    [SerializeField] private bool infiniteAmmo;

    [BoxGroup("基本/开火/弹药"), LabelText("弹匣容量"), HideIf("infiniteAmmo")]
    [Tooltip("单次装填后弹匣内最多可容纳的弹药数。")]
    [MinValue(1)]
    [SerializeField] private int magazineSize = 12;

    [BoxGroup("基本/开火/弹药"), LabelText("最大备用弹"), HideIf("infiniteAmmo")]
    [Tooltip("弹匣外可携带的备用弹药上限。换弹时从这里补入弹匣。")]
    [MinValue(0)]
    [SerializeField] private int maxReserveAmmo = 60;

    [BoxGroup("基本/开火/换弹"), LabelText("换弹时长"), SuffixLabel("秒", true), HideIf("infiniteAmmo")]
    [Tooltip("完成一次换弹所需时间。换弹期间无法开火。")]
    [MinValue(0.01f)]
    [SerializeField] private float reloadDuration = 1.5f;

    [BoxGroup("基本/开火/换弹"), LabelText("空仓自动换弹"), ToggleLeft, HideIf("infiniteAmmo")]
    [Tooltip("弹匣打空后是否自动开始换弹。")]
    [SerializeField] private bool autoReloadOnEmpty = true;

    [TabGroup("基本", "命中")]
    [BoxGroup("基本/命中/射线"), LabelText("射程"), SuffixLabel("米", true)]
    [Tooltip("射线检测的最大距离，供后续射线 Catcher 读取。")]
    [MinValue(0.1f)]
    [SerializeField] private float range = 50f;

    [BoxGroup("基本/命中/射线"), LabelText("射线半径"), SuffixLabel("米", true)]
    [Tooltip("0 为细射线（Raycast）。大于 0 时使用粗射线（SphereCast），半径越大越容易命中。")]
    [MinValue(0f)]
    [SerializeField] private float rayRadius;

    [BoxGroup("基本/命中/射线"), LabelText("命中层级")]
    [Tooltip("射线只检测这些 Layer 上的碰撞体。")]
    [SerializeField] private LayerMask hitMask = ~0;

    [TabGroup("基本", "GAS")]
    [BoxGroup("基本/GAS/技能"), LabelText("开火技能")]
    [Tooltip("开火成功后激活的 GAS 技能资产。会在装备时授予角色。优先于下方名称。")]
    [SerializeField] private AbilityAsset abilityAsset;

    [BoxGroup("基本/GAS/技能"), LabelText("激活技能名")]
    [Tooltip("开火成功后激活的 GAS 技能 UniqueName。开火技能为空时使用该名称。留空则只扣弹、不走 GAS。")]
    [SerializeField] private string abilityName = "GA_Fire";

    /// <summary>武器的唯一标识，用于存档、装备查找与事件区分。</summary>
    public string WeaponId => weaponId;

    /// <summary>界面上展示的武器名称。</summary>
    public string DisplayName => displayName;

    /// <summary>开火模式：单发或连发。</summary>
    public WeaponFireMode FireMode => fireMode;

    /// <summary>两次有效开火之间的最短间隔（秒）。</summary>
    public float FireInterval => fireInterval;

    /// <summary>是否不消耗弹药。</summary>
    public bool InfiniteAmmo => infiniteAmmo;

    /// <summary>弹匣容量。无限弹药时仍返回配置值。</summary>
    public int MagazineSize => Mathf.Max(1, magazineSize);

    /// <summary>弹匣外可携带的备用弹药上限。</summary>
    public int MaxReserveAmmo => Mathf.Max(0, maxReserveAmmo);

    /// <summary>完成一次换弹所需时间（秒）。</summary>
    public float ReloadDuration => Mathf.Max(0.01f, reloadDuration);

    /// <summary>弹匣打空后是否自动换弹。</summary>
    public bool AutoReloadOnEmpty => autoReloadOnEmpty;

    /// <summary>射线检测的最大距离（米）。</summary>
    public float Range => Mathf.Max(0.1f, range);

    /// <summary>粗射线半径。0 表示细射线。</summary>
    public float RayRadius => Mathf.Max(0f, rayRadius);

    /// <summary>射线命中的 Layer 掩码。</summary>
    public LayerMask HitMask => hitMask;

    /// <summary>开火时授予并激活的 GAS 技能资产。</summary>
    public AbilityAsset AbilityAsset => abilityAsset;

    /// <summary>开火成功后激活的 GAS 技能名。空字符串表示不激活技能。</summary>
    public string AbilityName =>
        abilityAsset != null && !string.IsNullOrEmpty(abilityAsset.UniqueName)
            ? abilityAsset.UniqueName
            : abilityName;
}
