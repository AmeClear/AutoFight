using UnityEngine;

public class ThirdPersonCameraController : CameraControllerBase
{
    [Header("第三人称")]
    [SerializeField] private float followDistance = 5f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float positionSmooth = 12f;
    [SerializeField] private float rotationSmooth = 12f;

    [Header("碰撞")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private LayerMask collisionMask = ~0;

    protected override void UpdateCameraTransform()
    {
        Vector3 pivot = GetPivotWorldPosition();
        Quaternion lookRotation = GetLookRotation();
        Vector3 desiredDirection = lookRotation * Vector3.back;
        float resolvedDistance = ResolveCollisionDistance(pivot, desiredDirection);
        Vector3 desiredPosition = pivot + desiredDirection * resolvedDistance;

        float positionLerp = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        float rotationLerp = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationLerp);
    }

    private float ResolveCollisionDistance(Vector3 pivot, Vector3 direction)
    {
        float distance = followDistance;
        if (!enableCollision)
            return distance;

        if (Physics.SphereCast(
                pivot,
                collisionRadius,
                direction,
                out RaycastHit hit,
                followDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, followDistance);
        }

        return distance;
    }
}
