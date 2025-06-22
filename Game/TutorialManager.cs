using UnityEngine;
using UnityEngine.UI; // Necesario para Button

public class TutorialManager : MonoBehaviour
{
    // Arrastra aqu� el panel y el bot�n desde la jerarqu�a
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Button understoodButton;

    void Start()
    {
        // Nos aseguramos de que el panel est� visible al iniciar la escena
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        // A�adimos un "listener" al evento OnClick del bot�n.
        // Esto har� que se llame al m�todo 'DismissTutorial' cuando se haga clic.
        if (understoodButton != null)
        {
            understoodButton.onClick.AddListener(DismissTutorial);
        }
        else
        {
            Debug.LogError("El bot�n 'Entendido' no est� asignado en el TutorialManager.");
        }
    }

    // Este m�todo simplemente oculta el panel del tutorial.
    void DismissTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
            Debug.Log("Tutorial cerrado.");
        }
    }
}