using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Used in a single scene, non-persistent scope to arrange for a wide variety of events to be called in response to
/// SceneEventCallers in scene.
/// </summary>
public class SceneEventManager : MonoBehaviour
{
    public static SceneEventManager Instance { get; private set; }
    
    [SerializeField] private SceneEvent[] sceneEvents;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            
            return;
        }
    }
    
    public void CallEvent(String otherSceneEventName)
    {
        if (string.IsNullOrEmpty(otherSceneEventName))
        {
            Debug.LogError("SceneEventName passed is null or empty!");
            
            return;
        }

        //checking our SceneEvents to see if it has the SceneEvent with corresponding SceneEventName
        foreach (var sceneEvent in sceneEvents)
        {
            //if our scene exists and is the same as the SceneEventName we are looking for
            if (sceneEvent.SceneEventName != null && sceneEvent.SceneEventName == otherSceneEventName)
            {
                
                //invoke the UnityEvent
                sceneEvent.OnCalled.Invoke();
                
                return;
            }
        }
    }
}

/// <summary>
/// A scene event houses a SceneEvent Name and a UnityEvent to be called when a SceneEventManager is called.
/// </summary>
[Serializable]
public class SceneEvent
{
    // The name of the SceneEvent for cross-reference between SceneEventCallers
    [field: SerializeField] public string SceneEventName {get; private set;}
    
    // Used to assign a variety of functions to be called through reference in the Inspector
    [field: SerializeField] public UnityEvent OnCalled {get; private set;}
}
