using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam_MLab_Uncommented : MonoBehaviour
{
    [Header("Sensitivity")]
    public float sensX = 10f;
    public float sensY = 10f;

    [Header("Assignables")]
    public Transform camT;
    public Camera cam;
    public Transform orientation;

    [Header("Effects")]
    public float baseFov = 90f;
    public float fovTransitionTime = 0.25f;
    public float tiltTransitionTime = 0.25f;

    [Header("Effects - HeadBob")]
    [HideInInspector] public bool hbEnabled;
    public float hbAmplitude = 0.5f;
    public float hbFrequency = 12f;

    private float hbToggleSpeed = 3f;
    private Vector3 hbStartPos;
    private Rigidbody rb;

    [HideInInspector] public float mouseX;
    [HideInInspector] public float mouseY;

    private float multiplier = 0.01f;

    private float xRotation;
    private float yRotation;

    private void Start()
    {
        hbStartPos = cam.transform.localPosition;

        camT = GameObject.Find("CameraHolder").transform;
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        RotateCamera();

        if (hbEnabled)
        {
            CheckMotion();
            cam.transform.LookAt(FocusTarget());
        }
    }

    public void RotateCamera()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY;

        // calculate rotation
        yRotation += mouseX * sensX * multiplier;
        xRotation -= mouseY * sensY * multiplier;

        xRotation = Mathf.Clamp(xRotation, -89f, 89f);

        // rotate realCam and player
        camT.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }


    /// double click the field below to show all fov, tilt and realCam shake code
    #region Fov, Tilt and CamShake

    public void DoFov(float endValue, float transitionTime = -1)
    {
        //Change
    }

    public void ResetFov()
    {
        //Change
    }

    public void DoTilt(float zTilt)
    {
        //Change
    }

    public void ResetTilt()
    {
        //Change
    }
    
    public void DoShake(float amplitude, float frequency)
    {
        //Change
    }

    public void ResetShake()
    {
        StartCoroutine(ResetShakeRoutine());
    }
    public IEnumerator ResetShakeRoutine()
    {
        //Change

        yield return null;
    }

    #endregion


    /// double click the field below to show all headBob code
    #region HeadBob

    private void CheckMotion()
    {
        //Change
        PlayMotion(FootStepMotion());
    }

    private void PlayMotion(Vector3 motion)
    {
        //Change
    }

    private void ResetPosition()
    {
        //Change
    }

    private Vector3 FootStepMotion()
    {
        //Change
        
        return Vector3.zero;
    }


    private Vector3 FocusTarget()
    {
        //Change
        return Vector3.zero;
    }

    #endregion
}