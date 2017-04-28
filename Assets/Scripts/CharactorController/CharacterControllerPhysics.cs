using System;
using UnityEngine;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CharacterControllerPhysics : MonoBehaviour
{
    #region serialized fields

    [SerializeField] private float _movingTurnSpeed = 360;
    [SerializeField] private float _stationaryTurnSpeed = 180;
    [SerializeField] [Range(0, 1)] private float _airborneAndCrouchedMovementModifier = 0.5f;
    [Range(1f, 4f)] [SerializeField] private float _gravityMultiplier = 2f;
    [SerializeField] private float _moveSpeedMultiplier = 1f;
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] [Range(0.1f, 1)] private float _crouchHeight = .5f;

    #region JumpModifiers

    [SerializeField] private float _jumpPower = 10f; //how high we jump
    [SerializeField] private float _crouchedJumpHeightModifier = 1.2f;
    [SerializeField] private float _crouchJumpBackwardsMomentum = 150f;
    [SerializeField] private float _longJumpVelocityThreshold = 4; //how fast we have to move to be long jumping rather than high junping when crouching.
    [SerializeField] [Range(0.1f,1)] private float _longJumpHeightModifier = .75f;
    [SerializeField] private float _longJumpDistanceModifier = 50f;
    [SerializeField] private float _maxLongJumpVelocity = 10;


    #endregion


    #endregion

    #region fields

    private Rigidbody _rigidbody;

    private bool _isGrounded;

    private float _originalGroundCheckDistance;

    private const float Half = 0.5f;

    private float _turnAmount;

    private float _forwardAmount;

    private Vector3 _groundNormal;

    private float _capsuleHeight;

    private Vector3 _capsuleCenter;

    private CapsuleCollider _capsule;

    private float CrouchedCapsuleHeight
    {
        get { return _capsuleHeight * _crouchHeight; }
    }

    private Vector3 CrouchedCapsuleCenter
    {
        get { return _capsuleCenter - transform.up * ((_capsuleHeight - _capsule.height) / 2); }
    } //make sure to call this after changing the height for accurate results

    private bool _isCrouching;

    #endregion

    private void Awake()
    {
//        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _capsuleHeight = _capsule.height;
        _capsuleCenter = _capsule.center;

        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY |
                                 RigidbodyConstraints.FreezeRotationZ;
        _originalGroundCheckDistance = _groundCheckDistance;
    }


    public void Move(Vector3 move, bool crouch, bool jump)
    {
        // convert the world relative moveInput vector into a local-relative
        // turn amount and forward amount required to head in the desired
        // direction.
        if (move.magnitude > 1f) move.Normalize();
        move = transform.InverseTransformDirection(move);
        CheckGroundStatus();
        move = move.RotatedToPlane(_groundNormal);
        _turnAmount = Mathf.Atan2(move.x, move.z);
        _forwardAmount = move.z;

        ApplyExtraTurnRotation();

        // control and velocity handling is different when grounded and airborne:
        if (_isGrounded)
        {
            Jump(crouch, jump);
        }
        else
        {
            HandleAirborneMovement();
        }

        ScaleCapsuleForCrouching(crouch);
        PreventStandingInLowHeadroom();
        MoveRigidbody(move);
    }

    private void MoveRigidbody(Vector3 move)
    {
        var modifiedMoveSpeed = !_isGrounded || _isCrouching
            ? _moveSpeedMultiplier * _airborneAndCrouchedMovementModifier
            : _moveSpeedMultiplier;
        _rigidbody.AddForce(transform.forward * move.magnitude * Time.deltaTime * modifiedMoveSpeed, ForceMode.Impulse);
    }

    #region Crouching

    private void ScaleCapsuleForCrouching(bool crouch)
    {
        if (_isGrounded && crouch)
        {
            if (_isCrouching) return;
            _capsule.height = CrouchedCapsuleHeight;
            _capsule.center = CrouchedCapsuleCenter;
            _isCrouching = true;
        }
        else
        {
            if (_isGrounded && Crouch()) return;
            _capsule.height = _capsuleHeight;
            _capsule.center = _capsuleCenter;
            _isCrouching = false;
        }
    }

    private void PreventStandingInLowHeadroom()
    {
        // prevent standing up in crouch-only zones
        if (!_isCrouching) Crouch();
    }

    private bool Crouch()
    {
        var halfRadius = _capsule.radius * Half;
        var crouchRay = new Ray(_rigidbody.position + transform.up * halfRadius, transform.up);
        var crouchRayLength = _capsuleHeight - halfRadius;
        if (!Physics.SphereCast(crouchRay, halfRadius, crouchRayLength, Physics.AllLayers,
            QueryTriggerInteraction.Ignore)) return false;
        _isCrouching = true;
        return true;
    }

    #endregion

    private void HandleAirborneMovement()
    {
        // apply extra gravity from multiplier:
        var extraGravityForce = Physics.gravity * _gravityMultiplier - Physics.gravity;
        _rigidbody.AddForce(extraGravityForce);

        //don't check for ground while we're accelerating upwards
        _groundCheckDistance = _rigidbody.velocity.y < 0 ? _originalGroundCheckDistance : 0.01f;
    }


    #region Jumping

    private void Jump(bool crouch, bool jump)
    {
        // check whether conditions are right to allow a jump:
        if (!jump || !_isGrounded) return;

        if (crouch)
        {
            CrouchJump();
        }
        else
        {
            ExecuteJump(_jumpPower);
        }
    }

    private void CrouchJump()
    {
        var velocity = _rigidbody.velocity.magnitude - _rigidbody.velocity.y; //we don't want to be able to launch into long jumps right after falling
        float modifiedJumpPower;
        Vector3 addedForce;

        if (velocity > _longJumpVelocityThreshold)
        {
            //Long jump
            modifiedJumpPower =_jumpPower *_longJumpHeightModifier;
            addedForce = transform.forward * Math.Min(velocity, _maxLongJumpVelocity) * _longJumpDistanceModifier;
        }
        else
        {
            //high jump /backflip
            modifiedJumpPower = _jumpPower * _crouchedJumpHeightModifier;
            addedForce = -transform.forward * _crouchJumpBackwardsMomentum;
        }
        _rigidbody.AddForce(addedForce);
        ExecuteJump(modifiedJumpPower);
    }

    private void ExecuteJump(float jumpForce)
    {
        _rigidbody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        _groundCheckDistance = 0.01f;
    }

    #endregion

    private void ApplyExtraTurnRotation()
    {
        // help the character turn faster (this is in addition to root rotation in the animation)
        var turnSpeed = Mathf.Lerp(_stationaryTurnSpeed, _movingTurnSpeed, _forwardAmount);
        transform.Rotate(0, _turnAmount * turnSpeed * Time.deltaTime, 0);
    }

    #region GroundChecks

    private void CheckGroundStatus()
    {
#if UNITY_EDITOR
        // helper to visualise the ground check ray in the scene view
        Debug.DrawLine(transform.position + transform.up * 0.1f,
            transform.position + transform.up * 0.1f + -transform.up * _groundCheckDistance);
#endif
        // 0.1f is a small offset to start the ray from inside the character
        // it is also good to note that the transform position in the sample assets is at the base of the character
        RaycastHit hitInfo;
        _isGrounded = Physics.SphereCast(transform.position + transform.up * 0.1f, transform.lossyScale.x/2, -transform.up, out hitInfo, _groundCheckDistance);
        _groundNormal = _isGrounded ? hitInfo.normal : transform.up;
//            Physics.Raycast(transform.position + transform.up * 0.1f, -transform.up, out hitInfo, _groundCheckDistance)
//                ? hitInfo.normal
//                : transform.up;
    }

    #endregion
}