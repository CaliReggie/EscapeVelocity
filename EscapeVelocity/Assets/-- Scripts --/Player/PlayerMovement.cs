using UnityEngine;
using TMPro;
using PhysicsExtensions;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerCam))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(MomentumExtension))]
[RequireComponent(typeof(Sliding))]
[RequireComponent(typeof(WallRunning))]
[RequireComponent(typeof(Dashing))]
[RequireComponent(typeof(Grappling))]
[RequireComponent(typeof(LedgeGrabbing))]
[RequireComponent(typeof(Detector))]
[RequireComponent(typeof(PlayerEnergy))]
[RequireComponent(typeof(PlayerAttacks))]

public class PlayerMovement : MonoBehaviour
{
    public enum MovementMode // here are all movement modes defined
    {
        unlimited, // players speed is not being limited at all
        limited, // limit speed to a specific value using EnableLimitedSpeed()
        freeze, // PlayerParent can't move at all
        dashing,
        sliding,
        crouching,
        sprinting,
        walking,
        wallrunning,
        walljumping,
        climbing,
        swinging,
        air
    };

    [Header("Player References")]

    [Tooltip("Here so unity serializes the header above... Click Away!")]
    [SerializeField] private bool placeholderButton;
    
    [field: SerializeField] public Transform PlayerParent { get; private set; }
    
    [field: SerializeField] public Transform PlayerObj { get; private set; }
    
    [field: SerializeField] public Transform Orientation { get; private set; }
    
    [Header("Input References")]
    
    [SerializeField] private string moveActionName = "Move";
    
    [SerializeField] private string jumpActionName = "Jump";
    
    [SerializeField] private string sprintActionName = "Sprint";
    
    [SerializeField] private string crouchActionName = "Crouch";
    
    [Header("Movement Forces")]
    
    [SerializeField] private float moveForce = 12f;
    
    // how much air control you have
    // for example: airMultiplier = 0.5f -> you can only move half as fast will being in the air
    [SerializeField] private float airMultiplier = 0.4f;
    
    [Space]

    [SerializeField] private float groundDrag = 5f;
    
    [Space]

    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float jumpCooldown = 0.25f;
    
    [Header("Player Height Settings")]
    
    [SerializeField] private float crouchColliderHeight = 1f;
    
    [SerializeField] private float crouchColliderCenterY = -0.5f;
    
    [field: SerializeField] public float BasePlayerHeight { get; private set; } = 2f;
    
    [Header("Jumping")]
    
    [SerializeField] private int airJumpsAllowed;
    
    // these are needed for precise jumping and Walljumping
    [field: SerializeField] public float MaxJumpRange { get; private set; } = 5.5f;
    [field: SerializeField] public float MaxJumpHeight { get; private set; } = 2f;

    [Header("Speed handling")]
    
    // these variables define how fast your PlayerParent can move while being in the specific movemt mode
    [SerializeField] private float walkMaxSpeed = 4f;
    [SerializeField] private float sprintMaxSpeed = 7f;
    [SerializeField] private float crouchMaxSpeed = 2f;
    [SerializeField] private float slopeSlideMaxSpeed = 30f;
    [SerializeField] private float wallJumpMaxSpeed = 12f;
    [SerializeField] private float climbMaxSpeed = 3f;
    [SerializeField] private float dashMaxSpeed = 15f;
    [SerializeField] private float swingMaxSpeed = 17f;
    
    [Space]
    
    [SerializeField] private float speedIncreaseMultiplier = 1.5f; // how fast the _maxSpeed changes
    [SerializeField] private float slopeIncreaseMultiplier = 2.5f; // how fast the _maxSpeed changes on a slope

    [Header("Detection")]

    [SerializeField] private float maxSlopeAngle = 40f; // how steep the slopes you walk on can be
    
    [field: SerializeField] public LayerMask WhatIsGround { get; private set; }
    
    [Header("Camera Effects")]
    
    [SerializeField] private float camEffectResetSpeed = 0.1f;
    
