using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.Serialization;

#if UNITY_EDITOR

//making custom GUI buttons
[CustomEditor(typeof(PlayerCam))]
public class PlayerCamEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlayerCam myScript = (PlayerCam)target;

        if (GUILayout.Button("Switch First Person"))
        {
            myScript.SwitchCamType(ECamType.FirstPerson);
        }

        if (GUILayout.Button("Switch Third Orbit"))
        {
            myScript.SwitchCamType(ECamType.ThirdOrbit);
        }

        if (GUILayout.Button("Switch Third Fixed"))
        {
            myScript.SwitchCamType(ECamType.ThirdFixed);
        }
        
        DrawDefaultInspector();
    }
}

#endif

public enum ECamType
{
    FirstPerson,
    ThirdOrbit,
    ThirdFixed,
}

public enum EControlScheme
    {
        KeyboardAndMouse,
        Gamepad
    }

[RequireComponent(typeof(PlayerMovement))]
public class PlayerCam : MonoBehaviour
{
    
    
    [Header("Cam References")]
    
    [SerializeField] private CinemachineCamera firstPersonCinCam;
    
    [SerializeField] private CinemachineCamera thirdPersonOrbitCinCam;
    
    [SerializeField] private CinemachineCamera thirdPersonFixedCam;
    
    [field: SerializeField] public Camera RealCam { get; private set; }
    
    
    [Header("Player References")]
    
    [SerializeField] private Transform hookPrediction;
    
    [SerializeField] private List<Transform> armBaseRigTargetRots;
    
    [field: SerializeField] public Transform CamOrientation { get; private set; }
    
    
    [Header("Input References")]
    
    public string lookActionName = "Look";
    
    public string moveActionName = "Move";
    
    [Header("General Cam Settings")]
    
    [SerializeField] private LayerMask firstPersonRenderMask = -1;
    
    [SerializeField] private LayerMask thirdPersonRenderMask = -1;
    
    [field: SerializeField] public ECamType CurrentCamType { get; private set; }
    
    [Header("First Person Cam Settings")]
    
    [SerializeField] private float keyAndMouseFirstPersonLookSpeedMult = 0.5f;
    
    [SerializeField] private float gamepadFirstPersonLookSpeedMult = 1f;
    
    [Range(0,1)]
    [SerializeField] private float gamepadFirstPersonVerticalModifier = 0.5f;
    
    [Header("Third Person Orbit Cam Settings")]
    
    [SerializeField] private float playerRotSpeed = 7;
    
    [Header("Third person Fixed Cam Settings")]
    
    [SerializeField] private float keyAndMouseThirdPersonFixedLookSpeedMult = 0.5f;
    
    [SerializeField] private float gamepadThirdPersonFixedLookSpeedMult = 1f;
    
    [Range(0,1)]
    [SerializeField] private float gamepadThirdPersonFixedVerticalModifier = 0.5f;

    [Header("Cam Effects Settings")]
    
    [SerializeField] private float baseFov = 100f;
    
    [Space]
    
    [SerializeField] private float baseTilt = 0f;
    
    //Dynamic, Non Serialized Below

    public event Action<ECamType> OnSwitchCamType;
    
    //Player References
    private Transform _orientation;

    private Grappling _grappling;
    
    private Transform _playerObj;
    
    private Rigidbody _rb;
    
    private InputAction _lookAction;
    
    private InputAction _moveAction;
    
    //Camera References
    private CinemachineThirdPersonFollow _thirdPersonFixedCamFollow;
    
    //Input
    private Vector2 _lookInput;
    
    private Vector2 _moveInput;

    private EControlScheme _controlScheme;
    
    //Rot Values

    private float _firstPersonXRot;
    private float _firstPersonYRot;
    
    private float _thirdFixedXRot;
    private float _thirdFixedYRot;
    
    private List<Vector3> _startRotDirs;
    
    private List<float> _startYRots;
    
    //Gamepad control schemes
    
    const string GAMEPADSCHEMENAME = "Gamepad";
    
