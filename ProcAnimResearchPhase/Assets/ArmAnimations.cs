using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ArmAnimations : MonoBehaviour
{
    [Header("Hook References")]
    
    [SerializeField] private Animator leftArmAnimator;
    
    [SerializeField] private Animator rightArmAnimator;
    
    [SerializeField] private Transform leftAnimRigTarget;
    
    [SerializeField] private Transform rightAnimRigTarget;
    
    [Header("Player References")]
    
    [SerializeField] private PlayerInput playerInput;
    
    [SerializeField] private string leftUseArmAction;
    
    [SerializeField] private string rightUseArmAction;
    
    private int _useLeftAnimID = Animator.StringToHash("UsedLeftArm");
    
    private int _useRightAnimID = Animator.StringToHash("UsedRightArm");
    
    private InputAction _leftUseArmActionReference;
    
    private InputAction _rightUseArmActionReference;
    
    private void Awake()
    {
        _leftUseArmActionReference = playerInput.actions[leftUseArmAction];
        
        _rightUseArmActionReference = playerInput.actions[rightUseArmAction];
    }
    
    private void OnEnable()
    {
        _leftUseArmActionReference.Enable();
        
        _rightUseArmActionReference.Enable();
    }
    
    private void OnDisable()
    {
        _leftUseArmActionReference.Disable();
        
        _rightUseArmActionReference.Disable();
    }
    
    private void Update()
    {
        //if action triggered, set trigger
        if (_leftUseArmActionReference.triggered)
        {
            leftArmAnimator.SetTrigger(_useLeftAnimID);
        }
        
        if (_rightUseArmActionReference.triggered)
        {
            rightArmAnimator.SetTrigger(_useRightAnimID);
        }
    }
}
