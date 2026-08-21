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

    [BoxGroup("基本/开火/射速"), LabelText("射速"), SuffixLabel("RPM", true)]
    [Tooltip("每分钟发射的子弹数量（Rounds Per Minute）。例如 600 RPM 为每秒 10 发。")]
    [MinValue(1)]
    [SerializeField] private int fireRateRpm = 300;

    [BoxGroup("基本/开火/射速"), ShowInInspector, ReadOnly, LabelText("开火间隔"), SuffixLabel("秒", true)]
    private float FireIntervalPreview => FireInterval;

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

    [TabGroup("基本", "后坐")]
    [BoxGroup("基本/后坐/开关"), LabelText("启用后坐"), ToggleLeft]
    [Tooltip("开火后把后坐力写进视角，准星随之后抬/偏移。关闭则弹着点完全跟手。")]
    [SerializeField] private bool recoilEnabled = true;

    [BoxGroup("基本/后坐/强度"), LabelText("垂直后坐"), SuffixLabel("度/发", true), ShowIf("recoilEnabled")]
    [Tooltip("每一发让准星上抬的基础角度。连发时再乘垂直曲线。")]
    [MinValue(0f)]
    [SerializeField] private float verticalRecoil = 0.85f;

    [BoxGroup("基本/后坐/强度"), LabelText("水平后坐"), SuffixLabel("度/发", true), ShowIf("recoilEnabled")]
    [Tooltip("每一发左右偏移的基础角度，再乘水平曲线。正值向右。")]
    [MinValue(0f)]
    [SerializeField] private float horizontalRecoil = 0.28f;

    [BoxGroup("基本/后坐/强度"), LabelText("水平偏向"), ShowIf("recoilEnabled")]
    [Tooltip("在 -1（左）到 1（右）之间，连发时额外往一侧拉。")]
    [Range(-1f, 1f)]
    [SerializeField] private float horizontalBias = 0.15f;

    [BoxGroup("基本/后坐/强度"), LabelText("随机幅度"), ShowIf("recoilEnabled")]
    [Tooltip("在基础后坐上叠加的比例抖动。0 为固定弹道，0.2 表示 ±20%。")]
    [Range(0f, 1f)]
    [SerializeField] private float recoilRandomness = 0.15f;

    [BoxGroup("基本/后坐/强度"), LabelText("开镜后坐倍率"), ShowIf("recoilEnabled")]
    [Tooltip("开镜时后坐乘这个系数。小于 1 表示开镜更稳。")]
    [MinValue(0f)]
    [SerializeField] private float adsRecoilScale = 0.6f;

    [BoxGroup("基本/后坐/曲线"), LabelText("垂直曲线"), ShowIf("recoilEnabled")]
    [Tooltip("横轴为弹匣进度 0–1，纵轴为垂直后坐倍率。前几发可压低，后续抬高。")]
    [SerializeField] private AnimationCurve verticalRecoilPattern = new AnimationCurve(
        new Keyframe(0f, 0.7f),
        new Keyframe(0.2f, 1f),
        new Keyframe(1f, 1.15f));

    [BoxGroup("基本/后坐/曲线"), LabelText("水平曲线"), ShowIf("recoilEnabled")]
    [Tooltip("横轴为弹匣进度 0–1，纵轴为水平方向倍率。负值向左，正值向右，用来做弹道。")]
    [SerializeField] private AnimationCurve horizontalRecoilPattern = new AnimationCurve(
        new Keyframe(0f, 0.2f),
        new Keyframe(0.25f, 1f),
        new Keyframe(0.55f, -0.75f),
        new Keyframe(0.8f, 0.85f),
        new Keyframe(1f, -0.35f));

    [BoxGroup("基本/后坐/恢复"), LabelText("回正延迟"), SuffixLabel("秒", true), ShowIf("recoilEnabled")]
    [Tooltip("最后一发之后，过多久开始把视角拉回压枪前的方向。")]
    [MinValue(0f)]
    [SerializeField] private float recoilRecoveryDelay = 0.08f;

    [BoxGroup("基本/后坐/恢复"), LabelText("回正速度"), ShowIf("recoilEnabled")]
    [Tooltip("指数回正速率。越大回正越快。")]
    [MinValue(0f)]
    [SerializeField] private float recoilRecoverySpeed = 8f;

    [BoxGroup("基本/后坐/恢复"), LabelText("连发重置"), SuffixLabel("秒", true), ShowIf("recoilEnabled")]
    [Tooltip("停火超过该时间后，弹道进度从第一发重新算。")]
    [MinValue(0f)]
    [SerializeField] private float recoilResetTime = 0.28f;

    [BoxGroup("基本/后坐/上限"), LabelText("垂直上限"), SuffixLabel("度", true), ShowIf("recoilEnabled")]
    [Tooltip("一次连发中最多上抬的角度，防止准星甩出屏幕。")]
    [MinValue(0f)]
    [SerializeField] private float maxVerticalRecoil = 12f;

    [BoxGroup("基本/后坐/上限"), LabelText("水平上限"), SuffixLabel("度", true), ShowIf("recoilEnabled")]
    [Tooltip("一次连发中左右累计偏移的上限。")]
    [MinValue(0f)]
    [SerializeField] private float maxHorizontalRecoil = 6f;

    /// <summary>武器的唯一标识，用于存档、装备查找与事件区分。</summary>
    public string WeaponId => weaponId;

    /// <summary>界面上展示的武器名称。</summary>
    public string DisplayName => displayName;

    /// <summary>开火模式：单发或连发。</summary>
    public WeaponFireMode FireMode => fireMode;

    /// <summary>每分钟发射的子弹数量（Rounds Per Minute）。</summary>
    public int FireRateRpm => Mathf.Max(1, fireRateRpm);

    /// <summary>两次有效开火之间的最短间隔（秒），由射速换算：60 / RPM。</summary>
    public float FireInterval => 60f / FireRateRpm;

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

    public bool RecoilEnabled => recoilEnabled;
    public float VerticalRecoil => Mathf.Max(0f, verticalRecoil);
    public float HorizontalRecoil => Mathf.Max(0f, horizontalRecoil);
    public float HorizontalBias => Mathf.Clamp(horizontalBias, -1f, 1f);
    public float RecoilRandomness => Mathf.Clamp01(recoilRandomness);
    public float AdsRecoilScale => Mathf.Max(0f, adsRecoilScale);
    public AnimationCurve VerticalRecoilPattern => verticalRecoilPattern;
    public AnimationCurve HorizontalRecoilPattern => horizontalRecoilPattern;
    public float RecoilRecoveryDelay => Mathf.Max(0f, recoilRecoveryDelay);
    public float RecoilRecoverySpeed => Mathf.Max(0f, recoilRecoverySpeed);
    public float RecoilResetTime => Mathf.Max(0f, recoilResetTime);
    public float MaxVerticalRecoil => Mathf.Max(0f, maxVerticalRecoil);
    public float MaxHorizontalRecoil => Mathf.Max(0f, maxHorizontalRecoil);
}
