using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


[RequireComponent(typeof(PlayerMovement))]
public class Sliding : MonoBehaviour
{
    [Header("Input References")]
    
    [SerializeField] private string slideActionName = "Crouch";

    [Header("Timings")]
    
    [SerializeField] private float slideCooldown = 0.5f;
    
    [SerializeField] private float minSlideTime = 0.2f;
    [SerializeField] private float maxSlideTime = 0.75f; // how long the slide maximally lasts
    
    [Header("Force Settings")]
    
    [SerializeField] private bool useDynamicSlideForce = true; //if true, PlayerParent momentum on slide equal to that of when pressed
    
    [SerializeField] private float slideForce = 200f; // Flat slide force applied whenever pressed
    
    [SerializeField] private float dynamicSlideForce = 200f; // Additive force based on PlayerParent momentum

    [Header("Behaviour Settings")]
    
    [SerializeField] private float slideColliderHeight = 1f;

    [SerializeField] private float slideColliderCenterY = -0.5f;
    
    [SerializeField] private bool reverseCoyoteTime = true; //held in air triggers when Grounded
    
    //Dynamic, Non-Serialized Below
    
    //References
    private Transform _orientation; // Orientation object inside the PlayerParent
    private Rigidbody _rb;
    private PlayerMovement _pm; // script reference to the PlayerMovement script
    
    private InputAction _slideAction; // the input action for Sliding
    
    //Player Collider
    private CapsuleCollider _playerColl;
    
    private float _startCollHeight;
    
    private float _startCollCenterY;
    
    //Timing
    private float _slideTimer;
    
    //Inputs
    private float _horizontalInput;
    private float _verticalInput;
    
    private Vector3 _startInputDirection;
    
    //State
    private bool _bufferSlide;
    private bool _readyToSlide = true;
    private bool _stopSlideAsap;
    
    // Dynamic Slide Force
    private float _dynamicStartForce;

    private void Awake()
    {
        // get references
        _rb             = GetComponent<Rigidbody>();
        _pm             = GetComponent<PlayerMovement>();
        _orientation    = _pm.Orientation;
        _playerColl = GetComponent<CapsuleCollider>();
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
        _slideAction = playerInput.actions.FindAction(slideActionName);
    }

    private void Start()
    {
        //store start collider settings
        _startCollHeight = _playerColl.height;
        
        _startCollCenterY = _playerColl.center.y;

        _readyToSlide = true;
    }

    private void OnEnable()
    {
        _slideAction.Enable();
    }
    
    private void OnDisable()
    {
        _slideAction.Disable();
    }

    private void Update()
    {
        // get the W,A,S,D keyboard inputs
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        // if you press down the slide key while moving and not crouching,
        // try to start Sliding, can be denied by movement extension
        if (_slideAction.triggered && (_horizontalInput != 0 || _verticalInput != 0) && !_pm.Crouching)
        {
            if (reverseCoyoteTime) _bufferSlide = true;
            else if (_pm.Grounded && _readyToSlide) _bufferSlide = true;
        }

        // if you release the slide key while Sliding -> StopSlide
        if (!_slideAction.IsPressed() && _pm.Sliding)
        {
            if (reverseCoyoteTime) _bufferSlide = false;

            if (_pm.Sliding) _stopSlideAsap = true;
        }

        // slide buffering
        if (_bufferSlide && _pm.Grounded && _readyToSlide)
        {
            StartSlide();
            _bufferSlide = false;
        }

        // unslide if slide key was released and minSlideTime exceeded
        if (_stopSlideAsap && maxSlideTime - _slideTimer > minSlideTime)
        {
            _stopSlideAsap = false;
            StopSlide();
        }

        // unsliding if no longer Grounded
        if (_pm.Sliding && !_pm.Grounded)
            StopSlide();
    }

    private void FixedUpdate()
    {
        // make sure that Sliding movement is continuously called while Sliding
        if (_pm.Sliding) SlidingMovement();
    }
    
    private void StartSlide()
    {
        if (!_pm.IsStateAllowed(PlayerMovement.MovementMode.sliding))
            return;

        if (!_pm.Grounded) return;

        // this causes the PlayerParent to change to MovementMode.Sliding
        _pm.Sliding = true;
        _readyToSlide = false;

        // change PlayerParent collider size
        _playerColl.height = slideColliderHeight;
        
        _playerColl.center = new Vector3(_playerColl.center.x, slideColliderCenterY, _playerColl.center.z);
        
        // store the start dynamic force
        _dynamicStartForce = _rb.linearVelocity.magnitude;
        
        // after shrinking, you'll be a bit in the air, so add downward force to hit the ground again
        // you don't really notice this while playing
        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        // set the _slideTimer
        _slideTimer = maxSlideTime;

        // idk, feels weird but the idea would be ok I guess
        // _startInputDirection = Orientation.forward * _pm._verticalInput + Orientation.right * _pm._horizontalInput;
    }

    private void SlidingMovement()
    {
        // calculate the direction of your keyboard input relative to the players Orientation (where the PlayerParent is looking)
        Vector3 inputDirection = Vector3.Normalize(_orientation.forward * _verticalInput + _orientation.right * _horizontalInput);
        
        // slide force calculated based on the dynamic force or the non-dynamic force
        float force = useDynamicSlideForce ? _dynamicStartForce + dynamicSlideForce : slideForce;

        // Mode 1 - Sliding Normal
        // slide time is limited
        if(!_pm.OnSlope() || _rb.linearVelocity.y > -0.1f)
        {
            // add force in the direction of your input
            _rb.AddForce(inputDirection * force, ForceMode.Force);

            // count down timer
            _slideTimer -= Time.deltaTime;
        }

        // Mode 2 - Sliding down slopes
        // can slide for as long as the slope lasts
        else
        {
            // add force in the direction of your keyboard input
            _rb.AddForce(_pm.GetSlopeMoveDirection(inputDirection) * force, ForceMode.Force);
        }

        // stop Sliding again if the timer runs out
        if (_slideTimer <= 0) StopSlide();
    }

    private void StopSlide()
    {
        _pm.Sliding = false;

        // reset PlayerParent collider size
        _playerColl.height = _startCollHeight;
        
        _playerColl.center = new Vector3(_playerColl.center.x, _startCollCenterY, _playerColl.center.z);

        Invoke(nameof(ResetSlide), slideCooldown);
    }

    private void ResetSlide()
    {
        _readyToSlide = true;
    }
}
