using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


// Dave MovementLab - Grappling
///
// Content:
/// - swinging ability
/// - grappling ability
/// 
// Note:
/// This script handles starting and stopping the swinging and grappling ability, as well as moving the player
/// The grappling rope is drawn and animated by the GrapplingRope_MLab script
/// 
/// If you don't understand the difference between swinging and grappling, please read the documentation
/// 
// Also, the swinging ability is based on Danis tutorial
// Credits: https://youtu.be/Xgh4v1w5DxU



/// single, or dual swinging
/// 
/// grappling left or right -> cancels any active swings and grapples
/// no grappling left/right twice in a row
/// swinging -> cancels any active grapples, exit limited state!
/// 
/// This implies that swinging and grappling can never be active at the same time, neither can there be 2 active grapples


public class Grappling_MLab: MonoBehaviour
{
    public enum GrappleMode
    {
        Basic,
        Precise
    }
    
    [Header("Player References")]
    
    public Transform orientation;
    
    [Header("Camera References")]
    
    public Transform cam;
    
    [Header("Hook Rig References")]
    
    public List<Transform> gunTips;
    public List<Transform> pointAimers;
    
    [Header("Prediction References")]
    
    public List<Transform> predictionPoints;
    
    [Header("Input References")]
    
    public string leftGrappleActionName = "LeftGrapple";
    
    public string rightGrappleActionName = "RightGrapple";
    
    public string leftSwingActionName = "LeftSwing";
    
    public string rightSwingActionName = "RightSwing";
    
    public string moveActionName = "Move";
    
    public string jumpActionName = "Jump";
    
    public InputAction leftGrappleAction;
    
    public InputAction rightGrappleAction;
    
    public InputAction leftSwingAction;
    
    public InputAction rightSwingAction;
    
    public InputAction moveAction;
    
    public InputAction jumpAction;
    
    [Header("General Hook Settings")]
    
    public float playerHeight = 2f;
    
    public int amountOfHookPoints = 2;
    
    public bool useChargeOnHookNotHit = true;
    
    public LayerMask whatIsGrappleable; // you can grapple & swing on all objects that are in this layermask
    
    public float aimLineSpherecastRadius = 3f;
    
    [Header("Grapple Settings")]
    
    public float maxGrappleDistance = 20f; // max distance you're able to grapple onto objects
    
    [Space]
    
    public float grappleDelayTime = 0.15f; // the time you freeze in the air before grappling
    public float grapplingCd = .25f; // cooldown of your grappling ability
    
    [Space]
    
    public GrappleMode grappleMode = GrappleMode.Precise;
    
    [Space]
    
    [Tooltip("Only applied when grappleMode is set to Precise")]
    public float overshootYAxis = 2f; // adjust the trajectory hight of the player when grappling
    
    [Space]
    
    [Tooltip("Only applied when grappleMode is set to Basic")]
    public float grappleForce = 35f;
    
    [Tooltip("Only applied when grappleMode is set to Basic")]
    public float grappleUpwardForce = 7.5f;
    
    [Space]
    
    public float grappleDistanceHeightMultiplier = 0.1f; // how much more force you gain when grappling toward objects that are further away
    
    [Space]
    
    public bool freezeOnGrappleNotHit;
    
    [Header("Swing Settings")]
    
    public float maxSwingDistance = 20f; // max distance you're able hit objects for swinging ability
    
    [Space]
    
    public float spring = 50f; // spring of the SpringJoint component
    public float damper = 50f; // damper of the SpringJoint component
    public float massScale = 1f; // massScale of the SpringJoint component
    
    [Space]
    
    public bool enableSwingingWithForces = true;
    
    [Space]
    
    public float lateralThrustForce = 2500;
    public float retractThrustForce = 3500;
    public float extendCableSpeed = 10;
    
    //Dynamic, Non-Serialized Below
    
    //Player References
    
    private Rigidbody rb;
    
    private PlayerMovement_MLab pm;
    
    //Prediction References
    
    private List<RaycastHit> predictionHits;
    
    private List<Vector3> grapplePoints; // the point you're grappling to / swinging on
    
    private List<Transform> grappleObjects; // the object transform you're grappling to
    
