#pragma warning disable 4014 // Suppress await warnings

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using System.Net;
using Unity.Netcode.Transports.UTP;
using Object = UnityEngine.Object;
using TMPro;
using System;

public class MultiplayerUI : MonoBehaviour
{
	[SerializeField] GameObject modalBackground; // Darkens the main screen when a modal is visible

	[Header("Lobby List")]
	[SerializeField] CanvasGroup lobbyListScreen;
	[SerializeField] GameObject lobbyList;
	[SerializeField] TMP_InputField playerNameInputField;
	[SerializeField] Button lobbyListItemTemplate;
	[SerializeField] TMP_Text lobbyListItemNameText;
	[SerializeField] TMP_Text lobbyListItemMapText;
	[SerializeField] Button lobbyListCreateButton;
	[SerializeField] Button lobbyListRefreshButton;

	public float autoRefreshTime = 1f;
	public float manualRefreshTime = 0.1f;
	private float refreshTimer = float.PositiveInfinity;

	[Header("Create Lobby")]
	[SerializeField] GameObject createLobbyModal;
	[SerializeField] TMP_InputField lobbyNameInputField;
	[SerializeField] TMP_Dropdown mapDropdown;
	[SerializeField] Button createLobbyButton;
	[SerializeField] Button createBackButton;

	[Header("Connecting")]
	[SerializeField] CanvasGroup connectingScreen;
	[SerializeField] Button connectingCancelButton;
	[SerializeField] GameObject connectingProgress;
	[SerializeField] GameObject connectingError;

	[Header("Lobby")]
	[SerializeField] CanvasGroup lobbyScreen;
	[SerializeField] TMP_Text lobbyNameText;
	[SerializeField] TMP_Text hostNameText;
	[SerializeField] TMP_Text clientNameText;
	[SerializeField] Button lobbyStartButton;
	[SerializeField] Button lobbyLeaveButton;
	bool gameStarted = false;


	List<CanvasGroup> Screens { get => new List<CanvasGroup>{lobbyListScreen, connectingScreen, lobbyScreen}; }
	bool IsActive(CanvasGroup screen) => screen.gameObject.activeInHierarchy;

	public CanvasGroup GetScreen()
	{
		var screen = Screens.Find(IsActive);
		SetScreen(screen);
		return screen;
	}

	public void SetScreen(CanvasGroup screen)
	{
		Debug.Assert(screen == null || Screens.Contains(screen));

		var prevScreen = Screens.Find(IsActive);
		Screens.ForEach(screen => screen.gameObject.SetActive(false));
		screen?.gameObject?.SetActive(true);

		if (prevScreen == screen) return;

		if (screen == connectingScreen)
		{
			connectingProgress.SetActive(true);
			connectingError.SetActive(false);
		}
		if (screen == lobbyListScreen)
		{
			RefreshLobbyList();
		}
		if (screen != null)
		{
			gameStarted = false;
		}

	}

	public void SetModal(GameObject modal)
	{
		Debug.Assert(
			modal == createLobbyModal ||
			modal == null
		);
		createLobbyModal.SetActive(false);
		modal?.SetActive(true);

		var screen = GetScreen();
		if (modal == null) // no modal open
		{
			modalBackground.SetActive(false);
			screen.alpha = 1f;
			screen.interactable = true;
		}
		else // modal is open
		{
			modalBackground.SetActive(true);
			screen.alpha = 0.5f;
			screen.interactable = false;
		}
	}

	void Reset()
	{
		SetScreen(lobbyListScreen);
		RefreshLobbyList();
		SetModal(null);
	}

