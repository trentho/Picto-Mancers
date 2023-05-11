using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/*

The SpellManager class is responsible for tracking all possible spells and storing the currently
selected spell. SpellManager also contains functionality for spawning spells.

When a new spell behavior is created, make sure to create a scriptable object of it and add it
to SpellManager's SpellTypes list.

*/

public class SpellManager : NetworkBehaviour
{
	[Tooltip("Base spell prefab to spawn.")] public GameObject spellBase;
	[Tooltip("All different spells available for the player to cast.")] public List<SpellBehavior> spellTypes;
	[Tooltip("All different spells available for the player to cast.")] public List<SpellBehavior> spellDeck;
	// [Tooltip("Currently selected spell type.")] public SpellBehavior selectedSpell;

	public static SpellManager Instance { get; private set; }

	// Start is called before the first frame update
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;

			//initialize spell deck
			shuffleDeck();
		}
		else
		{
			Debug.LogWarning("Multiple instances of singleton SpellManager");
		}
	}
	void Start()
	{
		MultiplayerManager.Instance.OnConnected += shuffleDeck;
	}
	public void shuffleDeck()
	{
		for (int i = 0; i < spellDeck.Count; i++)
		{
			spellDeck[i] = spellTypes[Random.Range(0, spellTypes.Count)];
		}
	}
	// public bool HasSelectedSpell()
	// {
	//     return spellDeck.Contains(selectedSpell);
	// }
	public bool HasSpell(int spellIndex)
	{
		return isValidSpell(spellIndex) && spellDeck.Contains(spellTypes[spellIndex]);
	}
	public bool isValidSpell(int spellIndex)
	{
		return spellIndex >= 0 && spellIndex < spellTypes.Count;
	}

	// Called on client to cast a spell
	public void CastSpell(
		int spellIndex, Vector3 position, Vector3 direction, Vector3 endPosition,
		XRPlayerController.Hand hand)
	{
		// if (!HasSelectedSpell()) selectedSpell = null;
		// if (selectedSpell == null) return;


		//get global index for selectedSpell
		// int selectedSpellIndex = spellTypes.IndexOf(selectedSpell);
		if (isValidSpell(spellIndex))
		{
			if (MultiplayerManager.Instance.IsConnected)
			{
				SpawnSpellServerRpc(
					spellIndex, position, direction, endPosition,
					NetworkManager.Singleton.IsServer, hand);
			}
			else
			{
				SpawnSpell(
					spellIndex, position, direction, endPosition,
					NetworkManager.Singleton.IsServer, hand);
			}
		}
		else
		{
			Debug.LogWarning("Tried to cast invalid spell: " + spellIndex);
		}
	}

	// Takes a spell from the hand. Returns true if success and false if spell is not in hand
	// Called on client
	public bool SelectSpell(int spellIndex)
	{
		if (HasSpell(spellIndex))
		{
			//exhaust chosen spell card, do mana here
			spellDeck[spellDeck.IndexOf(spellTypes[spellIndex])] = spellTypes[Random.Range(0, spellTypes.Count)];
			return true;
		}
		else
		{
			return false;
		}
	}

	// Gives a spell to one of the players. 
	// If giftToServer is true then gives the spell to the server. Otherwise to client
	[ClientRpc]
	public void GiftSpellClientRpc(bool giftToServer, int spellIndex, XRPlayerController.Hand hand)
	{
		if (giftToServer == IsServer)
		{
			bool canGiftSpell(XRPlayerHand hand) =>
				hand.State != XRPlayerHand.InteractionState.Holding &&
				hand.State != XRPlayerHand.InteractionState.Drawing;

			var xr = XRPlayerController.Main;

			if (canGiftSpell(xr.GetHand(hand)))
			{
				xr.GetHand(hand).Caster.HoldSpell(spellIndex);
			}
			else if (canGiftSpell(xr.GetHand(hand.Other())))
			{ // Give to other hand if main hand doesn't work
				xr.GetHand(hand.Other()).Caster.HoldSpell(spellIndex);
			}
		}
	}

	// Called by client to ask server to spawn a spell
	[ServerRpc(RequireOwnership = false)]
	public void SpawnSpellServerRpc(
		int spellIndex, Vector3 position, Vector3 direction, Vector3 endPosition, bool isServer, XRPlayerController.Hand hand
	) => SpawnSpell(spellIndex, position, direction, endPosition, isServer, hand);

	// Spawns a spell. Runs on server or if offline
	public void SpawnSpell(
		int spellIndex, Vector3 position, Vector3 direction, Vector3 endPosition, bool isServer, XRPlayerController.Hand hand)
	{
		Debug.Log("SpawnSpell " + spellIndex);
		bool isConnected = MultiplayerManager.Instance.IsConnected;
		Debug.Assert(IsServer || !isConnected);

		if (spellTypes[spellIndex].instantaneousTravel) position = endPosition; // Instantaneous spawn location
		SpellBase spell = Instantiate(spellBase, position, new Quaternion()).GetComponent<SpellBase>();

		if (isConnected)
		{
			spell.GetComponent<NetworkObject>().Spawn();
			spell.SpawnSpellClientRpc(spellIndex);
		}
		else
		{
			spell.SpawnSpell(spellIndex);
		}
		
		spell.Initialize(spellIndex, direction, isServer, hand);



		// spell.spellIndex.Value = spellIndex;
		// spell.direction = direction;
		// spell.endPosition = endPosition;
		// spell.isServerPlayer = isServer;
		// spell.hand = hand;
		// Debug.Log(spell.GetComponent<Rigidbody>().isKinematic);
		// spell.OnNetworkSpawn();
	}

}
