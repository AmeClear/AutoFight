using GAS.General;
using GAS.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 开火 Instant Cue：沿武器射线命中表面，刷一张颜料弹痕。
/// <para>挂在 GA_Fire 时间轴 Instant Cue 上，由射击者触发，可打在墙和角色上。</para>
/// </summary>
[CreateAssetMenu(fileName = "CuePaintDecal", menuName = "GAS/Cue/CuePaintDecal")]
public class CuePaintDecal : GameplayCueInstant
{
    [BoxGroup("射线")]
    [LabelText("使用武器开火射线")]
    [Tooltip("从 Owner 上的 IAbilityRayProvider 取枪口到准星目标点的射线。关闭则用角色朝向。")]
    public bool useWeaponRay = true;

    [BoxGroup("射线")]
    [LabelText("检测层级")]
    [Tooltip("与武器 HitMask 合并。应包含地形和角色，否则弹痕打不出来。")]
    public LayerMask hitMask = ~0;

    [BoxGroup("射线")]
    [LabelText("合并武器层级")]
    public bool mergeWeaponMask = true;

    [BoxGroup("外观")]
    [LabelText("颜料颜色")]
    public Color[] colors =
    {
        new Color(1f, 0.18f, 0.38f, 1f),
        new Color(0.15f, 0.75f, 1f, 1f),
        new Color(1f, 0.85f, 0.12f, 1f),
        new Color(0.35f, 0.95f, 0.28f, 1f),
        new Color(1f, 0.45f, 0.08f, 1f),
        new Color(0.72f, 0.28f, 1f, 1f)
    };

    [BoxGroup("外观")]
    [LabelText("最小尺寸"), SuffixLabel("米", true)]
    [MinValue(0.05f)]
    public float minSize = 0.18f;

    [BoxGroup("外观")]
    [LabelText("最大尺寸"), SuffixLabel("米", true)]
    [MinValue(0.05f)]
    public float maxSize = 0.42f;

    [BoxGroup("外观")]
    [LabelText("表面抬起"), SuffixLabel("米", true)]
    public float surfaceOffset = 0.02f;

    [BoxGroup("外观")]
    [LabelText("贴在命中物体上")]
    [Tooltip("动态物体移动时弹痕跟着走。")]
    public bool attachToHit = true;

    [BoxGroup("外观")]
    [LabelText("Paint Shader")]
    public Shader paintShader;

    [BoxGroup("生命周期")]
    [LabelText("停留时间"), SuffixLabel("秒", true)]
    [MinValue(0.05f)]
    public float lifetime = 8f;

    [BoxGroup("生命周期")]
    [LabelText("淡出时长"), SuffixLabel("秒", true)]
    public float fadeDuration = 1.6f;

    public override GameplayCueInstantSpec CreateSpec(GameplayCueParameters parameters)
    {
        return new CuePaintDecalSpec(this, parameters);
    }

#if UNITY_EDITOR
    public override void OnEditorPreview(GameObject previewObject, int frame, int startFrame)
    {
        if (previewObject == null || frame < startFrame)
            return;

        var origin = previewObject.transform.position + Vector3.up * 1.4f;
        var end = origin + previewObject.transform.forward * 2f;
        DebugExtension.DrawArrow(origin, end, Color.magenta, 0f);
    }
#endif
}

public class CuePaintDecalSpec : GameplayCueInstantSpec<CuePaintDecal>
{
    private static readonly RaycastHit[] Hits = new RaycastHit[16];

    public CuePaintDecalSpec(CuePaintDecal cue, GameplayCueParameters parameters) : base(cue, parameters)
    {
    }

    public override void Trigger()
    {
        if (Owner == null)
            return;

        if (!TryBuildRay(out var origin, out var direction, out var range, out var radius, out var mask))
            return;

        var ray = new Ray(origin, direction);
        if (PaintDecalPool.RaycastFirst(ray, range, radius, mask, Owner.transform, Hits, out var hit) <= 0)
            return;

        var colors = cue.colors;
        var color = colors != null && colors.Length > 0
            ? colors[Random.Range(0, colors.Length)]
            : Color.red;
        var size = Random.Range(Mathf.Min(cue.minSize, cue.maxSize), Mathf.Max(cue.minSize, cue.maxSize));

        PaintDecalPool.Ensure().Spawn(
            hit,
            color,
            size,
            cue.lifetime,
            cue.fadeDuration,
            cue.surfaceOffset,
            cue.attachToHit,
            cue.paintShader);
    }

    private bool TryBuildRay(out Vector3 origin, out Vector3 direction, out float range, out float radius,
        out LayerMask mask)
    {
        origin = Owner.transform.TransformPoint(new Vector3(0f, 1.4f, 0.4f));
        direction = Owner.transform.forward;
        range = 50f;
        radius = 0f;
        mask = cue.hitMask;

        if (!cue.useWeaponRay)
            return true;

        var provider = Owner.GetComponentInParent<IAbilityRayProvider>() ??
                       Owner.GetComponentInChildren<IAbilityRayProvider>();
        if (provider == null ||
            !provider.TryGetAbilityRay(out var query) ||
            query.Direction.sqrMagnitude < 0.0001f)
            return true;

        origin = query.Origin;
        direction = query.Direction;
        range = query.Range > 0f ? query.Range : range;
        radius = query.Radius;
        mask = cue.mergeWeaponMask ? (LayerMask)(query.Mask | cue.hitMask) : query.Mask;
        return true;
    }
}
