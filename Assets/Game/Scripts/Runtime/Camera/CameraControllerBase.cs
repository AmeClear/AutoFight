using UnityEngine;

public abstract class CameraControllerBase : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] protected Transform target;
    [SerializeField] protected Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    [Header("光标")]
    [SerializeField] protected bool lockCursorOnEnable = true;

    protected Camera cameraComponent;
    protected float yaw;
    protected float pitch;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    protected virtual void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
            cameraComponent = Camera.main;
    }

    protected virtual void OnEnable()
    {
        if (lockCursorOnEnable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    protected virtual void OnDisable()
    {
        if (lockCursorOnEnable)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    protected virtual void LateUpdate()
    {
        if (target == null)
            return;

        UpdateCameraTransform();
    }

    public void SetViewRotation(float viewYaw, float viewPitch)
    {
        yaw = viewYaw;
        pitch = viewPitch;
    }

    protected abstract void UpdateCameraTransform();

    protected Vector3 GetPivotWorldPosition()
    {
        return target.position + target.TransformDirection(pivotOffset);
    }

    protected Quaternion GetLookRotation()
    {
        return Quaternion.Euler(pitch, yaw, 0f);
    }
}
