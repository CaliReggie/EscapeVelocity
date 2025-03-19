using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// Content:
// - Swinging ability
// - grappling ability
// 
// Note:
// This script handles starting and stopping the Swinging and grappling ability, as well as moving the PlayerParent
// The grappling rope is drawn and animated by the GrapplingRope script

// single, or dual Swinging
// 
// grappling left or right -> cancels any active swings and grapples
// no grappling left/right twice in a row
// Swinging -> cancels any active grapples, exit limited state!
// 
// This implies that Swinging and grappling can never be active at the same time, neither can there be 2 active grapples

[RequireComponent(typeof(PlayerMovement))]
public class Grappling: MonoBehaviour
{
    private enum GrappleMode
    {
        Basic,
        Precise
    }
    
    [Header("Hook Rig References")]
    
    [SerializeField] private List<Transform> gunTips;
    
    [SerializeField] private List<Transform> pointAimers;
    
    [Header("Prediction References")]
    
    [SerializeField] private List<Transform> predictionPoints;
    
    [Header("Input References")]
    
    [SerializeField] private string leftGrappleActionName = "LeftGrapple";
    
    [SerializeField] private string rightGrappleActionName = "RightGrapple";
    
    [SerializeField] private string leftSwingActionName = "LeftSwing";
    
    [SerializeField] private string rightSwingActionName = "RightSwing";
    
    [SerializeField] private string moveActionName = "Move";
    
    [SerializeField] private string jumpActionName = "Jump";
    
    [Header("General Hook Settings")]
    
    [SerializeField] private float playerHeight = 2f;
    
    [SerializeField] private int amountOfHookPoints = 2;
    
    [SerializeField] private bool useChargeOnHookNotHit = true;
    
    [SerializeField] private LayerMask whatIsGrappleable; // you can grapple & swing on all objects that are in this layermask
    
    [SerializeField] private float aimLineSpherecastRadius = 3f;
    
    [Header("Grapple Settings")]
    
    [SerializeField] private float maxGrappleDistance = 20f; // max distance you're able to grapple onto objects
    
    [Space]
    
    [SerializeField] private float grappleDelayTime = 0.15f; // the time you Freeze in the air before grappling
    [SerializeField] private float grapplingCd = .25f; // cooldown of your grappling ability
    
    [Space]
    
    [SerializeField] private GrappleMode grappleMode = GrappleMode.Precise;
    
    [Space]
    
    [Tooltip("Only applied when grappleMode is set to Precise")]
    [SerializeField] private float overshootYAxis = 2f; // adjust the trajectory hight of the PlayerParent when grappling
    
    [Space]
    
    [Tooltip("Only applied when grappleMode is set to Basic")]
    [SerializeField] private float grappleForce = 35f;
    
    [Tooltip("Only applied when grappleMode is set to Basic")]
    [SerializeField] private float grappleUpwardForce = 7.5f;
    
    [Space]
    
    [SerializeField] private float grappleDistanceHeightMultiplier = 0.1f; // how much more force you gain when grappling toward objects that are further away
    
    [Space]
    
    [SerializeField] private bool freezeOnGrappleNotHit;
    
    [Header("Swing Settings")]
    
    [SerializeField] private float maxSwingDistance = 20f; // max distance you're able hit objects for Swinging ability
    
    [Space]
    
    [SerializeField] private float spring = 4.5f; // spring of the SpringJoint component
    [SerializeField] private float damper = 7f; // damper of the SpringJoint component
    [SerializeField] private float massScale = 4.5f; // massScale of the SpringJoint component
    
    [Space]
    
    [SerializeField] private bool enableSwingingWithForces = true;
    
    [FormerlySerializedAs("lateralThrustForce")]
    [Space]
    
    [SerializeField] private float directionalThrustForce = 2500;
    [Range(0,1)] [SerializeField] private float upwardThrustModifier = 0.5f;
    [SerializeField] private float retractThrustForce = 3500;
    [SerializeField] private float extendCableSpeed = 15;
    
    //Dynamic, Non-Serialized Below
    
