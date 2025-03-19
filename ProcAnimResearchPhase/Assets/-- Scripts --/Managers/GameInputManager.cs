using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(PlayerInputManager))]
public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }
    
    [Header("Player Spawning")]
    
    [SerializeField] private GameObject playableScenePlayerPrefab;

    [Header("Dynamic")]

    [Tooltip("Here so unity serializes the header above. Don't press the red button!")]
    [SerializeField] private bool placeholder;
    
    [field: SerializeField] public PlayerInput PlayerInput { get; private set; }
    
    //Private, or Non-Serialized Below
    
    // References
    
    private PlayerInputManager _playerInputManager;
    
    private Vector3 _currentSpawnPos, _currentSpawnRot;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        // Manually Configuring Attached PlayerInputManager
        
        if (_playerInputManager == null)
        {
            _playerInputManager = GetComponent<PlayerInputManager>();
            
            if (_playerInputManager == null)
            {
                Debug.LogError("No PlayerInputManager found on GameInputManager, disabling script.");
                
                enabled = false;
                
                return;
            }
        }

        JoiningEnabled = false;
        
        _playerInputManager.playerPrefab = null;
    }
    
    private void OnEnable()
    {
        GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        
        _playerInputManager.onPlayerJoined += OnPlayerJoined;
        
        _playerInputManager.onPlayerLeft += OnPlayerLeft;
    }
    
    private void OnDisable()
    {
        GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        
        // _playerInputManager.onPlayerJoined -= OnPlayerJoined;
        //
        // _playerInputManager.onPlayerLeft -= OnPlayerLeft;
    }
    
    private void EnablePlayableSpawn()
    {
        if (playableScenePlayerPrefab == null)
        {
            Debug.LogError("No Playable Scene Prefab assigned in the inspector!");
            
            return;
        }
        
        //setting prefab to spawn
        _playerInputManager.playerPrefab = playableScenePlayerPrefab;
        
        //setting spawn pos and rot from current scene info
        _currentSpawnPos = GameStateManager.Instance.CurrentGameStateSceneInfo.SceneSpawnWorldPos;
        
        _currentSpawnRot = GameStateManager.Instance.CurrentGameStateSceneInfo.SceneSpawnRot;
        
        //enabling joining
        JoiningEnabled = true;
    }
    
    private void OnPlayerJoined(PlayerInput playerInput)
    {
        PlayerInput = playerInput;
        
        //Setting player parent (player input) world spawn loc and rot
        playerInput.transform.position = _currentSpawnPos;
        playerInput.transform.rotation = Quaternion.Euler(_currentSpawnRot);
    }
    
    private void OnPlayerLeft(PlayerInput playerInput)
    {
        PlayerInput = null;
    }
    
    private void OnGameStateChanged(EGameState toState, EGameState fromState)
    {
        switch (fromState)
        {
            case EGameState.Reset:
                break;
            
            case EGameState.MainMenu:
                break;
            
            case EGameState.Game:
                break;
            
            case EGameState.Pause:
                break;
            
            case EGameState.GameOver:
                break;
        }
        
        switch (toState)
        {
            case EGameState.Reset:
                
                JoiningEnabled = false; // Disable joining
                
                // Reset spawn pos and rot
                _currentSpawnPos = Vector3.zero;
                
                _currentSpawnRot = Vector3.zero;
                
                break;
            
            case EGameState.MainMenu:
                //Do something
                break;
            
            case EGameState.Game:
                
                EnablePlayableSpawn();
                
                break;
            
            case EGameState.Pause:
                // Do something
                break;
            
            case EGameState.GameOver:
                // Do something
                break;
        }
    }
    
    public bool JoiningEnabled 
    {
        get => _playerInputManager.joiningEnabled;
        
        private set
        {
            if (_playerInputManager == null ) return;
            
            if (value == true)
            {
                
                _playerInputManager.EnableJoining();
            }
            else
            {
                _playerInputManager.DisableJoining();
            }
        }
    }
}
