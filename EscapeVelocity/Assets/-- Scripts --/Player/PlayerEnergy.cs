using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Player References")]
    
    [SerializeField] private Transform playerObjTransform;
    
    [Header("Potential Energy")]
    
    [SerializeField] private LayerMask whatIsGround;
    
    [Space]
    
    [SerializeField] private Vector2 heightRange = new Vector2(0, 100); //Height range
    
    [Header("Kinetic Energy")]

    [SerializeField] private float kineticSpeedGainThreshold; //speed required for gain
    
    [Range(0, 100)] [SerializeField] private float kineticGainPerSecond; //Kinetic gain per second
    
    [Space]
    
    [SerializeField] private float kineticSpeedLossThreshold; //speed required for loss
    
    [Range(0, 100)] [SerializeField] private float kineticLossAbovePerSecond; //Kinetic loss per second
    
    [Range(0, 100)] [SerializeField] private float kineticLossBelowPerSecond; //Kinetic loss per second below threshold
    
    //Dynamic, Non - Serialized Below
    
    //References

    private PlayerMovement_MLab pm;
    
    private Rigidbody playerRb;
    
    //Energy
    
    private float potentialEnergy;
    
    private float kineticEnergy;
    
    //State
    
    private float distanceToGround;
    
    private float lastSpeed;
    
    private float currentSpeed;
    
    private bool isGainingSpeed;
    
    private void Start()
    {
        pm = GetComponent<PlayerMovement_MLab>();
        
        playerRb = GetComponent<Rigidbody>();
        
        if (playerObjTransform == null)
        {
            Debug.LogError("Player Object Transform not set in Player Energy script!");
        }
    }

    private void Update()
    {
        HeightState();
        
        SpeedState();
        
        CalculateEnergy();
    }
    
    private void HeightState()
    {
        RaycastHit hit;
        
        if (Physics.Raycast(playerObjTransform.position, Vector3.down, out hit, heightRange.y, whatIsGround))
        {
            if (hit.collider != null)
            {
                distanceToGround = hit.distance;
            }
            else
            {
                distanceToGround = -1;
            }
        }
        else
        {
            distanceToGround = -1;
        }
    }
    
    private void SpeedState()
    {
        lastSpeed = currentSpeed;
        
        currentSpeed = playerRb.linearVelocity.magnitude;
        
        if (currentSpeed == 0)
        {
            isGainingSpeed = false;
            
            return;
        }
        
        if (currentSpeed >= lastSpeed)
        {
            isGainingSpeed = true;
        }
        else
        {
            isGainingSpeed = false;
        }
    }
    
    private void CalculateEnergy()
    {
        if (!PotentialGainBlocked)
        {
            //logic for kinetic energy
            if (distanceToGround == -1)
            {
                potentialEnergy = 100;
            }
            else if (distanceToGround >= heightRange.x)
            {
                // getting percentage as place between height range
                potentialEnergy = 100 - (100 - ((distanceToGround - heightRange.x) /
                    (heightRange.y - heightRange.x) * 100));
            }
            else
            {
                potentialEnergy = 0;
            }
            
            PotentialEnergy = potentialEnergy;
        }
        
        if (!KineticGainBlocked)
        {
            //logic for kinetic energy
            if (currentSpeed > kineticSpeedLossThreshold)
            {
                if (isGainingSpeed)
                {
                    kineticEnergy += kineticGainPerSecond * Time.deltaTime;
                }
                else
                {
                    kineticEnergy -= kineticLossAbovePerSecond * Time.deltaTime;
                }
            }
            else if (currentSpeed < kineticSpeedLossThreshold)
            {
                kineticEnergy -= kineticLossBelowPerSecond * Time.deltaTime;
            }
            
            KineticEnergy = kineticEnergy;
        }
    }

    #region Getters & Setters
    public float PotentialEnergy { 
        get => potentialEnergy;
        set
        {
            potentialEnergy = value;
            
            if (potentialEnergy > 100) potentialEnergy = 100;
            
            UIManager.Instance.SetPotentialEnergyFill(potentialEnergy / 100);
        }
    }
    
    public float KineticEnergy { 
        get => kineticEnergy;
        set
        {
            kineticEnergy = value;
            
            if (kineticEnergy > 100) kineticEnergy = 100;
            
            UIManager.Instance.SetKineticEnergyFill(kineticEnergy / 100);
        }
    }
    
    public bool PotentialGainBlocked { get; set; }
    
    public bool KineticGainBlocked { get; set; }

    #endregion
}
