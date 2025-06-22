using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : NetworkBehaviour
{
    [Header("Paneles de Tutorial")]
    [SerializeField] private GameObject hostScanTutorialPanel;
    [SerializeField] private Button hostScanTutorialButton;
    [SerializeField] private GameObject clientScanTutorialPanel;
    [SerializeField] private Button clientScanTutorialButton;
    [SerializeField] private GameObject imageTrackingTutorialPanel;

    [Header("Fase de Escaneo")]
    [SerializeField] private GameObject scanningPhasePanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float scanTime = 10f;

    [Header("Fase de Colocación (Host)")]
    [SerializeField] private GameObject hostCanPlacePanel;
    [SerializeField] private Button hostCanPlaceButton;
    [SerializeField] private GameObject hostWaitingMessage;
    [SerializeField] private GameObject missingPiecesPanel;
    [SerializeField] private TextMeshProUGUI missingPiecesText;
    [SerializeField] private Button okMissingPiecesButton;
    [SerializeField] private Button listoButton;

    [Header("Fase de Espera (Cliente)")]
    [SerializeField] private GameObject clientWaitingPanel;

    [Header("UI de Fin de Partida")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject quitButtonObject;
    [SerializeField] private GameObject restartButtonObject;
    [SerializeField] private Canvas quitRestartGameCanvas; // Referencia al canvas de los botones

    [Header("Componentes AR")]
    [SerializeField] private MonoBehaviour arTrackingComponent;

    [Header("Configuración de Victoria")]
    [SerializeField] private int figuresToCollectForVictory = 4;

    private void Awake()
    {
        // Asignación de listeners para todos los botones de la UI
        if (hostScanTutorialButton) hostScanTutorialButton.onClick.AddListener(OnHostAcknowledgedScan);
        if (clientScanTutorialButton) clientScanTutorialButton.onClick.AddListener(OnClientAcknowledgedScan);
        if (hostCanPlaceButton) hostCanPlaceButton.onClick.AddListener(OnHostAcknowledgedPlacement);
        if (okMissingPiecesButton) okMissingPiecesButton.onClick.AddListener(DismissMissingPiecesPanel);
        if (listoButton) listoButton.onClick.AddListener(OnListoButtonClicked);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        GameFlowManager.OnGameStateChanged += HandleGameStateChanged;

        if (!IsHost && AllPlayerDataManager.Instance != null)
        {
            AllPlayerDataManager.Instance.OnCollectedFiguresCountChanged += HandleFigureCountChanged;
        }

        // --- AÑADIDO: Nos aseguramos de que el canvas de los botones esté activo ---
        if (quitRestartGameCanvas != null) quitRestartGameCanvas.gameObject.SetActive(true);
        if (quitButtonObject != null) quitButtonObject.SetActive(true);
        if (restartButtonObject != null) restartButtonObject.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        GameFlowManager.OnGameStateChanged -= HandleGameStateChanged;
        if (AllPlayerDataManager.Instance != null && !IsHost)
        {
            AllPlayerDataManager.Instance.OnCollectedFiguresCountChanged -= HandleFigureCountChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        HideAllPanels();
        switch (newState)
        {
            case GameState.Scanning:
                if (scanningPhasePanel != null) scanningPhasePanel.SetActive(true);
                StartCoroutine(ScanCountdown());
                break;
            case GameState.HostPlacing:
                if (IsHost)
                {
                    if (hostCanPlacePanel != null) hostCanPlacePanel.SetActive(true);
                }
                else
                {
                    if (clientWaitingPanel != null) clientWaitingPanel.SetActive(true);
                    FreezeCamera(true);
                }
                break;
            case GameState.ClientSearching:
                if (IsHost)
                {
                    if (hostWaitingMessage != null) hostWaitingMessage.SetActive(true);
                }
                else FreezeCamera(false);
                break;
            case GameState.GameEnd:
                if (victoryPanel != null) victoryPanel.SetActive(true);
                if (quitButtonObject != null) quitButtonObject.SetActive(false);
                if (restartButtonObject != null) restartButtonObject.SetActive(true);
                break;
        }
    }

    // El resto del código no necesita cambios.
    // ...
    private void HandleFigureCountChanged(ulong clientId, int newCount)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId && newCount >= figuresToCollectForVictory)
        {
            ReportVictoryToServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportVictoryToServerRpc()
    {
        GameFlowManager.Instance.RequestStateChangeServerRpc(GameState.GameEnd);
    }

    public void ShowImageTrackingTutorial(bool show)
    {
        if (imageTrackingTutorialPanel != null) imageTrackingTutorialPanel.SetActive(show);
    }

    public void ShowInitialScanTutorial()
    {
        if (IsHost) { if (hostScanTutorialPanel != null) hostScanTutorialPanel.SetActive(true); }
        else { if (clientScanTutorialPanel != null) clientScanTutorialPanel.SetActive(true); }
    }

    private void OnHostAcknowledgedScan()
    {
        if (hostScanTutorialPanel != null) hostScanTutorialPanel.SetActive(false);
        GameFlowManager.Instance.RequestStateChangeServerRpc(GameState.Scanning);
    }

    private void OnClientAcknowledgedScan()
    {
        if (clientScanTutorialPanel != null) clientScanTutorialPanel.SetActive(false);
    }

    private void OnHostAcknowledgedPlacement()
    {
        if (hostCanPlacePanel != null) hostCanPlacePanel.SetActive(false);
        if (listoButton != null) listoButton.gameObject.SetActive(true);
    }

    private void OnListoButtonClicked()
    {
        if (!IsHost) return;
        int placedFigures = AllPlayerDataManager.Instance.GetPlacedFiguresCount(NetworkManager.Singleton.LocalClientId);

        if (placedFigures >= figuresToCollectForVictory)
        {
            GameFlowManager.Instance.RequestStateChangeServerRpc(GameState.ClientSearching);
        }
        else
        {
            int missing = figuresToCollectForVictory - placedFigures;
            if (missingPiecesText != null) missingPiecesText.text = $"Te faltan colocar {missing} piezas.";
            if (missingPiecesPanel != null) missingPiecesPanel.SetActive(true);
        }
    }

    private void DismissMissingPiecesPanel()
    {
        if (missingPiecesPanel != null) missingPiecesPanel.SetActive(false);
    }

    private IEnumerator ScanCountdown()
    {
        float timer = scanTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(timer).ToString();
            yield return null;
        }
        if (IsHost) GameFlowManager.Instance.RequestStateChangeServerRpc(GameState.HostPlacing);
    }

    private void FreezeCamera(bool freeze)
    {
        if (arTrackingComponent != null) arTrackingComponent.enabled = !freeze;
    }

    private void HideAllPanels()
    {
        if (hostScanTutorialPanel) hostScanTutorialPanel.SetActive(false);
        if (clientScanTutorialPanel) clientScanTutorialPanel.SetActive(false);
        if (imageTrackingTutorialPanel) imageTrackingTutorialPanel.SetActive(false);
        if (scanningPhasePanel) scanningPhasePanel.SetActive(false);
        if (hostCanPlacePanel) hostCanPlacePanel.SetActive(false);
        if (clientWaitingPanel) clientWaitingPanel.SetActive(false);
        if (hostWaitingMessage) hostWaitingMessage.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
        if (listoButton != null) listoButton.gameObject.SetActive(false);
    }
}