    const string KANDMSCHEMENAME = "Keyboard&Mouse";

    private void Awake()
    {
        // get references
        _rb = GetComponent<Rigidbody>();
        
        _thirdPersonFixedCamFollow = thirdPersonFixedCam.GetComponent<CinemachineThirdPersonFollow>();
        
        _grappling = GetComponent<Grappling>();
        
        PlayerMovement pm = GetComponent<PlayerMovement>();
        _orientation = pm.Orientation;
        _playerObj = pm.PlayerObj;
        
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
        //set the control scheme
        string currentControlScheme = playerInput.currentControlScheme;
        
        if (currentControlScheme == GAMEPADSCHEMENAME)
        {
            _controlScheme = EControlScheme.Gamepad;
        }
        else if (currentControlScheme == KANDMSCHEMENAME)
        {
            _controlScheme = EControlScheme.KeyboardAndMouse;
        }
        else
        {
            Debug.LogError("Unknown control scheme, disabling");
            
            enabled = false;
        }
        
        playerInput.onControlsChanged += OnControlSchemeChanged;
        
        _moveAction = playerInput.actions.FindAction(moveActionName);
        _lookAction = playerInput.actions.FindAction(lookActionName);
    }
    
    private void Start()
    {
        SwitchCamType(CurrentCamType);
    }
    
    private void OnControlSchemeChanged(PlayerInput playerInput)
    {
        _controlScheme = playerInput.currentControlScheme == GAMEPADSCHEMENAME ? 
            EControlScheme.Gamepad : EControlScheme.KeyboardAndMouse;
    }
    
     private void OnEnable()
    {
        _lookAction.Enable();
        
        _moveAction.Enable();
        
        //setting first person and third fixed rots based on current rots
        _firstPersonYRot = _orientation.eulerAngles.y;
        _firstPersonXRot = _orientation.eulerAngles.x;
        
        _thirdFixedYRot = _orientation.eulerAngles.y;
        _thirdFixedXRot = _orientation.eulerAngles.x;
        
        //setting arm rots
        _startRotDirs = new List<Vector3>();
        
        for (int i = 0; i < armBaseRigTargetRots.Count; i++)
        {
            _startRotDirs.Add(armBaseRigTargetRots[i].eulerAngles);
        }
    }
    
    private void OnDisable()
    {
        _lookAction.Disable();
        
        _moveAction.Disable();
    }


    private void Update()
    {

        GetInput();
        
        ManageCamera();
        
        ManageHooks();
    }
    
    private void GetInput()
    {
        _lookInput = _lookAction.ReadValue<Vector2>();
        
        _moveInput = _moveAction.ReadValue<Vector2>();
    }
    
    internal void SwitchCamType(ECamType toCamType)
    {
        firstPersonCinCam.gameObject.SetActive( false);
        thirdPersonOrbitCinCam.gameObject.SetActive( false);
        thirdPersonFixedCam.gameObject.SetActive( false);

        switch (toCamType)
        {
            case ECamType.FirstPerson:
                
                CurrentCamType = ECamType.FirstPerson;
                
                RealCam.cullingMask = firstPersonRenderMask;
                
                firstPersonCinCam.transform.position = CamOrientation.position;
                
                firstPersonCinCam.gameObject.SetActive( true);
                
                break;
            
            case ECamType.ThirdOrbit:
                
                CurrentCamType = ECamType.ThirdOrbit;
                
                RealCam.cullingMask = thirdPersonRenderMask;
                
                thirdPersonOrbitCinCam.gameObject.SetActive( true);
                
                break;
            
            case ECamType.ThirdFixed:
                
                CurrentCamType = ECamType.ThirdFixed;
                
                RealCam.cullingMask = thirdPersonRenderMask;
                
                thirdPersonFixedCam.gameObject.SetActive( true);
                
                break;
        }
        
        OnSwitchCamType?.Invoke(CurrentCamType);
    }