    [Header("Debug")]
    
    [SerializeField] private TextMeshProUGUI textSpeed;
    [SerializeField] private TextMeshProUGUI textYSpeed;
    [SerializeField] private TextMeshProUGUI textMoveState;
    [SerializeField] private TextMeshProUGUI textSpeedChangeFactor;
    
    //Dynamic, Non Serialized Below
    
    //References
    private Rigidbody _rb; // the players rigidbody
    private CapsuleCollider _playerColl;
    
    private PlayerCam _playerCamScript;
    
    private WallRunning _wallRunning;
    
    private MomentumExtension _momentumExtension;
    private bool _momentumExtensionEnabled;

    private float _startCollHeight;
    
    private float _startCollCenterY;
    
    private InputAction _moveAction;
    
    private InputAction _jumpAction;
    
    private InputAction _sprintAction;
    
    private InputAction _crouchAction;
    
    //Detection
    private RaycastHit _slopeHit; // variable needed for slopeCheck
    
    // State
    
    private bool _speedLimited;
    
    private int _doubleJumpsLeft;
    
    private bool _readyToJump = true;
    
    private bool _crouchStarted;
    
    //Speeds
    private float _currentLimitedSpeed = 20f; // changes based on how fast the PlayerParent needs to go
    
    private float _desiredMaxSpeed; // needed to smoothly change between speed limitations
    private float _maxSpeed; // this variable changes depending on which movement mode you are in
    
    //IDK if needed, wasn't used in script - Sid
    //private float _desiredMaxSpeedLastFrame; // the previous desired max speed
    
    //IDK if needed, wasn't used in script - Sid
    // public Transform groundCheck;
    //public float groundCheckRadius;

    // how fast your PlayerParent can maximally move on the y axis
    // if set to -1, y speed will not be limited
    //
    private void Awake()
    {
        // get references
        
        _playerCamScript = GetComponent<PlayerCam>();
        _wallRunning = GetComponent<WallRunning>();
        _rb = GetComponent<Rigidbody>();
        _playerColl = GetComponent<CapsuleCollider>();
        
        if (GetComponent<MomentumExtension>() != null)
        {
            _momentumExtension = GetComponent<MomentumExtension>();
            _momentumExtensionEnabled = true;
        }
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
        _moveAction = playerInput.actions.FindAction(moveActionName);
        _jumpAction = playerInput.actions.FindAction(jumpActionName);
        _sprintAction = playerInput.actions.FindAction(sprintActionName);
        _crouchAction = playerInput.actions.FindAction(crouchActionName);
        
        // Set Important Fields
        
        // if the PlayerParent has not yet assigned a groundMask, just set it to "Default"
        if (WhatIsGround.value == 0)
            WhatIsGround = LayerMask.GetMask("Default");
    }

    private void Start()
    {
        // Freeze all rotation on the rigidbody, otherwise the PlayerParent falls over
        // (like you would expect from a capsule with round surface)
        _rb.freezeRotation = true;

        // if MaxYSpeed is set to -1, the y speed of the PlayerParent will be unlimited
        // I only limit it while Climbing or Wallrunning
        MaxYSpeed = -1;
        
        _startCollHeight = _playerColl.height;
        
        _startCollCenterY = _playerColl.center.y;

        _readyToJump = true;
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
        _sprintAction.Enable();
        _crouchAction.Enable();
    }
    