    //Player References
    private Transform _orientation;
    
    private Rigidbody _rb;
    
    private PlayerMovement _pm;
    
    private InputAction _leftGrappleAction;
    
    private InputAction _rightGrappleAction;
    
    private InputAction _leftSwingAction;
    
    private InputAction _rightSwingAction;
    
    private InputAction _moveAction;
    
    private InputAction _jumpAction;
    
    //Camera References
    private Transform _camOrientation;
    
    //Prediction References
    
    private List<RaycastHit> _predictionHits;
    
    private List<Transform> _grappleObjects; // the object transform you're grappling to
    
    private List<Vector3> _grappleLocalPoints; //local position of hit point on object
    
    private Vector3 _pullPoint; // point in space to pull PlayerParent towards
    
    //Sprint joint holders
    
    private List<SpringJoint> _joints; // for swining we use Unitys SpringJoint component
    
    //Input
    private Vector2 _moveInput;
    
    //Timing
    
    private float _grapplingCdTimer;

    //State

    private List<bool> _grapplesExecuted;

    private void Awake()
    {
        // get references
        _pm = GetComponent<PlayerMovement>();
        _orientation = _pm.Orientation;
        _rb = GetComponent<Rigidbody>();
        
        PlayerCam playerCam = GetComponent<PlayerCam>();
        _camOrientation = playerCam.CamOrientation;
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
        _leftGrappleAction = playerInput.actions.FindAction(leftGrappleActionName);
        _rightGrappleAction = playerInput.actions.FindAction(rightGrappleActionName);
        _leftSwingAction = playerInput.actions.FindAction(leftSwingActionName);
        _rightSwingAction = playerInput.actions.FindAction(rightSwingActionName);
        _moveAction = playerInput.actions.FindAction(moveActionName);
        _jumpAction = playerInput.actions.FindAction(jumpActionName);
    }

    private void Start()
    {
        // if you don't set whatIsGrappleable to anything, it's automatically set to Default
        if (whatIsGrappleable.value == 0)
            whatIsGrappleable = LayerMask.GetMask("Default");

        ListSetup();
    }

    private void OnEnable()
    {
        _leftGrappleAction.Enable();
        _rightGrappleAction.Enable();
        _leftSwingAction.Enable();
        _rightSwingAction.Enable();
        _moveAction.Enable();
        _jumpAction.Enable();
    }
    
    private void OnDisable()
    {
        _leftGrappleAction.Disable();
        _rightGrappleAction.Disable();
        _leftSwingAction.Disable();
        _rightSwingAction.Disable();
        _moveAction.Disable();
        _jumpAction.Disable();
    }

    private void ListSetup()
    {
        HooksActive = new List<bool>();
        _predictionHits = new List<RaycastHit>();
        
        HookPoints = new List<Vector3>();
        _grappleObjects = new List<Transform>();
        _grappleLocalPoints = new List<Vector3>();
        _joints = new List<SpringJoint>();

        _grapplesExecuted = new List<bool>();
        GrapplesActive = new List<bool>();
        SwingsActive = new List<bool>();

        for (int i = 0; i < amountOfHookPoints; i++)
        {
            HooksActive.Add(false);
            _predictionHits.Add(new RaycastHit());
            _grappleObjects.Add(null);
            _grappleLocalPoints.Add(Vector3.zero);
            _joints.Add(null);
            HookPoints.Add(Vector3.zero);
            _grapplesExecuted.Add(false);
            GrapplesActive.Add(false);
            SwingsActive.Add(false);
        }
    }

    private void Update()
    {
        // cooldown timer
        if (_grapplingCdTimer > 0)
            _grapplingCdTimer -= Time.deltaTime;

        // make sure MyInput() is called every frame
        MyInput();

        if (enableSwingingWithForces && _joints[0] != null || _joints[1] != null) OdmGearMovement();

        CheckForSwingPoints();
    }

