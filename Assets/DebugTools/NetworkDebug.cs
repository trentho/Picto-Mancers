// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using Unity.Netcode;
// using System.Net;
// using Unity.Netcode.Transports.UTP;
// using Object = UnityEngine.Object;
// using TMPro;



// public class NetworkDebug : MonoBehaviour
// {
//     [SerializeField] private NetworkDiscovery netDiscovery;
// 	[SerializeField] private NetworkManager netManager;
// 	[SerializeField] private TextMeshProUGUI statusText;
// 	[SerializeField] private Button hostButton;
// 	[SerializeField] private Button clientButton;
	
// 	private IPAddress discoveredServer;


//     // Start is called before the first frame update
//     void Start()
//     {
//         var args = GetCommandlineArgs();
//         if (args.TryGetValue("-mode", out string mode))
//         {
//             switch (mode)
//             {
//                 case "server":
//                     netManager.StartServer();
//                     SetStatus("Running Server");
//                     break;
//                 case "host":
//                     netManager.StartHost();
//                     SetStatus("Running Host");
//                     break;
//                 case "client":
//                     netManager.StartClient();
//                     SetStatus("Running Client");
//                     break;
//             }
//         }
//         else
//         {
//             hostButton.onClick.AddListener(() =>
//             {
//                 MultiplayerManager.Instance.StartHost(() => {});
//                 SetStatus("Running Host at: " + MultiplayerManager.LocalIP);
//             });
//             clientButton.onClick.AddListener(() =>
//             {
// 				SetStatus("Discovering");
//                 MultiplayerManager.Instance.StartDiscovery((IPEndPoint sender, DiscoveryResponseData response) =>
//                 {
//                     MultiplayerManager.Instance.StartClient(sender.Address.ToString());
//                     SetStatus("Running Client");
//                 });
// 			});
//         }
//     }

//     private void SetStatus(string status)
//     {
//         hostButton.gameObject.SetActive(false);
//         clientButton.gameObject.SetActive(false);
//         statusText.gameObject.SetActive(true);
//         statusText.text = status;

//     }

//     private Dictionary<string, string> GetCommandlineArgs()
//     {
//         Dictionary<string, string> argDictionary = new Dictionary<string, string>();

//         if (Application.isEditor) return argDictionary;
//         var args = System.Environment.GetCommandLineArgs();

//         for (int i = 0; i < args.Length; ++i)
//         {
//             var arg = args[i].ToLower();
//             if (arg.StartsWith("-"))
//             {
//                 var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
//                 value = (value?.StartsWith("-") ?? false) ? null : value;

//                 argDictionary.Add(arg, value);
//             }
//         }
//         return argDictionary;
//     }
// }
