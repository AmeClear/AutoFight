using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 一次射线检测的世界参数，由武器持有器等提供给 <see cref="CatchRay3D"/>。
    /// </summary>
    public struct AbilityRayQuery
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Range;
        public float Radius;
        public LayerMask Mask;
    }

    /// <summary>
    /// 为射线 Catcher 提供开火射线。通常由角色上的武器组件实现。
    /// </summary>
    public interface IAbilityRayProvider
    {
        bool TryGetAbilityRay(out AbilityRayQuery ray);
    }
}