    private void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
        _sprintAction.Disable();
        _crouchAction.Disable();
    }

    private void Update()
    {
        // print("slope" + OnSlope());

        // make sure to call all functions every frame
        MyInput();
        LimitVelocity();
        HandleDrag();
        StateHandler();

        // shooting a raycast down from the middle of the PlayerParent and checking if it hits the ground
        Grounded = Physics.Raycast(transform.position, Vector3.down, BasePlayerHeight * 0.5f + 0.2f, WhatIsGround);

        // if you hit the ground again after double jumping, reset your double jumps
        if (Grounded && _doubleJumpsLeft != airJumpsAllowed)
            ResetDoubleJumps();

        DebugText();
    }

    /// functions that directly move the PlayerParent should be called in FixedUpdate()
    /// this way your movement is not dependent on how many FPS you have
    /// if you call it in void Update, a PlayerParent with 120FPS could move twice as fast as someone with just 60FPS
    private void FixedUpdate()
    {
        // if you're walking, Sprinting, Crouching or in the air, the MovePlayer function, which takes care of all basic movement, should be active
        // this also makes sure that you can't move left or right while Dashing for example
        if (MoveMode == MovementMode.walking || MoveMode == MovementMode.sprinting || MoveMode == MovementMode.crouching || MoveMode == MovementMode.air)
            MovePlayer();

        else
            LimitVelocity();
    }

    #region Input, Movement & Velocity Limiting

    private void MyInput()
    {
        // get movement input
        RawMoveInput = _moveAction.ReadValue<Vector2>();

        // whenever you press the jump key, you're Grounded and _readyToJump (which means jumping is not in cooldown),
        // you want to call the Jump() function
        if(_jumpAction.triggered && Grounded && _readyToJump)
        {
            _readyToJump = false;

            Jump();

            // This will set _readyToJump to true again after the cooldown is over
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // if you press the jump key while being in the air -> perform a double jump
        else if(_jumpAction.triggered && (MoveMode == MovementMode.air || MoveMode == MovementMode.walljumping))
        {
            DoubleJump();
        }

        // if you press the crouch key with no input, start Crouching
        if (_crouchAction.triggered && (RawMoveInput == Vector2.zero))
            StartCrouch();

        // uncrouch again when you release the crouch key
        if (Crouching && !_crouchAction.IsPressed())
            StopCrouch();

        // whenever you press the sprint key, Sprinting should be true
        Sprinting = _sprintAction.IsPressed();
    }

    /// entire function only called when MoveMode == walking, Sprinting Crouching or air
    private void MovePlayer()
    {
        if (Restricted || InternallyRestricted) return;

        // calculate the direction you need to move in
        OrientedMoveInput = Orientation.forward * RawMoveInput.y + Orientation.right * RawMoveInput.x;

        // To Add the movement force, just use Rigidbody.AddForce (with ForceMode.Force, because you are adding force continuously)

        // movement on a slope
        if (OnSlope())
            _rb.AddForce(moveForce * 7.5f * GetSlopeMoveDirection(OrientedMoveInput), ForceMode.Force);

        // movement on ground
        else if(Grounded)
            _rb.AddForce(moveForce * 10f * OrientedMoveInput.normalized, ForceMode.Force);

        // movement in air
        else if(!Grounded)
            _rb.AddForce(moveForce * 10f * airMultiplier * OrientedMoveInput.normalized, ForceMode.Force);
    }

    /// this function is always called
    private void LimitVelocity()
    {
        // get the velocity of your rigidbody without the y axis
        Vector3 rbFlatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        float currYVel = _rb.linearVelocity.y;

        // if you move faster over the x/z axis than you are allowed...
        if (rbFlatVelocity.magnitude > _maxSpeed)
        {
            // ...then first calculate what your maximal velocity would be
            Vector3 limitedFlatVelocity = rbFlatVelocity.normalized * _maxSpeed;

            // and then apply this velocity to your rigidbody
            _rb.linearVelocity = new Vector3(limitedFlatVelocity.x, _rb.linearVelocity.y, limitedFlatVelocity.z);
        }
        
        // if you move faster over the y axis than you are allowed...
        if(MaxYSpeed != -1 && currYVel > MaxYSpeed)
        {
            // special case for Swinging
            if (MoveMode == MovementMode.swinging)
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _maxSpeed * 0.5f, _rb.linearVelocity.z);

            // ...just set your rigidbodys y velocity to you MaxYSpeed, while leaving the x and z axis untouched
            else
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, MaxYSpeed, _rb.linearVelocity.z);
        }
    }

    /// function called the entire time
    private void HandleDrag()
    {
        // if you're walking or Sprinting, apply drag to your rigidbody in order to prevent slippery movement
        if (MoveMode == MovementMode.walking || MoveMode == MovementMode.sprinting)
            _rb.linearDamping = groundDrag;

        // in any other case you don't want any drag
        else
            _rb.linearDamping = 0;
    }

    #endregion

    #region Jump Abilities

    /// called when jumpKeyPressed, _readyToJump and Grounded
    private void Jump()
    {
        // while Dashing you shouldn't be able to jump
        if (Dashing) return;

        // reset the y velocity of your rigidbody, while leaving the x and z velocity untouched
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        // add upward force to your rigidbody
        // make sure to use ForceMode.Impulse, since you're only adding force once
        _rb.AddForce(Orientation.up * jumpForce, ForceMode.Impulse);
    }

    /// called when in air and jumpKey is pressed
    private void DoubleJump()
    {
        // if you don't have any double jumps left, stop the function
        if (_doubleJumpsLeft <= 0) return;

        // this is just for bug-fixing
        if (MoveMode == MovementMode.wallrunning || MoveMode == MovementMode.climbing) return;

        // get _rb velocity without y axis
        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        // find out how large this velocity is
        float flatVelMag = flatVel.magnitude;

        Vector3 inputDirection = Orientation.forward * RawMoveInput.y + Orientation.right * RawMoveInput.x;

        // reset _rb velocity in the correct direction while maintaing speed
        // for example, you're jumping forward, then in the air, you turn around and quickly jump back
        // you now want to take the speed you had in the forward direction and apply it to the backward direction
        // otherwise you would try to jump against your old forward speed
        _rb.linearVelocity = inputDirection.normalized * flatVelMag;

        // add jump force
        // make sure to use ForceMode.Impulse, since you're only adding force once
        _rb.AddForce(Orientation.up * jumpForce, ForceMode.Impulse);

        _doubleJumpsLeft--;
    }

    private void ResetJump()
    {
        _readyToJump = true;
    }

    public void ResetDoubleJumps()
    {
        _doubleJumpsLeft = airJumpsAllowed;
    }

    private Vector3 velocityToSet;
    // Uses Vector Maths to make the PlayerParent jump exactly to a desired position
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight, Vector3 startPosition = new Vector3(), float maxRestrictedTime = 1f)
    {
        InternallyRestricted = true;

        if (startPosition == Vector3.zero) startPosition = transform.position;

        Vector3 velocity = PhysicsExtension.CalculateJumpVelocity(startPosition, targetPosition, trajectoryHeight);

        // enter limited state
        Vector3 flatVel = new Vector3(velocity.x, 0f, velocity.z);
        EnableLimitedState(flatVel.magnitude);

        velocityToSet = velocity;
        Invoke(nameof(SetVelocity), 0.05f);
        Invoke(nameof(EnableMovementNextTouchDelayed), 0.01f);

        Invoke(nameof(ResetRestrictions), maxRestrictedTime);
    }
    
    public void JumpToPositionInTime(Vector3 targetPosition, float timeToReach, Vector3 startPosition = new Vector3(), float maxRestrictedTime = 1f)
    {
        InternallyRestricted = true;

        if (startPosition == Vector3.zero) startPosition = transform.position;

        Vector3 velocity = PhysicsExtension.CalculateJumpVelocityWithTime(startPosition, targetPosition, timeToReach);

        // enter limited state
        Vector3 flatVel = new Vector3(velocity.x, 0f, velocity.z);
        EnableLimitedState(flatVel.magnitude);

        GetComponent<Grappling>().CancelAllHooks();
        
        _rb.linearDamping = 0;
        
        velocityToSet = velocity;
        Invoke(nameof(SetVelocity), 0.05f);
        Invoke(nameof(EnableMovementNextTouchDelayed), 0.01f);

        Invoke(nameof(ResetRestrictions), maxRestrictedTime);
    }
    
    private void SetVelocity()
    {
        _rb.linearVelocity = velocityToSet;
        _playerCamScript.DoFov(-360, camEffectResetSpeed);
        _playerCamScript.DoTilt(-360, camEffectResetSpeed);
    }
    private void EnableMovementNextTouchDelayed()
    {
        enableMovementOnNextTouch = true;
    }

    public void ResetRestrictions()
    {
        if (InternallyRestricted)
        {
            InternallyRestricted = false;
            _playerCamScript.DoFov(-360, camEffectResetSpeed);
            _playerCamScript.DoTilt(-360, camEffectResetSpeed);
        }

        DisableLimitedState();
    }

    #endregion

    #region Crouching

    /// called when crouchKey is pressed down
    private void StartCrouch()
    {
        if (!IsStateAllowed(MovementMode.crouching))
            return;

        //change collider size
        _playerColl.height = crouchColliderHeight;
        
        _playerColl.center = new Vector3(_playerColl.center.x, crouchColliderCenterY, _playerColl.center.z);

        // after shrinking, you'll be a bit in the air, so add downward force to hit the ground again
        // you don't really notice this while playing
        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        Crouching = true;
    }

    /// called when crouchKey is released
    private void StopCrouch()
    {
        //reverse collider size
        _playerColl.height = _startCollHeight;
        
        _playerColl.center = new Vector3(_playerColl.center.x, _startCollCenterY, _playerColl.center.z);

        Crouching = false;
    }

    #endregion

    #region StateMachine

    // Now this is a giantic function I know, but don't worry, it's extremly simple
    // Basically it just decides in which movement mode the PlayerParent is currently in and sets the _maxSpeed accordingly
    // Also in a few states there needs to be done something extra (like in Freeze), then I just added that code in there
    MovementMode movementModeLastFrame;
    MovementMode previousMovementMode;
    private void StateHandler()
    {
        // Mode - Freeze
        if (Freeze)
        {
            MoveMode = MovementMode.freeze;
            _desiredMaxSpeed = 0f;

            // make sure the PlayerParent can't move at all
            _rb.linearVelocity = Vector3.zero;
        }

        // Mode - Unlimited
        else if (UnlimitedSpeed)
        {
            MoveMode = MovementMode.unlimited;

            // this way the PlayerParent can go as fast as he wants
            _desiredMaxSpeed = 1000f;
        }

        // Mode - Limited
        else if (_speedLimited)
        {
            MoveMode = MovementMode.limited;
            _desiredMaxSpeed = _currentLimitedSpeed;
        }

        // Mode - Dashing
        else if (Dashing)
        {
            MoveMode = MovementMode.dashing;
            _desiredMaxSpeed = dashMaxSpeed;
        }

        // SubMode - WallJumping
        else if (Walljumping)
        {
            MoveMode = MovementMode.walljumping;
            _desiredMaxSpeed = wallJumpMaxSpeed;
        }

        // Mode - Wallrunning
        else if (Wallrunning)
        { 
            MoveMode = MovementMode.wallrunning;
            _desiredMaxSpeed = sprintMaxSpeed;
        }

        // Mode - Climbing
        else if (Climbing)
        {
            MoveMode = MovementMode.climbing;
            _desiredMaxSpeed = climbMaxSpeed;
        }

        // Mode - Sliding
        else if (Sliding)
        {
            MoveMode = MovementMode.sliding;

            if (OnSlope() && _rb.linearVelocity.y < 0.2f)
            {
                _desiredMaxSpeed = slopeSlideMaxSpeed;
            }
            else
                _desiredMaxSpeed = _maxSpeed;
        }

        // Mode - Crouching
        else if (Crouching && Grounded)
        {
            MoveMode = MovementMode.crouching;
            _desiredMaxSpeed = crouchMaxSpeed;
        }

        // Mode - Sprint
        else if (Grounded && Sprinting)
        {
            MoveMode = MovementMode.sprinting;
            _desiredMaxSpeed = sprintMaxSpeed;
        }

        // Mode - Walk
        else if (Grounded)
        {
            MoveMode = MovementMode.walking;
            _desiredMaxSpeed = walkMaxSpeed;
        }

        // Mode - Swinging
        else if (Swinging)
        {
            MoveMode = MovementMode.swinging;
            _desiredMaxSpeed = swingMaxSpeed;
        }

        // Mode - Air
        else
        {
            MoveMode = MovementMode.air;

            if (_desiredMaxSpeed < walkMaxSpeed)
                _desiredMaxSpeed = sprintMaxSpeed;

            else
                _desiredMaxSpeed = walkMaxSpeed;
        }
        
        // minimum momentum
        if (_momentumExtensionEnabled && _maxSpeed < _momentumExtension.MinimalMomentum && MoveMode != MovementMode.freeze)
            _maxSpeed = _momentumExtension.MinimalMomentum;

        if (_momentumExtensionEnabled)
            UpdateMomentumBasedMaxSpeed();
        else
        {
            _maxSpeed = _desiredMaxSpeed;
        }
        
        // movement mode switched
        if (movementModeLastFrame != MoveMode)
        {
            previousMovementMode = movementModeLastFrame;

            // update increase and decrease speedChangeFactors
            if (_momentumExtensionEnabled)
            {
                if (MoveMode != MovementMode.air)
                {
                    increaseSpeedChangeFactor = _momentumExtension.GetIncreaseSpeedChangeFactor(MoveMode);
                }
                if (previousMovementMode != MovementMode.air)
                {
                    decreaseSpeedChangeFactor = _momentumExtension.GetDecreaseSpeedChangeFactor(previousMovementMode);
                }
                    
            }
        }

        //_desiredMaxSpeedLastFrame = _desiredMaxSpeed;
        movementModeLastFrame = MoveMode;
    }

    private bool isIncreasingMaxSpeed;
    private float increaseSpeedChangeFactor;
    private float decreaseSpeedChangeFactor;
    private void UpdateMomentumBasedMaxSpeed()
    {
        if (!_momentumExtensionEnabled)
        {
            Debug.LogError("Trying to update _maxSpeed based on momentum but _momentumExtension is not enabled");
            return;
        }

        if (_maxSpeed == _desiredMaxSpeed)
        {
            return;
        }
            

        isIncreasingMaxSpeed = _desiredMaxSpeed > _maxSpeed;
        float speedChangeFactor = isIncreasingMaxSpeed ? increaseSpeedChangeFactor : decreaseSpeedChangeFactor;

        if (!isIncreasingMaxSpeed)
            speedChangeFactor *= _momentumExtension.GetSurfaceSpeedDecreaseFactor(Grounded);

        if (speedChangeFactor == -1)
        {
            _maxSpeed = _desiredMaxSpeed;
            return;
        }
        
        if (isIncreasingMaxSpeed)
        {
            // only increase max speed if trying to reach it
            if (_rb.linearVelocity.magnitude < _maxSpeed - 1)
                return;

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f) * 2f;

                _maxSpeed += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease * speedChangeFactor;
            }
            else
                _maxSpeed += Time.deltaTime * speedIncreaseMultiplier * speedChangeFactor;
        }
        else
        {
            _maxSpeed -= Time.deltaTime * speedIncreaseMultiplier * speedChangeFactor;
        }
    }

    private void EnableLimitedState(float speedLimit)
    {
        _currentLimitedSpeed = speedLimit;
        _speedLimited = true;
    }
    private void DisableLimitedState()
    {
        _speedLimited = false;
    }

    #endregion

    #region Variables

    public bool OnSlope()
    {
        // shoot a raycast down to check if you hit something
        // the "out _slopeHit" bit makes sure that you store the information of the object you hit
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, BasePlayerHeight * 0.5f + 0.5f))
        {
            // calculate the angle of the ground you're standing on (how steep it is)
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);

            // check if the angle is smaller than your maxSlopeAngle
            // -> that means you're standing on a slope -> return true
            return angle < maxSlopeAngle && angle != 0;
        }

        // if the raycast doesn't hit anything, just return false
        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        // calcualte the direction you need to move relative to the slope you're standing on
        return Vector3.ProjectOnPlane(direction, _slopeHit.normal).normalized;
    }

    #endregion

    #region Moving Platforms

    private Rigidbody movingPlatform;
    public void AssignPlatform(Rigidbody platform)
    {
        movingPlatform = platform;
    }
    public void UnassignPlatform()
    {
        movingPlatform = null;
    }

    #endregion

    #region Collision Detection

    private bool enableMovementOnNextTouch;
    private void OnCollisionEnter(Collision collision)
    {
        bool touch = false;

        // for (int i = 0; i < collision.contactCount; i++)
        // {
        //     if (collision.collider.gameObject.layer == 9 || collision.collider.gameObject.layer == 10)
        //         touch = true;
        // }
        
        //touch true if layer is ground, not using layer numbers but comparing to WhatIsGround
        foreach (ContactPoint contact in collision.contacts)
        {
            if (WhatIsGround == (WhatIsGround | (1 << contact.otherCollider.gameObject.layer)))
            {
                touch = true;
                break;
            }
        }

        if (enableMovementOnNextTouch && touch)
        {
            // I don't know anymore lol
            GetComponent<Grappling>().OnObjectTouch(); // this stops active grapples
            
            enableMovementOnNextTouch = false;
            ResetRestrictions();
        }
    }

    #endregion

    #region Getters and Setters

    public bool IsStateAllowed(MovementMode movementMode)
    {
        if (!_momentumExtensionEnabled)
            return true;

        return _momentumExtension.IsStateAllowed(movementMode, _maxSpeed);
    }
    
    //State Handling
    
    // these bools are activated from different scripts
    // if for example the Wallrunning bool is set to true, the movement mode will change to MovementMode.Wallrunning#
    
    public bool Grounded { get; private set; }
    
    public MovementMode MoveMode { get; private set; } // this variable stores the current movement mode of the PlayerParent

    public bool Freeze { get; set; }
    public bool UnlimitedSpeed { get; set; }
    public bool Restricted { get; set; }
    public bool InternallyRestricted { get; private set; }
    public bool Sprinting { get; set; }
    public bool Climbing { get; set; }
    public bool Sliding { get; set; }
    public bool Dashing { get; set; }
    public bool Crouching { get; set; }
    public bool Swinging { get; set; }
    public bool Wallrunning { get; set; }
    public bool Walljumping { get; set; }
    
    public Vector2 RawMoveInput { get; private set; }
    
    public Vector3 OrientedMoveInput { get; private set; }
    
    public float MaxYSpeed { get; set; }

    #endregion

    #region Text Displaying

    private void DebugText()
    {
        if (textSpeed != null)
        {
            Vector3 rbFlatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            textSpeed.SetText("Speed: " + Round(rbFlatVelocity.magnitude, 1) + "/" + Round(_maxSpeed,0));
        }

        if (textYSpeed != null)
            textYSpeed.SetText("Y Speed: " + Round(_rb.linearVelocity.y, 1));

        if (textMoveState != null)
            textMoveState.SetText(MoveMode.ToString());

        if (!_momentumExtensionEnabled)
            return;

        if (textSpeedChangeFactor != null)
        {
            if (isIncreasingMaxSpeed)
                textSpeedChangeFactor.SetText("Increase: " + increaseSpeedChangeFactor.ToString());
            else
            {
                textSpeedChangeFactor.SetText("Decrease: " + (decreaseSpeedChangeFactor*_momentumExtension.GetSurfaceSpeedDecreaseFactor(Grounded)).ToString());
            }
        }
    }

    public static float Round(float value, int digits)
    {
        float mult = Mathf.Pow(10.0f, (float)digits);
        return Mathf.Round(value * mult) / mult;
    }
    
    public float MaxSpeed { get { return _maxSpeed; } }

    #endregion
}