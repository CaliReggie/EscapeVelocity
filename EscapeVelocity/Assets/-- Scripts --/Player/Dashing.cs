using System;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerMovement))]
public class Dashing : MonoBehaviour
{
    [Header("Input References")]
    
    [SerializeField] private string dashActionName = "Dash";
    
    [Header("Dash Forces")]
    
    [SerializeField] private float dashForce = 25f; //Max speed still limited by PlayerMovement script
    
    [SerializeField] private float dashUpwardForce; // how much upward force is added when Dashing
    
    [Space]
    
    [SerializeField] private float maxUpwardVel = -1; // limit the upwardVelocity if needed
                                                      // (if you keep it on -1, the upwardsVelocity is unlimited)
    
    [Header("Dash Timings")]
    
    [SerializeField] private float dashDuration = 0.25f;
    
    [SerializeField] private float dashCd = 1.5f; // cooldown of your dash ability

    [Header("Dash Behaviour")]

    [SerializeField] private bool useCameraForward = true;  // when active, the PlayerParent dashes in the forward direction of the camera (upwards if you look up)
    
    [Space]
    
    [SerializeField] private bool allowForwardDirection = true; // defines if the PlayerParent is allowed to dash forwards
    [SerializeField] private bool allowBackDirection = true; // defines if the PlayerParent is allowed to dash backwards
    [SerializeField] private bool allowSidewaysDirection = true; // defines if the PlayerParent is allowed to dash sideways
    
    [Space]
    
    [SerializeField] private bool disableGravity = true; // when active, gravity is disabled while Dashing
    
    [Space]
    
    [SerializeField] private bool resetYVel = true; // when active, y velocity is resetted before Dashing
    [SerializeField] private bool resetVel = true; // when active, full velocity reset before Dashing

    [Header("Camera Effects")]
    [SerializeField] private float dashFov = 125f;
    [SerializeField] private float dashFOVChangeSpeed = 0.2f;
    
    //Dynamic, Non Serialized Below
    
    //References
    
    //Player
    private Transform _orientation;
    
    private Rigidbody _rb;
    
    private PlayerMovement _pm;
    
    private InputAction _dashAction;
    
    //Camera
    private PlayerCam _playerCamScript;
    
    private Transform _realCamTrans;
    
    //Timing
    private float _dashCdTimer;

    private void Awake()
    {
        //get references
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
        _orientation = _pm.Orientation;
        _playerCamScript = GetComponent<PlayerCam>();
        _realCamTrans = _playerCamScript.RealCam.gameObject.transform;
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
        _dashAction = playerInput.actions.FindAction(dashActionName);
    }

    private void OnEnable()
    {
        _dashAction.Enable();
    }
    
    private void OnDisable()
    {
        _dashAction.Disable();
    }

    private void Update()
    {
        // if you press the dash key -> call Dash() function
        if (_dashAction.triggered)
            Dash();

        // cooldown timer
        if (_dashCdTimer > 0)
            _dashCdTimer -= Time.deltaTime;
    }

    private void Dash()
    {
        if (!_pm.IsStateAllowed(PlayerMovement.MovementMode.dashing))
            return;

        // cooldown implementation
        if (_dashCdTimer > 0) return;
        else _dashCdTimer = dashCd;

        _pm.ResetRestrictions();

        // if maxUpwardVel set to default (-1), don't limit the players upward velocity
        if (maxUpwardVel == -1)
            _pm.MaxYSpeed = -1;

        else
            _pm.MaxYSpeed = maxUpwardVel;

        // this will cause the PlayerMovement script to change to MovementMode.Dashing
        _pm.Dashing = true;

        // increase the fov of the camera (graphical effect)
        _playerCamScript.DoFov(dashFov, dashFOVChangeSpeed);

        Transform forwardT;

        // decide wheter you want to use the RealCamTrans or the playersOrientation as forward direction
        if (useCameraForward)
            forwardT = _realCamTrans;
        else
            forwardT = _orientation;

        // call the GetDirection() function below to calculate the direction
        Vector3 direction = GetDirection(forwardT);

        // calculate the forward and upward force
        Vector3 force = direction * dashForce + _orientation.up * dashUpwardForce;

        // disable gravity of the players rigidbody if needed
        if (disableGravity)
            _rb.useGravity = false;

        // add the dash force (deayed)
        delayedForceToApply = force;
        Invoke(nameof(DelayedDashForce), 0.025f);

        // make sure the dash stops after the dashDuration is over
        Invoke(nameof(ResetDash), dashDuration);
    }

    private Vector3 delayedForceToApply;
    private void DelayedDashForce()
    {
        // reset velocity based on settings
        if (resetVel)
            _rb.linearVelocity = Vector3.zero;
        else if (resetYVel)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.y);

        _rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        _pm.Dashing = false;

        // make sure players MaxYSpeed is no longer limited
        _pm.MaxYSpeed = -1;

        // reset the fov of your camera
        _playerCamScript.DoFov(-360, dashFOVChangeSpeed);

        // if you disabled it before, activate the gravity of the rigidbody again
        if (disableGravity)
            _rb.useGravity = true;
    }

    private Vector3 GetDirection(Transform forwardT)
    {
        // get the W,A,S,D input
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // 2 Vector3 for the forward and right velocity
        Vector3 forwardV = Vector3.zero;
        Vector3 rightV = Vector3.zero;

        // forward
        // if W is pressed and you're allowed to dash forwards, activate the forwardVelocity
        if (z > 0 && allowForwardDirection)
            forwardV = forwardT.forward;

        // back
        // if S is pressed and you're allowed to dash backwards, activate the backwardVelocity
        if (z < 0 && allowBackDirection)
            forwardV = -forwardT.forward;

        // right
        // if D is pressed and you're allowed to dash sideways, activate the right velocity
        if (x > 0 && allowSidewaysDirection)
            rightV = forwardT.right;

        // left
        // if A is pressed and you're allowed to dash sideways, activate the left velocity
        if (x < 0 && allowSidewaysDirection)
            rightV = -forwardT.right;

        // no input (forward)
        // If there's no input but Dashing forward is allowed, activate the forwardVelocity
        if (x == 0 && z == 0 && allowForwardDirection)
             forwardV = forwardT.forward;

        // forward only allowed direction
        // if forward is the only allowed direction, activate the forwardVelocity
        if (allowForwardDirection && !allowBackDirection && !allowSidewaysDirection)
            forwardV = forwardT.forward;

        // return the forward and right velocity
        // if for example both have been activated, the PlayerParent will now dash forward and to the right -> diagonally
        // this works for all 8 directions
        return (forwardV + rightV).normalized;
    }
}