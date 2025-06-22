using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Ya no necesitamos 'System' porque eliminamos el evento 'Action'.

public class RestartGame : NetworkBehaviour
{
    [SerializeField] private Button restartButton;

    // Se eliminó el evento 'OnRestartGame' porque no se usaba.

    void Start()
    {
        // El Listener ahora llama directamente al ServerRpc.
        restartButton.onClick.AddListener(() => RequestServerToRestartGameServerRpc());
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestServerToRestartGameServerRpc()
    {
        // CAMBIO PRINCIPAL:
        // En lugar de llamar a un ClientRpc, hacemos lo mismo que el script QuitGame:
        // El servidor carga la escena de limpieza para todos los jugadores.
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("LoadScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    // Se eliminó el método 'RestartGameClientRpc' porque ya no es necesario.
}