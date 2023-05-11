using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/*

SpellBase is the basis for spells cast in this game. When created, the SpellBase
object is assigned a SpellBehavior scriptable object. To give each spell unique
behavior, the instantiated SpellBase object will then call start, update, and
destruction functions that are defined in its assigned SpellBehavior field.

*/

public class SpellBase : NetworkBehaviour
{
    // All of these variables are only set on the server (and thus cannot be accessed on the client)
    // The client must perform a ServerRpc in order to do stuff with these variables
    // In general though only the server should need to use these
    public int spellIndex;
    public SpellBehavior behavior;
    public Vector3 direction;
    public bool isServerPlayer; // whether the spell was spawned by the player that is the server
    public XRPlayerController.Hand hand; // The hand this spell was cast from
    new public Rigidbody rigidbody { get; private set; }
    private AudioSource audioSource; // TODO: Do we still need this?
    private float spawnTime;

    //todo: 
    /*
        spells have option to be instantaneous, ie spawn location at end of ray
        modifiable gravity and acceleration
        change raycast line stats depending on stats of spell
    */

    // Start is called before the first frame update
    void Start()
    {
        // audioSource = GetComponent<AudioSource>();
    }

    // Can only run logic when we are the server or when we are offline
    bool CanRunLogic { get => IsServer || !NetworkManager.Singleton.IsConnectedClient; }

    // Initializes the members (Runs on server or when offline)
    public void Initialize(int spellIndex, Vector3 direction, bool isServer, XRPlayerController.Hand hand)
    {
        Debug.Log("Initialize IsServer: " + IsServer);
        Debug.Assert(CanRunLogic);

        this.spellIndex = spellIndex;
        this.direction = direction;
        this.isServerPlayer = isServer;
        this.hand = hand;

        behavior = Instantiate(SpellManager.Instance.spellTypes[spellIndex]);

        // get stats from assigned behavior
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.isKinematic = false;
        rigidbody.velocity = direction * behavior.speed;
        rigidbody.useGravity = behavior.useGravity;

        // give spell behavior reference to the spell object
        behavior.owner = this;

        spawnTime = Time.time;

        behavior.spellStartAction();
    }


    // Plays the spawning sound and particles (runs on clients)
    [ClientRpc] 
    public void SpawnSpellClientRpc(int spellIndex) => SpawnSpell(spellIndex);
    public void SpawnSpell(int spellIndex)
    {
        // Can't access this.behavior on client
        var behavior = SpellManager.Instance.spellTypes[spellIndex];

        //play spell cast sound
        SoundManager.Instance.playSound(behavior.castSound);

        //create particle systems to play
        Instantiate(behavior.createParticles, transform.position, new Quaternion());
        Instantiate(behavior.updateParticles, transform.position, new Quaternion()).transform.parent = this.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!CanRunLogic) return;

        if (Time.time - spawnTime > behavior.upTime)
            DestroySpell();

        behavior.spellUpdateAction();
    }

    //destroy spell when collides with something
    void OnTriggerEnter(Collider other)
    {
        if (!CanRunLogic) return;

        var collidedPlayer = other.GetComponentInParent<NetworkPlayer>();
        if (collidedPlayer != null && collidedPlayer.IsOwnedByServer == isServerPlayer)
        {
            return; // Ignore if collided with the person that cast the spell
        }

        GetComponent<SphereCollider>().enabled = false;
        behavior.spellCollideAction(other);
        if (behavior.destroyOnHit)
            DestroySpell();

    }

    // Destroys the spell. Run on server
    public void DestroySpell()
    {
        Debug.Assert(CanRunLogic);

        behavior.spellDestroyAction();
        if (MultiplayerManager.Instance.IsConnected)
        {
            LandSpellClientRPC(spellIndex, transform.position);
        }
        else
        {
            LandSpell(spellIndex, transform.position);
        }

        Destroy(gameObject);
    }

    // Plays the destroy spell sound and particles
    [ClientRpc]
    void LandSpellClientRPC(int spellIndex, Vector3 serverPosition) => LandSpell(spellIndex, serverPosition);
    void LandSpell(int spellIndex, Vector3 serverPosition)
    {
        // Can't access this.behavior on client
        var behavior = SpellManager.Instance.spellTypes[spellIndex];

        Instantiate(behavior.destroyParticles, serverPosition, new Quaternion());
        if (behavior.landSound != null)
            SoundManager.Instance.playSound(behavior.landSound);

        //base.OnDestroy();
    }
}
