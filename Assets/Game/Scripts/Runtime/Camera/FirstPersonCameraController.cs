using UnityEngine;

public class FirstPersonCameraController : CameraControllerBase
{
    protected override void UpdateCameraTransform()
    {
        transform.position = GetPivotWorldPosition();
        transform.rotation = GetLookRotation();
    }
}
