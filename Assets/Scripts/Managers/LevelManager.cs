using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
	[SerializeField] GameObject menu;   // menu scene
	[SerializeField] GameObject map;    // game scene
	[SerializeField] GameObject playerFollowers;
	[SerializeField] AnimationCurve mapAnimation;
	float mapPosition = 0; // 0 to 1

	public enum State { Game, Menu, TransitionToGame, TransitionToMenu }
	public State CurrentState { get; private set; } = State.Menu;
    private bool bouncing = false;

	public static LevelManager Instance { get; private set; }

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Debug.LogWarning("Multiple instances of singleton GameManager");
		}
	}

	void Start()
	{
        GameManager.Instance.restartGame.AddListener(Bounce);
        MultiplayerManager.Instance.OnConnected += LevelManager.Instance.SwitchToGame;
        MultiplayerManager.Instance.OnDisconnect += LevelManager.Instance.SwitchToMenu;
	}

	// Update is called once per frame
	void Update()
	{
		// if (Input.GetKeyDown(KeyCode.Space))
		// Debug.Log("Current State: " + CurrentState);
		// Move map into or out of view
		float targetPosition;
		if (CurrentState == State.TransitionToGame) targetPosition = 1;
		else if (CurrentState == State.TransitionToMenu) targetPosition = 0;
		else return;

		if ((targetPosition == 1 && mapPosition < 1) || (targetPosition == 0 && mapPosition > 0))
		{
			mapPosition += (targetPosition > mapPosition ? 1 : -1) * Time.deltaTime;
			float realPosition = mapAnimation.Evaluate(mapPosition) * 1000 - 1000;

			float mapY = 0;
			float menuY = 0;
			if (realPosition < -5)
			{
				mapY = realPosition + 5;
				// Stick to menu if ground is currently menu
				XRPlayerController.Main.transform.parent = menu.transform;
				playerFollowers.transform.parent = menu.transform;
			}
			else
			{
				menuY = -5 - realPosition;
				// Unstick from menu if ground is menu
				XRPlayerController.Main.transform.parent = menu.transform.parent;
				playerFollowers.transform.parent = menu.transform.parent;
			}
			map.transform.position = new Vector3(map.transform.position.x, mapY, map.transform.position.z);
			menu.transform.position = new Vector3(menu.transform.position.x, menuY, menu.transform.position.z);
		}

		// Stop transition if reached target
		if (CurrentState == State.TransitionToGame)
        {
			if (mapPosition > 1)
            {
                if (bouncing)
                {
                    bouncing = false;
					SwitchToMenu();
                }
                else
                {
                    CurrentState = State.Game;
                }
            }
		}
		else if (CurrentState == State.TransitionToMenu)
		{
			if (mapPosition < 0)
            {
                if (bouncing)
                {
                    bouncing = false;
					SwitchToGame();
                }
                else
                {
                    CurrentState = State.Menu;
                }
            }
		}
	}

	public void SwitchToGame()
	{
		if (CurrentState == State.Game || CurrentState == State.TransitionToGame) return;
		Debug.Log("Switch to game");

		XRPlayerController.Main.CharacterController.enabled = false; // need to disable to teleport

		// First rotate menu and player to face other side of map
		Vector3 targetDirection = new Vector3(MultiplayerManager.Instance.NetManager.IsHost ? -1 : 1, 0, 0);
		Vector3 currentDirection = XRPlayerController.Main.Camera.transform.forward;
		currentDirection.y = 0;
		float angleOffset = Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up);
		float currentAngle = menu.transform.rotation.eulerAngles.y;
		menu.transform.rotation = Quaternion.AngleAxis(currentAngle + angleOffset, Vector3.up);
		RenderSettings.skybox.SetFloat("_Rotation", (RenderSettings.skybox.GetFloat("_Rotation") - angleOffset) % 360);

		// Teleport menu and player to one of the 2 sides
		Vector3 targetPosition = new Vector3(MultiplayerManager.Instance.NetManager.IsHost ? 5 : -5, 0, 0);
		Vector3 offset = targetPosition - XRPlayerController.Main.transform.position;
		offset.y = 0;
		menu.transform.position += offset;

		XRPlayerController.Main.CharacterController.enabled = true;

		CurrentState = State.TransitionToGame;
	}

	public void SwitchToMenu()
	{
		if (CurrentState == State.Menu || CurrentState == State.TransitionToMenu) return;

		// Rotate menu so that player faces the title
		Vector3 currentDirection = menu.transform.forward;
		Vector3 targetDirection = XRPlayerController.Main.Camera.transform.forward;
		targetDirection.y = 0;
		float angleOffset = Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up);
		float currentAngle = menu.transform.rotation.eulerAngles.y;
		menu.transform.rotation = Quaternion.AngleAxis(currentAngle + angleOffset, Vector3.up);

		// Teleport menu to under where the player is at currently
		Vector3 playerPosition = XRPlayerController.Main.transform.position;
		menu.transform.position = new Vector3(playerPosition.x, menu.transform.position.y, playerPosition.z);

		CurrentState = State.TransitionToMenu;
	}

    // Transition to other state and back
    public void Bounce()
    {
        if (CurrentState == State.TransitionToGame || CurrentState == State.TransitionToMenu) return;
        if (CurrentState == State.Game) SwitchToMenu();
        if (CurrentState == State.Menu) SwitchToGame();
        bouncing = true;
    }


}
