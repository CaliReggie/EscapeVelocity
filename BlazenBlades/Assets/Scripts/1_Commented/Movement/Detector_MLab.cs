using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;


// Dave MovementLab - Detector
///
// Content:
/// - detection for jump predictions
///
// Note:
/// This script handles all kinds of extra detections and calculations needed as information for other scripts.
/// I made this extra script to keep all other scripts shorter and more understandable.


public class Detector_MLab : MonoBehaviour
{
    [Header("References")]
    
    public PlayerMovement_MLab pm;
    
    public Transform orientation;
    
    [Header("Detection")]
    
    public LayerMask whatIsGround;
    
    [Header("Behaviour")]
    
    public bool showMarkerSphere = false;
    
    [Header("Jump Prediction State")]
    
    [HideInInspector] public bool precisionTargetFound;
    [HideInInspector] public bool precisionTargetIsWall;

    [Header("Debugging")]
    
    public bool debuggingEnabled;
    
    public MeshRenderer renderMarkerSphere;
    
    public Transform markerSphere;
    
    public Transform someSecondSphere;
    
    public TextMeshProUGUI textPredictionState;

    private void Start()
    {
        // if no ground layermask is selected, set it to "Default"
        if (whatIsGround.value == 0)
            whatIsGround = LayerMask.GetMask("Default");

        if (!debuggingEnabled)
        {
            renderMarkerSphere.enabled = false;
            someSecondSphere.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void Update()
    {
        JumpPrediction();

        if(showMarkerSphere)
            renderMarkerSphere.enabled = precisionTargetFound;
    }

    /// This function tries to predict where the player wants to jump next.
    /// Needed for precise ground and wall jumping.
    private void JumpPrediction()
    {
        RaycastHit viewRayHit;
        string predictionState;

        if (Physics.Raycast(orientation.position, orientation.forward, out viewRayHit, pm.maxJumpRange, whatIsGround))
        {
            // Case 1 - raycast hits (in maxDistance)
            markerSphere.position = viewRayHit.point;

            predictionState = "in distance";

            precisionTargetFound = true;
        }

        else if (Physics.SphereCast(orientation.position, 1f, orientation.forward, out viewRayHit, 10f, whatIsGround))
        {
            // Case 2 - raycast hits (out of maxDistance)

            // calculate nearest possible point
            Vector3 maxRangePoint = orientation.position + orientation.forward * pm.maxJumpRange;

            RaycastHit wallHit;
            if (Physics.Raycast(maxRangePoint, -viewRayHit.normal, out wallHit, 4f, whatIsGround))
            {
                markerSphere.position = wallHit.point;
                predictionState = "out of distance, to wall";

                precisionTargetFound = true;
            }
            else
            {
                someSecondSphere.position = viewRayHit.point;

                if (Vector3.Distance(orientation.position, viewRayHit.point) <= pm.maxJumpRange)
                {
                    predictionState = "out of distance, hitPoint";
                    markerSphere.position = viewRayHit.point;

                    precisionTargetFound = true;
                }
                else
                {
                    predictionState = "out of distance, can't predict point..."; // -> same as case 3
                    markerSphere.position = orientation.position + orientation.forward * pm.maxJumpRange;

                    precisionTargetFound = false;
                }
            }
        }

        else
        {
            // Case 3 - raycast completely misses
            // -> Normal Jump
            // Gizmos.DrawWireSphere(realCam.transform.position + camHolder.forward * maxJumpRange, .5f);
            markerSphere.position = orientation.position + orientation.forward * pm.maxJumpRange;
            predictionState = "complete miss";

            precisionTargetFound = false;
        }

        if (precisionTargetFound)
            precisionTargetIsWall = viewRayHit.transform.gameObject.layer == 8;
        else
            precisionTargetIsWall = false;

        if (debuggingEnabled)
            textPredictionState.SetText(predictionState);
    }
}
