using UnityEngine;

/// <summary>
/// 过肩第三人称摄像机：镜头沿视角旋转，角色偏在一侧，屏幕正中即瞄准方向。
/// </summary>
public class ThirdPersonCameraController : CameraControllerBase
{
    [Header("跟随")]
    [SerializeField] private float followDistance = 4.5f;
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float positionSmooth = 14f;
    [SerializeField] private float rotationSmooth = 16f;

    [Header("过肩")]
    [SerializeField] private Vector2 shoulderOffset = new Vector2(0.65f, 0.15f);
    [SerializeField] [Tooltip("开镜时的过肩偏移。")]
    private Vector2 adsShoulderOffset = new Vector2(0.35f, 0.08f);
    [SerializeField] [Tooltip("开镜时的跟随距离。")]
    private float adsDistance = 2.2f;

    [Header("准星对齐")]
    [SerializeField] [Tooltip("默认关闭。开启后会按 PointCenter 平移镜头，容易和屏幕中心准星错开。")]
    private bool alignToCrosshair;
    [SerializeField] [Tooltip("用于对齐准星的预瞄距离。越大，准星越接近镜头朝向。")]
    private float lookAheadDistance = 25f;

    [Header("开镜")]
    [SerializeField] private float adsFov = 40f;
    [SerializeField] private float aimBlendSpeed = 10f;

    [Header("碰撞")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private static readonly RaycastHit[] CollisionHits = new RaycastHit[8];
    private bool _aiming;
    private float _aimBlend;
    private float _defaultFov;

    public bool IsAiming => _aiming;

    public void SetAiming(bool aiming)
    {
        _aiming = aiming;
    }

    protected override void Awake()
    {
        base.Awake();
        CacheDefaultFov();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheDefaultFov();
        _aimBlend = _aiming ? 1f : 0f;
    }

    protected override void UpdateCameraTransform()
    {
        CacheDefaultFov();

        var dt = Time.deltaTime;
        var aimLerp = 1f - Mathf.Exp(-aimBlendSpeed * dt);
        _aimBlend = Mathf.Lerp(_aimBlend, _aiming ? 1f : 0f, aimLerp);

        var lookRotation = GetLookRotation();
        var pivot = GetPivotWorldPosition();
        var shoulder = Vector2.Lerp(shoulderOffset, adsShoulderOffset, _aimBlend);
        var distance = Mathf.Lerp(followDistance, adsDistance, _aimBlend);

        var shoulderPivot = pivot + lookRotation * new Vector3(shoulder.x, shoulder.y, 0f);
        var back = lookRotation * Vector3.back;
        var resolvedDistance = ResolveCollisionDistance(shoulderPivot, back, distance);
        var desiredPosition = shoulderPivot + back * resolvedDistance;

        if (alignToCrosshair)
            desiredPosition += ComputeCrosshairFramingOffset(desiredPosition, lookRotation, pivot);

        var positionLerp = 1f - Mathf.Exp(-positionSmooth * dt);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerp);
        transform.rotation = lookRotation;

        if (cameraComponent != null)
            cameraComponent.fieldOfView = Mathf.Lerp(_defaultFov, adsFov, _aimBlend);
    }

    private Vector3 ComputeCrosshairFramingOffset(Vector3 cameraPosition, Quaternion lookRotation, Vector3 pivot)
    {
        if (cameraComponent == null)
            return Vector3.zero;

        var viewport = CrosshairAim.GetViewportPoint(cameraComponent);
        if (viewport.x < -0.25f || viewport.x > 1.25f || viewport.y < -0.25f || viewport.y > 1.25f)
            return Vector3.zero;

        var delta = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        delta.x = Mathf.Clamp(delta.x, -0.45f, 0.45f);
        delta.y = Mathf.Clamp(delta.y, -0.45f, 0.45f);
        if (delta.sqrMagnitude < 0.0000001f)
            return Vector3.zero;

        var forward = lookRotation * Vector3.forward;
        var lookAt = pivot + forward * lookAheadDistance;
        var depth = Vector3.Dot(lookAt - cameraPosition, forward);
        if (depth < 0.2f)
            depth = lookAheadDistance;

        var halfFovRad = cameraComponent.fieldOfView * 0.5f * Mathf.Deg2Rad;
        var frustumHeight = 2f * depth * Mathf.Tan(halfFovRad);
        var frustumWidth = frustumHeight * cameraComponent.aspect;
        var right = lookRotation * Vector3.right;
        var up = lookRotation * Vector3.up;

        return -right * (delta.x * frustumWidth) - up * (delta.y * frustumHeight);
    }

    private float ResolveCollisionDistance(Vector3 origin, Vector3 direction, float desiredDistance)
    {
        if (!enableCollision)
            return desiredDistance;

        var count = Physics.SphereCastNonAlloc(
            origin,
            collisionRadius,
            direction,
            CollisionHits,
            desiredDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        var distance = desiredDistance;
        for (var i = 0; i < count; i++)
        {
            var hit = CollisionHits[i];
            if (hit.collider == null || IsTargetCollider(hit.collider))
                continue;

            distance = Mathf.Min(distance, hit.distance - collisionRadius);
        }

        return Mathf.Clamp(distance, minDistance, desiredDistance);
    }

    private bool IsTargetCollider(Collider col)
    {
        if (target == null)
            return false;

        return col.transform == target || col.transform.IsChildOf(target);
    }

    private void CacheDefaultFov()
    {
        if (_defaultFov > 0.01f || cameraComponent == null)
            return;

        _defaultFov = cameraComponent.fieldOfView;
    }
}
