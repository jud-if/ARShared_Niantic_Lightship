using UnityEngine;
using TMPro;
using Unity.Netcode;
using System;

public class ClientUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI collectedFiguresText;

    // Bandera para evitar doble suscripción
    private bool isSubscribed = false;

    private void Start()
    {
        // 1. Asegurarse de que el texto esté oculto al inicio.
        // El GameObject ya está desactivado en el inspector, pero esto es una doble seguridad.
        if (collectedFiguresText != null)
        {
            collectedFiguresText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Collected Figures Text UI no asignado en el Inspector.");
            return;
        }

        // 2. Suscribirse al evento de conexión del NetworkManager.
        // Este evento se dispara cuando el cliente local se conecta con éxito al servidor (Host).
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        // 3. Comprobar si el cliente que se conectó es ESTE cliente local.
        if (clientId == NetworkManager.Singleton.LocalClientId && !isSubscribed)
        {
            Debug.Log($"[ClientUIManager] El cliente local {clientId} se ha conectado. Activando UI de conteo.");

            // 4. Activar el objeto de texto para que sea visible.
            collectedFiguresText.gameObject.SetActive(true);

            // Ahora que estamos en el juego, nos suscribimos a los eventos del AllPlayerDataManager.
            if (AllPlayerDataManager.Instance != null)
            {
                AllPlayerDataManager.Instance.OnFigureCountChanged += UpdateCollectedFiguresUI;
                isSubscribed = true; // Marcamos como suscrito

                // Actualizar la UI con el valor inicial al conectarse.
                UpdateCollectedFiguresUI(NetworkManager.Singleton.LocalClientId, AllPlayerDataManager.Instance.GetPlacedFiguresCount(NetworkManager.Singleton.LocalClientId));
            }
            else
            {
                Debug.LogError("[ClientUIManager] AllPlayerDataManager.Instance no está disponible al momento de la conexión.");
            }
        }
    }

    private void UpdateCollectedFiguresUI(ulong clientID, int count)
    {
        if (clientID == NetworkManager.Singleton.LocalClientId)
        {
            if (collectedFiguresText != null)
            {
                collectedFiguresText.text = $"Cubos: {count}/{PlaceFigure.MaxCubesPerUser}";
                Debug.Log($"[ClientUIManager - LOCAL {NetworkManager.Singleton.LocalClientId}] UI actualizada para cliente {clientID}: {count}");
            }
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse de todos los eventos para evitar errores.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }

        if (AllPlayerDataManager.Instance != null && isSubscribed)
        {
            AllPlayerDataManager.Instance.OnFigureCountChanged -= UpdateCollectedFiguresUI;
        }
    }
}