    private List<Vector3> grappleLocalPoints; //local position of hit point on object
    
    private Vector3 pullPoint; // point in space to pull player towards
    
    //General References
    
    private List<SpringJoint> joints; // for swining we use Unitys SpringJoint component
    
    //Input
    private Vector2 moveInput;
    
    //Timing
    
    private float grapplingCdTimer;

    //State
    [HideInInspector] public List<bool> grapplesExecuted;
    [HideInInspector] public List<bool> grapplesActive;
    [HideInInspector] public List<bool> swingsActive;
    
    private List<bool> hooksActive;
    
    private void Start()
    {
        // if you don't set whatIsGrappleable to anything, it's automatically set to Default
        if (whatIsGrappleable.value == 0)
            whatIsGrappleable = LayerMask.GetMask("Default");

        // get references
        pm = GetComponent<PlayerMovement_MLab>();
        rb = GetComponent<Rigidbody>();

        ListSetup();
        
        PlayerInput playerInput = GetComponent<PlayerInput>();
        
        leftGrappleAction = playerInput.actions.FindAction(leftGrappleActionName);
        rightGrappleAction = playerInput.actions.FindAction(rightGrappleActionName);
        leftSwingAction = playerInput.actions.FindAction(leftSwingActionName);
        rightSwingAction = playerInput.actions.FindAction(rightSwingActionName);
        moveAction = playerInput.actions.FindAction(moveActionName);
        jumpAction = playerInput.actions.FindAction(jumpActionName);
    }

    private void OnEnable()
    {
        leftGrappleAction.Enable();
        rightGrappleAction.Enable();
        leftSwingAction.Enable();
        rightSwingAction.Enable();
        moveAction.Enable();
        jumpAction.Enable();
    }
    
    private void OnDisable()
    {
        leftGrappleAction.Disable();
        rightGrappleAction.Disable();
        leftSwingAction.Disable();
        rightSwingAction.Disable();
        moveAction.Disable();
        jumpAction.Disable();
    }

    private void ListSetup()
    {
        hooksActive = new List<bool>();
        predictionHits = new List<RaycastHit>();
        
        grapplePoints = new List<Vector3>();
        grappleObjects = new List<Transform>();
        grappleLocalPoints = new List<Vector3>();
        joints = new List<SpringJoint>();

        grapplesExecuted = new List<bool>();
        grapplesActive = new List<bool>();
        swingsActive = new List<bool>();

        for (int i = 0; i < amountOfHookPoints; i++)
        {
            hooksActive.Add(false);
            predictionHits.Add(new RaycastHit());
            grappleObjects.Add(null);
            grappleLocalPoints.Add(Vector3.zero);
            joints.Add(null);
            grapplePoints.Add(Vector3.zero);
            grapplesExecuted.Add(false);
            grapplesActive.Add(false);
            swingsActive.Add(false);
        }
    }

    private void Update()
    {
        // cooldown timer
        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;

        // make sure MyInput() is called every frame
        MyInput();

        if (enableSwingingWithForces && joints[0] != null || joints[1] != null) OdmGearMovement();

        CheckForSwingPoints();
    }

    private void MyInput()
    {
        //due to modifiers on input for k&m, we want to always read if grapple is pressed first
        
        
        if (leftGrappleAction.triggered)
        {
            StartGrapple(0);
        }
        
        if (!grapplesActive[0] && leftSwingAction.triggered)
        {
            StartSwing(0);
        }
        
        if (rightGrappleAction.triggered)
        {
            StartGrapple(1);
        }
        
        if (!grapplesActive[1] && rightSwingAction.triggered)
        {
            StartSwing(1);
        }
        
        if (grapplesActive[0])
        {
            if (leftGrappleAction.phase != InputActionPhase.Performed) TryStopGrapple(0);
        }
        
        if (grapplesActive[1])
        {
            if (rightGrappleAction.phase != InputActionPhase.Performed) TryStopGrapple(1);
        }
        
        if (swingsActive[0])
        {
            if (!leftSwingAction.IsPressed()) StopSwing(0);
        }
        
        if (swingsActive[1])
        {
            if (!rightSwingAction.IsPressed()) StopSwing(1);
        }
        
        moveInput = moveAction.ReadValue<Vector2>();
    }