    private void MyInput()
    {
        //due to modifiers on input for k&m, we want to always read if grapple is pressed first
        
        if (_leftGrappleAction.triggered)
        {
            StartGrapple(0);
        }
        
        if (!GrapplesActive[0] && _leftSwingAction.triggered)
        {
            StartSwing(0);
        }
        
        if (_rightGrappleAction.triggered)
        {
            StartGrapple(1);
        }
        
        if (!GrapplesActive[1] && _rightSwingAction.triggered)
        {
            StartSwing(1);
        }
        
        if (GrapplesActive[0])
        {
            if (_leftGrappleAction.phase != InputActionPhase.Performed) TryStopGrapple(0);
        }
        
        if (GrapplesActive[1])
        {
            if (_rightGrappleAction.phase != InputActionPhase.Performed) TryStopGrapple(1);
        }
        
        if (SwingsActive[0])
        {
            if (!_leftSwingAction.IsPressed()) StopSwing(0);
        }
        
        if (SwingsActive[1])
        {
            if (!_rightSwingAction.IsPressed()) StopSwing(1);
        }
        
        _moveInput = _moveAction.ReadValue<Vector2>();
    }

    #region Swinging

    private void CheckForSwingPoints()
    {
        for (int i = 0; i < amountOfHookPoints; i++)
        {
            if (HooksActive[i])
            {
                TrackObject(i);
            }
            else
            {
                RaycastHit hit = _predictionHits[i];
                Physics.SphereCast(pointAimers[i].position, aimLineSpherecastRadius, pointAimers[i].forward, out hit, maxSwingDistance, whatIsGrappleable);

                // check if direct hit is available
                RaycastHit directHit;
                Physics.Raycast(_orientation.position, _camOrientation.forward, out directHit, maxSwingDistance, whatIsGrappleable);

                Vector3 realHitPoint = Vector3.zero;

                // Option 1 - Direct Hit
                if (directHit.point != Vector3.zero)
                    realHitPoint = directHit.point;

                // Option 2 - Indirect (predicted) Hit
                else if (hit.point != Vector3.zero)
                    realHitPoint = hit.point;

                // Option 3 - Miss
                else
                    realHitPoint = Vector3.zero;

                // realHitPoint found
                if (realHitPoint != Vector3.zero)
                {
                    predictionPoints[i].gameObject.SetActive(true);
                    predictionPoints[i].position = realHitPoint;
                }
                // realHitPoint not found
                else
                {
                    predictionPoints[i].gameObject.SetActive(false);
                    predictionPoints[i].position = Vector3.zero;
                }

                // print("hit: " + hit.point);

                _predictionHits[i] = directHit.point == Vector3.zero ? hit : directHit;
            }
        }
    }
    private void StartSwing(int swingIndex)
    {
        if (!_pm.IsStateAllowed(PlayerMovement.MovementMode.swinging))
            return;
        
        // no Swinging point can be found
        if (!TargetPointFound(swingIndex))
        {
            if (useChargeOnHookNotHit)
            {
                // the grapple point is now just a point in the air
                // calculated by taking your cameras position + the forwardDirection times your maxGrappleDistance
                HookPoints[swingIndex] = _camOrientation.position + _camOrientation.forward * maxGrappleDistance;
                
                //setting grapple active for rope to show
                SwingsActive[swingIndex] = true;
                UpdateHooksActive();
                
                //no grapple object
                _grappleObjects[swingIndex] = null;
                
                //no local point
                _grappleLocalPoints[swingIndex] = Vector3.zero;
                
                StartCoroutine(StopFailedSwing(swingIndex, 0.15f));
            }
            
            return;
        }
        
        // cancel all active grapples
        CancelActiveGrapples();
        _pm.ResetRestrictions();
        
        //if Stopfailedswing is running, stop it
        StopCoroutine(nameof(StopFailedSwing));
        
        // this will cause the PlayerMovement script to enter MovementMode.Swinging
        _pm.Swinging = true;

        //corresponding _grappleObjects is the object the raycast hit
        _grappleObjects[swingIndex] = _predictionHits[swingIndex].transform;
        
        //converting hit point to local position of hit on object
        _grappleLocalPoints[swingIndex] = _grappleObjects[swingIndex].
            InverseTransformPoint(_predictionHits[swingIndex].point);

        // the exact point where you swing on
        HookPoints[swingIndex] = _predictionHits[swingIndex].point;

        // add a springJoint component to your PlayerParent
        _joints[swingIndex] = gameObject.AddComponent<SpringJoint>();
        _joints[swingIndex].autoConfigureConnectedAnchor = false;

        // set the anchor of the springJoint
        _joints[swingIndex].connectedAnchor = HookPoints[swingIndex];

        // calculate the distance to the grapplePoint
        float distanceFromPoint = Vector3.Distance(transform.position, HookPoints[swingIndex]);

        // the distance grapple will try to keep from grapple point.
        // _joints[swingIndex].maxDistance = distanceFromPoint * 0.8f;
        // _joints[swingIndex].minDistance = distanceFromPoint * 0.25f;

        _joints[swingIndex].maxDistance = distanceFromPoint;
        _joints[swingIndex].minDistance = 0;

        // adjust these values to fit your game
        _joints[swingIndex].spring = spring;
        _joints[swingIndex].damper = damper;
        _joints[swingIndex].massScale = massScale;

        SwingsActive[swingIndex] = true;
        UpdateHooksActive();
    }

