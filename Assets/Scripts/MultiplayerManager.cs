#pragma warning disable 4014 // Suppress await warnings

using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Http;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Networking.Transport.Relay;

[RequireComponent(typeof(NetworkManager))]
public class MultiplayerManager : MonoBehaviour
{
	public static MultiplayerManager Instance { get; private set; }

	public readonly string[] randomNameArray = { "Dumbledore", "Gandalf ", "Harry", "Merlin", "Suaron", "Jadis", "Oz", "Hermione", "The Wicked Witch", "Circe", "Kiki", "Rudeus", "Roxy" };

	private float heartbeatTimer;
	[SerializeField]
	private float heartbeatTimerMax = 15;
	private float lobbyUpdateTimer;
	[SerializeField]
	private float lobbyUpdateTimerMax = 1.1f;
	private float lobbyQueryTimer;
	[SerializeField]
	private float lobbyQueryTimerMax = 1.1f;

	public List<Lobby> lobbies = new List<Lobby>();
	public int lobbyCount = 25;

	public Lobby joinedLobby = null;
	public const string KEY_START_GAME = "Start";
	public const string KEY_PLAYER_NAME = "PlayerName";
	private const string NO_SERVER_VALUE = "0";
	public NetworkManager NetManager { get; private set; }

	public event Action OnConnected = null; // invoked when 2 players are in
	public event Action OnDisconnect = null;// invoked when 1 player disconnects
	public bool IsConnected { get; private set; } = false;

	void Awake()
	{
		if (Instance == null) Instance = this;
		else Debug.LogWarning("Multiple instances of singleton MultiplayerManager.");
		NetManager = NetworkManager.Singleton;
		Authenticate();
	}

