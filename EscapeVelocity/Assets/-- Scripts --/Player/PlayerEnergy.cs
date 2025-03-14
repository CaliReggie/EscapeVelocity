using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerEnergy : MonoBehaviour
{
    [Header("Potential Energy")]
    
    [Tooltip("Below the min, PE can not be gained. Above, no extra gain or effect, considered at max.")]
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
    
    private Transform _playerObjTrans;
    
    private LayerMask _whatIsGround;

    private PlayerMovement _pm;
    
    private Rigidbody _playerRb;
    
    //Energy
    
    private float _potentialEnergy = 0;
    
    private float _kineticEnergy = 0;
    
    //State
    
    private float _distanceToGround;
    
    private float _lastSpeed;
    
    private float _currentSpeed;
    
    private bool _isGainingSpeed;

    private void Awake()
    {
        //get references
        _pm = GetComponent<PlayerMovement>();
        
        _playerObjTrans = _pm.PlayerObj;
        
        _playerRb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        //waited for _pm to initialize
        _whatIsGround = _pm.WhatIsGround;
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
        
        if (Physics.Raycast(_playerObjTrans.position, Vector3.down, out hit, heightRange.y, _whatIsGround))
        {
            if (hit.collider != null)
            {
                _distanceToGround = hit.distance;
            }
            else
            {
                _distanceToGround = -1;
            }
        }
        else
        {
            _distanceToGround = -1;
        }
    }
    
    private void SpeedState()
    {
        _lastSpeed = _currentSpeed;
        
        _currentSpeed = _playerRb.linearVelocity.magnitude;
        
        if (_currentSpeed == 0)
        {
            _isGainingSpeed = false;
            
            return;
        }
        
        if (_currentSpeed >= _lastSpeed)
        {
            _isGainingSpeed = true;
        }
        else
        {
            _isGainingSpeed = false;
        }
    }
    
    private void CalculateEnergy()
    {
        if (!PotentialGainBlocked)
        {
            //logic for kinetic energy
            if (_distanceToGround == -1)
            {
                _potentialEnergy = 100;
            }
            else if (_distanceToGround >= heightRange.x)
            {
                // getting percentage as place between height range
                _potentialEnergy = 100 - (100 - ((_distanceToGround - heightRange.x) /
                    (heightRange.y - heightRange.x) * 100));
            }
            else
            {
                _potentialEnergy = 0;
            }
            
            PotentialEnergy = _potentialEnergy;
        }
        
        if (!KineticGainBlocked)
        {
            //logic for kinetic energy
            if (_currentSpeed >= kineticSpeedLossThreshold)
            {
                if (_isGainingSpeed)
                {
                    _kineticEnergy += kineticGainPerSecond * Time.deltaTime;
                }
                else
                {
                    _kineticEnergy -= kineticLossAbovePerSecond * Time.deltaTime;
                }
            }
            else if (_currentSpeed < kineticSpeedLossThreshold)
            {
                _kineticEnergy -= kineticLossBelowPerSecond * Time.deltaTime;
            }
            
            KineticEnergy = _kineticEnergy;
        }
    }

    #region Getters & Setters
    public float PotentialEnergy { 
        get => _potentialEnergy;
        set
        {
            _potentialEnergy = value;
            
            if (_potentialEnergy > 100) _potentialEnergy = 100;
            
            UIManager.Instance.SetPotentialEnergyFill(_potentialEnergy / 100);
        }
    }
    
    public float KineticEnergy { 
        get => _kineticEnergy;
        set
        {
            _kineticEnergy = value;
            
            if (_kineticEnergy > 100) _kineticEnergy = 100;
            
            UIManager.Instance.SetKineticEnergyFill(_kineticEnergy / 100);
        }
    }
    
    public bool PotentialGainBlocked { get; set; }
    
    public bool KineticGainBlocked { get; set; }

    #endregion
}