	void Start()
	{
		Reset();
		lobbyListItemTemplate.gameObject.SetActive(false);
		Debug.Log(lobbyListItemTemplate.gameObject.activeSelf);
        MultiplayerManager.Instance.OnDisconnect += () => gameStarted = false;

		// Lobby list buttons
		lobbyListCreateButton.onClick.AddListener(() => SetModal(createLobbyModal));
		lobbyListRefreshButton.onClick.AddListener(() => 
		{
			lobbyListRefreshButton.interactable = false;
			RefreshLobbyList();
		});

		// Create lobby buttons
		createBackButton.onClick.AddListener(() => SetModal(null));
		createLobbyButton.onClick.AddListener(async () =>
		{
			SetModal(null);
			SetScreen(connectingScreen);
			await MultiplayerManager.Instance.CreateLobby(lobbyNameInputField.text, playerNameInputField.text);
			SetScreen(lobbyScreen);
		});

		lobbyLeaveButton.onClick.AddListener(() => 
		{
			MultiplayerManager.Instance.LeaveLobby();
			SetScreen(lobbyListScreen);
		});

		lobbyStartButton.onClick.AddListener(() =>
		{
			MultiplayerManager.Instance.StartGame();
			gameStarted = true;
			lobbyStartButton.interactable = false;
		});

		// connectingCancelButton.onClick.AddListener(() => 
		// {
			
		// })

		// TODO: connectingCancelButton


	}

	void Update()
	{
		refreshTimer += Time.deltaTime;
		if (refreshTimer > manualRefreshTime) lobbyListRefreshButton.interactable = true;
		if (refreshTimer > autoRefreshTime) RefreshLobbyList();

		Lobby joinedLobby = MultiplayerManager.Instance.joinedLobby;
		if (joinedLobby != null)
		{
			// TODO: disconnect if host crashes?

			lobbyNameText.text = joinedLobby.Name;

			// TODO: map name

			hostNameText.text = "Waiting...";
			clientNameText.text = "Waiting...";
			Debug.Log("Players: " + joinedLobby.Players.Count);
			foreach(Player player in joinedLobby.Players){
				String name = player.Data[MultiplayerManager.KEY_PLAYER_NAME].Value;
				if(player.Id == joinedLobby.HostId){
					hostNameText.text = name;
				}else{
					clientNameText.text = name;
				}
			}

			lobbyStartButton.gameObject.SetActive(MultiplayerManager.Instance.IsLobbyHost());
			lobbyStartButton.interactable = !gameStarted && joinedLobby.Players.Count > 1;
		}
	}

	public void RefreshLobby()
	{
		Lobby joinedLobby = MultiplayerManager.Instance.joinedLobby;
		if (joinedLobby != null)
		{
			lobbyNameText.text = joinedLobby.Name;

			// TODO: map name

			hostNameText.text = "Waiting...";
			clientNameText.text = "Waiting...";
			foreach(Player player in joinedLobby.Players){
				String name = player.Data[MultiplayerManager.KEY_PLAYER_NAME].Value;
				if(player.Id == joinedLobby.HostId){
					hostNameText.text = name;
				}else{
					clientNameText.text = name;
				}
			}

			lobbyStartButton.gameObject.SetActive(MultiplayerManager.Instance.IsLobbyHost());
			lobbyStartButton.interactable = !gameStarted && joinedLobby.Players.Count > 1;
		}
	}

	public void RefreshLobbyList()
	{
		if (refreshTimer < manualRefreshTime && refreshTimer < autoRefreshTime) return;
		if (refreshTimer > manualRefreshTime) // So user doesn't wait longer than manual refresh time
			refreshTimer = 0f;

		// clear server list
		foreach (Transform lobbyListItem in lobbyList.transform)
			if (lobbyListItem.gameObject != lobbyListItemTemplate.gameObject) // don't clear prefab item
				Destroy(lobbyListItem.gameObject);

		foreach (Lobby lobby in MultiplayerManager.Instance.lobbies)
		{
			lobbyListRefreshButton.interactable = true;
			lobbyListItemNameText.text = lobby.Name;
			lobbyListItemMapText.text = "Unknown Map"; // TODO: pass map index and set name and color based on index
			Button item = Instantiate(lobbyListItemTemplate.gameObject, lobbyList.transform).GetComponent<Button>();
			item.onClick.AddListener(async () => // When lobby is clicked, join it
			{
                SetScreen(connectingScreen);
				await MultiplayerManager.Instance.JoinLobby(lobby, playerNameInputField.text);
				SetScreen(lobbyScreen);
			});
			item.gameObject.SetActive(true);
		}
	}

}
