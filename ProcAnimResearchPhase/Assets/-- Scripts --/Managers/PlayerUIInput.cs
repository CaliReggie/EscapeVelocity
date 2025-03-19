using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInput))]
public class PlayerUIInput : MonoBehaviour
{
    [SerializeField] private string pauseAction = "Cancel";
    
    //Private, or Non-Serialized Below
    
    private InputAction _pauseAction;

    private void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        
        _pauseAction = playerInput.actions.FindAction(pauseAction);
    }
    
    private void OnEnable()
    {
        _pauseAction.Enable();
    }
    
    private void OnDisable()
    {
        _pauseAction.Disable();
    }
    
    private void Update()
    {
        if (_pauseAction.triggered)
        {
            if (GameStateManager.Instance.Paused)
            {
                GameStateManager.Instance.EnterGameState(EGameState.Game, GameStateManager.Instance.GameStateSO.GameState);
            }
            else
            {
                GameStateManager.Instance.EnterGameState(EGameState.Pause, GameStateManager.Instance.GameStateSO.GameState);
            }
        }
    }
}
