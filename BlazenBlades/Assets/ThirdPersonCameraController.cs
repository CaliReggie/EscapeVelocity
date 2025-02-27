using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField]
    public Transform orientation;
    
    [SerializeField]
    public Transform player;
    
    [SerializeField]
    public Transform playerObj;

    [SerializeField]
    public float rotationSpeed = 1;
    
    [SerializeField]
    InputActionReference lookAction;
    
    //Dynamic, Non Serialized below
    
    private Vector2 lookInput;

    private void Awake()
    {
        if (lookAction == null)
        {
            Debug.LogError("Look action not set in ThirdPersonCameraController");
            
            Destroy(gameObject);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        
        Cursor.visible = false;
    }
    
    private void OnEnable()
    {
        lookAction.action.Enable();
    }
    
    private void OnDisable()
    {
        lookAction.action.Disable();
    }

    private void Update()
    {
        // GetInput();
        
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);

        orientation.forward = viewDir.normalized;
        
        Vector3 inputDir = orientation.forward * lookInput.y + orientation.right * lookInput.x;
        
        if (inputDir != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir, Time.deltaTime * rotationSpeed);
        }
    }
    
    
    private void GetInput()
    {
        lookInput = lookAction.action.ReadValue<Vector2>();
    }
}
