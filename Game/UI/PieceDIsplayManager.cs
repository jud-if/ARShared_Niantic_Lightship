using UnityEngine;
using UnityEngine.UI; // Necesario para Image
using System; // Necesario para Action

// System.Collections ya no es necesario porque no usamos Coroutines
// using System.Collections; 

public class PieceDisplayManager : MonoBehaviour
{
    // --- NUEVO: Evento público ---
    // Cualquier otro script podrá suscribirse a este evento.
    public static event Action OnPiecePanelDismissed;

    public static PieceDisplayManager Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject pieceFoundPanel;
    [SerializeField] private Image foundPieceDisplay;

    [Header("Piece Images")]
    [SerializeField] private Sprite[] pieceSprites;

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

    // Este método solo activa el panel, ya no inicia un temporizador.
    public void ShowFoundPiece(int pieceId)
    {
        if (pieceId > 0 && pieceId <= pieceSprites.Length)
        {
            foundPieceDisplay.sprite = pieceSprites[pieceId - 1];
            pieceFoundPanel.SetActive(true);
        }
        else
        {
            Debug.LogError($"ID de pieza inválido: {pieceId}");
        }
    }

    // --- CAMBIO PRINCIPAL ---
    // Se añade el método Update para escuchar el clic/toque del usuario
    private void Update()
    {
        // Primero, comprueba si el panel está activo.
        if (pieceFoundPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            // Luego, comprueba si el usuario ha hecho clic con el botón izquierdo del ratón
            // o ha tocado la pantalla en un dispositivo móvil.
                // Si ambas condiciones son verdaderas, oculta el panel.
            pieceFoundPanel.SetActive(false);
            // --- NUEVO: Anunciar que el panel se ha cerrado ---
            OnPiecePanelDismissed?.Invoke();
        }
    }
}