    private void ManageCamera()
    {
        
        switch (CurrentCamType)
        {
            case ECamType.FirstPerson:
                
                //get look input and modify based on control scheme
                float firstLookSpeedMult = _controlScheme == EControlScheme.Gamepad ?
                gamepadFirstPersonLookSpeedMult : keyAndMouseFirstPersonLookSpeedMult;
                
                _firstPersonYRot += _lookInput.x * firstLookSpeedMult;
                
                _firstPersonXRot = _controlScheme == EControlScheme.Gamepad ?
                    _firstPersonXRot - _lookInput.y * firstLookSpeedMult * gamepadFirstPersonVerticalModifier :
                    _firstPersonXRot - _lookInput.y * firstLookSpeedMult;
                
                // make sure that you can't look up or down more than 90* degrees
                _firstPersonXRot = Mathf.Clamp(_firstPersonXRot, -89f, 89f);
                
                //rotate PlayerParent object and Orientation along only the y axis
                _orientation.rotation = Quaternion.Euler(0, _firstPersonYRot, 0);
                _playerObj.rotation = Quaternion.Euler(0, _firstPersonYRot, 0);
                
                
                //rotate RealCamTrans Orientation fully
                CamOrientation.rotation = Quaternion.Euler(_firstPersonXRot, _firstPersonYRot, 0);
                
                //apply position and rotation to the CinCamera
                firstPersonCinCam.transform.rotation = CamOrientation.rotation;
                firstPersonCinCam.transform.position = CamOrientation.position; // CamOrientation is childed to PlayerParent
                
                break;
            
            case ECamType.ThirdOrbit:
                
                //rotate RealCamTrans Orientation fully to direction from RealCamTrans to PlayerParent
                CamOrientation.rotation = Quaternion.LookRotation(_playerObj.position - new Vector3(thirdPersonOrbitCinCam.transform.position.x,
                    RealCam.transform.position.y, thirdPersonOrbitCinCam.transform.position.z));
                
                //rotate PlayerParent Orientation along only the y axis of RealCamTrans Orientation
                _orientation.rotation = Quaternion.Euler(0, CamOrientation.rotation.eulerAngles.y, 0);

                //use relative input direction to rotate PlayerParent object relative to PlayerParent Orientation
                Vector3 relativeInputDir = _orientation.forward * _moveInput.y + _orientation.right * _moveInput.x;
                
                if (relativeInputDir != Vector3.zero)
                {
                    _playerObj.forward = Vector3.Slerp(_playerObj.forward, relativeInputDir, Time.deltaTime *
                        playerRotSpeed);
                }
                
                break;
            
            case ECamType.ThirdFixed:
                
                //similar to first person, just not managing the camera position or rotation
                float thirdLookSpeedMult = _controlScheme == EControlScheme.Gamepad ?
                    gamepadThirdPersonFixedLookSpeedMult : keyAndMouseThirdPersonFixedLookSpeedMult;
                
                _thirdFixedYRot += _lookInput.x * thirdLookSpeedMult;
                
                _thirdFixedXRot = _controlScheme == EControlScheme.Gamepad ?
                    _thirdFixedXRot - _lookInput.y * thirdLookSpeedMult * gamepadThirdPersonFixedVerticalModifier :
                    _thirdFixedXRot - _lookInput.y * thirdLookSpeedMult;
                
                _thirdFixedXRot = Mathf.Clamp(_thirdFixedXRot, -89, 89);
                
                CamOrientation.transform.rotation = Quaternion.Euler(_thirdFixedXRot, _thirdFixedYRot, 0);
                
                _orientation.rotation = Quaternion.Euler(0, _thirdFixedYRot, 0);
                _playerObj.rotation = Quaternion.Euler(0, _thirdFixedYRot, 0);
                
                break;
                
        }
    }
    
