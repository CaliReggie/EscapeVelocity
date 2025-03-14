using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MomentumExtension : MonoBehaviour
{
    [Header("Momentum General Settings")]
    
    [Range(0f,10f)] [SerializeField] private float momentumIncreaseFactor = 1;
    [Range(0f, 10f)] [SerializeField] private float momentumDecreaseFactorOnGround = 2;
    [Range(0f, 10f)] [SerializeField] private float momentumDecreaseFactorInAir = 0.5f;
    
    [Range(0f, 10f)] [field: SerializeField] public float MinimalMomentum { get; private set; } = 3f;

    [Header("State Settings")]
    
    [SerializeField] private List<MovementState> movementStates = new List<MovementState>()
    {
        new MovementState("Walking", PlayerMovement.MovementMode.walking, 1, 1),
        new MovementState("Sprinting",PlayerMovement.MovementMode.sprinting, 1, 1),
        new MovementState("Crouching", PlayerMovement.MovementMode.crouching, 1, 1),
        new MovementState("Swinging", PlayerMovement.MovementMode.swinging, 1, 1),
        new MovementState("Sliding", PlayerMovement.MovementMode.sliding, 2, 1),
        new MovementState("Wallrunning", PlayerMovement.MovementMode.wallrunning, 1, 1),
        new MovementState("Walljumping", PlayerMovement.MovementMode.walljumping, 1, 1),
        new MovementState("Climbing", PlayerMovement.MovementMode.climbing, 1, 1),
        new MovementState("Dashing", PlayerMovement.MovementMode.dashing, -1, 10),
    };

    [SerializeField] private List<MovementState> hardcodedMovementStates = new List<MovementState>()
    {
        new MovementState("Unlimited", PlayerMovement.MovementMode.unlimited, -1, -1),
        new MovementState("Limited", PlayerMovement.MovementMode.limited, -1, 1),
        new MovementState("Freeze", PlayerMovement.MovementMode.freeze, -1, -1),
    };

    private MovementState GetMovementState(PlayerMovement.MovementMode movementMode)
    {
        foreach (MovementState state in movementStates)
        {
            if (state.MovementMode == movementMode)
            {
                return state;
            }
        }
        
        foreach (MovementState state in hardcodedMovementStates)
        {
            if (state.MovementMode == movementMode)
            {
                return state;
            }
        }

        return null;
    }

    public float GetIncreaseSpeedChangeFactor(PlayerMovement.MovementMode movementMode)
    {
        float speedChangeFactor = 0f;
        
        MovementState movementState = GetMovementState(movementMode);
        
        if (movementState != null)
        {
            if (movementState.SpeedBuildupFactor == -1)
            {
                speedChangeFactor = -1;
            }
                
            else
            {
                speedChangeFactor = momentumIncreaseFactor * movementState.SpeedBuildupFactor;
            }
        }
        else
        {
            Debug.LogError("MovementState not found");
            
            speedChangeFactor = -1;
        }

        return speedChangeFactor;
    }

    public float GetDecreaseSpeedChangeFactor(PlayerMovement.MovementMode movementMode)
    {
        float speedChangeFactor = 0f;

        MovementState movementState = GetMovementState(movementMode);
        
        if (movementState != null)
        {
            if (movementState.SpeedBuilddownFactor == -1)
            {
                speedChangeFactor = -1;
            }
                
            else
            {
                speedChangeFactor = momentumIncreaseFactor * movementState.SpeedBuilddownFactor;
            }
        }
        else
        {
            speedChangeFactor = -1;
        }

        return speedChangeFactor;
    }

    public float GetSurfaceSpeedDecreaseFactor(bool grounded)
    {
        return grounded ? momentumDecreaseFactorOnGround : momentumDecreaseFactorInAir;
    }

    public bool IsStateAllowed(PlayerMovement.MovementMode movementMode, float currMomentum)
    {
        MovementState movementState = GetMovementState(movementMode);
        bool stateAllowed = true;

        if(currMomentum < movementState.MinNeededMomentum)
            stateAllowed = false;
        else if (currMomentum > movementState.MaxAllowedMomentum)
            stateAllowed = false;

        // Debug.Log($"CurrMomentum {currMomentum}, MinMomentum {movementState.MinNeededMomentum},
        // MaxMomentum {movementState.MaxAllowedMomentum} -> State {movementState.StateName} allowed -> {stateAllowed}");

        return stateAllowed;
    }
}

[Serializable]
public class MovementState
{
    [field: SerializeField] public string StateName { get; private set; }

    [field: SerializeField] public PlayerMovement.MovementMode MovementMode { get; private set; }

    [Tooltip("-1 means instant speed change")]
    [field: SerializeField] public float SpeedBuildupFactor { get; private set; } = -1;
    
    [Tooltip("-1 means instant speed change")]
    [field: SerializeField] public float SpeedBuilddownFactor { get; private set; } = -1;

    [field: SerializeField] [Range(0f, 100f)] public float MinNeededMomentum { get; private set; } = 0f;
    
    [field: SerializeField] [Range(0f, 100f)] public float MaxAllowedMomentum { get; private set; } = 100f;

    public MovementState(string stateName, PlayerMovement.MovementMode movementMode, float speedBuildupFactor, float speedBuilddownFactor)
    {
        this.StateName = stateName;
        this.MovementMode = movementMode;
        this.SpeedBuildupFactor = speedBuildupFactor;
        this.SpeedBuilddownFactor = speedBuilddownFactor;
    }

    public MovementState(PlayerMovement.MovementMode movementMode)
    {
        this.MovementMode = movementMode;
    }
}