    #region Swinging

    private void CheckForSwingPoints()
    {
        for (int i = 0; i < amountOfHookPoints; i++)
        {
            if (hooksActive[i])
            {
                TrackObject(i);
            }
            else
            {
                RaycastHit hit = predictionHits[i];
                Physics.SphereCast(pointAimers[i].position, aimLineSpherecastRadius, pointAimers[i].forward, out hit, maxSwingDistance, whatIsGrappleable);

                // check if direct hit is available
                RaycastHit directHit;
                Physics.Raycast(orientation.position, cam.forward, out directHit, maxSwingDistance, whatIsGrappleable);

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

                predictionHits[i] = directHit.point == Vector3.zero ? hit : directHit;
            }
        }
    }
    public void StartSwing(int swingIndex)
    {
        if (!pm.IsStateAllowed(PlayerMovement_MLab.MovementMode.swinging))
            return;
        
        // no swinging point can be found
        if (!TargetPointFound(swingIndex))
        {
            if (useChargeOnHookNotHit)
            {
                // the grapple point is now just a point in the air
                /// calculated by taking your cameras position + the forwardDirection times your maxGrappleDistance
                grapplePoints[swingIndex] = cam.position + cam.forward * maxGrappleDistance;
                
                //setting grapple active for rope to show
                swingsActive[swingIndex] = true;
                UpdateHooksActive();
                
                //no grapple object
                grappleObjects[swingIndex] = null;
                
                //no local point
                grappleLocalPoints[swingIndex] = Vector3.zero;
                
                StartCoroutine(StopFailedSwing(swingIndex, 0.15f));
            }
            
            return;
        }
        
        // cancel all active grapples
        CancelActiveGrapples();
        pm.ResetRestrictions();
        
        //if Stopfailedswing is running, stop it
        StopCoroutine(nameof(StopFailedSwing));
        
        // this will cause the PlayerMovement script to enter MovementMode.swinging
        pm.swinging = true;

        //corresponding grappleObjects is the object the raycast hit
        grappleObjects[swingIndex] = predictionHits[swingIndex].transform;
        
        //converting hit point to local position of hit on object
        grappleLocalPoints[swingIndex] = grappleObjects[swingIndex].
            InverseTransformPoint(predictionHits[swingIndex].point);

        // the exact point where you swing on
        grapplePoints[swingIndex] = predictionHits[swingIndex].point;

        // add a springJoint component to your player
        joints[swingIndex] = gameObject.AddComponent<SpringJoint>();
        joints[swingIndex].autoConfigureConnectedAnchor = false;

        // set the anchor of the springJoint
        joints[swingIndex].connectedAnchor = grapplePoints[swingIndex];

        // calculate the distance to the grapplePoint
        float distanceFromPoint = Vector3.Distance(transform.position, grapplePoints[swingIndex]);

        // the distance grapple will try to keep from grapple point.
        // joints[swingIndex].maxDistance = distanceFromPoint * 0.8f;
        // joints[swingIndex].minDistance = distanceFromPoint * 0.25f;

        joints[swingIndex].maxDistance = distanceFromPoint;
        joints[swingIndex].minDistance = 0;

        // adjust these values to fit your game
        joints[swingIndex].spring = spring;
        joints[swingIndex].damper = damper;
        joints[swingIndex].massScale = massScale;

        swingsActive[swingIndex] = true;
        UpdateHooksActive();
    }

    public void StopSwing(int swingIndex)
    {
        pm.swinging = false;
        swingsActive[swingIndex] = false;

        UpdateHooksActive();

        // destroy the SpringJoint again after you stopped swinging 
        Destroy(joints[swingIndex]);
    }
    
    private IEnumerator StopFailedSwing(int swingIndex, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        
        swingsActive[swingIndex] = false;
        UpdateHooksActive();
    }

    #endregion

