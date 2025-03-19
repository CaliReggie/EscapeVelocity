using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class WallRunning : MonoBehaviour
{
    private enum State
    {
        ledgegrabbing,
        wallrunning,
        climbing,
        sliding,
        exiting,
        none
    }
    
    [Header("Toggle Abilites")]
    
    [SerializeField] private bool enablePrecisionMode = true;
    
    [Header("Input References")]
    
    [SerializeField] private string wallJumpActionName = "Jump";
    
    //not enough keybinds, rip
    // [SerializeField] private string upwardsRunActionName = "AltRunUp";
    //
    // [SerializeField] private string downwardsRunActionName = "AltRunDown";
    
    [Header("Detection")]
    
    [SerializeField] private LayerMask whatIsWall;
    
    [Space]
    
    [SerializeField] private float minWallNormalAngleChange = 15f; // the minimum angle change that is needed to count a surface as a new wall
    
    [Header("Wall Run Forces")]
    
    [SerializeField] private float wallRunForce = 200f; // forward wallRunForce, Note: maxSpeed while Wallrunning is defined in PlayerMovement
    [SerializeField] private float wallJumpSideForce = 10f; // sidewards force of your wall jump (pushes you away from the wall when jumping)
    
    [Space]
    
    [SerializeField] private bool useGravity;
    
    [SerializeField] private float gravityCounterForce = 28f; // the higher the value, the lower the effect of gravity while Wallrunning
    
    [SerializeField] private float wallJumpUpForce = 10f; // upward force of your wall jump
    
    [Space]
    
    [SerializeField] private float wallrunClimbSpeed = 4f; // how fast you can move on the y axis when Wallrunning diagonally
    
    [Header("Wall Run Behaviour")]
    
    [SerializeField] private float maxWallRunTime = 1f;
    
    [SerializeField] private float wallJumpDuration = 0.5f; // the duration of the wallJump, afterwards speed gained decreases quickly back to 
    
    [Space]
    
    [SerializeField] private bool doJumpOnEndOfTimer; // when active, wall jump happens automatically when timer runs out
    
    [Space]
    [SerializeField] private bool resetJumpsOnNewWall = true; // when active, double jumps get resetted when PlayerParent hits a new wall
    [SerializeField] private bool resetJumpsOnEveryWall = false; // when active, double jumps get resetted when PlayerParent hits a wall (always!)
    
    [Space]
    
    [SerializeField] private int allowedWallJumps = 1; // wall jumps allowed (resets on new wall)

    [Header("Climbing Forces")]
    
    [SerializeField] private float climbJumpUpForce = 10f; // upward force of your climb jump
    [SerializeField] private float climbJumpBackForce = 5f; // backwards force of your climb jump (pushes you away from the wall)
    
    [Space]
    
    [SerializeField] private float maxClimbYSpeed = 10f; // max upward speed while Climbing
    
    [Space]
    
    [SerializeField] private float backWallJumpUpForce = 5f;
    [SerializeField] private float backWallJumpForwardForce = 12f;
    
    
    [Header("Climbing Behaviour")]
    
    [SerializeField] private float maxClimbTime = 0.75f;
    
    [Space]
    
    [SerializeField] private float minFrontWallAngle = 80; // how steep the wall needs to be
    
    [Space]
    
    [SerializeField] private float maxWallLookAngle = 30f; // if you look at the wall with an angle of let's say 45 degrees, you can't climb
    
    [Space]
    
    [SerializeField] private int allowedClimbJumps = 1; // climb jumps allowed (resets on new wall)
    
    [Header("Vaulting")]
    
    [SerializeField] private float vaultDetectionLength = 1.2f;
    
    [Space]
    
    [SerializeField] private float maxVaultClimbYSpeed = 10f;
    
    [Space]
    
    [SerializeField] private bool topReached;
    
    [Header("Camera Effects")]
    
    [SerializeField] private float wallRunFOV = 110f; // the fov of the camera while Wallrunning
    [SerializeField] private float wallRunFOVChangeSpeed = 0.2f; // how fast the fov changes while Wallrunning
    
    [Space]
    
    [SerializeField] private float wallRunTilt = 5f; // the tilt of the camera while Wallrunning
    [SerializeField] private float wallRunTiltChangeSpeed = 0.2f; // how fast the tilt changes while Wallrunning
    
    //Dynamic, Non-Serialized Below
    
    // this entire section is for defining how long the raycasts forward, sideways and backwards are
    /// these values should work just fine for your game, but if you still want to change them
    /// just set them to public and change them inside of Unity
    private float _doubleRayCheckDistance = 0.1f;
    private float _wallDistanceSide = 0.7f;
    private float _wallDistanceFront = 1f;
    private float _wallDistanceBack = 1f;

    private float _minJumpHeight = 2f; // the minimal height the PlayerParent needs to have in order to wallRun

    private float _exitWallTime = 0.2f; // just a variable needed to exit walls correctly
    private float _exitWallTimer;

    private bool _vaultClimbStarted;
    private bool _readyToVault;
    private bool _vaultPerformed;
    private bool _midCheck;
    private bool _feetCheck;
    
    //IDK if needed, these weren't used in the script - Sid
    // public float climbForce = 200f; // upward force while Climbing

    // public float pushToWallForce = 100f; // the force that keeps you on the wall

    // private bool readyToClimb; // is true if PlayerParent hits a new wall or has sucessfully exited the old one

    // public float vaultJumpForwardForce;
    // public float vaultJumpUpForce;
    // public float vaultCooldown;
    
    // public float customGravity; // apply custom gravity while Wallrunning
    
    //Player References
    
    private PlayerMovement _pm;
    
    private LedgeGrabbing _lg;
    
    private Detector _dt;
    
    private PlayerCam _playerCamScript;
    
    private Rigidbody _rb;
    
    private Transform _orientation;
    
    private InputAction _wallJumpAction;
    
    // private InputAction _upwardsRunAction;
    //
    // private InputAction _downwardsRunAction;
    
    private LayerMask _whatIsGround;
    
    //Timing
    private float _wallRunTimer;
    
    private float _climbTimer;
    
    //Detection
    
    private bool _wallFront;
    private bool _wallBack;
    
    
    private float _wallLookAngle;
    private float _frontWallAngle;
    
    private Transform _lastWall; // the transform of the wall the PlayerParent previously touched
    private Vector3 _lastWallNormal; // the normal of the wall the prlayer previously touched
    
    //State
    private State _state; // this variable stores the current State
    
    //bools that signal _state of the PlayerParent input
    private bool _upwardsRunning;
    private bool _downwardsRunning;
    private float _horizontalInput;
    private float _verticalInput;
    
    // booleans that get activated when the raycasts hit something
    private bool _wallLeft;
    private bool _wallLeft2;
    private bool _wallRight;
    private bool _wallRight2;
    
    private bool _exitingWall; // needed to exit walls correctly

    private bool _wallRemembered;

    // RaycastHit variables for all of the raycasts that get performed
    /// These variables store the information of where objects were hit
    private RaycastHit _leftWallHit;
    private RaycastHit _leftWallHit2;
    private RaycastHit _rightWallHit;
    private RaycastHit _rightWallHit2;

    private RaycastHit _frontWallHit;
    private RaycastHit _backWallHit;

    // jump counters
    private int _wallJumpsDone;
    private int _climbJumpsDone;
    
    //Debug
    private TextMeshProUGUI _text_wallState; // displaying text ingame

    private void Awake()
    {
        //get references
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
        _orientation = _pm.Orientation;
        _playerCamScript = GetComponent<PlayerCam>();
        _lg = GetComponent<LedgeGrabbing>();
        _dt = GetComponent<Detector>();
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
        _wallJumpAction = playerInput.actions.FindAction(wallJumpActionName);
        // upwardsRunAction = playerInput.actions.FindAction(upwardsRunActionName);
        // downwardsRunAction = playerInput.actions.FindAction(downwardsRunActionName);
        
        _text_wallState = UIManager.Instance.WallStateText;
    }

    private void Start()
    {
        // if the layermasks are not set to anything, set them to "Default"
        if (whatIsWall.value == 0)
            whatIsWall = LayerMask.GetMask("Default");
        
        //wait on this for pm to have initialized
        _whatIsGround = _pm.WhatIsGround;

        if (_whatIsGround.value == 0)
            _whatIsGround = LayerMask.GetMask("Default");
    }

    private void OnEnable()
    {
        _wallJumpAction.Enable();
        // upwardsRunAction.Enable();
        // downwardsRunAction.Enable();
    }
    
    private void OnDisable()
    {
        _wallJumpAction.Disable();
        // upwardsRunAction.Disable();
        // downwardsRunAction.Disable();
    }

    private void Update()
    {
        // make sure to call these funcitons every frame
        CheckForWall();
        StateMachine();
        MyInput();

        // if Grounded, next wall should be considered a new one
        if (_pm.Grounded && _lastWall != null && !_pm.Climbing)
            _lastWall = null; // by setting the _lastWall to null -> next wall will definitely be seen as a new one

        // just setting the Ui text, ignore
        if(_text_wallState != null)
            _text_wallState.SetText(_state.ToString());
    }

    private void FixedUpdate()
    {
        // if Wallrunning has started continously call WallRunningMovement()
        if (_pm.Wallrunning && !_exitingWall)
            WallRunningMovement();

        // if Climbing has started continuously call ClimbingMovment()
        if (_pm.Climbing && !_exitingWall)
            ClimbingMovement();
    }

    #region Input & Wallchecks

    private void MyInput()
    {
        // get W,A,S,D input
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        // // upwards and downwards running
        // _upwardsRunning   = upwardsRunAction.IsPressed();
        // _downwardsRunning = downwardsRunAction.IsPressed();

        // if you're pressing the jump key while Wallrunning, or if there's a wall in front or back of you
        if (_wallJumpAction.triggered && (_pm.Wallrunning || _wallFront || _wallBack) && !_lg.ExitingLedge)
        {
            _pm.MaxYSpeed = -1; // make sure the players Y speed is unlimited
            WallJump(); // perform a wall jump
        }

        // when Climbing, then stopping before the _climbTimer runs out, then reentering
        // -> Start Climbing again
        // Note: This is just for reentering, the main way to start Climbing is executed by the StateHandler
        if (!_pm.Climbing && _verticalInput > 1 && _wallFront && _climbTimer > 0 && !_exitingWall)
            StartClimbing();
    }

    // this function seems complex, but it's really just a bunch of simple raycasts
    // to better understand it, check out the explanation in the 10 Day Learning Plan
    private void CheckForWall()
    {
        float difference = _doubleRayCheckDistance * 0.5f;
        Vector3 differenceV = _orientation.forward * difference;

        _wallLeft = Physics.Raycast(transform.position - differenceV, -_orientation.right, out _leftWallHit, _wallDistanceSide, whatIsWall);
        _wallLeft2 = Physics.Raycast(transform.position + differenceV, -_orientation.right, out _leftWallHit2, _wallDistanceSide, whatIsWall);

        _wallRight = Physics.Raycast(transform.position - differenceV, _orientation.right, out _rightWallHit, _wallDistanceSide, whatIsWall);
        _wallRight2 = Physics.Raycast(transform.position + differenceV, _orientation.right, out _rightWallHit2, _wallDistanceSide, whatIsWall);

        // _wallFront = Physics.Raycast(transform.position, Orientation.forward, out _frontWallHit, _wallDistanceFront, whatIsWall);
        _wallFront = Physics.SphereCast(transform.position, 0.25f, _orientation.forward, out _frontWallHit, _wallDistanceFront, whatIsWall);
        _wallBack = Physics.Raycast(transform.position, -_orientation.forward, out _backWallHit, _wallDistanceBack, whatIsWall);

        _wallLookAngle = Vector3.Angle(_orientation.forward, -_frontWallHit.normal);

        // reset readyToClimb and wallJumps whenever PlayerParent hits a new wall
        if (_wallLeft || _wallRight || _wallFront || _wallBack)
        {
            if (NewWallHit())
            {
                // ResetReadyToClimb();
                ResetWallJumpsDone();

                if (resetJumpsOnNewWall)
                    _pm.ResetDoubleJumps();

                _wallRunTimer = maxWallRunTime;
                _climbTimer = maxClimbTime;
            }

            if(resetJumpsOnEveryWall)
                _pm.ResetDoubleJumps();
        }

        // vaulting
        _midCheck = Physics.Raycast(transform.position, _orientation.forward, vaultDetectionLength, whatIsWall);

        RaycastHit feetHit;
        _feetCheck = Physics.Raycast(transform.position + new Vector3(0, -0.9f, 0), _orientation.forward, out feetHit, vaultDetectionLength, whatIsWall);

        _frontWallAngle = Vector3.Angle(_orientation.up, feetHit.normal); // calculate how steep the wall is

        // _feetCheck = Physics.SphereCast(transform.position + new Vector3(0, -0.9f, 0), 0.1f, Orientation.forward, out feetHit, vaultDetectionLength, whatIsWall);

        topReached = _feetCheck && !_midCheck;
    }

    /// a bool called to check if PlayerParent has hit a new wall
    private bool NewWallHit()
    {
        // if _lastWall is null, next one is definitely a new wall -> return true
        if (_lastWall == null)
            return true;

        // the following statements just check if the new wall (stored in the WallHit variables) is equal to the _lastWall
        // if not -> new Wall has been hit -> return true

        if (_wallLeft && (_leftWallHit.transform != _lastWall || Mathf.Abs(Vector3.Angle(_lastWallNormal, _leftWallHit.normal)) > minWallNormalAngleChange))
            return true;

        else if (_wallRight && (_rightWallHit.transform != _lastWall || Mathf.Abs(Vector3.Angle(_lastWallNormal, _rightWallHit.normal)) > minWallNormalAngleChange))
            return true;

        else if (_wallFront && (_frontWallHit.transform != _lastWall || Mathf.Abs(Vector3.Angle(_lastWallNormal, _frontWallHit.normal)) > minWallNormalAngleChange))
            return true;

        else if (_wallBack && (_backWallHit.transform != _lastWall || Mathf.Abs(Vector3.Angle(_lastWallNormal, _backWallHit.normal)) > minWallNormalAngleChange))
            return true;

        // if everything above did not happen, no new wall has been hit -> return false
        return false;
    }

    /// This function is called in WallRunningMovement() and ClimbingMovement()
    /// It simply stores the Transform and Normal of the current wall
    private void RememberLastWall()
    {
        if (_wallLeft)
        {
            _lastWall = _leftWallHit.transform;
            _lastWallNormal = _leftWallHit.normal;
        }

        if (_wallRight)
        {
            _lastWall = _rightWallHit.transform;
            _lastWallNormal = _rightWallHit.normal;
        }

        if (_wallFront)
        {
            _lastWall = _frontWallHit.transform;
            _lastWallNormal = _frontWallHit.normal;
        }

        if (_wallBack)
        {
            _lastWall = _backWallHit.transform;
            _lastWallNormal = _backWallHit.normal;
        }
    }

    private bool CanWallRun()
    {
        // cast a ray down using the _minJumpHeight as distance
        // if this ray hits something, the PlayerParent is not high enough in the air to wallrun
        return !Physics.Raycast(transform.position, Vector3.down, _minJumpHeight, _whatIsGround);
    }

    #endregion

    #region Wallrunning

    private void StartWallRun()
    {
        if (!_pm.IsStateAllowed(PlayerMovement.MovementMode.wallrunning))
            return;

        // this will cause the PlayerMovement script to enter the MovementState.Wallrunning
        _pm.Wallrunning = true;

        // limit the y speed while Wallrunning
        _pm.MaxYSpeed = maxClimbYSpeed;

        _wallRemembered = false;

        // increase camera fov
        _playerCamScript.DoFov(wallRunFOV, wallRunFOVChangeSpeed);

        RememberLastWall();

        // set camera tilt
        if (_wallRight) _playerCamScript.DoTilt(wallRunTilt ,wallRunTiltChangeSpeed);
        if(_wallLeft) _playerCamScript.DoTilt(-wallRunTilt, wallRunTiltChangeSpeed);
    }

    private void WallRunningMovement()
    {
        // make sure normal gravity is turned off while Wallrunning
        _rb.useGravity = useGravity;

        // choose the correct wall normal (direction away from the wall)
        Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;

        // calculate the forward direction of the wall
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        /// sometimes you need to switch it
        if ((_orientation.forward - wallForward).magnitude > (_orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        // forward force
        _rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        // upwards/downwards force
        if (_upwardsRunning)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, wallrunClimbSpeed, _rb.linearVelocity.z);
        if (_downwardsRunning)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -wallrunClimbSpeed, _rb.linearVelocity.z);

        // push to wall force
        if (!(_wallLeft && _horizontalInput > 0) && !(_wallRight && _horizontalInput < 0))
            _rb.AddForce(-wallNormal * 100, ForceMode.Force);

        // weaken gravity
        if (useGravity)
            _rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);

        // remember the last wall
        if (!_wallRemembered)
        {
            RememberLastWall();
            _wallRemembered = true;
            // Note: The _wallRemembered bool might seem unnecessary but it isn't
            // Without it, the wall would be remembered every single frame, but this way, it's only called once for every new wallrun
        }
    }

    /// called when _wallRunTimer runs out, PlayerParent performs a walljump or PlayerParent exits the wall on another way
    private void StopWallRun()
    {
        // activate gravity again
        _rb.useGravity = true;

        // leave the MovementMove.Wallrunning
        _pm.Wallrunning = false;

        // this will make sure that the y speed of the PlayerParent is unlimited again
        _pm.MaxYSpeed = -1;

        // reset camera fov and tilt
        _playerCamScript.DoFov(-360, wallRunFOVChangeSpeed);
        _playerCamScript.DoTilt(-360, wallRunTiltChangeSpeed);
    }

    #endregion

    #region Climbing

    private void StartClimbing()
    {
        if (!_pm.IsStateAllowed(PlayerMovement.MovementMode.climbing))
            return;

        // this will cause the PlayerMovement script to enter MovementState.Climbing
        _pm.Climbing = true;

        // limit the players y speed to your maxClimbSpeed, or vaultClimbSpeed
        // _pm.MaxYSpeed = topReached ? maxVaultClimbYSpeed : maxClimbYSpeed;
        // _pm.MaxYSpeed = maxClimbYSpeed;

        if (topReached) _vaultClimbStarted = true;

        // disable gravity
        _rb.useGravity = false;

        _wallRemembered = false;

        RememberLastWall();

        //CHANGE TODO: THIS
        // shake the camera, just a cool visual effect
        // _playerCamScript.DoShake(1, 1);
    }

    private void ClimbingMovement()
    {
        // make sure that gravity stays off
        if (_rb.useGravity != false)
            _rb.useGravity = false;

        float speed = topReached ? maxVaultClimbYSpeed : maxClimbYSpeed;
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, speed, _rb.linearVelocity.z);

        /* calculate directions

        Vector3 upwardsDirection = Vector3.up;

        // calculate the direction from the PlayerParent to the wall
        Vector3 againstWallDirection = (_frontWallHit.point - Orientation.position).normalized;

        // add upwards force
        _rb.AddForce(upwardsDirection * climbForce, ForceMode.Force);

        // push PlayerParent to wall
        // _rb.AddForce(againstWallDirection * pushToWallForce, ForceMode.Force);

        // remember the last wall
        if (!_wallRemembered)
        {
            RememberLastWall();
            _wallRemembered = true;
        }
        */
    }

    private void StopClimbing()
    {
        // activate the gravity again
        _rb.useGravity = true;

        // exit MovementMode.Climbing
        _pm.Climbing = false;

        // make sure the players y speed is no longer limited
        _pm.MaxYSpeed = -1;

        // no longer readyToClimb until the wall is sucessfully exited
        // readyToClimb = false;

        //CHANGE TODO: THIS
        // reset RealCam shake
        // _playerCamScript.ResetShake();

        //CHANGE
        // reset RealCam fov and tilt
        // _playerCamScript.ResetFov();
        // _playerCamScript.ResetTilt();

        _vaultClimbStarted = false;
    }

    // called when the PlayerParent has sucessfully exited a wall
    // makes Climbing possible again
    // private void ResetReadyToClimb()
    // {
    //     readyToClimb = true;
    // }

    #endregion

    #region Walljumping

    /// Inside of this function the decision between normal or precise wall/climb jumping is made
    private void WallJump()
    {
        if (!_pm.IsStateAllowed(PlayerMovement.MovementMode.walljumping))
            return;

        if (!enablePrecisionMode)
        {
            NormalWallJump();
            return;
        }

        bool sideWall = _wallLeft || _wallRight;

        if (sideWall && _dt.PrecisionTargetFound) PreciseWallJump();
        else NormalWallJump();
    }

    private void NormalWallJump()
    {
        if (LedgeGrabbing) return;

        // once again, the full explanation how wall jumping works can be found in the 10 Day Learning Plan

        bool applyUpwardForce = true;

        _exitingWall = true;
        _exitWallTimer = _exitWallTime;

        // create a new vector to store the forceToApply
        Vector3 forceToApply = new Vector3();

        // I'll explain the next if, else if statements all at once here:
        //
        // 1. calculate the force to apply by using transform.up and
        // the normal of the wall hit (which is the direction away from the wall!)
        // 
        // 2. if you still have allowed jumps on this wall, set applyUpwardForce to true
        // 
        // 3. count up the jumps done

        if (_wallLeft)
        {
            forceToApply = transform.up * wallJumpUpForce + _leftWallHit.normal * wallJumpSideForce;

            applyUpwardForce = _wallJumpsDone < allowedWallJumps;
            _wallJumpsDone++;
        }

        else if(_wallRight)
        {
            forceToApply = transform.up * wallJumpUpForce + _rightWallHit.normal * wallJumpSideForce;

            applyUpwardForce = _wallJumpsDone < allowedWallJumps;
            _wallJumpsDone++;
        }

        else if (_wallFront)
        {
            Vector3 againstWallDirection = (_frontWallHit.point - _orientation.position).normalized;

            forceToApply = Vector3.up * climbJumpUpForce + -againstWallDirection * climbJumpBackForce;

            applyUpwardForce = _climbJumpsDone < allowedClimbJumps;
            _climbJumpsDone++;
        }

        else if (_wallBack)
        {
            // remember the last wall
            _lastWall = _backWallHit.transform;

            Vector3 againstWallDirection = (_backWallHit.point - _orientation.position).normalized;

            forceToApply = Vector3.up * backWallJumpUpForce + -againstWallDirection * backWallJumpForwardForce;

            applyUpwardForce = true;
        }

        else
        {
            // print("WallJump was called, but there is no wall in range");
        }

        // if your jump is "allowed", apply the full jump force, including upward force
        if (applyUpwardForce)
        {
            // also reset the y velocity first (as always when jumping)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            delayedForceToApply = forceToApply;
            Invoke(nameof(DelayedForce), 0.05f);

            _pm.Walljumping = true;
            Invoke(nameof(ResetWallJumpingState), wallJumpDuration);
        }

        // if you try to jump, but you aren't "allowed" to anymore, push yourself away from the wall, but without upward force
        /// For example: you already performed a wallJump on this wall and you're only allowed to do one
        else
        {
            // also reset the y velocity first
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            // remove upward force (y axis) from forceToApply
            Vector3 noUpwardForce = new Vector3(forceToApply.x, 0f, forceToApply.z);

            // apply force without upward component
            _rb.AddForce(noUpwardForce, ForceMode.Impulse);
        }

        // stop wallRun and Climbing immediately
        StopWallRun();
        StopClimbing();
    }

    private Vector3 delayedForceToApply;
    private void DelayedForce()
    {
        _rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetWallJumpingState()
    {
        _pm.Walljumping = false;
    }

    /// called whenever PlayerParent hits a new wall
    private void ResetWallJumpsDone()
    {
        _wallJumpsDone = 0;
        _climbJumpsDone = 0;
    }

    private void PreciseWallJump()
    {
        _exitingWall = true;
        _exitWallTimer = _exitWallTime;

        // before jumping off, make sure that the last wall is remembered
        RememberLastWall();

        Vector3 midPoint = _orientation.position;

        float markerSphereRelativeYPos = _dt.MarkerSphere.position.y - midPoint.y;
        float highestPointOfArc = markerSphereRelativeYPos + _pm.MaxJumpHeight;

        // no upwards force when point is below PlayerParent
        if (markerSphereRelativeYPos < 0) highestPointOfArc = 1;

        // print("predicted wall jump " + markerSphereRelativeYPos + " " + highestPointOfArc);

        _pm.JumpToPosition(_dt.MarkerSphere.position, highestPointOfArc, midPoint);

        // _rb.AddForce(PhysicsExtension.CalculateJumpVelocity(lowestPoint, jumpTarget.position, MaxJumpHeight), ForceMode.Impulse);
        // _rb.velocity = PhysicsExtension.CalculateJumpVelocity(midPoint, _dt.MarkerSphere.position, highestPointOfArc);

        _wallJumpsDone++;

        // stop wallRun and Climbing immediately
        StopWallRun();
        StopClimbing();
    }

    #endregion

    #region StateMachine

    // and here's the _state machine that handles all wallmovement states
    private void StateMachine()
    {
        // I just defined the bools up here because I'm going to use them multiple times in this StateMachine

        bool leftWall = _wallLeft && _wallLeft2;
        bool rightWall = _wallRight && _wallRight2;
        bool sideWall = leftWall || rightWall;
        bool noInput = _horizontalInput == 0 && _horizontalInput == 0;

        bool climbing = _wallFront && _verticalInput > 0;


        // State 0 - LedgeGrabbing
        if (LedgeGrabbing)
        {
            _state = State.ledgegrabbing;

            // everything else in this _state gets handled by the LedgeGrabbing script

            if (_pm.Climbing) StopClimbing();
        }

        // State 1 - Climbing
        // Enter _state when: there's a wall in front, you're pressing W,
        // you're ready to climb and not currently exiting a wall
        // also the wall needs to be steep enough
        else if ((_wallFront || topReached) && _verticalInput > 0 && _wallLookAngle < maxWallLookAngle && !_exitingWall && _frontWallAngle > minFrontWallAngle)
        {
            // print("Climbing...");

            _state = State.climbing;

            // start Climbing if not already started
            if (!_pm.Climbing && _climbTimer > 0) StartClimbing();

            // restart Climbing if top has been reached (changes climbspeed)
            if (!_vaultClimbStarted && topReached) StartClimbing();

            // count down _climbTimer
            if (_climbTimer > 0) _climbTimer -= Time.deltaTime;

            // exit wall once the timer runs out
            if ((_climbTimer < 0) && _pm.Climbing)
            {
                _climbTimer = -1;

                _exitingWall = true;
                _exitWallTimer = _exitWallTime;

                StopClimbing();
            }
        }


        // State 2 - Wallrunning
        // Enter _state when: there's a wall left or right, you're pressing W, 
        // you're high enough over the ground and not exiting a wall
        else if (sideWall && _verticalInput > 0 && CanWallRun() && !_exitingWall)
        {
            _state = State.wallrunning;

            // startwallrun
            if (!_pm.Wallrunning) StartWallRun();

            // count down the _wallRunTimer
            _wallRunTimer -= Time.deltaTime;

            // what happens when the _wallRunTimer runs out
            if (_wallRunTimer < 0 && _pm.Wallrunning)
            {
                _wallRunTimer = 0;

                // if needed, perform a wall jump
                if (doJumpOnEndOfTimer)
                    WallJump();

                else
                {
                    _exitingWall = true; // this will set the _state to State.exiting
                    _exitWallTimer = _exitWallTime; // set the _exitWallTimer
                    StopWallRun(); // stop the wallrun
                }
            }
        }


        // State 3 - Sliding
        // Enter _state when: wall back + S Input, sidewalls with A/D but no W input,
        // or Climbing but the _climbTimer ran out
        else if ((_wallBack && _verticalInput < 0) || (((leftWall && _horizontalInput < 0) || (rightWall && _horizontalInput > 0)) && _verticalInput <= 0) || (climbing && _climbTimer <= 0))
        {
            _state = State.sliding;

            // bug fix
            if (_pm.Wallrunning)
                StopWallRun();
        }


        // State 4 - Exiting
        // Enter _state when: _exitingWall was set to true by other functions
        else if (_exitingWall)
        {
            _state = State.exiting;

            // make sure the PlayerParent can't move for a quick time
            _pm.Restricted = true;

            // exit out of wall run or climb when active
            if (_pm.Wallrunning)
                StopWallRun();

            if (_pm.Climbing)
                StopClimbing();

            // handle wall-exiting
            // make sure the timer counts down
            if (_exitWallTimer > 0) _exitWallTimer -= Time.deltaTime;

            // what happens if the _exitWallTimer runs out
            if (_exitWallTimer <= 0 && _exitingWall)
            {
                // set extiWall to false again -> _state will now change to Stat.none
                _exitingWall = false;

                // // reset readyToClimb when PlayerParent has sucessfully exited the wall
                // ResetReadyToClimb();
            }
        }


        // State 5 - None
        else
        {
            _state = State.none;

            // exit out of wall run or climb when active
            if (_pm.Wallrunning)
                StopWallRun();

            if (_pm.Climbing)
                StopClimbing();
        }

        // when no longer exiting the wall, allow normal PlayerParent movement again
        if (_state != State.exiting && _pm.Restricted)
            _pm.Restricted = false;
    }

    #endregion

    #region Getters and Setters
    public bool LedgeGrabbing { get; set; }
    

    #endregion

    #region Gizmos (Debugging & Visualizing)

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        float difference = _doubleRayCheckDistance * 0.5f;
        Vector3 differenceV = _orientation.forward * difference;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position - differenceV, _orientation.right * _wallDistanceSide);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + differenceV, _orientation.right * _wallDistanceSide);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position - differenceV, -_orientation.right * _wallDistanceSide);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + differenceV, -_orientation.right * _wallDistanceSide);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, _orientation.forward * _wallDistanceFront);

        Gizmos.color = Color.grey;
        Gizmos.DrawRay(transform.position, -_orientation.forward * _wallDistanceBack);
    }

    #endregion
}