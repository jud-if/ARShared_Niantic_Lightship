using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class StartGame : NetworkBehaviour
{
    [SerializeField] private Button startHost;
    [SerializeField] private Button startClient;


    // Start is called before the first frame update
    void Start()
    {
        startHost.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });

        startClient.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });

    }

}