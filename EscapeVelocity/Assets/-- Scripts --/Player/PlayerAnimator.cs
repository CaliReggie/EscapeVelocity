using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum EPlayerAnimationState
{
    Idle,
    Walk,
    Run,
    Jump,
    Fall,
    Crouch,
    Slide,
    Dash,
    Grapple,
    Swing
}

[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimator : MonoBehaviour
{
    private enum EMovementStyle
    {
        Strafe,
        Forward
    }
    
    [Header("Animator References")]
    
    [SerializeField] private Animator animator;
    
    [Header("Animation Behaviours")]
    
    [SerializeField] private EMovementStyle movementStyle;
    
    //Dynamic, Non-Serialized Below
    
    //Player References
    private Rigidbody _rb;
    
    private PlayerCam _playerCamScript;
    
    private PlayerMovement _pm;
    
    private EPlayerAnimationState _currentState;

    private void Awake()
    {
        //get references
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
        _playerCamScript = GetComponent<PlayerCam>();
    }


    #region Animation Variable Hashes

    private readonly int _something = Animator.StringToHash("Something");

    #endregion

    #region Getters and Setters

    //Something

    #endregion

    #region Properties

    public bool HasAnimator => animator != null;

    #endregion
}