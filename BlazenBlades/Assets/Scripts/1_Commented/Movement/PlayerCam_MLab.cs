using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.Serialization; // I use DoTween for the camera effects, so this reference is needed


// Dave MovementLab - PlayerCam
///
// Content:
/// - first person camera rotation
/// - camera effects such as fov changes, tilt or cam shake
/// - headBob effect while walking or sprinting
///
// Note:
/// This script is assigned to the player (like every other script).
/// It rotates the camera vertically and horizontally, while also rotating the orientation of the player, but only horizontally.
/// -> Most scripts then use this orientation to find out where "forward" is.
/// 
/// If you're a beginner, just ignore the effects and headBob stuff and focus on the rotation code.

#if UNITY_EDITOR

//maaking custom GUI buttons
[CustomEditor(typeof(PlayerCam_MLab))]
public class PlayerCam_MLabEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlayerCam_MLab myScript = (PlayerCam_MLab)target;

        if (GUILayout.Button("Switch First Person"))
        {
            myScript.SwitchToCamType(eCamType.FirstPerson);
        }

        if (GUILayout.Button("Switch Third Orbit"))
        {
            myScript.SwitchToCamType(eCamType.ThirdOrbit);
        }

        if (GUILayout.Button("Switch Third Fixed"))
        {
            myScript.SwitchToCamType(eCamType.ThirdFixed);
        }
        
        DrawDefaultInspector();
    }
}

#endif

public enum eCamType
{
    FirstPerson,
    ThirdOrbit,
    ThirdFixed,
}

public class PlayerCam_MLab : MonoBehaviour
{
    [Header("General Cam Settings")]
    
    public eCamType camType = eCamType.FirstPerson;
    
    public LayerMask firstPersonRenderMask = -1;
    
    public LayerMask thirdPersonRenderMask = -1;
    
    [Header("First Person Cam")]
    
    public GameObject firstPersonCamGameObject;
    
    public float firstPersonLookSpeedMult;
    
    [Header("Third Person Orbit Cam")]

    public GameObject thirdPersonOrbitGameObject;
    
    public float playerRotSpeed = 1;
    
    [Header("Third person Fixed Cam")]
    
    public GameObject thirdPersonFixedGameObject;
    
    public Transform thirdPersonFixedCamOrientation;
    
    public float thirdPersonFixedLookSpeedMult;
    
    [Header("Grapple View Management")]
    
    public GameObject grappleRig;
    
    [Header("Input Assignable")]
    
    public InputActionReference lookAction;
    
    public InputActionReference moveAction;
    
    [Header("Assignables")]
    public Camera cam; // the camera (inside the cameraHolder)
    public Transform orientation; // reference to the orientation of the player
    public Transform player;
    public Transform playerObj;

    [Header("Effects")]
    public float baseFov = 90f;
    public float fovTransitionTime = 0.25f; // how fast the cameras fov changes
    public float tiltTransitionTime = 0.25f; // how fast the cameras tilt changes

    [Header("Effects - HeadBob")]
    [HideInInspector] public bool hbEnabled; // this bool is changed by the PlayerMovement script, depending on the MovementState
    public float hbAmplitude = 0.5f; // how large the headBob effect is
    public float hbFrequency = 12f; // how fast the headBob effect plays


    // the rest are just private variables to store information

    private float hbToggleSpeed = 3f; // if the players speed is smaller than the hbToggleSpeed, the headBob effect wont play
    private Vector3 hbStartPos;
    private Rigidbody rb;
    
    private Vector2 lookInput;
    
    private Vector2 moveInput;

    private float firstPersonXRot;
    private float firstPersonYRot;
    
    private float thirdFixedXRot;
    private float thirdFixedYRot;

