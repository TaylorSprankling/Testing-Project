using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
public class PlayerMovement3D : MonoBehaviour
{
#region FIELDS
    [Header("Ground Settings")]
    [SerializeField] private float _baseMovementSpeed = 4f;
    [SerializeField] private float _sprintMovementSpeed = 6f;
    [SerializeField] private float _groundedDistance = 0.01f;
    [SerializeField] private float _groundedLinearDamping = 25f;
    [SerializeField] private float _maxSlopeAngle = 45f;
    
    [Header("Air Settings")]
    [SerializeField] private float _jumpHeight = 2f;
    [Tooltip("The scale at which to apply the movement speed force while in the air. 1 = 1s, 2 = 0.5s, 4 = 0.25s etc.")]
    [SerializeField] private float _airControlMultiplier = 5f;
    [SerializeField] private float _terminalVelocity = 100f;
    [SerializeField] private float _gravityMultiplier = 1f;
    
    private CapsuleCollider _capsuleCollider;
    private Rigidbody _rigidBody;
    private PlayerInput _playerInput;
    private Transform _playerOrientation;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;
    private RaycastHit _groundedHit;
    private float _effectiveMovementSpeed;
    private bool _isGrounded;
    private bool _onSteepSlope;
    private bool _inJumpStartup;
    private bool _sprintPressed;
#endregion
    
#region UNITY METHODS
    private void Reset()
    {
        GetReferences();
    }
    
    private void Awake()
    {
        GetReferences();
    }
    
    private void OnEnable()
    {
        _playerInput.onActionTriggered += HandleInputAction;
    }
    
    private void OnDisable()
    {
        _playerInput.onActionTriggered -= HandleInputAction;
    }
    
    private void Update()
    {
        ConvertMoveInputDirection();
        CheckGround();
        HandleDamping();
    }
    
    private void FixedUpdate()
    {
        ApplyMovementForce();
        ApplyAirDrag();
        ApplyGravityMultiplier();
    }
#endregion
    
#region PRIVATE METHODS
    private void GetReferences()
    {
        if (!_capsuleCollider) _capsuleCollider = GetComponent<CapsuleCollider>();
        if (!_rigidBody) _rigidBody = GetComponent<Rigidbody>();
        if (!_playerInput) _playerInput = GetComponent<PlayerInput>();
        if (!_playerOrientation) _playerOrientation = GetComponentInChildren<PlayerOrientation>().transform;
    }
    
    private void HandleInputAction(InputAction.CallbackContext context)
    {
        switch (context.action.name)
        {
            case "Move":
            {
                _moveInput = context.ReadValue<Vector2>();
                break;
            }
            
            case "Sprint":
            {
                _sprintPressed = context.ReadValueAsButton();
                break;
            }
            
            case "Jump":
            {
                if (context.performed) Jump();
                break;
            }
        }
    }
    
    private void ConvertMoveInputDirection()
    {
        // Get the movement direction relative to players facing direction
        Vector3 inputDirection3D = new Vector3(_moveInput.x, 0, _moveInput.y);
        Quaternion relativeRotation = Quaternion.AngleAxis(_playerOrientation.eulerAngles.y, Vector3.up);
        _moveDirection = relativeRotation * inputDirection3D;
    }
    
    private void CheckGround()
    {
        float sphereCastRadius = _capsuleCollider.radius * 0.5f;
        Vector3 originOffset = Vector3.up * sphereCastRadius + Vector3.up * 0.1f;
        float maxDistance = _groundedDistance + originOffset.magnitude;
        bool castHit = Physics.SphereCast(transform.position + originOffset, sphereCastRadius, Vector3.down, out _groundedHit, maxDistance);
        _onSteepSlope = Vector3.Angle(Vector3.up, _groundedHit.normal) >= _maxSlopeAngle;
        _isGrounded = castHit && !_onSteepSlope;
    }
    
