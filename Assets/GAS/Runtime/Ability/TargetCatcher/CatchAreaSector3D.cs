using System.Collections.Generic;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    public sealed class CatchAreaSector3D : CatchAreaBase
    {
        public float radius;
        public float angle = 90f;
        public float thickness = 0.5f;
        public Vector2 offset;
        public EffectCenterType centerType;

        public void Init(AbilitySystemComponent owner, LayerMask tCheckLayer, Vector2 offset, float radius,
            float angle, float thickness)
        {
            base.Init(owner, tCheckLayer);
            this.offset = offset;
            this.radius = radius;
            this.angle = angle;
            this.thickness = thickness;
        }

        private static readonly Collider[] Colliders = new Collider[32];

        protected override void CatchTargetsNonAlloc(AbilitySystemComponent mainTarget, List<AbilitySystemComponent> results)
        {
            int count = centerType switch
            {
                EffectCenterType.SelfOffset => Owner.OverlapSector3DNonAlloc(offset, radius, angle, thickness, Colliders, checkLayer),
                EffectCenterType.WorldSpace => AbilityAreaUtil.OverlapSector3DNonAlloc(
                    new Vector3(offset.x, 0f, offset.y), Vector3.forward, radius, angle, thickness, Colliders, checkLayer),
                EffectCenterType.TargetOffset => mainTarget.OverlapSector3DNonAlloc(offset, radius, angle, thickness, Colliders, checkLayer),
                _ => 0
            };

            for (var i = 0; i < count; ++i)
            {
                var targetUnit = Colliders[i].GetComponent<AbilitySystemComponent>();
                if (targetUnit != null && Colliders[i].gameObject != Owner.gameObject)
                {
                    results.Add(targetUnit);
                }
            }
        }

#if UNITY_EDITOR
        public override void OnEditorPreview(GameObject previewObject)
        {
            var relativeTransform = previewObject.transform;
            var yawRotation = Quaternion.Euler(0f, relativeTransform.eulerAngles.y, 0f);
            Vector3 center;
            Vector3 forward;
            switch (centerType)
            {
                case EffectCenterType.WorldSpace:
                    center = new Vector3(offset.x, 0f, offset.y);
                    forward = Vector3.forward;
                    break;
                default:
                    center = relativeTransform.position + yawRotation * new Vector3(offset.x, 0f, offset.y);
                    forward = yawRotation * Vector3.forward;
                    break;
            }

            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var halfThickness = Mathf.Max(thickness, 0.001f) * 0.5f;
            var upOffset = Vector3.up * halfThickness;
            DebugExtension.DrawSector(center + upOffset, rotation, radius, angle, 18, Color.red, 1f);
            DebugExtension.DrawSector(center - upOffset, rotation, radius, angle, 18, Color.red, 1f);
            DebugExtension.DrawSector(center, rotation, radius, angle, 18, Color.green, 1f);
        }
#endif
    }
}
