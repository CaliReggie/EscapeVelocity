using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerAttacks : MonoBehaviour
{
    [Header("Player References")]
    
    [SerializeField] private Transform playerObjTransform;
    
    [SerializeField] private SwordSpin swordScript;

    [SerializeField] private GameObject potentialEnergyField;
    
    [Header("Player Cam References")]
    
    [SerializeField] private Transform realCamPos;
    
    [Header("Input References")]
    
    [SerializeField] private string potentialEnergyAttackActionName;
    
    [SerializeField] private string kineticEnergyAttackActionName;
    
    [SerializeField] private InputAction potentialEnergyAttackAction;
    
    [SerializeField] private InputAction kineticEnergyAttackAction;
    
    [Header("Detection References")]
    
    [SerializeField] private LayerMask whatIsAttackable;
    
    [SerializeField] private GameObject potentialEnergyAttackPrediction;
    
    [SerializeField] private float potentialPredictionSpherecastRadius = 3f;
    
    [Header("Potential Energy Attack Settings")]
    
    [SerializeField] private float potentialEnergyAttackDistance = 25;
    
    [SerializeField] private float potentialEnergyAttackDuration = 1;

    [SerializeField] private float potentialEnergyAttackCooldown = 5;
    
    [Header("Kinetic Energy Attack Settings")]

    [SerializeField] private float kineticEnergyAttackDuration = 3;
    
    [SerializeField] private float kineticEnergyAttackCooldown = 5;
    
    //Dynamic, Non - Serialized Below
    
    //References
    private PlayerEnergy playerEnergy;

    private PlayerMovement_MLab pm;
    
    //State
    private float timePotentialEnergyAttackReady;
    
    private float timePotentialEnergyAttackDone;
    
    private float timeKineticEnergyAttackReady;

    private float timeKineticEnergyAttackDone;

    private void Start()
    {
        playerEnergy = GetComponent<PlayerEnergy>();
        
        pm = GetComponent<PlayerMovement_MLab>();
        
        PlayerInput playerInput = GetComponent<PlayerInput>();
        
        potentialEnergyAttackAction = playerInput.actions.FindAction(potentialEnergyAttackActionName);
        
        kineticEnergyAttackAction = playerInput.actions.FindAction(kineticEnergyAttackActionName);
    }

    private void OnEnable()
    {
        potentialEnergyAttackAction.Enable();
        kineticEnergyAttackAction.Enable();
    }
    
    private void OnDisable()
    {
        potentialEnergyAttackAction.Disable();
        kineticEnergyAttackAction.Disable();
    }

    private void Update()
    {
        ManageCooldowns();
        
        DetectionPrediction();
        
        GetInput();
    }
    
    private void ManageCooldowns()
    {
        if (playerEnergy.PotentialGainBlocked && Time.time >= timePotentialEnergyAttackReady)
        {
            playerEnergy.PotentialGainBlocked = false;
        }
        
        if (playerEnergy.KineticGainBlocked && Time.time >= timeKineticEnergyAttackReady)
        {
            playerEnergy.KineticGainBlocked = false;
        }
        
        if (Time.time >= timePotentialEnergyAttackDone)
        {
            potentialEnergyField.SetActive(false);
        }
        
        if (Time.time >= timeKineticEnergyAttackDone)
        {
            swordScript.gameObject.SetActive(false);
        }
    }
    
    
    private void DetectionPrediction()
    {
        if (playerEnergy.PotentialEnergy > 0)
        {
            RaycastHit hit;
        
            if (Physics.SphereCast(playerObjTransform.position, potentialPredictionSpherecastRadius,
                    realCamPos.forward, out hit, potentialEnergyAttackDistance, whatIsAttackable))
            {
                if (hit.collider != null)
                {
                    potentialEnergyAttackPrediction.transform.position = hit.point;
                    
                    potentialEnergyAttackPrediction.SetActive(true);
                }
                else
                {
                    potentialEnergyAttackPrediction.SetActive(false);
                }
            }
            else
            {
                potentialEnergyAttackPrediction.SetActive(false);
            }
        }
    }
    
    private void GetInput()
    {
        if (potentialEnergyAttackAction.triggered && CanPotentialEnergyAttack)
        {
            PotentialEnergyAttack();
        }
        
        if (kineticEnergyAttackAction.triggered && CanKineticEnergyAttack)
        {
            KineticEnergyAttack();
        }
    }
    
    private void PotentialEnergyAttack()
    {
        timePotentialEnergyAttackReady = Time.time + potentialEnergyAttackCooldown;
        
        timePotentialEnergyAttackDone = Time.time + potentialEnergyAttackDuration;
        
        pm.JumpToPositionInTime(potentialEnergyAttackPrediction.transform.position, potentialEnergyAttackDuration);
        
        playerEnergy.PotentialGainBlocked = true;
        playerEnergy.PotentialEnergy = 0;
        
        potentialEnergyAttackPrediction.SetActive(false);
        
        potentialEnergyField.SetActive(true);
    }
    
    private void KineticEnergyAttack()
    {
        timeKineticEnergyAttackReady = Time.time + kineticEnergyAttackCooldown;
        
        timeKineticEnergyAttackDone = Time.time + kineticEnergyAttackDuration;
        
        playerEnergy.KineticGainBlocked = true;
        playerEnergy.KineticEnergy      = 0;
        
        swordScript.gameObject.SetActive(true);
    }
    
    private bool CanPotentialEnergyAttack => 
        (Time.time >= timePotentialEnergyAttackReady) && potentialEnergyAttackPrediction.activeSelf && playerEnergy
            .PotentialEnergy > 0;
    
    private bool CanKineticEnergyAttack => Time.time >= timeKineticEnergyAttackReady && playerEnergy.KineticEnergy > 0;
}
