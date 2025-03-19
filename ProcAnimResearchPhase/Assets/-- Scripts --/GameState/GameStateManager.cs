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
    public event Action<EGameState, EGameState> OnGameStateChanged;
    
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
            
            // Utilize scene load to know when to start state logic
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
    
    //When destroyed (Editor quit, for some reason getting destroyed, etc.), reset the game state SO
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        GameStateSO.SetGameState(EGameState.Reset, this);
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
        if (!ValidInfo(CurrentGameStateSceneInfo)) { return; }
        
        EGameState toState = CurrentGameStateSceneInfo.SceneStartState;
        
        EGameState fromState = GameStateSO.GameState;
        
        EnterGameState(toState, fromState);
    }
    
    // This is the internal reaction to state change. Done right before broadcasting the change to any subscribers
    private void ThisOnGameStateChanged(EGameState toState, EGameState fromState)
    {
        switch (fromState)
        {
            case EGameState.Reset:
                // Do something
                break;
            
            case EGameState.MainMenu:
                // Do something
                break;
            
            case EGameState.Game:
                // Do something
                break;
            
            case EGameState.Pause:
                Time.timeScale = 1;
                break;
            
            case EGameState.GameOver:
                // Do something
                break;
        }
        
        switch (toState)
        {
            case EGameState.Reset:
                // Do something
                break;
            case EGameState.MainMenu:
                // Do something
                break;
            case EGameState.Game:
                // Do something
                break;
            case EGameState.Pause:
                Time.timeScale = 0;
                break;
            case EGameState.GameOver:
                // Do something
                break;
        }
    }
    
    // For implementing logic to prevent and control state changes - Work in progress switching logic,
    // gotta be a better way
    public bool CanEnterGameState(EGameState toState)
    {
        bool canEnter = false;
        
        EGameState fromState = GameStateSO.GameState;
        
        if (fromState == toState) { canEnter = false; }

        switch (fromState)
        {
            case EGameState.Reset:
                
                switch (toState)
                {
                    case EGameState.MainMenu:
                        canEnter = true;
                        break;
                    case EGameState.Game:
                        canEnter = true;
                        break;
                    case EGameState.Pause:
                        canEnter = false;
                        break;
                    case EGameState.GameOver:
                        canEnter = false;
                        break;
                    default:
                        canEnter = false;
                        break;
                }
                
                break;
                
            case EGameState.MainMenu:
                
                switch (toState)
                {
                    case EGameState.Reset:
                        canEnter = true;
                        break;
                    case EGameState.Game:
                        canEnter = true;
                        break;
                    case EGameState.Pause:
                        canEnter = false;
                        break;
                    case EGameState.GameOver:
                        canEnter = false;
                        break;
                    default:
                        canEnter = false;
                        break;
                }
                
                break;
                
            case EGameState.Game:
                
                switch (toState)
                {
                    case EGameState.Reset:
                        canEnter = true;
                        break;
                    case EGameState.MainMenu:
                        canEnter = true;
                        break;
                    case EGameState.Pause:
                        canEnter = true;
                        break;
                    case EGameState.GameOver:
                        canEnter = true;
                        break;
                    default:
                        canEnter = false;
                        break;
                }
                
                break;
                
            case EGameState.Pause:
                
                switch (toState)
                {
                    case EGameState.Reset:
                        canEnter = true;
                        break;
                    case EGameState.MainMenu:
                        canEnter = true;
                        break;
                    case EGameState.Game:
                        canEnter = true;
                        break;
                    case EGameState.GameOver:
                        canEnter = true;
                        break;
                    default:
                        canEnter = false;
                        break;
                }
                
                break;
                
            case EGameState.GameOver:
                
                switch (toState)
                {
                    case EGameState.Reset:
                        canEnter = true;
                        break;
                    case EGameState.MainMenu:
                        canEnter = true;
                        break;
                    case EGameState.Game:
                        canEnter = true;
                        break;
                    case EGameState.Pause:
                        canEnter = false;
                        break;
                    default:
                        canEnter = false;
                        break;
                }
                
                break;
                
            default:
                
                canEnter = false;
                
                break;
        }
        
        if (!canEnter)
        {
            Debug.LogError("Cannot enter " + toState + " from " + fromState);
        }
        
        return canEnter;
    }
    
    // For setting the game , reacting internally, and broadcasting the change
    public void EnterGameState(EGameState toState, EGameState fromState)
    {
        if (!CanEnterGameState(toState)) { return; }
        
        GameStateSO.SetGameState(toState, this);
        
        ThisOnGameStateChanged(toState, fromState);
        
        OnGameStateChanged?.Invoke(toState, fromState);
    }
    
    // For loading a scene from a scene load info SO
    public void LoadSceneWithGameStateSceneInfo(GameStateSceneInfo info)
    {
        if (!ValidInfo(info)) { return; }
        
        if (!CanEnterGameState(EGameState.Reset)) { return; }
        
        SetGameStateSceneInfo(info);
        
        EnterGameState(EGameState.Reset, GameStateSO.GameState);
        
        SceneManager.LoadScene(info.SceneName);
    }
    
    // For reloading with the current scene load info SO
    public void ReloadScene()
    {
        if (!ValidInfo(CurrentGameStateSceneInfo)) { return; }
        
        if (!CanEnterGameState(EGameState.Reset)) { return; }
        
        EnterGameState(EGameState.Reset, GameStateSO.GameState);
        
        SceneManager.LoadScene(CurrentGameStateSceneInfo.SceneName);
    }
    
    public void QUIT_GAME()
    {
        Application.Quit();
    }
    
    public bool Paused
    {
        get
        {
            if (GameStateSO == null)
            {
                Debug.LogError("GameStateSO is null!");
                
                return false;
            }
            
            return GameStateSO.GameState == EGameState.Pause;
        }
    }
}