using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class AllPlayerDataManager : NetworkBehaviour
{
    public static AllPlayerDataManager Instance;

    private NetworkList<FigureData> allPlayerData;

    public event Action<ulong, int> OnFigureCountChanged;
    // Este es el evento que usaremos para la victoria
    public event Action<ulong, int> OnCollectedFiguresCountChanged;

    private void Awake()
    {
        allPlayerData = new NetworkList<FigureData>();
        allPlayerData.OnListChanged += HandleAllPlayerDataListChanged;
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (allPlayerData != null) allPlayerData.OnListChanged -= HandleAllPlayerDataListChanged;
    }

    private void HandleAllPlayerDataListChanged(NetworkListEvent<FigureData> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<FigureData>.EventType.Value)
        {
            // Este evento gen�rico sigue siendo �til para el contador de piezas del Host
            OnFigureCountChanged?.Invoke(changeEvent.Value.clientID, changeEvent.Value.figureCount);
        }
    }

    // --- L�GICA DE RECOLECCI�N MODIFICADA ---
    [ServerRpc(RequireOwnership = false)]
    public void CollectFigureServerRpc(ulong collectingClientId)
    {
        for (int i = 0; i < allPlayerData.Count; i++)
        {
            if (allPlayerData[i].clientID == collectingClientId)
            {
                FigureData data = allPlayerData[i];
                data.figureCount++;
                allPlayerData[i] = data; // Actualiza la NetworkList

                Debug.Log($"[AllPlayerDataManager] Servidor: Cliente {collectingClientId} recolect� una figura. Total: {data.figureCount}");

                // Preparamos el env�o para un cliente espec�fico
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { collectingClientId } }
                };

                // El servidor le notifica SOLO al cliente que recolect�, pas�ndole su nuevo contador
                NotifyClientOfCollectionClientRpc(collectingClientId, data.figureCount, clientRpcParams);
                return;
            }
        }
    }

    // --- NUEVO CLIENTRPC ---
    [ClientRpc]
    private void NotifyClientOfCollectionClientRpc(ulong collectingClientId, int newCount, ClientRpcParams clientRpcParams = default)
    {
        // Este c�digo se ejecuta SOLO en el dispositivo del cliente que recolect� la pieza.
        // Aqu� disparamos el evento espec�fico para la recolecci�n.
        Debug.Log($"[AllPlayerDataManager] Cliente {collectingClientId} notificado. Recolectadas: {newCount}");
        OnCollectedFiguresCountChanged?.Invoke(collectingClientId, newCount);
    }

    // El resto de los m�todos se quedan igual
    // ...
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            AddNewClientToList(NetworkManager.LocalClientId);
            NetworkManager.Singleton.OnClientConnectedCallback += AddNewClientToList;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AddNewClientToList;
        }
    }

    void AddNewClientToList(ulong clientID)
    {
        if (!IsServer) return;
        for (int i = 0; i < allPlayerData.Count; i++)
        {
            if (allPlayerData[i].clientID == clientID) return;
        }
        allPlayerData.Add(new FigureData(clientID, 0));
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncrementPlacedFigureCountServerRpc(ulong clientID)
    {
        for (int i = 0; i < allPlayerData.Count; i++)
        {
            if (allPlayerData[i].clientID == clientID)
            {
                FigureData data = allPlayerData[i];
                data.figureCount++;
                allPlayerData[i] = data;
                return;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DecrementPlacedFigureCountServerRpc(ulong clientID)
    {
        for (int i = 0; i < allPlayerData.Count; i++)
        {
            if (allPlayerData[i].clientID == clientID)
            {
                FigureData data = allPlayerData[i];
                if (data.figureCount > 0)
                {
                    data.figureCount--;
                    allPlayerData[i] = data;
                }
                return;
            }
        }
    }

    public int GetPlacedFiguresCount(ulong clientID)
    {
        for (int i = 0; i < allPlayerData.Count; i++)
        {
            if (allPlayerData[i].clientID == clientID) return allPlayerData[i].figureCount;
        }
        return 0;
    }

    [ClientRpc]
    public void NotifyClientOfCollectedPieceClientRpc(int pieceId, ClientRpcParams clientRpcParams = default)
    {
        if (PieceDisplayManager.Instance != null)
        {
            PieceDisplayManager.Instance.ShowFoundPiece(pieceId);
        }
    }
}