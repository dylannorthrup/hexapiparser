using UnityEngine;
using System.Collections;
using System.Net;
using UnityEngine.Networking;

public class HTTPListener : MonoBehaviour
{

//	public string IP = "127.0.0.1";
//	public int port = 5000;                  // The port number of the socket we'll be listening on
//	private int socket;               // The socket we'll be listening on
//	private int requestTotal = 0;
//	private bool listenerRunning;     // Whether or not the listener's running
//
//	// Let's us find out if the listener is running from other methods/classes
//	public bool isListenerRunning ()
//	{
//		return listenerRunning;
//	}
//
//	void OnGUI ()
//	{
//		// We don't have a server running
//		if (Network.peerType == NetworkPeerType.Disconnected) {
//			GUI.Label (new Rect (10, 125, 100, 25), "Server Halted");
//			if (GUI.Button (new Rect (100, 125, 100, 25), "Start Server")) {
//				Network.InitializeServer (10, port);
//			}
//		// We do have a server running
//		} else if (Network.peerType == NetworkPeerType.Server) {
//			GUI.Label (new Rect (10, 125, 100, 25), "Server Running");
//			GUI.Label (new Rect (10, 100, 100, 25), "Total Requests: " + requestTotal);
//			if (GUI.Button (new Rect (100, 125, 100, 25), "Stop Server")) {
//				Network.Disconnect (20);
//			}
//		}
//	}
//	// Use this for initialization
//	void Start () {
//		NetworkTransport.Init();
//		ConnectionConfig config = new ConnectionConfig();
//		HostTopology topology = new HostTopology(config, 2);
//		socket = NetworkTransport.AddWebsocketHost(topology, port);
//
//	}
//	
//	// Update is called once per frame
//	void Update ()
//	{
//	
//	}
}
