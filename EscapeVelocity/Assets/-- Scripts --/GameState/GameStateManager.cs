using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game state. On a global scene scope, it utilizes the GameStateSO to both set and get the current game
/// state.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    
    // Event that broadcasts enumerated game state changes to any subscribers
    public event Action<EGameState> OnGameStateChanged;
    
    // The game state SO, set and leave alone in the inspector
    [field: SerializeField] public GameStateSO GameStateSO { get; private set; }
    
    // The current scene load info SO, should be set in the inspector and can be set by other scripts via the public
    // method SetGameStateSceneInfo
    [field: SerializeField] public GameStateSceneInfo CurrentGameStateSceneInfo {get; private set;}
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
            
            //For the first scene load, set from inspector
            if (CurrentGameStateSceneInfo != null) {SetGameStateSceneInfo(CurrentGameStateSceneInfo);}
            
            // Utilize scene load to know when to get start state logic
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            
            return;
        }
        
        if (GameStateSO == null)
        {
            Debug.LogError("GameStateSO is not assigned in the inspector!");
            
            return;
        }
    }

    // This is the internal reaction to state change. Done right before broadcasting the change to any subscribers
    private void ThisOnGameStateChanged(EGameState state)
    {
        switch (state)
        {
            case EGameState.MainMenu:
                // Do something
                break;
            case EGameState.Game:
                // Do something
                break;
            case EGameState.Pause:
                // Do something
                break;
            case EGameState.GameOver:
                // Do something
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
    
    //For implementing logic to prevent and control state changes
    private bool CanEnterGameState(EGameState state)
    {
        return true;
    }

    //For ensuring a scene load info SO is valid before using it
    private bool ValidInfo(GameStateSceneInfo info)
    {
        if (info == null)
        {
            Debug.LogError("Scene info is null. " +
                           "Please assign a GameStateSceneInfo object to the info variable.");
            return false;
        }
        
        if (info.SceneName == "")
        {
            Debug.LogError("Scene name in scene info is empty. " +
                           "Please assign a Scene Name to the GameStateSceneInfo object passed in.");
            return false;
        }
        
        return true;
    }
    
    // For setting the current scene load info SO
    private void SetGameStateSceneInfo(GameStateSceneInfo info)
    {
        if (!ValidInfo(info)) { return; }
        
        CurrentGameStateSceneInfo = info;
    }
    
    // For reacting to scene load to start state logic
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (GameStateSO.GameState)
        {
            case EGameState.MainMenu:
            case EGameState.Game:
                EnterGameState(CurrentGameStateSceneInfo.GameState);
                break;
        } 
    }
    
    // For setting the game , reacting, and broadcasting the change
    public void EnterGameState(EGameState state)
    {
        if (!CanEnterGameState(state)) { return; }
        
        GameStateSO.SetGameState(state, this);
        
        ThisOnGameStateChanged(state);
        
        OnGameStateChanged?.Invoke(state);
    }
    
    // For loading a scene from a scene load info SO
    public void LoadSceneWithGameStateSceneInfo(GameStateSceneInfo info)
    {
        if (!ValidInfo(info)) { return; }
        
        SetGameStateSceneInfo(info);
        
        SceneManager.LoadScene(info.SceneName);
    }
    
    // For reloading with the current scene load info SO
    public void ReloadScene()
    {

        if (CurrentGameStateSceneInfo== null)
        {
            Debug.LogError("CurrentGameStateSceneInfo is null!");
            
            return;
        }
        
        SceneManager.LoadScene(CurrentGameStateSceneInfo.SceneName);
    }
    
    public void QUIT_GAME()
    {
        Application.Quit();
    }
}