using UnityEngine;
using UnityEngine.InputSystem;

//This script is an extension of the WallRunning script

[RequireComponent(typeof(PlayerMovement))]
public class LedgeGrabbing : MonoBehaviour
{
    [Header("Input References")]
    
    [SerializeField] private string jumpActionName = "Jump";
    
    [Header("Detection Settings")]
    
    [SerializeField] private float ledgeDetectionLength = 3;
    [SerializeField] private float ledgeSphereCastRadius = 0.5f;
    
    [Space]
    
    [SerializeField] private float maxLedgeGrabDistance = 2;
    
    [Space]
    
    [SerializeField] private float minTimeOnLedge = 0.5f;
    
    [Space]
    
    [SerializeField] private LayerMask whatIsLedge;
    
    [Header("Ledge Grabbing Behaviour Settings")]
    
    [SerializeField] private float moveToLedgeSpeed = 12;
    
    [Space]
    
    [SerializeField] private float ledgeJumpForwardForce = 14;
    [SerializeField] private float ledgeJumpUpForce = 5;
    
    [Space]
    
    [SerializeField] private float exitLedgeTime = 0.2f;
    
    //Dynamic, Non Serialized Below
    
    //Player
    private Transform _orientation;
    
    private WallRunning _mainWr;
    
    private PlayerMovement _pm;
    
    private Rigidbody _rb;
    
    private InputAction _jumpAction;
    
    //Camera References
    private Transform _realCamTrans;
    
    //Timing
    private float _timeOnLedge;

    //State
    private bool _holding;
    
    // Detection
    private RaycastHit _ledgeHit;
    
    private Transform _currLedge;
    
    private Vector3 _directionToLedge;
    
    private float _distanceToLedge;
    
    private Transform _lastLedge;
    
    //Timing
    
    private float _exitLedgeTimer;
    
    //IDK if needed, wasn't used - Sid
    // public float maxLedgeJumpUpSpeed;

    private void Awake()
    {
        //get references
        _pm = GetComponent<PlayerMovement>();
        _orientation = _pm.Orientation;
        _rb = GetComponent<Rigidbody>();
        _mainWr = GetComponent<WallRunning>();
        
        PlayerCam playerCamScript = GetComponent<PlayerCam>();
        _realCamTrans = playerCamScript.RealCam.transform;
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
        _jumpAction = playerInput.actions.FindAction(jumpActionName);
    }

    private void OnEnable()
    {
        _jumpAction.Enable();
    }
    
    private void OnDisable()
    {
        _jumpAction.Disable();
    }

    private void Update()
    {
        LedgeDetection();
        SubStateMachine();
    }

    // a very simple state machine which takes care of the ledge grabbing state
    private void SubStateMachine()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector2 inputDirection = _orientation.forward * verticalInput + _orientation.right * horizontalInput;

        // SubState 1 - Holding onto ledge
        if (_holding)
        {
            FreezeRigidbodyOnLedge();

            if (_timeOnLedge > minTimeOnLedge && inputDirection != Vector2.zero) ExitLedgeHold();

            _timeOnLedge += Time.deltaTime;
        }

        // SubState 2 - Exiting Ledge
        else if(ExitingLedge)
        {
            if (_exitLedgeTimer > 0) _exitLedgeTimer -= Time.deltaTime;
            else ExitingLedge = false;
        }

        if (_holding && _jumpAction.triggered) LedgeJump();
    }

    private void LedgeDetection()
    {
        bool ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, _realCamTrans.forward, out _ledgeHit, ledgeDetectionLength, whatIsLedge);

        if (_ledgeHit.transform == null) return;

        _directionToLedge = _ledgeHit.transform.position - transform.position;
        _distanceToLedge = _directionToLedge.magnitude;

        if (_lastLedge != null && _ledgeHit.transform == _lastLedge) return;

        if (ledgeDetected && _distanceToLedge < maxLedgeGrabDistance && !_holding) EnterLedgeHold();
    }

    private void LedgeJump()
    {
        // print("ledge jump");

        ExitLedgeHold();

        Invoke(nameof(DelayedForce), 0.05f);
    }

    private void DelayedForce()
    {
        Vector3 forceToAdd = _realCamTrans.forward * ledgeJumpForwardForce + _orientation.up * ledgeJumpUpForce;
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void EnterLedgeHold()
    {
        if (ExitingLedge) return;

        // print("entered ledge hold");

        _mainWr.LedgeGrabbing = true;
        _holding = true;

        _pm.Restricted = true;
        
        //USED TO BE IN, I UNCOMMENTED BC MAX SPEED STAYED UNLIMITED, IDK WHY - Sid
        // _pm.UnlimitedSpeed = true;

        _currLedge = _ledgeHit.transform;
        _lastLedge = _ledgeHit.transform;

        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
    }

    //Didn't seem to do anything, so I commented it out - Sid
    // bool touchingLedge;
    private void FreezeRigidbodyOnLedge()
    {
        _rb.useGravity = false;

        Vector3 directionToLedge = _currLedge.position - transform.position;

        if (directionToLedge.magnitude > maxLedgeGrabDistance && _holding) ExitLedgeHold();

        // Move PlayerParent towards ledge
        if (directionToLedge.magnitude > 1f)
        {
            // Vector3 _directionToLedge = _ledgeHit.transform.position - transform.position;
            // _rb.velocity = _directionToLedge.normalized * moveToLedgeSpeed;

            if (_rb.linearVelocity.magnitude < moveToLedgeSpeed)
                _rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime);

            // The current problem is that I can't set the velocity from here, I can only add force
            // -> but then the force is mainly upwards :D

            // print("moving to ledge");
        }

        // Hold onto ledge
        else
        {
            if (_pm.UnlimitedSpeed) _pm.UnlimitedSpeed = false;
            if (!_pm.Freeze) _pm.Freeze = true;
            //_rb.velocity = Vector3.zero;
            // print("hanging on ledge");
        }
    }

    private void ExitLedgeHold()
    {
        ExitingLedge = true;
        _exitLedgeTimer = exitLedgeTime;

        _mainWr.LedgeGrabbing = false;
        _holding = false;
        _timeOnLedge = 0;

        _pm.Freeze = false;
        _pm.UnlimitedSpeed = false;
        _pm.Restricted = false;

        _rb.useGravity = true;

        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f);
    }

    private void ResetLastLedge()
    {
        _lastLedge = null;
    }

    #region Getters and Setters

    public bool ExitingLedge { get; private set; }

    #endregion


    //Wasn't used, so I commented it out - Sid
    // checking with collisionEnter an Exit if the ledge has been reached (touched)
    // private void OnCollisionEnter(Collision collision)
    // {
    //     if (collision.transform.tag == "Ledge")
    //     {
    //         touchingLedge = true;
    //     }
    // }

    // private void OnCollisionExit(Collision collision)
    // {
    //     if (collision.transform.tag == "Ledge")
    //     {
    //         touchingLedge = false;
    //     }
    // }
}