    private void Awake()
    {
        if (lookAction == null)
        {
            Debug.LogError("Look action not set in ThirdPersonCameraController");
            
            Destroy(gameObject);
        }
        
        if (moveAction == null)
        {
            Debug.LogError("Move action not set in ThirdPersonCameraController");
            
            Destroy(gameObject);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        
        Cursor.visible = false;
        
        SwitchToCamType(camType);
    }

    private void Start()
    {
        // store the startPosition of the camera
        hbStartPos = cam.transform.localPosition;

        // get the components
        rb = GetComponent<Rigidbody>();

        // lock the mouse cursor in the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
        // make the mouse coursor invisible
        Cursor.visible = false;
    }
    
     private void OnEnable()
    {
        lookAction.action.Enable();
        
        moveAction.action.Enable();
    }
    
    private void OnDisable()
    {
        lookAction.action.Disable();
        
        moveAction.action.Disable();
    }


    private void Update()
    {

        GetInput();
        
        ManageCamera();
        
        ManageGrappleGear();

        // if headBob is enabled, start the CheckMotion() function
        /// which then starts -> PlayMotion() and ResetPosition()
        if (hbEnabled)
        {
            CheckMotion();
            cam.transform.LookAt(FocusTarget());
        }
    }
    
    private void GetInput()
    {
        lookInput = lookAction.action.ReadValue<Vector2>();
        
        moveInput = moveAction.action.ReadValue<Vector2>();
    }

    public void SwitchToCamType(eCamType toCamType)
    {
        firstPersonCamGameObject.SetActive( false);
        thirdPersonOrbitGameObject.SetActive( false);
        thirdPersonFixedGameObject.SetActive( false);

        switch (toCamType)
        {
            case eCamType.FirstPerson:
                
                camType = eCamType.FirstPerson;
                
                cam.cullingMask = firstPersonRenderMask;
                
                firstPersonCamGameObject.transform.position = player.position;
                
                firstPersonCamGameObject.SetActive( true);
                
                break;
            
            case eCamType.ThirdOrbit:
                
                camType = eCamType.ThirdOrbit;
                
                cam.cullingMask = thirdPersonRenderMask;
                
                thirdPersonOrbitGameObject.SetActive( true);
                
                break;
            
            case eCamType.ThirdFixed:
                
                camType = eCamType.ThirdFixed;
                
                cam.cullingMask = thirdPersonRenderMask;
                
                thirdPersonFixedGameObject.SetActive( true);
                
                break;
        }
    }

    public void ManageCamera()
    {
        switch (camType)
        {
            case eCamType.FirstPerson:
                
                //set rotation
                firstPersonYRot += lookInput.x * firstPersonLookSpeedMult;
                firstPersonXRot -= lookInput.y * firstPersonLookSpeedMult;
                
                // make sure that you can't look up or down more than 90* degrees
                firstPersonXRot = Mathf.Clamp(firstPersonXRot, -89f, 89f);
                
                firstPersonCamGameObject.transform.position = player.position;
                
                firstPersonCamGameObject.transform.rotation = Quaternion.Euler(firstPersonXRot, firstPersonYRot, 0);
                
                //rotate player object and orientation along the y axis
                playerObj.rotation = Quaternion.Euler(0, firstPersonYRot, 0);
                orientation.rotation = Quaternion.Euler(0, firstPersonYRot, 0);
                
                break;
            
            case eCamType.ThirdOrbit:
                Vector3 orbitViewDir = player.position - new Vector3(thirdPersonOrbitGameObject.transform.position.x,
                    player.position.y, thirdPersonOrbitGameObject.transform.position.z);
                
                orbitViewDir.y = 0;

                orientation.forward = orbitViewDir.normalized;

                Vector3 orbitInputDir = orientation.forward * moveInput.y + orientation.right * moveInput.x;
                
                orbitInputDir = new Vector3(orbitInputDir.x, 0, orbitInputDir.z).normalized;

                if (orbitInputDir != Vector3.zero)
                {
                    playerObj.forward = Vector3.Slerp(playerObj.forward, orbitInputDir, Time.deltaTime *
                        playerRotSpeed);
                }
                
                break;
            
            case eCamType.ThirdFixed:
                
                thirdFixedYRot += lookInput.x * thirdPersonFixedLookSpeedMult;
                thirdFixedXRot -= lookInput.y * thirdPersonFixedLookSpeedMult;
                
                thirdFixedXRot = Mathf.Clamp(thirdFixedXRot, -89, 89);
                
                thirdPersonFixedCamOrientation.transform.rotation = Quaternion.Euler(thirdFixedXRot, thirdFixedYRot, 0);
                
                playerObj.rotation = Quaternion.Euler(0, thirdFixedYRot, 0);
                
                orientation.rotation = Quaternion.Euler(0, thirdFixedYRot, 0);
                
                break;
                
        }
    }
    
    public void ManageGrappleGear()
    {
        switch(camType)
        {
            case eCamType.FirstPerson:
                
                grappleRig.transform.rotation = Quaternion.Euler(firstPersonXRot, firstPersonYRot, 0);
                
                break;
            
            case eCamType.ThirdOrbit:
                
                Vector3 orbitViewDir = player.position - thirdPersonOrbitGameObject.transform.position;
                
                grappleRig.transform.rotation = Quaternion.LookRotation(orbitViewDir);
                
                break;
            
            case eCamType.ThirdFixed:
                
                grappleRig.transform.rotation = Quaternion.Euler(thirdFixedXRot, thirdFixedYRot, 0);
                
                break;
        }
    }


    /// double click the field below to show all fov, tilt and cam shake code
    /// Note: For smooth transitions I use the free DoTween Asset!
    #region Fov, Tilt and CamShake

    /// function called when starting to wallrun or starting to dash
    /// a simple function that just takes in an endValue, and then smoothly sets the cameras fov to this end value
    public void DoFov(float endValue, float transitionTime = -1)
    {
        if(transitionTime == -1)
            cam.DOFieldOfView(endValue, fovTransitionTime);

        else
            cam.DOFieldOfView(endValue, transitionTime);
    }

    public void ResetFov()
    {
        cam.DOFieldOfView(baseFov, fovTransitionTime);
    }

    /// function called when starting to wallrun
    /// smoothly tilts the camera
    public void DoTilt(float zTilt)
    {
        cam.transform.DOLocalRotate(new Vector3(0, 0, zTilt), tiltTransitionTime);
    }

    public void ResetTilt()
    {
        cam.transform.DOLocalRotate(Vector3.zero, tiltTransitionTime);
    }

    private Tweener shakeTween;
    public void DoShake(float amplitude, float frequency)
    {
        shakeTween = cam.transform.DOShakePosition(1f, .4f, 1, 90).SetLoops(-1);
    }

    public void ResetShake()
    {
        StartCoroutine(ResetShakeRoutine());
    }
    public IEnumerator ResetShakeRoutine()
    {
        /// needs to be fixed!

        shakeTween.SetLoops(1);
        cam.transform.DOKill(); // not optimal, sometimes kills the tilt or fov stuff too...

        if(shakeTween != null)
            yield return shakeTween.WaitForCompletion();

        cam.transform.DOLocalMove(Vector3.zero, .2f);
    }

    #endregion


    /// double click the field below to show all headBob code
    /// as a beginner, just ignore this code for now
    #region HeadBob

    // Important Note: I learned how to create this code by following along with a YouTube tutorial
    // Credits: https://www.youtube.com/watch?v=5MbR2qJK8Tc&ab_channel=Hero3D

    private void CheckMotion()
    {
        // get the current speed of the players rigidbody (y axis excluded)
        float speed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

        ResetPosition();

        // check if the speed is high enough to activate the headBob effect
        if (speed < hbToggleSpeed) return;

        PlayMotion(FootStepMotion());
    }

    private void PlayMotion(Vector3 motion)
    {
        // take the calculated motion and apply it to the camera
        cam.transform.localPosition += motion * Time.deltaTime;
    }

    private void ResetPosition()
    {
        if (cam.transform.localPosition == hbStartPos) return;

        // smoothly reset the position of the camera back to normal
        cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, hbStartPos, 1 * Time.deltaTime);
    }

    private Vector3 FootStepMotion()
    {
        // use Sine and Cosine to create a smooth looking motion that swings from left to right and from up to down 
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Sin(Time.time * hbFrequency) * hbAmplitude;
        pos.x += Mathf.Cos(Time.time * hbFrequency * 0.5f) * hbAmplitude * 2f;
        return pos;
    }


    private Vector3 FocusTarget()
    {
        // make sure the camera focuses (Looks at) a point 15 tiles away from the player
        // this stabilizes the camera
        Vector3 pos = new Vector3(transform.position.x, cam.transform.position.y, transform.position.z);
        pos += cam.transform.forward * 15f;
        return pos;
    }

    #endregion
}