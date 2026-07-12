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

    private void ApplyTarget(Transform followTarget)
    {
        if (firstPersonController != null)
            firstPersonController.Target = followTarget;

        if (thirdPersonController != null)
            thirdPersonController.Target = followTarget;
    }
}
