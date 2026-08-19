#if UNITY_EDITOR
namespace GAS.Editor
{
    using Runtime;
    using UnityEngine;
    using Sirenix.OdinInspector;

    public class CatchRay3DInspector : TargetCatcherInspector<CatchRay3D>
    {
        [BoxGroup] [LabelText("使用武器开火上下文")] [OnValueChanged("OnCatcherChanged")]
        public bool UseWeaponFireContext;

        [BoxGroup] [LabelText("合并 Catcher 检测层")]
        [Tooltip("与武器 HitMask 做或运算，便于武器只勾角色层时仍能被地形挡住。")]
        [OnValueChanged("OnCatcherChanged")]
        public bool MergeCheckLayer;

        [BoxGroup] [Delayed] [LabelText("Detect Layer")] [OnValueChanged("OnCatcherChanged")]
        public LayerMask Layer;

        [BoxGroup] [Delayed] [LabelText("后备射程")] [OnValueChanged("OnCatcherChanged")]
        public float FallbackRange;

        [BoxGroup] [Delayed] [LabelText("后备半径")] [OnValueChanged("OnCatcherChanged")]
        public float FallbackRadius;

        [BoxGroup] [Delayed] [LabelText("后备枪口偏移")] [OnValueChanged("OnCatcherChanged")]
        public Vector3 FallbackOriginOffset;

        [BoxGroup] [Delayed] [LabelText("最大目标数")] [MinValue(1)] [OnValueChanged("OnCatcherChanged")]
        public int MaxTargets;

        [BoxGroup] [LabelText("穿透目标")] [OnValueChanged("OnCatcherChanged")]
        public bool PierceTargets;

        [BoxGroup] [LabelText("碰到阻挡物停止")]
        [Tooltip("没有 AbilitySystemComponent 的碰撞体视为阻挡（墙、地面）。")]
        [OnValueChanged("OnCatcherChanged")]
        public bool StopOnBlocker;

        [BoxGroup] [LabelText("绘制调试射线")] [OnValueChanged("OnCatcherChanged")]
        public bool DrawDebug;

        public CatchRay3DInspector(CatchRay3D targetCatcherBase) : base(targetCatcherBase)
        {
            UseWeaponFireContext = targetCatcherBase.useWeaponFireContext;
            MergeCheckLayer = targetCatcherBase.mergeCheckLayer;
            Layer = targetCatcherBase.checkLayer;
            FallbackRange = targetCatcherBase.fallbackRange;
            FallbackRadius = targetCatcherBase.fallbackRadius;
            FallbackOriginOffset = targetCatcherBase.fallbackOriginOffset;
            MaxTargets = targetCatcherBase.maxTargets;
            PierceTargets = targetCatcherBase.pierceTargets;
            StopOnBlocker = targetCatcherBase.stopOnBlocker;
            DrawDebug = targetCatcherBase.drawDebug;
        }

        void OnCatcherChanged()
        {
            _targetCatcher.useWeaponFireContext = UseWeaponFireContext;
            _targetCatcher.mergeCheckLayer = MergeCheckLayer;
            _targetCatcher.checkLayer = Layer;
            _targetCatcher.fallbackRange = FallbackRange;
            _targetCatcher.fallbackRadius = FallbackRadius;
            _targetCatcher.fallbackOriginOffset = FallbackOriginOffset;
            _targetCatcher.maxTargets = Mathf.Max(1, MaxTargets);
            _targetCatcher.pierceTargets = PierceTargets;
            _targetCatcher.stopOnBlocker = StopOnBlocker;
            _targetCatcher.drawDebug = DrawDebug;
            Save();
        }
    }
}
#endif
