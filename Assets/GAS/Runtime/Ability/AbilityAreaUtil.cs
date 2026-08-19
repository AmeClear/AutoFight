using System;
using UnityEngine;

namespace GAS.Runtime
{
    public enum EffectCenterType
    {
        SelfOffset,
        WorldSpace,
        TargetOffset
    }

    public static class AbilityAreaUtil
    {
        [Obsolete("请使用OverlapBox2DNonAlloc方法来避免产生垃圾收集（GC）。")]
        public static Collider2D[] OverlapBox2D(this AbilitySystemComponent asc, Vector2 offset, Vector2 size,
            float angle, int layerMask, Transform relativeTransform = null)
        {
            relativeTransform ??= asc.transform;
            var center = (Vector2)relativeTransform.position;
            offset.x *= relativeTransform.lossyScale.x > 0 ? 1 : -1;
            center += offset;
            angle += asc.transform.eulerAngles.z;

            return Physics2D.OverlapBoxAll(center, size, angle, layerMask);
        }

        public static int OverlapBox2DNonAlloc(this AbilitySystemComponent asc, Vector2 offset, Vector2 size,
            float angle, Collider2D[] results, int layerMask, Transform relativeTransform = null)
        {
            relativeTransform ??= asc.transform;
            var center = (Vector2)relativeTransform.position;
            offset.x *= relativeTransform.lossyScale.x > 0 ? 1 : -1;
            center += offset;
            angle += asc.transform.eulerAngles.z;

            var count = Physics2D.OverlapBoxNonAlloc(center, size, angle, results, layerMask);
            return count;
        }

        public static int TimelineAbilityOverlapBox2D(this TimelineAbilitySpec spec,
            Vector2 offset, Vector2 size, float angle, int layerMask, Collider2D[] results,
            EffectCenterType centerType, Transform relativeTransform = null)
        {
            return centerType switch
            {
                EffectCenterType.SelfOffset => spec.Owner.OverlapBox2DNonAlloc(offset, size, angle, results, layerMask, relativeTransform),
                EffectCenterType.WorldSpace => Physics2D.OverlapBoxNonAlloc(offset, size, angle, results, layerMask),
                EffectCenterType.TargetOffset => spec.Target.OverlapBox2DNonAlloc(offset, size, angle, results, layerMask, relativeTransform),
                _ => 0
            };
        }

        [Obsolete("请使用OverlapCircle2DNonAlloc方法来避免产生垃圾收集（GC）。")]
        public static Collider2D[] OverlapCircle2D(this AbilitySystemComponent asc, Vector2 offset, float radius,
            int layerMask, Transform relativeTransform = null)
        {
            relativeTransform ??= asc.transform;
            var center = (Vector2)relativeTransform.position;
            offset.x *= relativeTransform.lossyScale.x > 0 ? 1 : -1;
            center += offset;

            return Physics2D.OverlapCircleAll(center, radius, layerMask);
        }

        public static int OverlapCircle2DNonAlloc(this AbilitySystemComponent asc, Vector2 offset, float radius,
            Collider2D[] results, int layerMask, Transform relativeTransform = null)
        {
            relativeTransform ??= asc.transform;
            var center = (Vector2)relativeTransform.position;
            offset.x *= relativeTransform.lossyScale.x > 0 ? 1 : -1;
            center += offset;

            var count = Physics2D.OverlapCircleNonAlloc(center, radius, results, layerMask);
            return count;
        }

        public static int OverlapCircle3DNonAlloc(this AbilitySystemComponent asc, Vector2 offset, float radius,
           Collider[] results, int layerMask, Transform relativeTransform = null)
        {
            relativeTransform ??= asc.transform;
            var center = (Vector2)relativeTransform.position;
            
            var count = Physics.OverlapSphereNonAlloc(center, radius, results, layerMask);
            return count;
        }

        /// <summary>
        /// 在水平面（XZ）上做扇形碰撞检测，仅保留该平面指定厚度内的物体。
        /// </summary>
        /// <param name="asc">检测发起者，用于取默认坐标系。</param>
        /// <param name="offset">相对偏移。x 为局部右，y 为局部前（投影到水平面）。</param>
        /// <param name="radius">扇形半径。</param>
        /// <param name="angle">扇形张角（度），以朝向为中线向两侧展开。大于等于 360 时视为整圆。</param>
        /// <param name="thickness">平面厚度（沿世界 Y）。</param>
        /// <param name="results">结果缓冲，需由调用方预分配。</param>
        /// <param name="layerMask">检测层级。</param>
        /// <param name="relativeTransform">相对坐标系，为空时使用 <paramref name="asc"/> 自身。</param>
        /// <returns>写入 <paramref name="results"/> 的碰撞体数量。</returns>
        public static int OverlapSector3DNonAlloc(this AbilitySystemComponent asc, Vector2 offset, float radius,
            float angle, float thickness, Collider[] results, int layerMask, Transform relativeTransform = null)
        {
            relativeTransform ??= asc.transform;
            GetHorizontalSectorPose(relativeTransform, offset, out var center, out var forward);
            return OverlapSector3DNonAlloc(center, forward, radius, angle, thickness, results, layerMask);
        }

