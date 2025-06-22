using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class GameUIController : NetworkBehaviour
{
    [Header("Paneles de Tutorial")]
    [SerializeField] private GameObject hostScanTutorialPanel;
    [SerializeField] private GameObject clientScanTutorialPanel;
    [SerializeField] private Button hostScanTutorialButton;
    [SerializeField] private Button clientScanTutorialButton;
    [SerializeField] private GameObject imageTrackingTutorialPanel;

    [Header("Fase de Escaneo")]
    [SerializeField] private GameObject scanningPhasePanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float scanTime = 10f;

    [Header("Fase de Colocación (Host)")]
    [SerializeField] private GameObject hostCanPlacePanel;
    [SerializeField] private Button hostCanPlaceButton;
    [SerializeField] private Button listoButton;
    [SerializeField] private GameObject missingPiecesPanel;
    [SerializeField] private TextMeshProUGUI missingPiecesText;
    [SerializeField] private GameObject hostWaitingMessage;
    [SerializeField] private Button okMissingPiecesButton; // --- NUEVO ---

    [Header("Fase de Espera (Cliente)")]
    [SerializeField] private GameObject clientWaitingPanel;

    [Header("Componentes AR")]
    [SerializeField] private MonoBehaviour arTrackingComponent;

    private void OnEnable()
    {
        GameFlowManager.OnGameStateChanged += HandleGameStateChanged;
        hostScanTutorialButton.onClick.AddListener(OnHostAcknowledgedScan);
        clientScanTutorialButton.onClick.AddListener(OnClientAcknowledgedScan);
        hostCanPlaceButton.onClick.AddListener(OnHostAcknowledgedPlacement);
        listoButton.onClick.AddListener(OnListoButtonClicked);
        okMissingPiecesButton.onClick.AddListener(DismissMissingPiecesPanel); // --- NUEVO ---
    }

    private void OnDisable()
    {
        GameFlowManager.OnGameStateChanged -= HandleGameStateChanged;
        hostScanTutorialButton.onClick.RemoveListener(OnHostAcknowledgedScan);
        clientScanTutorialButton.onClick.RemoveListener(OnClientAcknowledgedScan);
        hostCanPlaceButton.onClick.RemoveListener(OnHostAcknowledgedPlacement);
        listoButton.onClick.RemoveListener(OnListoButtonClicked);
        okMissingPiecesButton.onClick.RemoveListener(DismissMissingPiecesPanel); // --- NUEVO ---
    }

    // El método HandleGameStateChanged se queda igual

    private void OnListoButtonClicked()
    {
        int placedFigures = AllPlayerDataManager.Instance.GetPlacedFiguresCount(NetworkManager.Singleton.LocalClientId);
        int maxFigures = PlaceFigure.MaxCubesPerUser;

        if (placedFigures >= maxFigures)
        {
            GameFlowManager.Instance.RequestStateChangeServerRpc(GameState.ClientSearching);
        }
        else
        {
            int missing = maxFigures - placedFigures;
            missingPiecesText.text = $"Te faltan colocar {missing} piezas.";

            // --- MODIFICADO: Ya no usamos la corutina, solo activamos el panel ---
            if (missingPiecesPanel != null)
            {
                missingPiecesPanel.SetActive(true);
            }
        }
    }

    // --- NUEVO: Método para cerrar el panel al hacer clic en OK ---
    private void DismissMissingPiecesPanel()
    {
        if (missingPiecesPanel != null)
        {
            missingPiecesPanel.SetActive(false);
        }
    }

    // --- ELIMINADO: La corutina ShowMissingPiecesWarning ya no es necesaria ---
    /* private IEnumerator ShowMissingPiecesWarning()
    {
        missingPiecesPanel.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        missingPiecesPanel.SetActive(false);
    }
    */

    // El resto de los métodos (ScanCountdown, FreezeCamera, HideAllPanels, etc.) se quedan igual
    // ...
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
                    if (listoButton != null) listoButton.gameObject.SetActive(false);
                    if (hostWaitingMessage != null) hostWaitingMessage.SetActive(true);
                }
                else
                {
                    FreezeCamera(false);
                }
                break;
        }
    }

    public void ShowInitialTutorial()
    {
        if (IsHost)
        {
            if (hostScanTutorialPanel != null) hostScanTutorialPanel.SetActive(true);
        }
        else
        {
            if (clientScanTutorialPanel != null) clientScanTutorialPanel.SetActive(true);
        }
    }

    public void ShowImageTrackingTutorial(bool show)
    {
        if (imageTrackingTutorialPanel != null)
        {
            imageTrackingTutorialPanel.SetActive(show);
        }
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

    private IEnumerator ScanCountdown()
    {
        float timer = scanTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(timer).ToString();
            yield return null;
        }

        if (IsHost)
        {
            GameFlowManager.Instance.RequestStateChangeServerRpc(GameState.HostPlacing);
        }
    }

    private void FreezeCamera(bool freeze)
    {
        if (arTrackingComponent != null)
        {
            arTrackingComponent.enabled = !freeze;
        }
    }

    private void HideAllPanels()
    {
        if (hostScanTutorialPanel != null) hostScanTutorialPanel.SetActive(false);
        if (clientScanTutorialPanel != null) clientScanTutorialPanel.SetActive(false);
        if (scanningPhasePanel != null) scanningPhasePanel.SetActive(false);
        if (hostCanPlacePanel != null) hostCanPlacePanel.SetActive(false);
        if (clientWaitingPanel != null) clientWaitingPanel.SetActive(false);
        if (missingPiecesPanel != null) missingPiecesPanel.SetActive(false);
        if (hostWaitingMessage != null) hostWaitingMessage.SetActive(false);
        if (imageTrackingTutorialPanel != null) imageTrackingTutorialPanel.SetActive(false);
    }


}