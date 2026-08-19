#if UNITY_EDITOR
namespace GAS.Editor
{
    using Runtime;
    using UnityEngine;
    using Sirenix.OdinInspector;

    public class CatchAreaSector3DInspector : TargetCatcherInspector<CatchAreaSector3D>
    {
        [BoxGroup] [Delayed] [OnValueChanged("OnCatcherChanged")]
        public Vector2 Offset;

        [BoxGroup] [Delayed] [OnValueChanged("OnCatcherChanged")]
        public float Radius;

        [BoxGroup] [Delayed] [LabelText("Angle")] [OnValueChanged("OnCatcherChanged")]
        public float Angle;

        [BoxGroup] [Delayed] [LabelText("Thickness")] [OnValueChanged("OnCatcherChanged")]
        public float Thickness;

        [BoxGroup] [Delayed] [LabelText("Detect Layer")] [OnValueChanged("OnCatcherChanged")]
        public LayerMask Layer;

        [BoxGroup] [Delayed] [LabelText("Center Type")] [OnValueChanged("OnCatcherChanged")]
        public EffectCenterType CenterType;

        public CatchAreaSector3DInspector(CatchAreaSector3D targetCatcherBase) : base(targetCatcherBase)
        {
            Offset = targetCatcherBase.offset;
            Radius = targetCatcherBase.radius;
            Angle = targetCatcherBase.angle;
            Thickness = targetCatcherBase.thickness;
            Layer = targetCatcherBase.checkLayer;
            CenterType = targetCatcherBase.centerType;
        }

        void OnCatcherChanged()
        {
            _targetCatcher.offset = Offset;
            _targetCatcher.radius = Radius;
            _targetCatcher.angle = Angle;
            _targetCatcher.thickness = Thickness;
            _targetCatcher.checkLayer = Layer;
            _targetCatcher.centerType = CenterType;
            Save();
        }
    }
}
#endif