    private void StopSwing(int swingIndex)
    {
        _pm.Swinging = false;
        SwingsActive[swingIndex] = false;

        UpdateHooksActive();

        // destroy the SpringJoint again after you stopped Swinging 
        Destroy(_joints[swingIndex]);
    }
    
    private IEnumerator StopFailedSwing(int swingIndex, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        
        SwingsActive[swingIndex] = false;
        UpdateHooksActive();
    }

    #endregion

    #region Odm Gear
    private void OdmGearMovement()
    {
        if (SwingsActive[0] && !SwingsActive[1]) _pullPoint = HookPoints[0];
        if (SwingsActive[1] && !SwingsActive[0]) _pullPoint = HookPoints[1];
        // get midpoint if both swing points are active
        if (SwingsActive[0] && SwingsActive[1])
        {
            Vector3 dirToGrapplePoint1 = HookPoints[1] - HookPoints[0];
            _pullPoint = HookPoints[0] + dirToGrapplePoint1 * 0.5f;
        }
        //Force will always be added in dir of camoriented move input (look where you want to go)
        
        // We will use a quadrant ideology for retraction. If angle from oriented move input relative to direction to grapple point
        // is within -45 to 45 degrees, we will retract, if otherwise within -135 to 135 degrees, we do nothing,
        // and otherwise we extend
        
        Vector3 orientedInputDir = _camOrientation.forward * _moveInput.y + _camOrientation.right * _moveInput.x;
        
        Vector3 dirToPoint = _pullPoint - transform.position;
        
        float distToPoint = Vector3.Magnitude(dirToPoint);
        
        if (_moveInput != Vector2.zero)
        {
            float angle = Vector3.SignedAngle(orientedInputDir, dirToPoint, Vector3.up);
        
            bool retract = angle > -45 && angle < 45;
        
            bool extend = angle < -135 || angle > 135;
            
            Vector3 forceToAdd = Time.deltaTime * directionalThrustForce * orientedInputDir;
            
            if (retract)
            {
                forceToAdd += Time.deltaTime * retractThrustForce * dirToPoint.normalized;
            }
            else if (extend)
            {
                distToPoint += extendCableSpeed;
            }
            
            if (forceToAdd.y > 0)
            {
                forceToAdd.y *= upwardThrustModifier;
            }
            
            _rb.AddForce(forceToAdd);
        }
        
        UpdateJoints(distToPoint);
    }

    private void UpdateJoints(float distanceFromPoint)
    {
        for (int i = 0; i < _joints.Count; i++)
        {
            if (_joints[i] != null)
            {
                _joints[i].maxDistance = distanceFromPoint * 0.8f;
                _joints[i].minDistance = distanceFromPoint * 0.25f;
            }
        }
    }

