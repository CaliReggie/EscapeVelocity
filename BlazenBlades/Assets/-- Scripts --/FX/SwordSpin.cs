using UnityEngine;

public class SwordSpin : MonoBehaviour
{
    [Header("Rotate Settings")]

    [SerializeField] private Vector3 rotateDirection;
    
    [SerializeField] private float rotateSpeed;
    
    [Header("Play/Stop On Enable/Disable")]
    
    [SerializeField] private bool respondToEnabling;
    
    //Dynamic, Non - Serialized Below
    
    private bool isRotating;
    
    private void OnEnable()
    {
        if (respondToEnabling) StartRotating();
    }
    
    private void OnDisable()
    {
        if (respondToEnabling) StopRotating();
    }
    
    
    private void Update()
    {
        if (isRotating) transform.Rotate(rotateSpeed * Time.deltaTime * rotateDirection.normalized);
    }
    
    public void StartRotating()
    {
        isRotating = true;
    }
    
    public void StopRotating()
    {
        isRotating = false;
    }
}
