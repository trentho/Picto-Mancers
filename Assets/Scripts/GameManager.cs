using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<int> serverHealth;
    public NetworkVariable<int> clientHealth;

    public int rematchCount = 0;
    //called when someone dies, whether the server/host won is passed in
    public UnityEvent<bool> gameEnd;
    //called when both players hit rematch on the game end screen
    public UnityEvent restartGame;
    public int maxHealth = 5;
    // public int maxMana = 10;
    // [SerializeField]
    // private int startingMana = 5;
    // [SerializeField]
    // private int manaPerSecond = 1;
    // private float lastMana;
    // public int mana;
    public static GameManager Instance { get; private set; }

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
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ServerResetHealth();
            ResetRematchCount();
        }
        // ResetMana();
    }
    void ServerResetHealth()
    {
        serverHealth.Value = maxHealth;
        clientHealth.Value = maxHealth;
    }
    void ResetRematchCount(){
        rematchCount = 0;
    }
    // void ResetMana()
    // {
    //     mana = startingMana;
    //     lastMana = Time.time;
    // }
    [ClientRpc]
    void EndGameClientRpc(bool winner)
    {
        gameEnd.Invoke(winner);
    }
    [ClientRpc]
    void RestartGameClientRpc()
    {
        // mana = startingMana;
        restartGame.Invoke();
    }
    void ServerRestartGame()
    {
        // mana = startingMana;
        ServerResetHealth();
        restartGame.Invoke();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RematchServerRpc()
    {
        rematchCount++;
        if (rematchCount == 2)
        {
            ResetRematchCount();
            RestartGameClientRpc();
            ServerRestartGame();
        }
    }
    [ClientRpc] 
    public void playDamageSoundClientRpc(bool serverTookDamage)
    {
        SoundManager.Instance.playDamageSound(serverTookDamage == IsServer);
    }
    public void changeHealth(int value, bool isServer)
    {
        //SoundManager.Instance.playDamageSound(isServer);
        if (serverHealth.Value <= 0 || clientHealth.Value <= 0)
        {
            return;
        }
        if (isServer)
        {
            serverHealth.Value += value;
            playDamageSoundClientRpc(true);
        }
        else
        {
            clientHealth.Value += value;
            playDamageSoundClientRpc(false);
        }
        if (serverHealth.Value <= 0)
        {
            EndGameClientRpc(false);
            gameEnd.Invoke(false);
        }
        else if (clientHealth.Value <= 0)
        {
            EndGameClientRpc(true);
            gameEnd.Invoke(true);
        }
    }
    public void Update()
    {
        // if (Time.time - lastMana >= 1)
        // {
        //     lastMana = Time.time;
        //     if (mana < 10)
        //     {
        //         mana++;
        //     }
        // }
    }
    // public bool useMana(int spellCost)
    // {
    //     if (mana < spellCost)
    //     {
    //         return false;
    //     }
    //     else
    //     {
    //         mana -= spellCost;
    //         return true;
    //     }
    // }
}