    #endregion

    // Here you'll find all the code specifically needed for the grappling ability
    #region Grappling

    private void StartGrapple(int grappleIndex)
    {
        // in cooldown
        if (_grapplingCdTimer > 0) return;

        // cancel active swings and grapples
        CancelActiveSwings();
        CancelAllGrapplesExcept(grappleIndex);

        // Case 1 - target point found
        if (TargetPointFound(grappleIndex))
        {
            // print("grapple: target found");

            // set cooldown
            _grapplingCdTimer = grapplingCd;

            // this will cause the PlayerMovement script to change to MovemementMode.Freeze
            // -> therefore the PlayerParent will Freeze mid-air for some time before grappling
            _pm.Freeze = true;

            // same stuff as in StartSwing() function
            _grappleObjects[grappleIndex] = _predictionHits[grappleIndex].transform;
            
            //same as in StartSwing()
            _grappleLocalPoints[grappleIndex] = _grappleObjects[grappleIndex].
                InverseTransformPoint(_predictionHits[grappleIndex].point);

            HookPoints[grappleIndex] = _predictionHits[grappleIndex].point;

            GrapplesActive[grappleIndex] = true;
            
            UpdateHooksActive();

            // call the ExecuteGrapple() function after the grappleDelayTime is over
            StartCoroutine(ExecuteGrapple(grappleIndex));
        }
        // Case 2 - target point not found, preferential Freeze and show grapple point
        else
        {
            // print("grapple: target missed");

            if (freezeOnGrappleNotHit)
            {
                // we still want to Freeze the PlayerParent for a bit
                _pm.Freeze = true;
            }
            
            //if using a charge, we want to use the charge and show like the PlayerParent is attempting to grapple air
            if (useChargeOnHookNotHit)
            {
                // set cooldown
                _grapplingCdTimer = grapplingCd;

                // the grapple point is now just a point in the air
                // calculated by taking your cameras position + the forwardDirection times your maxGrappleDistance
                HookPoints[grappleIndex] = _camOrientation.position + _camOrientation.forward * maxGrappleDistance;
                
                //setting grapple active for rope to show
                GrapplesActive[grappleIndex] = true;
                UpdateHooksActive();
                
                //no grapple object
                _grappleObjects[grappleIndex] = null;
                
                //no local point
                _grappleLocalPoints[grappleIndex] = Vector3.zero;

                // call the StopGrapple() function after the grappleDelayTime is over
                StartCoroutine(StopGrapple(grappleIndex, grappleDelayTime));
            }
        }
    }

    private IEnumerator ExecuteGrapple(int grappleIndex)
    {
        yield return new WaitForSeconds(grappleDelayTime);

        // make sure that the PlayerParent can move again
        _pm.Freeze = false;

        if(grappleMode == GrappleMode.Precise)
        {
            // find the lowest point of the PlayerParent
            Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - (playerHeight / 2), transform.position.z);

            // calculate how much higher the grapple point is relative to the PlayerParent
            float grapplePointRelativeYPos = HookPoints[grappleIndex].y - lowestPoint.y;
            
            //if relative y offset is above relative PlayerParent height, add all needed height, otherwise add less
            float highestPointOfArc = grapplePointRelativeYPos >= playerHeight ?
                grapplePointRelativeYPos + overshootYAxis : overshootYAxis / 2;

            // print("trying to grapple to " + grapplePointRelativeYPos + " which arc " + highestPointOfArc);

            _pm.JumpToPosition(HookPoints[grappleIndex], highestPointOfArc, default, 3f);
        }

        if(grappleMode == GrappleMode.Basic)
        {
            // calculate the direction from the PlayerParent to the grapplePoint
            Vector3 direction = (HookPoints[grappleIndex] - transform.position).normalized;

            // reset the y velocity of your rigidbody
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            // the further the grapple point is away, the higher the distanceBoost should be
            float distanceBoost = Vector3.Distance(transform.position, HookPoints[grappleIndex]) * grappleDistanceHeightMultiplier;

            // apply force to your rigidbody in the direction towards the grapplePoint
            _rb.AddForce(direction * grappleForce , ForceMode.Impulse);
            // also apply upwards force that scales with the distanceBoost
            _rb.AddForce(Vector3.up * (grappleUpwardForce * distanceBoost), ForceMode.Impulse);
            // -> make sure to use ForceMode.Impulse because you're only applying force once
        }

