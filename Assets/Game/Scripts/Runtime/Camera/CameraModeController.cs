using UnityEngine;

public enum CameraControlMode
{
    FirstPerson = 0,
    ThirdPerson = 1,
}

[DisallowMultipleComponent]
public class CameraModeController : MonoBehaviour
{
    [SerializeField] private CameraControlMode defaultMode = CameraControlMode.ThirdPerson;
    [SerializeField] private Transform target;
    [SerializeField] private FirstPersonCameraController firstPersonController;
    [SerializeField] private ThirdPersonCameraController thirdPersonController;

    public CameraControlMode CurrentMode { get; private set; }
    public CameraControllerBase ActiveController { get; private set; }
    public bool IsAiming { get; private set; }

    public Camera ActiveCamera =>
        ActiveController != null ? ActiveController.Camera : null;

    private void Awake()
    {
        if (firstPersonController == null)
            firstPersonController = GetComponent<FirstPersonCameraController>();

        if (thirdPersonController == null)
            thirdPersonController = GetComponent<ThirdPersonCameraController>();

        ApplyTarget(target);
        SetMode(defaultMode);
    }

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        ApplyTarget(target);
    }

    public void SetMode(CameraControlMode mode)
    {
        CurrentMode = mode;

        if (firstPersonController != null)
            firstPersonController.enabled = mode == CameraControlMode.FirstPerson;

        if (thirdPersonController != null)
            thirdPersonController.enabled = mode == CameraControlMode.ThirdPerson;

        ActiveController = mode switch
        {
            CameraControlMode.FirstPerson => firstPersonController,
            CameraControlMode.ThirdPerson => thirdPersonController,
            _ => null
        };
    }

    public void ToggleMode()
    {
        SetMode(CurrentMode == CameraControlMode.FirstPerson
            ? CameraControlMode.ThirdPerson
            : CameraControlMode.FirstPerson);
    }

    /// <summary>
    /// 开镜/腰射。第三人称会拉近过肩距离并降低 FOV。
    /// </summary>
    public void SetAiming(bool aiming)
    {
        IsAiming = aiming;
        if (thirdPersonController != null)
            thirdPersonController.SetAiming(aiming);
    }

    private void ApplyTarget(Transform followTarget)
    {
        if (firstPersonController != null)
            firstPersonController.Target = followTarget;

        if (thirdPersonController != null)
            thirdPersonController.Target = followTarget;
    }
}
