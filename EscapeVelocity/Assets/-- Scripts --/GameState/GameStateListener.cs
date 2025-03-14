using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Can be added to a GameObject to listen for changes in the game state and respond to them.
/// </summary>
public class GameStateListener : MonoBehaviour
{
    [SerializeField] private StateResponse[] stateResponses;
    
    private void OnEnable()
    {
        GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }
    
    private void OnDisable()
    {
        GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }
    
    // Responds to state changes and attempts to invoke the UnityEvent associated with the state
    private void OnGameStateChanged(EGameState state)
    {
        foreach (var stateResponse in stateResponses)
        {
            if (stateResponse.State == state)
            {
                stateResponse.Response.Invoke();
                
                return;
            }
        }
    }
}

/// <summary>
/// Manages customizing responses by setting functions to be called by the UnityEvent when the game state changes.
/// </summary>
[Serializable]
public class StateResponse
{ 
    [field: SerializeField] public EGameState State {get; private set;}
    
    [field: SerializeField] public UnityEvent Response {get; private set;}
}
