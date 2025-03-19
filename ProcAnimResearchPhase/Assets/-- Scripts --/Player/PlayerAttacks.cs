using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerAttacks : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private SwordSpin swordScript;

    [SerializeField] private GameObject potentialEnergyField;
    
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
    
    //Player References
    
    private PlayerEnergy _playerEnergy;

    private PlayerMovement pm;
    
    private Transform _playerObjTrans;
    
    //Camera References
    private Transform _realCamPos;
    
    //State
    private float _timePotentialEnergyAttackReady;
    
    private float _timePotentialEnergyAttackDone;
    
    private float _timeKineticEnergyAttackReady;

    private float _timeKineticEnergyAttackDone;

    private void Awake()
    {
        //get references
        pm = GetComponent<PlayerMovement>();

        _playerObjTrans = pm.PlayerObj;
        
        PlayerCam playerCamScript = GetComponent<PlayerCam>();
        _realCamPos = playerCamScript.RealCam.gameObject.transform;
        
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        
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
        if (_playerEnergy.PotentialGainBlocked && Time.time >= _timePotentialEnergyAttackReady)
        {
            _playerEnergy.PotentialGainBlocked = false;
        }
        
        if (_playerEnergy.KineticGainBlocked && Time.time >= _timeKineticEnergyAttackReady)
        {
            _playerEnergy.KineticGainBlocked = false;
        }
        
        if (Time.time >= _timePotentialEnergyAttackDone)
        {
            potentialEnergyField.SetActive(false);
        }
        
        if (Time.time >= _timeKineticEnergyAttackDone)
        {
            swordScript.gameObject.SetActive(false);
        }
    }
    
    
    private void DetectionPrediction()
    {
        if (_playerEnergy.PotentialEnergy > 0)
        {
            RaycastHit hit;
        
            if (Physics.SphereCast(_playerObjTrans.position, potentialPredictionSpherecastRadius,
                    _realCamPos.forward, out hit, potentialEnergyAttackDistance, whatIsAttackable))
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
        _timePotentialEnergyAttackReady = Time.time + potentialEnergyAttackCooldown;
        
        _timePotentialEnergyAttackDone = Time.time + potentialEnergyAttackDuration;
        
        pm.JumpToPositionInTime(potentialEnergyAttackPrediction.transform.position, potentialEnergyAttackDuration);
        
        _playerEnergy.PotentialGainBlocked = true;
        _playerEnergy.PotentialEnergy = 0;
        
        potentialEnergyAttackPrediction.SetActive(false);
        
        potentialEnergyField.SetActive(true);
    }
    
    private void KineticEnergyAttack()
    {
        _timeKineticEnergyAttackReady = Time.time + kineticEnergyAttackCooldown;
        
        _timeKineticEnergyAttackDone = Time.time + kineticEnergyAttackDuration;
        
        _playerEnergy.KineticGainBlocked = true;
        _playerEnergy.KineticEnergy      = 0;
        
        swordScript.gameObject.SetActive(true);
    }
    
    private bool CanPotentialEnergyAttack => 
        (Time.time >= _timePotentialEnergyAttackReady) && potentialEnergyAttackPrediction.activeSelf && _playerEnergy
            .PotentialEnergy > 0;
    
    private bool CanKineticEnergyAttack => Time.time >= _timeKineticEnergyAttackReady && _playerEnergy.KineticEnergy > 0;
}
