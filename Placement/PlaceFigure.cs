using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlaceFigure : NetworkBehaviour
{
    [SerializeField] private GameObject[] placementPrefabs;
    [SerializeField] private LayerMask figuresLayer;

    private Camera mainCam;
    public const int MaxCubesPerUser = 4;

    private void Start()
    {
        mainCam = FindFirstObjectByType<Camera>();
        if (mainCam == null)
        {
            Debug.LogError("No se encontró una cámara principal.");
        }
        if (figuresLayer.value == 0)
        {
            figuresLayer = LayerMask.GetMask("FiguresLayer");
        }
    }

    void Update()
    {
        // --- LÓGICA CORREGIDA: Bloqueo por estado de juego y rol ---
        if (GameFlowManager.Instance == null) return; // Chequeo de seguridad

        GameState currentState = GameFlowManager.Instance.CurrentGameState.Value;

        if (IsHost)
        {
            // El Host solo puede interactuar (colocar/borrar) durante su fase.
            if (currentState != GameState.HostPlacing)
            {
                return; // Si no es su turno, no hace nada.
            }
        }
        else // Si es un Cliente
        {
            // El Cliente solo puede interactuar (recolectar) durante su fase.
            if (currentState != GameState.ClientSearching)
            {
                return; // Si no es su turno, no hace nada.
            }
        }
        // --- FIN DE LA LÓGICA CORREGIDA ---

        // Si el código llega hasta aquí, significa que el jugador actual SÍ tiene permiso para interactuar.

        if (AllPlayerDataManager.Instance == null) return;

        // El resto del método Update se mantiene igual que antes
        if (IsHost && AllPlayerDataManager.Instance.GetPlacedFiguresCount(NetworkManager.Singleton.LocalClientId) >= MaxCubesPerUser)
        {
            HandleInputForDeletion();
            return;
        }

        HandleInputForPlacementAndDeletion();
    }

    // El resto de los métodos (HandleInputForPlacementAndDeletion, ProcessTouchOrClick, etc.)
    // se quedan exactamente igual que en tu versión, ya que la lógica de Raycast y los RPCs
    // es correcta.
    // ... (Copia y pega el resto de tus métodos de PlaceFigure.cs aquí) ...
    void HandleInputForPlacementAndDeletion()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            ProcessTouchOrClick(Input.mousePosition);
        }
#endif
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            ProcessTouchOrClick(touch.position);
        }
#endif
    }

    void HandleInputForDeletion()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            ProcessTouchOrClickForDeletion(Input.mousePosition);
        }
#endif
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            ProcessTouchOrClickForDeletion(touch.position);
        }
#endif
    }

    void ProcessTouchOrClick(Vector3 screenPoint)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, figuresLayer))
        {
            if (hit.collider.CompareTag("PlayerFigure"))
            {
                NetworkObject hitNetworkObject = hit.collider.GetComponentInParent<NetworkObject>();
                if (hitNetworkObject != null)
                {
                    HandleFigureDeletion(hitNetworkObject, NetworkManager.Singleton.LocalClientId);
                    return;
                }
            }
        }
        else
        {
            HandlePlacementRaycast(screenPoint);
        }
    }

    void ProcessTouchOrClickForDeletion(Vector3 screenPoint)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, figuresLayer))
        {
            if (hit.collider.CompareTag("PlayerFigure"))
            {
                NetworkObject hitNetworkObject = hit.collider.GetComponentInParent<NetworkObject>();
                if (hitNetworkObject != null)
                {
                    HandleFigureDeletion(hitNetworkObject, NetworkManager.Singleton.LocalClientId);
                }
            }
        }
    }

    void HandlePlacementRaycast(Vector3 screenPoint)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (IsHost && AllPlayerDataManager.Instance.GetPlacedFiguresCount(NetworkManager.Singleton.LocalClientId) < MaxCubesPerUser)
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                SpawnFigureServerRpc(hit.point, rotation, NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    void HandleFigureDeletion(NetworkObject figureNetworkObject, ulong callerID)
    {
        if (IsHost)
        {
            if (figureNetworkObject.OwnerClientId == callerID)
            {
                AllPlayerDataManager.Instance.DecrementPlacedFigureCountServerRpc(callerID);
                figureNetworkObject.Despawn(true);
            }
        }
        else
        {
            RequestFigureCollectionServerRpc(figureNetworkObject.NetworkObjectId, callerID);
            Debug.Log($"Cliente {callerID} ha solicitado recolectar un cubo.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnFigureServerRpc(Vector3 position, Quaternion rotation, ulong callerID)
    {
        int currentCount = AllPlayerDataManager.Instance.GetPlacedFiguresCount(callerID);

        if (currentCount >= placementPrefabs.Length) return;

        GameObject selectedPrefab = placementPrefabs[currentCount];
        GameObject character = Instantiate(selectedPrefab, position, rotation);

        NetworkObject characterNetworkObject = character.GetComponent<NetworkObject>();
        characterNetworkObject.SpawnWithOwnership(callerID);

        AllPlayerDataManager.Instance.IncrementPlacedFigureCountServerRpc(callerID);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestFigureCollectionServerRpc(ulong networkObjectId, ulong collectingClientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObjectToDestroy))
        {
            FigureIdentifier identifier = networkObjectToDestroy.GetComponent<FigureIdentifier>();
            if (identifier != null)
            {
                int collectedPieceId = identifier.pieceID;
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { collectingClientId }
                    }
                };
                AllPlayerDataManager.Instance.NotifyClientOfCollectedPieceClientRpc(collectedPieceId, clientRpcParams);
            }

            networkObjectToDestroy.Despawn(true);
            AllPlayerDataManager.Instance.CollectFigureServerRpc(collectingClientId);
        }
    }
}