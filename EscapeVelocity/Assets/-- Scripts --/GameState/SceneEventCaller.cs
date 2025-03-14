using System;
using UnityEngine;
using System.Linq;

/// <summary>
/// Can be placed on a GameObject that exists in a scene along with a SceneEventManager to call a SceneEvent.
/// Attach the script, assign the name to the desired SceneEvent in the SceneEventManager,
/// and configure settings for calling the SceneEvent in said SceneEventSO.
/// </summary>
public class SceneEventCaller : MonoBehaviour
{
    [Header("Event Reference")]
    
    [Tooltip("The exact name of the SceneEvent in the SceneEventManager to call.")]
    [SerializeField] private string sceneEventName;
    
    [Header("Event Settings")]

    [Tooltip("Game states in which the event CAN be called.")]
    [SerializeField] private EGameState[] availableStates;

    [Tooltip("Game states in which the event WILL be called no matter the available state settings.")]
    [SerializeField] private EGameState[] callOnStates;
    
    [Space]

    [Tooltip("If true, event will be called on start for GameObjects with SceneEventCaller attached if it can.")]
    [SerializeField] private bool callOnStart;

    [Tooltip("Logic Like Above")]
    [SerializeField] private bool callOnEnable;

    [Tooltip("Logic Like Above")]
    [SerializeField] private bool callOnTriggerEnter;
    
    [Space]
    
    [Header("Debug")]
    
    [Tooltip("During runtime, click this to try calling the SceneEvent.")]
    [SerializeField] private bool callEvent;

    private void Start()
    {
        if (callOnStart)
        {
            CallEvent();
        }
    }
    
    private void OnEnable()
    {
        if (CanCallEvent() && callOnEnable)
        {
            CallEvent();
        }
        
        if (callOnStates.Length > 0)
        {
            GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
    }
    
    private void OnDisable()
    {
        GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnValidate()
    {
        if (CanCallEvent() && callEvent && Application.isPlaying)
        {
            CallEvent();
            
            callEvent = false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (CanCallEvent() && callOnTriggerEnter)
        {
            CallEvent();
        }
    }
    
    private void OnGameStateChanged(EGameState state)
    {
        if (callOnStates.Contains(state))
        {
            CallEvent();
        }
    }
    
    private bool CanCallEvent()
    {
        return availableStates.Length == 0 || availableStates.Contains(GameStateManager.Instance.GameStateSO.GameState);
    }

    // Calls the SceneEventManager to try and find and enact the SceneEvent with the SceneEventName passed
    public void CallEvent()
    {
        if (sceneEventName == "")
        {
            Debug.LogError("SceneEventName is not assigned in the inspector!");
            
            return;
        }
        
        SceneEventManager.Instance.CallEvent(sceneEventName);
    }
}
