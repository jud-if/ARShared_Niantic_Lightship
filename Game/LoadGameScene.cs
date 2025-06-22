using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadGameScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.Shutdown();

        // Updated to use FindObjectsByType with FindObjectsSortMode.None
        List<GameObject> netObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(obj => obj.transform.gameObject).ToList();

        foreach (var obj in netObjects)
        {
            Destroy(obj);
        }

        GameObject startGameARObject = FindFirstObjectByType<StartGameAR>().gameObject;
        Destroy(startGameARObject);

        Destroy(FindFirstObjectByType<NetworkManager>().transform.gameObject);
        SceneManager.LoadScene("ayuda", LoadSceneMode.Single);
    }
}

