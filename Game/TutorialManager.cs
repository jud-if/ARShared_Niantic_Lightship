using UnityEngine;
using UnityEngine.UI; // Necesario para Button

public class TutorialManager : MonoBehaviour
{
    // Arrastra aquí el panel y el botón desde la jerarquía
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Button understoodButton;

    void Start()
    {
        // Nos aseguramos de que el panel esté visible al iniciar la escena
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        // Añadimos un "listener" al evento OnClick del botón.
        // Esto hará que se llame al método 'DismissTutorial' cuando se haga clic.
        if (understoodButton != null)
        {
            understoodButton.onClick.AddListener(DismissTutorial);
        }
        else
        {
            Debug.LogError("El botón 'Entendido' no está asignado en el TutorialManager.");
        }
    }

    // Este método simplemente oculta el panel del tutorial.
    void DismissTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
            Debug.Log("Tutorial cerrado.");
        }
    }
}