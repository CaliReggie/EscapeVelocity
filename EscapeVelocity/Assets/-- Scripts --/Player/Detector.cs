using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

// This script handles all kinds of extra detections and calculations needed as information for other scripts.
// keeps all other scripts shorter and more understandable.

[RequireComponent(typeof(PlayerMovement))]
public class Detector : MonoBehaviour
{
    [Header("Behaviour")]
    
    [SerializeField] private bool showMarkerSphere;
    
    [Header("Debugging")]
    
    [SerializeField] private bool debuggingEnabled;
    
    [field: SerializeField] public Transform MarkerSphere { get; private set; }
    
    [SerializeField] private MeshRenderer markerSphereRenderer;
    
    [SerializeField] private TextMeshProUGUI textPredictionState;
    
    //Private, or Non-Serialized Below
    
    //Player References
    private PlayerMovement _pm;
    
    private Transform _orientation;
    
    //Detection
    private LayerMask _whatIsGround;

    private void Awake()
    {
        //get references
        _pm = GetComponent<PlayerMovement>();
        
        _orientation = _pm.Orientation;
    }
    
    private void Start()
    {
        //waited for pm to initialize
        _whatIsGround = _pm.WhatIsGround;
        
        // if no ground layermask is selected, set it to "Default"
        if (_whatIsGround.value == 0)
            _whatIsGround = LayerMask.GetMask("Default");

        if (!debuggingEnabled)
        {
            markerSphereRenderer.enabled = false;
        }
    }

    private void Update()
    {
        JumpPrediction();

        if(showMarkerSphere)
            markerSphereRenderer.enabled = PrecisionTargetFound;
    }

    // This function tries to predict where the PlayerParent wants to jump next.
    // Needed for precise ground and wall jumping.
    private void JumpPrediction()
    {
        RaycastHit viewRayHit;
        string predictionState;

        if (Physics.Raycast(_orientation.position, _orientation.forward, out viewRayHit, _pm.MaxJumpRange, _whatIsGround))
        {
            // Case 1 - raycast hits (in maxDistance)
            MarkerSphere.position = viewRayHit.point;

            predictionState = "in distance";

            PrecisionTargetFound = true;
        }

        else if (Physics.SphereCast(_orientation.position, 1f, _orientation.forward, out viewRayHit, 10f, _whatIsGround))
        {
            // Case 2 - raycast hits (out of maxDistance)

            // calculate nearest possible point
            Vector3 maxRangePoint = _orientation.position + _orientation.forward * _pm.MaxJumpRange;

            RaycastHit wallHit;
            if (Physics.Raycast(maxRangePoint, -viewRayHit.normal, out wallHit, 4f, _whatIsGround))
            {
                MarkerSphere.position = wallHit.point;
                predictionState = "out of distance, to wall";

                PrecisionTargetFound = true;
            }
            else
            {
                if (Vector3.Distance(_orientation.position, viewRayHit.point) <= _pm.MaxJumpRange)
                {
                    predictionState = "out of distance, hitPoint";
                    MarkerSphere.position = viewRayHit.point;

                    PrecisionTargetFound = true;
                }
                else
                {
                    predictionState = "out of distance, can't predict point..."; // -> same as case 3
                    MarkerSphere.position = _orientation.position + _orientation.forward * _pm.MaxJumpRange;

                    PrecisionTargetFound = false;
                }
            }
        }

        else
        {
            // Case 3 - raycast completely misses
            // -> Normal Jump
            // Gizmos.DrawWireSphere(RealCam.transform.position + camHolder.forward * MaxJumpRange, .5f);
            MarkerSphere.position = _orientation.position + _orientation.forward * _pm.MaxJumpRange;
            predictionState = "complete miss";

            PrecisionTargetFound = false;
        }

        if (PrecisionTargetFound)
            PrecisionTargetIsWall = viewRayHit.transform.gameObject.layer == 8;
        else
            PrecisionTargetIsWall = false;

        if (debuggingEnabled)
            textPredictionState.SetText(predictionState);
    }
    
    public bool PrecisionTargetFound { get; private set; }
    
    public bool PrecisionTargetIsWall { get; private set; }
}