    private void ManageHooks()
    {
        hookPrediction.rotation = Quaternion.Euler(CamOrientation.eulerAngles.x, CamOrientation.eulerAngles.y, 0);
        
        //updating target arm rotations
        for ( int i = 0; i < armBaseRigTargetRots.Count; i++)
        {
            Vector3 targetRotDir = armBaseRigTargetRots[i].eulerAngles;
            
            if (_grappling.HooksActive[i])
            {
                Vector3 toGrappleDir = _grappling.HookPoints[i] - hookPrediction.position;
                
                targetRotDir.x = Quaternion.LookRotation(toGrappleDir).eulerAngles.x;
            }
            else
            {
                targetRotDir.x = CamOrientation.eulerAngles.x;
            }

            armBaseRigTargetRots[i].eulerAngles = targetRotDir;
        }
    }


    // double click the field below to show all fov, tilt and RealCam shake code
    // Note: For smooth transitions I use the free DoTween Asset!
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
        
        //Can simply get RealCamTrans and set fov to fov
        switch (CurrentCamType)
        {
            case ECamType.FirstPerson:
                firstPersonCinCam.Lens.FieldOfView = fov;
                break;
            case ECamType.ThirdOrbit:
                thirdPersonOrbitCinCam.Lens.FieldOfView = fov;
                break;
            case ECamType.ThirdFixed:
                thirdPersonFixedCam.Lens.FieldOfView = fov;
                break;
        }
    }
    
    private IEnumerator ChangeFOV(float endValue, float transitionTime)
    {
        CinemachineCamera cam = null;
        
        switch (CurrentCamType)
        {
            case ECamType.FirstPerson:
                cam = firstPersonCinCam;
                break;
            case ECamType.ThirdOrbit:
                cam = thirdPersonOrbitCinCam;
                break;
            case ECamType.ThirdFixed:
                cam = thirdPersonFixedCam;
                break;
            default:
                Debug.LogError("No RealCamTrans type set in ChangeFOV");
                
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

        switch (CurrentCamType)
        {
            case ECamType.FirstPerson:
                firstPersonCinCam.Lens.Dutch = tilt;
                break;
            case ECamType.ThirdOrbit:
                thirdPersonOrbitCinCam.Lens.Dutch = tilt;
                break;
            case ECamType.ThirdFixed:
                
                thirdPersonFixedCam.Lens.Dutch = tilt;
                
                //if tilting to new state, change side
                if (tilt != baseTilt)
                {
                    _thirdPersonFixedCamFollow.CameraSide = tilt < 0 ? 1 : 0;
                }
                break;
        }
    }
    
    private IEnumerator ChangeTilt (float endValue, float transitionTime)
    {
        CinemachineCamera cam = null;

        float targetFixedSide = _thirdPersonFixedCamFollow.CameraSide;
        
        switch (CurrentCamType)
        {
            case ECamType.FirstPerson:
                cam = firstPersonCinCam;
                break;
            case ECamType.ThirdOrbit:
                cam = thirdPersonOrbitCinCam;
                break;
            case ECamType.ThirdFixed:
                
                cam = thirdPersonFixedCam;
                
                if (endValue != baseTilt)
                {
                    targetFixedSide = endValue < 0 ? 1 : 0;
                }
                
                break;
            default:
                Debug.LogError("No RealCamTrans type set in ChangeTilt");
                
                yield break;
        }
        
        float tiltIncrement = (endValue - cam.Lens.Dutch) / transitionTime;
        
        float sideIncrement;
        
        //RealCamTrans range is slider 0-1, so we need to convert the target side to this range
        sideIncrement = (targetFixedSide - _thirdPersonFixedCamFollow.CameraSide) / transitionTime;
        
        float timeStop = Time.time + transitionTime;
        
        while (Time.time < timeStop)
        {
            cam.Lens.Dutch += tiltIncrement * Time.deltaTime;
            
            if (CurrentCamType == ECamType.ThirdFixed)
            {
                _thirdPersonFixedCamFollow.CameraSide += sideIncrement * Time.deltaTime;
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

[Serializable]
public class ArmRotationInfo
{
    public Transform ArmAim;
    
    public Quaternion StartRot;
    
    public Quaternion LowerAdditiveBounds;
    
    public Quaternion UpperAdditiveBounds;
}