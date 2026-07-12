using System.Collections.Generic;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    public sealed class CatchAreaCircle3D : CatchAreaBase
    {
        public float radius;
        public Vector2 offset;
        public EffectCenterType centerType;

        public void Init(AbilitySystemComponent owner, LayerMask tCheckLayer, Vector2 offset, float radius)
        {
            base.Init(owner, tCheckLayer);
            this.offset = offset;
            this.radius = radius;
        }

        private static readonly Collider[] Colliders = new Collider[32];
        protected override void CatchTargetsNonAlloc(AbilitySystemComponent mainTarget, List<AbilitySystemComponent> results)
        {
            int count = centerType switch
            {
                EffectCenterType.SelfOffset => Owner.OverlapCircle3DNonAlloc(offset, radius, Colliders, checkLayer),
                EffectCenterType.WorldSpace => Physics.OverlapSphereNonAlloc(offset, radius, Colliders, checkLayer),
                EffectCenterType.TargetOffset => mainTarget.OverlapCircle3DNonAlloc(offset, radius, Colliders, checkLayer),
                _ => 0
            };


            for (var i = 0; i < count; ++i)
            {
                var targetUnit = Colliders[i].GetComponent<AbilitySystemComponent>();
                if (targetUnit != null && Colliders[i].gameObject!=Owner.gameObject)
                {
                    results.Add(targetUnit);
                }
            }
        }
#if UNITY_EDITOR
        public override void OnEditorPreview(GameObject previewObject)
        {
            // 使用Debug 绘制box预览
            float showTime = 1;
            Color color = Color.green;
            var relativeTransform = previewObject.transform;
            var center = offset;
            switch (centerType)
            {
                case EffectCenterType.SelfOffset:
                    center = relativeTransform.position;
                    break;
                case EffectCenterType.WorldSpace:
                    center = offset;
                    break;
                case EffectCenterType.TargetOffset:
                    //center = _targetCatcher.Target.transform.position;
                    break;
            }

            DebugExtension.DrawCircle(center, relativeTransform.forward, radius,36,Color.red,1f);
        }
#endif
    }
}