	// Start is called before the first frame update
	private void Start()
	{
		NetManager.OnClientConnectedCallback += async (ulong clientId) =>
		{
			Debug.Log("OnClientConnectedCallback");
			if (clientId == NetManager.LocalClientId)
			{
				return;
			}
			joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>{
					{KEY_START_GAME,new DataObject(DataObject.VisibilityOptions.Member,NO_SERVER_VALUE)}
				}
			});
			OnConnected?.Invoke();
		};
		NetManager.OnClientDisconnectCallback += (ulong clientId) =>
		{
			Debug.Log("OnClientDisconnectCallback");
			NetManager.Shutdown();
			OnDisconnect?.Invoke();
		};
		OnConnected += () => IsConnected = true;
		OnDisconnect += () => IsConnected = false;

	}

	private async Task Authenticate()
	{
		await UnityServices.InitializeAsync();
		AuthenticationService.Instance.SignedIn += () =>
		{
			Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
		};
		await AuthenticationService.Instance.SignInAnonymouslyAsync();
	}

	private string randomPlayerNameIfEmpty(string playerName)
	{
		if (playerName == "")
		{
			return randomNameArray[UnityEngine.Random.Range(0, randomNameArray.Length - 1)];
		}
		else
		{
			return playerName;
		}
	}
	private Player CreatePlayer(String playerName)
	{
		return new Player
		{
			Data = new Dictionary<string, PlayerDataObject>{
				{KEY_PLAYER_NAME,new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
			}
		};
	}
	public async Task CreateLobby(String lobbyName = "", String playerName = "")
	{
		try
		{
			playerName = randomPlayerNameIfEmpty(playerName);
			if (lobbyName == "")
			{
				lobbyName = playerName + "'s lobby";
			}
			CreateLobbyOptions options = new CreateLobbyOptions
			{
				IsPrivate = false,
				Player = CreatePlayer(playerName),
				Data = new Dictionary<string, DataObject>{
					{KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member,NO_SERVER_VALUE)}
				}
			};
			joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
			Debug.Log("Created Lobby " + joinedLobby.Name);
		}
		catch (LobbyServiceException e)
		{
			Debug.Log("failed to create lobby: " + e.Message);
		}
	}
	public async Task ListLobbies(String lobbySearch = "")
	{
		lobbyQueryTimer -= Time.deltaTime;
		if (lobbyQueryTimer >= 0f)
		{
			return;
		}
		lobbyQueryTimer = float.PositiveInfinity;
		try
		{
			QueryLobbiesOptions options = new QueryLobbiesOptions
			{
				Count = lobbyCount,
				Filters = new List<QueryFilter>{
					new QueryFilter(QueryFilter.FieldOptions.AvailableSlots,NO_SERVER_VALUE,QueryFilter.OpOptions.GT),
					new QueryFilter(QueryFilter.FieldOptions.Name,lobbySearch,QueryFilter.OpOptions.CONTAINS)
				},
				Order = new List<QueryOrder>{
					new QueryOrder(true,QueryOrder.FieldOptions.Name)
				}
			};
			QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
			// Debug.Log("lobbies found: " + queryResponse.Results.Count);
			lobbies = queryResponse.Results;
			// foreach (Lobby lobby in queryResponse.Results)
			// {
			//     Debug.Log("lobby: " + lobby.Name);
			// }
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e.Message);
		}
		lobbyQueryTimer = lobbyQueryTimerMax;
	}
	public async Task JoinLobby(Lobby lobby, String playerName)
	{
		try
		{
			playerName = randomPlayerNameIfEmpty(playerName);
			JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
			{
				Player = CreatePlayer(playerName)
			};
			joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id, options);
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e.Message);
		}
	}

	public async Task LeaveLobby()
	{
		try
		{
			await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
			joinedLobby = null;
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e);
		}
	}
	public async Task KickPlayer(int index)
	{
		if (IsLobbyHost() == false)
		{
			return;
		}
		try
		{
			await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[index].Id);
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e);
		}
	}

	private async Task LobbyHeartbeat()
	{
		if (joinedLobby != null && IsLobbyHost())
		{
			heartbeatTimer -= Time.deltaTime;
			if (heartbeatTimer < 0f)
			{
				heartbeatTimer = float.PositiveInfinity;
				await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
				heartbeatTimer = heartbeatTimerMax;
			}
		}
	}
	private bool IsPlayerInLobby()
	{
		if (joinedLobby == null)
		{
			return false;
		}
		foreach (Player player in joinedLobby.Players)
		{
			if (player.Id == AuthenticationService.Instance.PlayerId)
			{
				return true;
			}
		}
		return false;
	}
	private async Task LobbyUpdate()
	{
		if (joinedLobby != null)
		{
			lobbyUpdateTimer -= Time.deltaTime;
			if (lobbyUpdateTimer < 0f)
			{
				lobbyUpdateTimer = float.PositiveInfinity;
				joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
				if (IsPlayerInLobby() == false)
				{
					//player kicked from lobby
					joinedLobby = null;
					// TODO : ui update
					lobbyUpdateTimer = lobbyUpdateTimerMax;
					return;
				}
				if (joinedLobby.Data[KEY_START_GAME].Value != NO_SERVER_VALUE)
				{
					if (IsLobbyHost() == false)
					{
						await StartClient(joinedLobby.Data[KEY_START_GAME].Value);
					}
				}
				lobbyUpdateTimer = lobbyUpdateTimerMax;
			}
		}
	}
    private async void OnApplicationQuit(){
        if(joinedLobby != null){
            await LeaveLobby();
        }
    }
	private void Update()
	{
		if (UnityServices.State != ServicesInitializationState.Initialized || 
			!AuthenticationService.Instance.IsAuthorized)
		{
			return;
		}
		LobbyHeartbeat();
		LobbyUpdate();
		ListLobbies();


		// TODO: remove this or change it
		if (Input.GetKeyDown(KeyCode.M))
		{
			if (IsConnected) OnDisconnect?.Invoke();
			else OnConnected?.Invoke();
		}
	}
	public void Shutdown()
	{
		OnDisconnect.Invoke();
		NetManager.Shutdown();
	}
	public bool IsLobbyHost()
	{
		if (joinedLobby == null)
		{
			return false;
		}
		return joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
	}

	/// <summary>Starts host using unity relay </summary>

	public async Task StartGame()
	{
		if (IsLobbyHost() == false)
		{
			return;
		}
		if (joinedLobby.Players.Count < 2)
		{
			return;
		}
		try
		{

			string relayCode = await StartHost();
			Debug.Log("host started");
			if (relayCode == null)
			{
				throw new LobbyServiceException(new SystemException("null relay code"));
			}
			// Debug.Log("pushing relay code");
			Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>{
					{KEY_START_GAME,new DataObject(DataObject.VisibilityOptions.Member,relayCode)}
				}
			});
			joinedLobby = lobby;
			// Debug.Log("pushed relay code");
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e);
		}

	}
	private async Task<string> StartHost(Action onStarted = null)
	{

		Allocation allocation;
		string joinCode;
		try
		{
			allocation = await RelayService.Instance.CreateAllocationAsync(1);



			joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

			Debug.Log("join code: " + joinCode);

			RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

			Action startedCallback = null;
			NetManager.OnServerStarted += startedCallback = () =>
			{
				NetManager.OnServerStarted -= startedCallback;
				onStarted?.Invoke();
			};

			NetworkManager.Singleton.StartHost();
			return joinCode;
		}
		catch (RelayServiceException e)
		{
			Debug.LogError($"Relay request failed {e.Message}");
			return null;
		}
	}
	public async Task StartClient(string joinCode, Action onFailure = null)
	{

		JoinAllocation allocation;
		try
		{
			allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

			RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

			Action<ulong> connectedCallback = null, disconnectCallback = null;
			Action<Action> callback = (Action callBackCallBack) =>
			{
				NetManager.OnClientConnectedCallback -= connectedCallback;
				NetManager.OnClientDisconnectCallback -= disconnectCallback;
				callBackCallBack?.Invoke();
			};
			NetManager.OnClientConnectedCallback += connectedCallback = (ulong clientId) => callback(OnConnected);
			NetManager.OnClientDisconnectCallback += disconnectCallback = (ulong clientId) => callback(onFailure);

			NetworkManager.Singleton.StartClient();
		}
		catch (RelayServiceException e)
		{
			Debug.LogError($"Relay join code request failed {e.Message}");
		}
	}

}