        /// <summary>
        /// Timeline 技能按中心类型做水平面扇形检测。
        /// </summary>
        public static int TimelineAbilityOverlapSector3D(this TimelineAbilitySpec spec,
            Vector2 offset, float radius, float angle, float thickness, int layerMask, Collider[] results,
            EffectCenterType centerType, Transform relativeTransform = null)
        {
            return centerType switch
            {
                EffectCenterType.SelfOffset => spec.Owner.OverlapSector3DNonAlloc(offset, radius, angle, thickness, results, layerMask, relativeTransform),
                EffectCenterType.WorldSpace => OverlapSector3DNonAlloc(new Vector3(offset.x, 0f, offset.y), Vector3.forward, radius, angle, thickness, results, layerMask),
                EffectCenterType.TargetOffset => spec.Target.OverlapSector3DNonAlloc(offset, radius, angle, thickness, results, layerMask, relativeTransform),
                _ => 0
            };
        }

        /// <summary>
        /// 以世界坐标在水平面（XZ）上做扇形碰撞检测，仅保留该平面指定厚度内的物体。
        /// </summary>
        /// <param name="center">扇形圆心。</param>
        /// <param name="forward">扇形朝向，会投影到水平面。</param>
        /// <param name="radius">扇形半径。</param>
        /// <param name="angle">扇形张角（度）。</param>
        /// <param name="thickness">平面厚度（沿世界 Y）。</param>
        /// <param name="results">结果缓冲，需由调用方预分配。</param>
        /// <param name="layerMask">检测层级。</param>
        /// <returns>写入 <paramref name="results"/> 的碰撞体数量。</returns>
        public static int OverlapSector3DNonAlloc(Vector3 center, Vector3 forward, float radius, float angle,
            float thickness, Collider[] results, int layerMask)
        {
            if (results == null || results.Length == 0)
                return 0;

            if (radius <= 0f || angle <= 0f)
                return 0;

            var safeThickness = Mathf.Max(thickness, 0.001f);
            var planeNormal = Vector3.up;
            var planarForward = Vector3.ProjectOnPlane(forward, planeNormal);
            if (planarForward.sqrMagnitude < 0.0001f)
                planarForward = Vector3.forward;
            planarForward.Normalize();

            var orientation = Quaternion.LookRotation(planarForward, planeNormal);
            var halfExtents = new Vector3(radius, safeThickness * 0.5f, radius);
            var count = Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask);

            var valid = 0;
            for (var i = 0; i < count; i++)
            {
                var collider = results[i];
                if (collider == null)
                    continue;

                if (!IsColliderInHorizontalSector(collider, center, planarForward, planeNormal, radius, angle, safeThickness))
                    continue;

                results[valid++] = collider;
            }

            return valid;
        }

        private static void GetHorizontalSectorPose(Transform relativeTransform, Vector2 offset,
            out Vector3 center, out Vector3 forward)
        {
            var yawRotation = Quaternion.Euler(0f, relativeTransform.eulerAngles.y, 0f);
            center = relativeTransform.position + yawRotation * new Vector3(offset.x, 0f, offset.y);
            forward = yawRotation * Vector3.forward;
        }

        private static bool IsColliderInHorizontalSector(Collider collider, Vector3 center, Vector3 forward,
            Vector3 planeNormal, float radius, float angle, float thickness)
        {
            var closest = collider.ClosestPoint(center);
            var delta = closest - center;

            if (Mathf.Abs(Vector3.Dot(delta, planeNormal)) > thickness * 0.5f)
                return false;

            var planar = Vector3.ProjectOnPlane(delta, planeNormal);
            var planarSqr = planar.sqrMagnitude;
            var radiusSqr = radius * radius;
            if (planarSqr > radiusSqr)
                return false;

            if (angle >= 360f)
                return true;

            if (planarSqr <= 0.0001f)
                return true;

            var halfAngle = angle * 0.5f;
            return Vector3.Angle(forward, planar) <= halfAngle;
        }

        /// <summary>
        /// 3D 射线 / 粗射线检测。结果按距离升序写入 <paramref name="hits"/>。
        /// </summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direction">射线方向，会归一化。</param>
        /// <param name="range">最大距离。</param>
        /// <param name="radius">0 为细射线，大于 0 为 SphereCast 半径。</param>
        /// <param name="hits">结果缓冲，需由调用方预分配。</param>
        /// <param name="layerMask">检测层级。</param>
        /// <param name="triggerInteraction">是否检测 Trigger。</param>
        /// <returns>写入 <paramref name="hits"/> 的数量。</returns>
        public static int Raycast3DNonAlloc(Vector3 origin, Vector3 direction, float range, float radius,
            RaycastHit[] hits, int layerMask,
            QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
        {
            if (hits == null || hits.Length == 0)
                return 0;

            if (range <= 0f || direction.sqrMagnitude < 0.0001f)
                return 0;

            direction.Normalize();
            var count = radius > 0.0001f
                ? Physics.SphereCastNonAlloc(origin, radius, direction, hits, range, layerMask, triggerInteraction)
                : Physics.RaycastNonAlloc(origin, direction, hits, range, layerMask, triggerInteraction);

            SortRaycastHitsByDistance(hits, count);
            return count;
        }

        private static void SortRaycastHitsByDistance(RaycastHit[] hits, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var key = hits[i];
                var j = i - 1;
                while (j >= 0 && hits[j].distance > key.distance)
                {
                    hits[j + 1] = hits[j];
                    j--;
                }

                hits[j + 1] = key;
            }
        }
    }
}