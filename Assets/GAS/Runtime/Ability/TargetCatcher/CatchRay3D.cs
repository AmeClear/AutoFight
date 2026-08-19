using System.Collections.Generic;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 3D 射线命中：从枪口沿开火方向检测，收集带有 <see cref="AbilitySystemComponent"/> 的目标。
    /// <para>
    /// 优先使用拥有者上的 <see cref="IAbilityRayProvider"/>（TPS 下为枪口指向准星落点）。
    /// 没有武器上下文时，退回拥有者朝向与 Catcher 上的后备参数。
    /// </para>
    /// </summary>
    public sealed class CatchRay3D : CatchAreaBase
    {
        public bool useWeaponFireContext = true;
        public bool mergeCheckLayer = true;
        public float fallbackRange = 50f;
        public float fallbackRadius;
        public Vector3 fallbackOriginOffset = new Vector3(0f, 1.4f, 0.4f);
        public int maxTargets = 1;
        public bool pierceTargets;
        public bool stopOnBlocker = true;
        public bool drawDebug;

        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        public void Init(AbilitySystemComponent owner, LayerMask tCheckLayer, float range, float radius)
        {
            base.Init(owner, tCheckLayer);
            fallbackRange = range;
            fallbackRadius = radius;
        }

        protected override void CatchTargetsNonAlloc(AbilitySystemComponent mainTarget,
            List<AbilitySystemComponent> results)
        {
            if (!TryBuildQuery(out var query))
                return;

            var count = AbilityAreaUtil.Raycast3DNonAlloc(
                query.Origin, query.Direction, query.Range, query.Radius, Hits, query.Mask);

            var remaining = Mathf.Max(1, maxTargets);
            var endPoint = query.Origin + query.Direction.normalized * query.Range;

            for (var i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit.collider == null)
                    continue;

                if (IsOwnedCollider(hit.collider))
                    continue;

                var targetUnit = hit.collider.GetComponentInParent<AbilitySystemComponent>();
                if (targetUnit != null && targetUnit != Owner)
                {
                    if (!results.Contains(targetUnit))
                    {
                        results.Add(targetUnit);
                        remaining--;
                    }

                    endPoint = hit.point;
                    if (!pierceTargets || remaining <= 0)
                        break;

                    continue;
                }

                if (stopOnBlocker)
                {
                    endPoint = hit.point;
                    break;
                }
            }

            if (drawDebug)
                DebugExtension.DrawLine(query.Origin, endPoint, Color.red, 1f);
        }

        private bool TryBuildQuery(out AbilityRayQuery query)
        {
            IAbilityRayProvider provider = null;
            if (useWeaponFireContext && Owner != null)
            {
                provider = Owner.GetComponentInParent<IAbilityRayProvider>();
                if (provider == null)
                    provider = Owner.GetComponentInChildren<IAbilityRayProvider>();
            }

            if (provider != null &&
                provider.TryGetAbilityRay(out query) &&
                query.Direction.sqrMagnitude > 0.0001f)
            {
                if (mergeCheckLayer)
                    query.Mask = (LayerMask)(query.Mask | checkLayer);
                return true;
            }

            if (Owner == null)
            {
                query = default;
                return false;
            }

            var origin = Owner.transform.TransformPoint(fallbackOriginOffset);
            query = new AbilityRayQuery
            {
                Origin = origin,
                Direction = Owner.transform.forward,
                Range = fallbackRange > 0f ? fallbackRange : 50f,
                Radius = Mathf.Max(0f, fallbackRadius),
                Mask = checkLayer
            };
            return true;
        }

        private bool IsOwnedCollider(Collider col)
        {
            if (Owner == null)
                return false;

            return col.transform == Owner.transform || col.transform.IsChildOf(Owner.transform);
        }

#if UNITY_EDITOR
        public override void OnEditorPreview(GameObject previewObject)
        {
            if (previewObject == null)
                return;

            var origin = previewObject.transform.TransformPoint(fallbackOriginOffset);
            var direction = previewObject.transform.forward;
            var range = fallbackRange > 0f ? fallbackRange : 50f;
            var end = origin + direction * range;
            DebugExtension.DrawArrow(origin, end, Color.red, 1f);
            if (fallbackRadius > 0.0001f)
                DebugExtension.DrawCircle(end, direction, fallbackRadius, 24, Color.red, 1f);
        }
#endif
    }
}
