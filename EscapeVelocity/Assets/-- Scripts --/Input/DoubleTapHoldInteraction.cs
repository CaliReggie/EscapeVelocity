using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Simple custom interaction for managing grappling hook input
// The swing is just a hold of the same button
// For the grapple, the player, must double tap the button and hold the second tap to fire the grapple
public class DoubleTapHoldInteraction : IInputInteraction
{
    private float maxTapSpacing = 0.5f;
    
    private float pressPoint = 0.5f;
    
    private float currentInput;
    
    private float previousInput;
    
    // The interaction needs to be registered with the InputSystem in order to be used.
    // This happens in a static constructor which gets called when the class is loaded.
    static DoubleTapHoldInteraction()
    {
        InputSystem.RegisterInteraction<DoubleTapHoldInteraction>();
    }
    
    public void Process(ref InputInteractionContext context)
    {
        currentInput = context.ReadValue<float>();
        
        bool tapped = currentInput > pressPoint && previousInput < pressPoint;
        
        if (context.timerHasExpired)
        {
            context.Canceled();
            
            return;
        }

        switch (context.phase)
        {
            case InputActionPhase.Waiting:
                
                if (tapped)
                {
                    context.Started();
                    
                    context.SetTimeout(maxTapSpacing);
                }
                
                break;
            
            //once started, we need to recognize a release, and re - tap
            case InputActionPhase.Started:
                
                if (tapped)
                {
                    context.PerformedAndStayPerformed();
                }
                
                break;
            
            //once performed, we cancel once no longer held
            case InputActionPhase.Performed:
                
                if (currentInput < pressPoint)
                {
                    context.Canceled();
                }
                
                break;
                
        }
        
        previousInput = currentInput;
    }
    
    //required for class to implement IInputInteraction
    public void Reset()
    {
    }
    
}
