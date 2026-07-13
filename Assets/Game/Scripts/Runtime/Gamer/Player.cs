using GAS.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class Player : Actor
{
    [Header("摄像机")]
    [SerializeField] private CameraModeController cameraModeController;

    private PlayerInput _input;
    private BodyRotationMode _lastBodyRotationMode = (BodyRotationMode)(-1);

    protected override void Init()
    {
        base.Init();
        _input = new PlayerInput();
        _input.Enable();

        _input.Player.Move.performed += OnMove;
        _input.Player.Move.canceled += OnMove;
        _input.Player.Fire.performed += OnFire;
        _input.Player.Aim.performed += OnAim;

        if (moveComponent == null)
            Debug.LogWarning($"[Player] {name} 缺少 MoveComponent，移动输入将被忽略。", this);

        if (cameraModeController == null)
            cameraModeController = Camera.main != null
                ? Camera.main.GetComponent<CameraModeController>()
                : null;

        if (cameraModeController != null)
            cameraModeController.SetTarget(transform);
        else
            Debug.LogWarning($"[Player] {name} 未找到 CameraModeController，摄像机将无法同步视角。", this);
    }

    private void Update()
    {
        HandleLookInput();
        ApplyCameraModeRules();
        SyncCameraView();
    }

    private void OnDestroy()
    {
        if (_input == null) return;

        _input.Player.Move.performed -= OnMove;
        _input.Player.Move.canceled -= OnMove;
        _input.Player.Fire.performed -= OnFire;
        _input.Player.Aim.performed -= OnAim;
        _input.Disable();
        _input.Dispose();
    }

    private void HandleLookInput()
    {
        if (moveComponent == null || Mouse.current == null)
            return;

        moveComponent.AddLookInput(Mouse.current.delta.ReadValue());
    }

    private void ApplyCameraModeRules()
    {
        if (moveComponent == null || cameraModeController == null)
            return;

        BodyRotationMode desiredMode = cameraModeController.CurrentMode switch
        {
            CameraControlMode.FirstPerson => BodyRotationMode.FaceViewYaw,
            CameraControlMode.ThirdPerson => BodyRotationMode.FaceMoveDirection,
            _ => BodyRotationMode.None
        };

        if (desiredMode == _lastBodyRotationMode)
            return;

        moveComponent.RotationMode = desiredMode;
        _lastBodyRotationMode = desiredMode;
    }

    private void SyncCameraView()
    {
        if (moveComponent == null || cameraModeController?.ActiveController == null)
            return;

        cameraModeController.ActiveController.SetViewRotation(
            moveComponent.ViewYaw,
            moveComponent.ViewPitch);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (moveComponent == null) return;

        moveComponent.SetMoveInput(context.ReadValue<Vector2>());
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        asc.TryActivateAbility(GAbilityLib.Atk.Name);
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        asc.TryActivateAbility(GAbilityLib.Def.Name);
    }
    protected override void OnHpChange(AttributeBase attributeBase, float oldValue, float newValue)
    {
        
    }
}
