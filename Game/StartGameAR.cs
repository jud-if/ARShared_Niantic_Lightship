using System;
using UnityEngine;
using UnityEngine.UI;
using Niantic.Lightship.SharedAR.Colocalization;
using Unity.Netcode;

public class StartGameAR : MonoBehaviour
{
    [SerializeField] private SharedSpaceManager _sharedSpaceManager;
    private const int MAX_AMOUND_CLIENTS_ROOM = 5;

    [SerializeField] private Texture2D _targetImage;
    [SerializeField] private float _targetImagenSize;
    private string roomName = "TestRoom";

    [Header("Botones de Menú")]
    [SerializeField] private Button StartGameButton;
    [SerializeField] private Button CreateRoomButton;
    [SerializeField] private Button JoinRoomButton;

    // Referencia al GameUIManager unificado
    [SerializeField] private GameUIManager _gameUIManager;

    private bool isHost;

    public static event Action OnStartSharedSpaceHost;
    public static event Action OnJoinSharedSpaceClient;
    public static event Action OnStartGame;
    public static event Action OnStartSharedSpace;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _sharedSpaceManager.sharedSpaceManagerStateChanged += SharedSpaceManagerOnSharedSpaceManagerStateChanged;

        StartGameButton.onClick.AddListener(StartGame);
        CreateRoomButton.onClick.AddListener(CreateGameHost);
        JoinRoomButton.onClick.AddListener(JoinGameClient);

        StartGameButton.interactable = false;
    }

    private void OnDestroy()
    {
        _sharedSpaceManager.sharedSpaceManagerStateChanged -= SharedSpaceManagerOnSharedSpaceManagerStateChanged;
    }

    private void SharedSpaceManagerOnSharedSpaceManagerStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs obj)
    {
        if (obj.Tracking)
        {
            StartGameButton.interactable = true;
            CreateRoomButton.interactable = false;
            JoinRoomButton.interactable = false;

            if (_gameUIManager != null) _gameUIManager.ShowImageTrackingTutorial(false);
        }
    }

    void StartGame()
    {
        OnStartGame?.Invoke();

        if (isHost) NetworkManager.Singleton.StartHost();
        else NetworkManager.Singleton.StartClient();

        CreateRoomButton.gameObject.SetActive(false);
        JoinRoomButton.gameObject.SetActive(false);
        StartGameButton.gameObject.SetActive(false);

        if (_gameUIManager != null)
        {
            // El nombre correcto ahora es ShowInitialScanTutorial
            _gameUIManager.ShowInitialScanTutorial();
        }
    }

    void CreateGameHost()
    {
        isHost = true;
        OnStartSharedSpaceHost?.Invoke();
        StartSharedSpace();

        if (_gameUIManager != null) _gameUIManager.ShowImageTrackingTutorial(true);
    }

    void JoinGameClient()
    {
        isHost = false;
        OnJoinSharedSpaceClient?.Invoke();
        StartSharedSpace();

        if (_gameUIManager != null) _gameUIManager.ShowImageTrackingTutorial(true);
    }

    void StartSharedSpace()
    {
        OnStartSharedSpace?.Invoke();

        // El resto de este método se queda igual
        if (_sharedSpaceManager.GetColocalizationType() == SharedSpaceManager.ColocalizationType.MockColocalization)
        {
            var mockTrackingArgs = ISharedSpaceTrackingOptions.CreateMockTrackingOptions();
            var roomArgs = ISharedSpaceRoomOptions.CreateLightshipRoomOptions(roomName, MAX_AMOUND_CLIENTS_ROOM, "MockColocalizationDemo");
            _sharedSpaceManager.StartSharedSpace(mockTrackingArgs, roomArgs);
        }
        else if (_sharedSpaceManager.GetColocalizationType() == SharedSpaceManager.ColocalizationType.ImageTrackingColocalization)
        {
            var imageTrackingOptions = ISharedSpaceTrackingOptions.CreateImageTrackingOptions(_targetImage, _targetImagenSize);
            var roomArgs = ISharedSpaceRoomOptions.CreateLightshipRoomOptions(roomName, MAX_AMOUND_CLIENTS_ROOM, "ImageColocalization");
            _sharedSpaceManager.StartSharedSpace(imageTrackingOptions, roomArgs);
        }
    }
}