        // Stop grapple after a second, (by this time you'll already have travelled most of the distance anyway)
        // StartCoroutine(StopGrapple(grappleIndex, 1f));
        
        _grapplesExecuted[grappleIndex] = true;
    }

    private void TryStopGrapple(int grappleIndex)
    {
        // can't stop grapple if not even executed
        if (!_grapplesExecuted[grappleIndex]) return;

        StartCoroutine(StopGrapple(grappleIndex));
    }

    private IEnumerator StopGrapple(int grappleIndex, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        // make sure PlayerParent can move
        if(_pm.Freeze) _pm.Freeze = false;

        _pm.ResetRestrictions();

        // reset the _grapplesExecuted bool
        _grapplesExecuted[grappleIndex] = false;

        GrapplesActive[grappleIndex] = false;
        
        UpdateHooksActive();

        // print("grapple: stop " + grappleIndex);
    }

    private void CancelActiveGrapples()
    {
        StartCoroutine(StopGrapple(0));
        StartCoroutine(StopGrapple(1));
    }

    private void CancelAllGrapplesExcept(int grappleIndex)
    {
        for (int i = 0; i < amountOfHookPoints;  i++)
            if (i != grappleIndex) StartCoroutine(StopGrapple(i));
    }

    private void CancelActiveSwings()
    {
        StopSwing(0);
        StopSwing(1);
    }

    private void UpdateHooksActive()
    {
        for (int i = 0; i < HookPoints.Count; i++)
            HooksActive[i] = GrapplesActive[i] || SwingsActive[i];
    }

    public void OnObjectTouch()
    {
        if (AnyGrappleExecuted())
        {
            // print("grapple: objecttouch");
            CancelActiveGrapples();
        }
    }
    
    public void CancelAllHooks()
    {
        CancelActiveGrapples();
        CancelActiveSwings();
    }

    #endregion

    #region Tracking Objects
    
    private void TrackObject(int grappleIndex)
    {
        //implement canceling of grapple if object is destroyed later //TODO: THIS
        if (_grappleObjects[grappleIndex] == null) return;
        
        //use local position of hit point on object
        HookPoints[grappleIndex] = _grappleObjects[grappleIndex].TransformPoint(_grappleLocalPoints[grappleIndex]);
        
        //was going null in grapple cancel
        if (_joints[grappleIndex] != null)
        {
            _joints[grappleIndex].connectedAnchor = HookPoints[grappleIndex];
        }
    }

    #endregion

    #region Getters and Setters

    private Vector3 currentGrapplePosition;

    private bool TargetPointFound(int index)
    {
        return _predictionHits[index].point != Vector3.zero;
    }

    // a bool to check if we're currently Swinging or grappling
    /// function needed and called from the GrapplingRope script
    public bool IsHooking(int index)
    {
        return HooksActive[index];
    }
    
    private bool AnyGrappleExecuted()
    {
        for (int i = 0; i < _grapplesExecuted.Count; i++)
            if (_grapplesExecuted[i]) return true;
        
        return false;
    }

    // a Vetor3 to quickly access the grapple point
    /// function needed and called from the GrapplingRope script
    public Vector3 GetGrapplePoint(int index)
    {
        return HookPoints[index];
    }

    public Vector3 GetGunTipPosition(int index)
    {
        return gunTips[index].position;
    }
    
    public List<bool> HooksActive { get; private set; }
    
    public List<Vector3> HookPoints { get; private set; } // the point you're grappling to / Swinging on
    
    public List<bool> GrapplesActive { get; private set; }
    
    public List<bool> SwingsActive { get; private set; }
    #endregion
}
