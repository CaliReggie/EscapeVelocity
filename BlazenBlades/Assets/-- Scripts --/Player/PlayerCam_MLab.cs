using System;
using System.Collections;
using Unity.Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.Serialization;


// Dave MovementLab - PlayerCam
///
// Content:
/// - first person camera rotation
/// - camera effects such as fov changes, tilt or realCam shake
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
    public enum EControlScheme
    {
        KeyboardAndMouse,
        Gamepad
    }
    
    [Header("Cam References")]
    
    public Camera realCam; 
    
    public CinemachineCamera firstPersonCinCam;
    
    public CinemachineCamera thirdPersonOrbitCinCam;
    
    public CinemachineCamera thirdPersonFixedCam;
    
    [Header("Player References")]
    
    public Transform orientation;
    
    public Transform player;
    
    public Transform playerObj;
    
    public Transform thirdPersonFixedCamOrientation;
    
    public Transform grappleRig;
    
    [Header("Input References")]
    
    public string lookActionName = "Look";
    
    public string moveActionName = "Move";
    
    public InputAction lookAction;
    
    public InputAction moveAction;
    
    [Header("General Cam Settings")]
    
    public eCamType camType = eCamType.FirstPerson;
    
    public LayerMask firstPersonRenderMask = -1;
    
    public LayerMask thirdPersonRenderMask = -1;
    
    [Header("First Person Cam Settings")]
    
    public float keyAndMouseFirstPersonLookSpeedMult = 0.5f;
    
    public float gamepadFirstPersonLookSpeedMult = 0.5f;
    
    [Range(0,1)]
    public float gamepadFirstPersonVerticalModifier = 0.5f;
    
    [Header("Third Person Orbit Cam Settings")]
    
    public float playerRotSpeed = 7;
    
    [Header("Third person Fixed Cam Settings")]
    
    public float keyAndMouseThirdPersonFixedLookSpeedMult = 0.5f;
    
    public float gamepadThirdPersonFixedLookSpeedMult = 0.5f;
    
    [Range(0,1)]
    public float gamepadThirdPersonFixedVerticalModifier = 0.5f;

    [Header("Cam Effects Settings")]
    public float baseFov = 100f;
    
    [Space]
    
    public float baseTilt = 0f;
    
    //Dynamic, Non Serialized Below
    
    //References
    private CinemachineThirdPersonFollow thirdPersonFixedCamFollow;
    
    private Rigidbody rb;
    
    //Input
    private Vector2 lookInput;
    
    private Vector2 moveInput;

    private EControlScheme controlScheme;
    
    //Rot Values

    private float firstPersonXRot;
    private float firstPersonYRot;
    
    private float thirdFixedXRot;
    private float thirdFixedYRot;
    
    //Gamepad control schemes
    
    const string gamepadControlSchemeName = "Gamepad";
    
    const string keyAndMouseControlSchemeName = "Keyboard&Mouse";

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
        
        PlayerInput playerInput = GetComponent<PlayerInput>();
        
        //set the control scheme
        string currentControlScheme = playerInput.currentControlScheme;
        
        if (currentControlScheme == gamepadControlSchemeName)
        {
            controlScheme = EControlScheme.Gamepad;
        }
        else if (currentControlScheme == keyAndMouseControlSchemeName)
        {
            controlScheme = EControlScheme.KeyboardAndMouse;
        }
        else
        {
            Debug.LogError("Unknown control scheme, destroying attached Player Input GameObject");
            
            Destroy(gameObject);
        }
        
        playerInput.onControlsChanged += OnControlSchemeChanged;
        
        moveAction = playerInput.actions.FindAction(moveActionName);
        lookAction = playerInput.actions.FindAction(lookActionName);
        
    }
    
    public void OnControlSchemeChanged(PlayerInput playerInput)
    {
        controlScheme = playerInput.currentControlScheme == gamepadControlSchemeName ? 
            EControlScheme.Gamepad : EControlScheme.KeyboardAndMouse;
    }

    private void Start()
    {
        // get the components
        rb = GetComponent<Rigidbody>();

        // lock the mouse cursor in the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
        // make the mouse coursor invisible
        Cursor.visible = false;
        
        // get the third person fixed cam follow component
        thirdPersonFixedCamFollow = thirdPersonFixedCam.GetComponent<CinemachineThirdPersonFollow>();
    }
    
     private void OnEnable()
    {
        lookAction.Enable();
        
        moveAction.Enable();
    }
    
    private void OnDisable()
    {
        lookAction.Disable();
        
        moveAction.Disable();
    }


    private void Update()
    {

        GetInput();
        
        ManageCamera();
        
        ManageGrappleGear();
    }
    
    private void GetInput()
    {
        lookInput = lookAction.ReadValue<Vector2>();
        
        moveInput = moveAction.ReadValue<Vector2>();
    }
    
    public void SwitchToCamType(eCamType toCamType)
    {
        firstPersonCinCam.gameObject.SetActive( false);
        thirdPersonOrbitCinCam.gameObject.SetActive( false);
        thirdPersonFixedCam.gameObject.SetActive( false);

        switch (toCamType)
        {
            case eCamType.FirstPerson:
                
                camType = eCamType.FirstPerson;
                
                realCam.cullingMask = firstPersonRenderMask;
                
                firstPersonCinCam.transform.position = player.position;
                
                firstPersonCinCam.gameObject.SetActive( true);
                
                break;
            
            case eCamType.ThirdOrbit:
                
                camType = eCamType.ThirdOrbit;
                
                realCam.cullingMask = thirdPersonRenderMask;
                
                thirdPersonOrbitCinCam.gameObject.SetActive( true);
                
                break;
            
            case eCamType.ThirdFixed:
                
                camType = eCamType.ThirdFixed;
                
                realCam.cullingMask = thirdPersonRenderMask;
                
                thirdPersonFixedCam.gameObject.SetActive( true);
                
                break;
        }
    }

    public void ManageCamera()
    {
        
        switch (camType)
        {
            case eCamType.FirstPerson:
                
                float firstLookSpeedMult = controlScheme == EControlScheme.Gamepad ?
                gamepadFirstPersonLookSpeedMult : keyAndMouseFirstPersonLookSpeedMult;
                
                
                //set rotation
                firstPersonYRot += lookInput.x * firstLookSpeedMult;
                
                firstPersonXRot = controlScheme == EControlScheme.Gamepad ?
                    firstPersonXRot - lookInput.y * firstLookSpeedMult * gamepadFirstPersonVerticalModifier :
                    firstPersonXRot - lookInput.y * firstLookSpeedMult;
                
                // make sure that you can't look up or down more than 90* degrees
                firstPersonXRot = Mathf.Clamp(firstPersonXRot, -89f, 89f);
                
                firstPersonCinCam.transform.position = player.position;
                
                firstPersonCinCam.transform.rotation = Quaternion.Euler(firstPersonXRot, firstPersonYRot, 0);
                
                //rotate player object and orientation along the y axis
                playerObj.rotation = Quaternion.Euler(0, firstPersonYRot, 0);
                orientation.rotation = Quaternion.Euler(0, firstPersonYRot, 0);
                
                break;
            
            case eCamType.ThirdOrbit:
                Vector3 orbitViewDir = player.position - new Vector3(thirdPersonOrbitCinCam.transform.position.x,
                    player.position.y, thirdPersonOrbitCinCam.transform.position.z);
                
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
                
                float thirdLookSpeedMult = controlScheme == EControlScheme.Gamepad ?
                    gamepadThirdPersonFixedLookSpeedMult : keyAndMouseThirdPersonFixedLookSpeedMult;
                
                thirdFixedYRot += lookInput.x * thirdLookSpeedMult;
                
                thirdFixedXRot = controlScheme == EControlScheme.Gamepad ?
                    thirdFixedXRot - lookInput.y * thirdLookSpeedMult * gamepadThirdPersonFixedVerticalModifier :
                    thirdFixedXRot - lookInput.y * thirdLookSpeedMult;
                
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
                
                grappleRig.rotation = Quaternion.Euler(firstPersonXRot, firstPersonYRot, 0);
                
                break;
            
            case eCamType.ThirdOrbit:
                
                Vector3 orbitViewDir = player.position - thirdPersonOrbitCinCam.transform.position;
                
                grappleRig.rotation = Quaternion.LookRotation(orbitViewDir);
                
                break;
            
            case eCamType.ThirdFixed:
                
                grappleRig.rotation = Quaternion.Euler(thirdFixedXRot, thirdFixedYRot, 0);
                
                break;
        }
    }


    /// double click the field below to show all fov, tilt and realCam shake code
    /// Note: For smooth transitions I use the free DoTween Asset!
    #region Fov, Tilt and CamShake

    /// function called when starting to wallrun or starting to dash
    /// a simple function that just takes in an endValue, and then smoothly sets the cameras fov to this end value
    public void DoFov(float endValue = -360, float transitionTime = -1)
    {
        //stop coroutine if running
        StopCoroutine(nameof(ChangeFOV));
        
        //if end value is -1, set to base
        if (endValue == -360) endValue = baseFov;
        
        //if transition time is -1, instantly set fov, otherwise use time
        if (transitionTime == -1)
        {
            SetFOV(endValue);
        }
        else
        {
            //Start coroutine to change fov
            StartCoroutine(ChangeFOV(endValue, transitionTime));
        }
    }
    
    private void SetFOV(float fov)
    {
        
        //Can simply get cam and set fov to fov
        switch (camType)
        {
            case eCamType.FirstPerson:
                firstPersonCinCam.Lens.FieldOfView = fov;
                break;
            case eCamType.ThirdOrbit:
                thirdPersonOrbitCinCam.Lens.FieldOfView = fov;
                break;
            case eCamType.ThirdFixed:
                thirdPersonFixedCam.Lens.FieldOfView = fov;
                break;
        }
    }
    
    private IEnumerator ChangeFOV(float endValue, float transitionTime)
    {
        CinemachineCamera cam = null;
        
        switch (camType)
        {
            case eCamType.FirstPerson:
                cam = firstPersonCinCam;
                break;
            case eCamType.ThirdOrbit:
                cam = thirdPersonOrbitCinCam;
                break;
            case eCamType.ThirdFixed:
                cam = thirdPersonFixedCam;
                break;
            default:
                Debug.LogError("No cam type set in ChangeFOV");
                
                yield break;
        }
        
        float incrementAmount = (endValue - cam.Lens.FieldOfView) / transitionTime;
        
        float timeStop = Time.time + transitionTime;
        
        while (Time.time < timeStop)
        {
            cam.Lens.FieldOfView += incrementAmount * Time.deltaTime;
            
            yield return null;
        }
    }
    
    
    public void DoTilt(float zTilt = -360, float transitionTime = -1)
    { 
        StopCoroutine(nameof(ChangeTilt));
        
        if (zTilt == -360) zTilt = baseTilt;
        
        if (transitionTime == -1)
        {
            SetTilt(zTilt);
        }
        else
        {
            //Start coroutine to change tilt
            StartCoroutine(ChangeTilt(zTilt, transitionTime));
        }
    }
    
    private void SetTilt(float tilt)
    {

        switch (camType)
        {
            case eCamType.FirstPerson:
                firstPersonCinCam.Lens.Dutch = tilt;
                break;
            case eCamType.ThirdOrbit:
                thirdPersonOrbitCinCam.Lens.Dutch = tilt;
                break;
            case eCamType.ThirdFixed:
                
                thirdPersonFixedCam.Lens.Dutch = tilt;
                
                //if tilting to new state, change side
                if (tilt != baseTilt)
                {
                    thirdPersonFixedCamFollow.CameraSide = tilt < 0 ? 1 : 0;
                }
                break;
        }
    }
    
    private IEnumerator ChangeTilt (float endValue, float transitionTime)
    {
        CinemachineCamera cam = null;

        float targetFixedSide = thirdPersonFixedCamFollow.CameraSide;
        
        switch (camType)
        {
            case eCamType.FirstPerson:
                cam = firstPersonCinCam;
                break;
            case eCamType.ThirdOrbit:
                cam = thirdPersonOrbitCinCam;
                break;
            case eCamType.ThirdFixed:
                
                cam = thirdPersonFixedCam;
                
                if (endValue != baseTilt)
                {
                    targetFixedSide = endValue < 0 ? 1 : 0;
                }
                
                break;
            default:
                Debug.LogError("No cam type set in ChangeTilt");
                
                yield break;
        }
        
        float tiltIncrement = (endValue - cam.Lens.Dutch) / transitionTime;
        
        float sideIncrement;
        
        //cam range is slider 0-1, so we need to convert the target side to this range
        sideIncrement = (targetFixedSide - thirdPersonFixedCamFollow.CameraSide) / transitionTime;
        
        float timeStop = Time.time + transitionTime;
        
        while (Time.time < timeStop)
        {
            cam.Lens.Dutch += tiltIncrement * Time.deltaTime;
            
            if (camType == eCamType.ThirdFixed)
            {
                thirdPersonFixedCamFollow.CameraSide += sideIncrement * Time.deltaTime;
            }
            
            yield return null;
        }
    }
    
    public void DoShake(float amplitude, float frequency)
    {
        //Change
    }
    
    public void ResetShake()
    {
        //Change
    }

    #endregion
}