    private void HandleDamping()
    {
        switch (_isGrounded)
        {
            case true when !_inJumpStartup:
            {
                _rigidBody.linearDamping = _groundedLinearDamping;
                _rigidBody.useGravity = _onSteepSlope;
                break;
            }
            
            case true when _inJumpStartup:
            {
                _rigidBody.linearDamping = 0;
                _rigidBody.useGravity = _rigidBody.linearVelocity.y > -_terminalVelocity;
                break;
            }
            
            case false:
            {
                _rigidBody.linearDamping = 0;
                _rigidBody.useGravity = _rigidBody.linearVelocity.y > -_terminalVelocity;
                _inJumpStartup = false;
                break;
            }
        }
    }
    
    private void ApplyMovementForce()
    {
        // Adjust move direction in direction of ground slope angle
        Vector3 relativeSlopeMoveDirection = Vector3.ProjectOnPlane(_moveDirection, _groundedHit.normal);
        
        switch (_isGrounded)
        {
            case true:
            {
                // Set movement speed accordingly (could probably make this better if movement speed will be changed often)
                _effectiveMovementSpeed = _sprintPressed switch { true => _sprintMovementSpeed, _ => _baseMovementSpeed };
                
                // Calculate multiplier required to equalize drag
                float fixedTimestepFrequency = 1f / Time.fixedDeltaTime;
                float dragMultiplier = _rigidBody.linearDamping / ((fixedTimestepFrequency - _rigidBody.linearDamping) / fixedTimestepFrequency);
                
                // Add the force in the move direction relative to the object facing direction
                _rigidBody.AddForce(_effectiveMovementSpeed * dragMultiplier * relativeSlopeMoveDirection, ForceMode.Acceleration);
                break;
            }
            
            case false when _onSteepSlope && relativeSlopeMoveDirection.y > 0:
            {
                // When trying to move up the steep slope prevent adding force in direction of the slope
                Vector3 horizontalAwayFromSlope = Quaternion.Euler(0, 90f, 0) * Vector3.Cross(_groundedHit.normal, Vector3.up);
                float moveDirectionAngleToSlope = Vector3.Angle(_moveDirection, horizontalAwayFromSlope) - 90f;
                Vector3 newMoveDirection = _moveDirection + Mathf.Sin(moveDirectionAngleToSlope * Mathf.Deg2Rad) * horizontalAwayFromSlope;
                _rigidBody.AddForce(_effectiveMovementSpeed * _airControlMultiplier * newMoveDirection, ForceMode.Acceleration);
                break;
            }
            
            case false:
            {
                // With no object underneath or not trying to move up slope, apply normal air movement
                _rigidBody.AddForce(_effectiveMovementSpeed * _airControlMultiplier * _moveDirection, ForceMode.Acceleration);
                break;
            }
        }
    }
    
    private void ApplyAirDrag()
    {
        // Calculate if moving through the air faster than movement speed allows
        Vector3 horizontalMovement = new Vector3(_rigidBody.linearVelocity.x, 0f, _rigidBody.linearVelocity.z);
        if (horizontalMovement.magnitude >= _effectiveMovementSpeed)
        {
            // Apply some artificial horizontal drag if over movement speed magnitude
            _rigidBody.AddForce(_effectiveMovementSpeed * _airControlMultiplier * -horizontalMovement.normalized, ForceMode.Acceleration);
        }
    }
    
    private void ApplyGravityMultiplier()
    {
        if (!_rigidBody.useGravity) return;
        _rigidBody.AddForce((_gravityMultiplier - 1f) * Physics.gravity.magnitude * Vector3.down, ForceMode.Acceleration);
    }
    
    private void Jump()
    {
        if (!_isGrounded) return;
        // Calculated required force to achieve jump height
        float gravityForce = _gravityMultiplier * Physics.gravity.magnitude;
        float jumpForce = Mathf.Sqrt(2 * gravityForce * _jumpHeight);
        _inJumpStartup = true;
        _rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }
#endregion
}