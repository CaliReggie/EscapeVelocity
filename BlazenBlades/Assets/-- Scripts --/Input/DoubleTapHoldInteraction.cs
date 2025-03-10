using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Simple custom interaction for managing grappling hook input
// The swing is just a hold of the same button
// For the grapple, the player, must double tap the button and hold the second tap to fire the grapple
public class DoubleTapHoldInteraction : IInputInteraction
{
    private int tapsRequired = 2;
    
    private float maxTapSpacing = 0.5f;
    
    private float pressPoint = 0.5f;
    
    private int tapCount;
    
    private bool pressed;
    
    private bool wasPressed;
    
    // The interaction needs to be registered with the InputSystem in order to be used.
    // This happens in a static constructor which gets called when the class is loaded.
    static DoubleTapHoldInteraction()
    {
        InputSystem.RegisterInteraction<DoubleTapHoldInteraction>();
    }
    
    public void Process(ref InputInteractionContext context)
    {
        pressed = context.ControlIsActuated(pressPoint);
        
        if (context.timerHasExpired)
        {
            Debug.Log("Timer expired");
            
            context.Canceled();

            Reset();
            
            return;
        }

        switch (context.phase)
        {
            case InputActionPhase.Waiting:
                
                if (!wasPressed && pressed)
                {
                    Debug.Log("Started");
                    
                    context.Started();
                    
                    context.SetTimeout(maxTapSpacing);
                    
                    tapCount = 1;
                }
                
                break;
            
            //once started, we need to recognize a release, and re - tap
            case InputActionPhase.Started:
                
                if (!wasPressed && pressed)
                {
                    tapCount++;
                    
                    if (tapCount == tapsRequired)
                    {
                        Debug.Log("Performed");
                        
                        context.PerformedAndStayPerformed();
                    }
                    else
                    {
                        Debug.Log("Continuing");
                        
                        context.SetTimeout(maxTapSpacing);
                    }
                }
                
                break;
            
            //once performed, we cancel once no longer held
            case InputActionPhase.Performed:
                
                if (!pressed)
                {
                    Debug.Log("Canceled");
                    
                    context.Canceled();
                    
                    Reset();
                }
                
                return;
                
        }
        
        wasPressed = pressed;
    }
    
    public void Reset()
    {
        tapCount = 0;
        pressed = false;
        wasPressed = false;
    }
    
}
