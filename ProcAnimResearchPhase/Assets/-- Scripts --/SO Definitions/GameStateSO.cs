using System;
using UnityEngine;

public enum EGameState
{
    Reset,
    MainMenu,
    Game,
    Pause,
    GameOver
}

/// <summary>
/// The container for the current game state. Managed by GameStateManager. Don't set elsewhere in code.
/// </summary>
[CreateAssetMenu(fileName = "GameStateSO", menuName = "ScriptableObjects/GameStateSO" , order = 1)]
public class GameStateSO : ScriptableObject
{
    /// <summary>
    /// The current game state. Do not set this directly; use GameStateManager to set the game state.
    /// </summary>
    [field: SerializeField] public EGameState GameState { get; private set; } = EGameState.Reset;
    
    public void SetGameState(EGameState state, GameStateManager authorizer)
    {
        if (authorizer == GameStateManager.Instance) { GameState = state; }
    }
}
