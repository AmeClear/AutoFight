using UnityEngine;

public enum MoveMode
{
    Transform = 0,
    Rigidbody = 1,
    CharacterController = 2,
}

public enum BodyRotationMode
{
    None = 0,
    FaceViewYaw = 1,
    FaceMoveDirection = 2,
}

[DisallowMultipleComponent]
public class MoveComponent : MonoBehaviour
{
    [Header("移动模式")]
    [SerializeField] private MoveMode moveMode = MoveMode.Transform;

    [Header("通用参数")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private BodyRotationMode bodyRotationMode = BodyRotationMode.FaceViewYaw;

    [Header("视角参数")]
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private bool invertY;

    [Header("Rigidbody 参数")]
    [SerializeField] private bool rigidbodyMovePosition = true;
    [SerializeField] private bool preserveVerticalVelocity = true;

    [Header("CharacterController 参数")]
    [SerializeField] private bool applyGravity = true;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedStickForce = -2f;

    private Transform _transform;
    private Rigidbody _rigidbody;
    private CharacterController _characterController;

    private Vector3 _moveInput;
    private float _viewYaw;
    private float _viewPitch;
    private float _verticalVelocity;

    public MoveMode Mode
    {
        get => moveMode;
        set
        {
            moveMode = value;
            CacheComponents();
        }
    }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public BodyRotationMode RotationMode
    {
        get => bodyRotationMode;
        set => bodyRotationMode = value;
    }

    public float ViewYaw => _viewYaw;
    public float ViewPitch => _viewPitch;
    public Vector3 ViewForward => Quaternion.Euler(0f, _viewYaw, 0f) * Vector3.forward;
    public Vector3 MoveInput => _moveInput;
    public bool IsMoving => _moveInput.sqrMagnitude > 0.0001f;

    private void Awake()
    {
        CacheComponents();
        SyncViewYawFromTransform();
    }

    private void Reset()
    {
        CacheComponents();
    }

    private void Update()
    {
        switch (moveMode)
        {
            case MoveMode.Transform:
                ApplyTransformMove(Time.deltaTime);
                break;
            case MoveMode.CharacterController:
                ApplyCharacterControllerMove(Time.deltaTime);
                break;
        }

        ApplyBodyRotation(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (moveMode == MoveMode.Rigidbody)
            ApplyRigidbodyMove(Time.fixedDeltaTime);
    }

    public void AddLookInput(Vector2 delta)
    {
        if (delta.sqrMagnitude <= Mathf.Epsilon)
            return;

        float invert = invertY ? -1f : 1f;
        _viewYaw += delta.x * lookSensitivity;
        _viewPitch += delta.y * lookSensitivity * invert;
        _viewPitch = Mathf.Clamp(_viewPitch, minPitch, maxPitch);
    }

    public void SetViewRotation(float yaw, float pitch)
    {
        _viewYaw = yaw;
        _viewPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    public void SetMoveInput(Vector2 input)
    {
        _moveInput = new Vector3(input.x, 0f, input.y);
    }

    public void SetMoveInput(Vector3 direction)
    {
        _moveInput = new Vector3(direction.x, 0f, direction.z);
    }

    public void SetMoveInput(float x, float z)
    {
        _moveInput = new Vector3(x, 0f, z);
    }

    public void Stop()
    {
        _moveInput = Vector3.zero;

        if (moveMode == MoveMode.Rigidbody && _rigidbody != null && !preserveVerticalVelocity)
            _rigidbody.velocity = Vector3.zero;
    }

    public void MoveBy(Vector3 worldDelta)
    {
        switch (moveMode)
        {
            case MoveMode.Transform:
                _transform.position += worldDelta;
                break;
            case MoveMode.Rigidbody when _rigidbody != null:
                if (rigidbodyMovePosition || _rigidbody.isKinematic)
                    _rigidbody.MovePosition(_rigidbody.position + worldDelta);
                else
                    _rigidbody.position += worldDelta;
                break;
            case MoveMode.CharacterController when _characterController != null:
                _characterController.Move(worldDelta);
                break;
        }
    }

    public void Teleport(Vector3 worldPosition)
    {
        switch (moveMode)
        {
            case MoveMode.Transform:
                _transform.position = worldPosition;
                break;
            case MoveMode.Rigidbody when _rigidbody != null:
                _rigidbody.position = worldPosition;
                if (!_rigidbody.isKinematic)
                    _rigidbody.velocity = Vector3.zero;
                break;
            case MoveMode.CharacterController when _characterController != null:
                _characterController.enabled = false;
                _transform.position = worldPosition;
                _characterController.enabled = true;
                break;
        }

        _verticalVelocity = 0f;
    }

    private void CacheComponents()
    {
        _transform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        _characterController = GetComponent<CharacterController>();

        ValidateModeComponent();
    }

    private void ValidateModeComponent()
    {
        switch (moveMode)
        {
            case MoveMode.Rigidbody when _rigidbody == null:
                Debug.LogWarning($"[MoveComponent] {name} 使用 Rigidbody 模式但未找到 Rigidbody 组件。", this);
                break;
            case MoveMode.CharacterController when _characterController == null:
                Debug.LogWarning($"[MoveComponent] {name} 使用 CharacterController 模式但未找到 CharacterController 组件。", this);
                break;
        }
    }

    private void SyncViewYawFromTransform()
    {
        _viewYaw = _transform.eulerAngles.y;
    }

    private Vector3 GetWorldMoveDirection()
    {
        if (_moveInput.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        Quaternion yawRotation = Quaternion.Euler(0f, _viewYaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;
        return (forward * _moveInput.z + right * _moveInput.x).normalized;
    }

    private void ApplyTransformMove(float deltaTime)
    {
        Vector3 direction = GetWorldMoveDirection();
        if (direction == Vector3.zero)
            return;

        _transform.position += direction * (moveSpeed * deltaTime);
    }

    private void ApplyRigidbodyMove(float deltaTime)
    {
        if (_rigidbody == null)
            return;

        Vector3 direction = GetWorldMoveDirection();
        Vector3 displacement = direction * (moveSpeed * deltaTime);

        if (direction == Vector3.zero)
        {
            if (!preserveVerticalVelocity)
                _rigidbody.velocity = Vector3.zero;
            return;
        }

        if (rigidbodyMovePosition || _rigidbody.isKinematic)
        {
            _rigidbody.MovePosition(_rigidbody.position + displacement);
            return;
        }

        Vector3 velocity = direction * moveSpeed;
        if (preserveVerticalVelocity)
            velocity.y = _rigidbody.velocity.y;

        _rigidbody.velocity = velocity;
    }

    private void ApplyCharacterControllerMove(float deltaTime)
    {
        if (_characterController == null)
            return;

        Vector3 direction = GetWorldMoveDirection();
        Vector3 horizontalMotion = direction * moveSpeed;

        if (applyGravity)
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = groundedStickForce;
            else
                _verticalVelocity += gravity * deltaTime;
        }
        else
        {
            _verticalVelocity = 0f;
        }

        Vector3 motion = (horizontalMotion + Vector3.up * _verticalVelocity) * deltaTime;
        _characterController.Move(motion);
    }

    private void ApplyBodyRotation(float deltaTime)
    {
        Vector3 targetDirection = bodyRotationMode switch
        {
            BodyRotationMode.FaceViewYaw => ViewForward,
            BodyRotationMode.FaceMoveDirection => GetWorldMoveDirection(),
            _ => Vector3.zero
        };

        if (targetDirection.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        ApplyRotationInternal(targetRotation, deltaTime);
    }

    private void ApplyRotationInternal(Quaternion targetRotation, float deltaTime)
    {
        switch (moveMode)
        {
            case MoveMode.Rigidbody when _rigidbody != null && !_rigidbody.isKinematic:
                Quaternion rigidbodyRotation = Quaternion.RotateTowards(
                    _rigidbody.rotation,
                    targetRotation,
                    rotationSpeed * deltaTime);
                _rigidbody.MoveRotation(rigidbodyRotation);
                break;
            default:
                _transform.rotation = Quaternion.RotateTowards(
                    _transform.rotation,
                    targetRotation,
                    rotationSpeed * deltaTime);
                break;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);

        if (Application.isPlaying)
            ValidateModeComponent();
    }
#endif
}