    #region Odm Gear
    private void OdmGearMovement()
    {
        if (swingsActive[0] && !swingsActive[1]) pullPoint = grapplePoints[0];
        if (swingsActive[1] && !swingsActive[0]) pullPoint = grapplePoints[1];
        // get midpoint if both swing points are active
        if (swingsActive[0] && swingsActive[1])
        {
            Vector3 dirToGrapplePoint1 = grapplePoints[1] - grapplePoints[0];
            pullPoint = grapplePoints[0] + dirToGrapplePoint1 * 0.5f;
        }

        // rightmoveInput.
        if (moveInput.x > 0) rb.AddForce(orientation.right * lateralThrustForce * Time.deltaTime);
        // left
        if (moveInput.x < 0) rb.AddForce(-orientation.right * lateralThrustForce * Time.deltaTime);
        // forward
        if (moveInput.y > 0) rb.AddForce(orientation.forward * lateralThrustForce * Time.deltaTime);
        // backward
        /// if (moveInput.y < 0) rb.AddForce(-orientation.forward * lateralThrustForce * Time.deltaTime);
        // shorten cable
        if (jumpAction.IsPressed())
        {
            Vector3 directionToPoint = pullPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * retractThrustForce * Time.deltaTime);

            // calculate the distance to the grapplePoint
            float distanceFromPoint = Vector3.Distance(transform.position, pullPoint);

            // the distance grapple will try to keep from grapple point
            UpdateJoints(distanceFromPoint);
        }
        // extend cable
        if (moveInput.y < 0)
        {
            // calculate the distance to the grapplePoint
            float extendedDistanceFromPoint = Vector3.Distance(transform.position, pullPoint) + extendCableSpeed;

            // the distance grapple will try to keep from grapple point
            UpdateJoints(extendedDistanceFromPoint);
        }
    }

    private void UpdateJoints(float distanceFromPoint)
    {
        for (int i = 0; i < joints.Count; i++)
        {
            if (joints[i] != null)
            {
                joints[i].maxDistance = distanceFromPoint * 0.8f;
                joints[i].minDistance = distanceFromPoint * 0.25f;
            }
        }
    }

    #endregion

    /// Here you'll find all of the code specificly needed for the grappling ability
    #region Grappling

    public void StartGrapple(int grappleIndex)
    {
        // in cooldown
        if (grapplingCdTimer > 0) return;

        // cancel active swings and grapples
        CancelActiveSwings();
        CancelAllGrapplesExcept(grappleIndex);

        // Case 1 - target point found
        if (TargetPointFound(grappleIndex))
        {
            // print("grapple: target found");

            // set cooldown
            grapplingCdTimer = grapplingCd;

            // this will cause the PlayerMovement script to change to MovemementMode.freeze
            /// -> therefore the player will freeze mid-air for some time before grappling
            pm.freeze = true;

            // same stuff as in StartSwing() function
            grappleObjects[grappleIndex] = predictionHits[grappleIndex].transform;
            
            //same as in StartSwing()
            grappleLocalPoints[grappleIndex] = grappleObjects[grappleIndex].
                InverseTransformPoint(predictionHits[grappleIndex].point);

            grapplePoints[grappleIndex] = predictionHits[grappleIndex].point;

            grapplesActive[grappleIndex] = true;
            
            UpdateHooksActive();

            // call the ExecuteGrapple() function after the grappleDelayTime is over
            StartCoroutine(ExecuteGrapple(grappleIndex));
        }
        // Case 2 - target point not found, preferential freeze and show grapple point
        else
        {
            // print("grapple: target missed");

            if (freezeOnGrappleNotHit)
            {
                // we still want to freeze the player for a bit
                pm.freeze = true;
            }
            
            //if using a charge, we want to use the charge and show like the player is attempting to grapple air
            if (useChargeOnHookNotHit)
            {
                // set cooldown
                grapplingCdTimer = grapplingCd;

                // the grapple point is now just a point in the air
                /// calculated by taking your cameras position + the forwardDirection times your maxGrappleDistance
                grapplePoints[grappleIndex] = cam.position + cam.forward * maxGrappleDistance;
                
                //setting grapple active for rope to show
                grapplesActive[grappleIndex] = true;
                UpdateHooksActive();
                
                //no grapple object
                grappleObjects[grappleIndex] = null;
                
                //no local point
                grappleLocalPoints[grappleIndex] = Vector3.zero;

                // call the StopGrapple() function after the grappleDelayTime is over
                StartCoroutine(StopGrapple(grappleIndex, grappleDelayTime));
            }
        }
    }

    public IEnumerator ExecuteGrapple(int grappleIndex)
    {
        yield return new WaitForSeconds(grappleDelayTime);

        // make sure that the player can move again
        pm.freeze = false;

        if(grappleMode == GrappleMode.Precise)
        {
            // find the lowest point of the player
            Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - (playerHeight / 2), transform.position.z);

            // calculate how much higher the grapple point is relative to the player
            float grapplePointRelativeYPos = grapplePoints[grappleIndex].y - lowestPoint.y;
            
            //if relative y offset is above relative player height, add all needed height, otherwise add less
            float highestPointOfArc = grapplePointRelativeYPos >= playerHeight ?
                grapplePointRelativeYPos + overshootYAxis : overshootYAxis / 2;

            // print("trying to grapple to " + grapplePointRelativeYPos + " which arc " + highestPointOfArc);

            pm.JumpToPosition(grapplePoints[grappleIndex], highestPointOfArc, default, 3f);
        }

        if(grappleMode == GrappleMode.Basic)
        {
            // calculate the direction from the player to the grapplePoint
            Vector3 direction = (grapplePoints[grappleIndex] - transform.position).normalized;

            // reset the y velocity of your rigidbody
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // the further the grapple point is away, the higher the distanceBoost should be
            float distanceBoost = Vector3.Distance(transform.position, grapplePoints[grappleIndex]) * grappleDistanceHeightMultiplier;

            // apply force to your rigidbody in the direction towards the grapplePoint
            rb.AddForce(direction * grappleForce , ForceMode.Impulse);
            // also apply upwards force that scales with the distanceBoost
            rb.AddForce(Vector3.up * (grappleUpwardForce * distanceBoost), ForceMode.Impulse);
            /// -> make sure to use ForceMode.Impulse because you're only applying force once
        }

        // Stop grapple after a second, (by this time you'll already have travelled most of the distance anyway)
        // StartCoroutine(StopGrapple(grappleIndex, 1f));
        
        grapplesExecuted[grappleIndex] = true;
    }

    private void TryStopGrapple(int grappleIndex)
    {
        // can't stop grapple if not even executed
        if (!grapplesExecuted[grappleIndex]) return;

        StartCoroutine(StopGrapple(grappleIndex));
    }

    private IEnumerator StopGrapple(int grappleIndex, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        // make sure player can move
        if(pm.freeze) pm.freeze = false;

        pm.ResetRestrictions();

        // reset the grapplesExecuted bool
        grapplesExecuted[grappleIndex] = false;

        grapplesActive[grappleIndex] = false;
        
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
        for (int i = 0; i < grapplePoints.Count; i++)
            hooksActive[i] = grapplesActive[i] || swingsActive[i];
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
        return;
        
        //implement canceling of grapple if object is destroyed later //TODO: THIS
        if (grappleObjects[grappleIndex] == null) return;
        
        //use local position of hit point on object
        grapplePoints[grappleIndex] = grappleObjects[grappleIndex].TransformPoint(grappleLocalPoints[grappleIndex]);
        
        //was going null in grapple cancel
        if (joints[grappleIndex] != null)
        {
            joints[grappleIndex].connectedAnchor = grapplePoints[grappleIndex];
        }
    }

    #endregion

    #region Getters

    private Vector3 currentGrapplePosition;

    private bool TargetPointFound(int index)
    {
        return predictionHits[index].point != Vector3.zero;
    }

    // a bool to check if we're currently swinging or grappling
    /// function needed and called from the GrapplingRope_MLab script
    public bool IsHooking(int index)
    {
        return hooksActive[index];
    }
    
    private bool AnyGrappleExecuted()
    {
        for (int i = 0; i < grapplesExecuted.Count; i++)
            if (grapplesExecuted[i]) return true;
        
        return false;
    }

    // a Vetor3 to quickly access the grapple point
    /// function needed and called from the GrapplingRope_MLab script
    public Vector3 GetGrapplePoint(int index)
    {
        return grapplePoints[index];
    }

    public Vector3 GetGunTipPosition(int index)
    {
        return gunTips[index].position;
    }

    #endregion
}
