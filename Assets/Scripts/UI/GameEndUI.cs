using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameEndUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text resultText;
    [SerializeField] Button rematchButton;

	void Start()
	{
        panel.SetActive(false);
		GameManager.Instance.gameEnd.AddListener(serverWon =>
		{
            bool meWon = serverWon == MultiplayerManager.Instance.NetManager.IsHost;
            resultText.text = meWon ? "You Won!" : "You Lost";
            panel.SetActive(true);
            rematchButton.interactable = true;
		});
        GameManager.Instance.restartGame.AddListener(() => panel.SetActive(false));
        MultiplayerManager.Instance.OnDisconnect += () =>
        {
            panel.SetActive(false);
            LevelManager.Instance.SwitchToMenu();
        };

	}

	public void OnRematchButtonPressed()
	{
		GameManager.Instance.RematchServerRpc();
        rematchButton.interactable = false;
	}

	public void OnDisconnectButtonPressed()
	{
		MultiplayerManager.Instance.Shutdown();
		// var netManager = MultiplayerManager.Instance.NetManager;
		// if (netManager.IsServer)
		// {
		//     netManager.Shutdown();
		// }
		// else
		// {
		//     netManager.Shutdown();
		// }
	}
}
