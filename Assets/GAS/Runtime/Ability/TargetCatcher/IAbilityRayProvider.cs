using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 一次射线检测的世界参数，由武器持有器等提供给 <see cref="CatchRay3D"/>。
    /// <para>
    /// TPS：<see cref="Origin"/> 为枪口指向 <see cref="AimPoint"/>；
    /// <see cref="CameraOrigin"/> 为摄像机穿过准星指向同一 <see cref="AimPoint"/>。
    /// </para>
    /// </summary>
    public struct AbilityRayQuery
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 CameraOrigin;
        public Vector3 CameraDirection;
        public Vector3 AimPoint;
        public float Range;
        public float CameraRange;
        public float Radius;
        public LayerMask Mask;
        public bool HasCameraRay;
    }

    /// <summary>
    /// 为射线 Catcher 提供开火射线。通常由角色上的武器组件实现。
    /// </summary>
    public interface IAbilityRayProvider
    {
        bool TryGetAbilityRay(out AbilityRayQuery ray);
    }
}
