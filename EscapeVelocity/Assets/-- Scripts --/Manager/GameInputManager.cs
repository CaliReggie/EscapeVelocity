using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
            
            return;
        }
    }
    
    private void OnEnable()
    {
        GameStateManager.Instance.OnGameStateChanged += OnStateChange;
    }
    
    private void OnDisable()
    {
        GameStateManager.Instance.OnGameStateChanged -= OnStateChange;
    }
    
    
    private void OnStateChange(EGameState state)
    {
        switch (state)
        {
            case EGameState.MainMenu:
                break;
            case EGameState.Game:
                break;
            case EGameState.Pause:
                break;
            case EGameState.GameOver:
                break;
            default:
                break;
        }
    }
}
