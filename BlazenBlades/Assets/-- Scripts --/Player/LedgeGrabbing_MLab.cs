using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Dave MovementLab - LedgeGrabbing
///
// Content:
/// - detecting and moving towards ledges
/// - holding onto ledges
/// - jumping away from ledges
///
// Note:
/// This script is an extension of the WallRunning_MLab script, I did it this way, because 
/// the WallRunning script is already like 700 lines long
/// 

public class LedgeGrabbing_MLab : MonoBehaviour
{
    [Header("Player References")]
    
    private WallRunning_MLab main; // this script is an extension of the main wallrunning script
    
    private PlayerMovement_MLab pm;
    
    public Transform orientation;
    
    private Rigidbody rb;
    
    
    [Header("Camera References")]
    
    public Transform cam;
    
    [Header("Input References")]
    
    public string jumpActionName = "Jump";
    
    public InputAction jumpAction;
    
    [Header("Detection Settings")]
    
    public float ledgeDetectionLength = 3;
    public float ledgeSphereCastRadius = 0.5f;
    
    [Space]
    
    public float maxLedgeGrabDistance;
    
    [Space]
    
    public float minTimeOnLedge;
    
    [Space]
    
    public LayerMask whatIsLedge;
    
    [Header("Ledge Grabbing Behaviour Settings")]
    
    public float moveToLedgeSpeed = 12;
    
    [Space]
    
    public float ledgeJumpForwardForce = 14;
    public float ledgeJumpUpForce = 5;
    
    [Space]
    
    public float exitLedgeTime = 0.2f;
    
    //IDK if needed, wasn't used - Sid
    // public float maxLedgeJumpUpSpeed;
    
    [Header("State")]
    
    public bool exitingLedge;
    
    public Transform currLedge;
    
    //Dynamic, Non Serialized Below
    
    //Timing
    private float timeOnLedge;

    //State
    private bool holding;
    
    // Detection
    private RaycastHit ledgeHit;
    
    private Vector3 directionToLedge;
    
    private float distanceToLedge;
    
    private Transform lastLedge;
    
    private float exitLedgeTimer = 0.2f;


    private void Start()
    {
        // get references
        pm = GetComponent<PlayerMovement_MLab>();
        rb = GetComponent<Rigidbody>();
        main = GetComponent<WallRunning_MLab>();
        
        PlayerInput playerInput = GetComponent<PlayerInput>();
        
        jumpAction = playerInput.actions.FindAction(jumpActionName);
    }

    private void OnEnable()
    {
        jumpAction.Enable();
    }
    
    private void OnDisable()
    {
        jumpAction.Disable();
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
        Vector2 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // SubState 1 - Holding onto ledge
        if (holding)
        {
            FreezeRigidbodyOnLedge();

            if (timeOnLedge > minTimeOnLedge && inputDirection != Vector2.zero) ExitLedgeHold();

            timeOnLedge += Time.deltaTime;
        }

        // SubState 2 - Exiting Ledge
        else if(exitingLedge)
        {
            if (exitLedgeTimer > 0) exitLedgeTimer -= Time.deltaTime;
            else exitingLedge = false;
        }

        if (holding && jumpAction.triggered) LedgeJump();
    }

    private void LedgeDetection()
    {
        bool ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, cam.forward, out ledgeHit, ledgeDetectionLength, whatIsLedge);

        if (ledgeHit.transform == null) return;

        directionToLedge = ledgeHit.transform.position - transform.position;
        distanceToLedge = directionToLedge.magnitude;

        if (lastLedge != null && ledgeHit.transform == lastLedge) return;

        if (ledgeDetected && distanceToLedge < maxLedgeGrabDistance && !holding) EnterLedgeHold();
    }

    private void LedgeJump()
    {
        // print("ledge jump");

        ExitLedgeHold();

        Invoke(nameof(DelayedForce), 0.05f);
    }

    private void DelayedForce()
    {
        Vector3 forceToAdd = cam.forward * ledgeJumpForwardForce + orientation.up * ledgeJumpUpForce;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void EnterLedgeHold()
    {
        if (exitingLedge) return;

        // print("entered ledge hold");

        main.ledgegrabbing = true;
        holding = true;

        pm.restricted = true;
        
        //USED TO BE IN, I UNCOMMENTED BC MAX SPEED STAYED UNLIMITED, IDK WHY - Sid
        // pm.unlimitedSpeed = true;

        currLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
    }

    //Didn't seem to do anything, so I commented it out - Sid
    // bool touchingLedge;
    private void FreezeRigidbodyOnLedge()
    {
        rb.useGravity = false;

        Vector3 directionToLedge = currLedge.position - transform.position;

        if (directionToLedge.magnitude > maxLedgeGrabDistance && holding) ExitLedgeHold();

        // Move player towards ledge
        if (directionToLedge.magnitude > 1f)
        {
            // Vector3 directionToLedge = ledgeHit.transform.position - transform.position;
            // rb.velocity = directionToLedge.normalized * moveToLedgeSpeed;

            if (rb.linearVelocity.magnitude < moveToLedgeSpeed)
                rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime);

            // The current problem is that I can't set the velocity from here, I can only add force
            // -> but then the force is mainly upwards :D

            // print("moving to ledge");
        }

        // Hold onto ledge
        else
        {
            if (pm.unlimitedSpeed) pm.unlimitedSpeed = false;
            if (!pm.freeze) pm.freeze = true;
            //rb.velocity = Vector3.zero;
            // print("hanging on ledge");
        }
    }

    private void ExitLedgeHold()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;

        main.ledgegrabbing = false;
        holding = false;
        timeOnLedge = 0;

        pm.freeze = false;
        pm.unlimitedSpeed = false;
        pm.restricted = false;

        rb.useGravity = true;

        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f);
    }

    private void ResetLastLedge()
    {
        lastLedge = null;
    }


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
