using Unity.Netcode;
using UnityEngine;
using System;

// Definimos los estados posibles del juego
public enum GameState
{
    PreGame,
    Scanning,
    HostPlacing,
    ClientSearching,
    GameEnd
}

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance;

    // Esta variable se sincroniza autom�ticamente entre el servidor y los clientes
    public NetworkVariable<GameState> CurrentGameState = new NetworkVariable<GameState>(GameState.PreGame);

    // Evento para que otros scripts (como el de la UI) reaccionen a los cambios de estado
    public static event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        // Cuando un jugador se une, queremos que el UI reaccione al estado actual.
        // Tambi�n nos suscribimos para reaccionar a futuros cambios.
        CurrentGameState.OnValueChanged += GameStateChanged;
        // Invocamos el evento con el estado actual para la configuraci�n inicial
        OnGameStateChanged?.Invoke(CurrentGameState.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentGameState.OnValueChanged -= GameStateChanged;
    }

    private void GameStateChanged(GameState previousState, GameState newState)
    {
        Debug.Log($"Game state changed from {previousState} to {newState}");
        // Anunciamos el cambio de estado a otros scripts
        OnGameStateChanged?.Invoke(newState);
    }

    // El Host es el �nico que puede solicitar un cambio de estado
    [ServerRpc(RequireOwnership = false)]
    public void RequestStateChangeServerRpc(GameState newState)
    {
        // Aqu� podr�as a�adir l�gica para validar la transici�n de estado si quisieras
        CurrentGameState.Value = newState;
    }
}