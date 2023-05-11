using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*

SpellBehavior is an abstract class that we can extend from to make different spell
behaviors. Each new SpellBehavior we create must have definitions of each of the 
below functions. Each behavior also needs a line like this:

[CreateAssetMenu(menuName = "SpellBehaviors/SpellBehaviorName")]

This allows us to create a scriptable object in the asset directory. From there, we
can assign each spell a unique speed or mana cost, etc.

When a new spell behavior is created, make sure to create a scriptable object of it and add it
to SpellManager's SpellTypes list.

*/

public abstract class SpellBehavior : ScriptableObject
{
    [HideInInspector] public SpellBase owner;
    public LayerMask playerLayer;
    public LayerMask rayCastHitLayer;
    public bool instantaneousTravel;
    public bool destroyOnHit;
    public float upTime = 5;
    public float speed; // For now this is used for distance when instantaneous and not arc
    public bool useGravity;
    public bool arcTrajectory;
    public int damage;
    public float manaCost;
    public string spellName;
    public string spellDesc;
    public Material spellSymbol;
    public GameObject createParticles;
    public GameObject updateParticles;
    public GameObject destroyParticles;
    public AudioClip castSound;
    public AudioClip landSound;
    public abstract void spellStartAction();
    public abstract void spellUpdateAction();
    public abstract void spellCollideAction(Collider other);
    public abstract void spellDestroyAction();

    public bool isServer()
    {
        return owner.isServerPlayer;
    }
}
