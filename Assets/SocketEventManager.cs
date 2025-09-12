using System;
using System.Collections;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class SocketEventManager : MonoBehaviour
{
    public SocketIOUnity socket;

    // Start is called before the first frame update
    void Start()
    {
        var uri = new Uri("http://localhost:3000");

        socket = new SocketIOUnity(uri);

        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("socket.OnConnected");
        };

#if UNITY_EDITOR
        Debug.Log("Connecting (Editor)...");
        socket.Connect();
#else
        Debug.Log("Socket.IO disabled on device build");
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
