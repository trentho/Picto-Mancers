using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SoundManager : MonoBehaviour
{
    public AudioSource audioSource0;
    public AudioSource audioSource1;
    public static SoundManager Instance { get; private set; }
    public AudioClip takeDamageSound;
    public AudioClip dealDamageSound;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource0 = GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("Multiple instances of singleton SpellManager");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playSound(AudioClip sound, bool mainSource = true)
    {
        if(mainSource)
        {
            audioSource0.clip = sound;
            audioSource0.Play();
        } else {
            audioSource1.clip = sound;
            audioSource1.Play();
        }
    }

    public void playDamageSound(bool selfTookDamage)
    {
        //server took damage
        if (selfTookDamage)
        {
            playSound(takeDamageSound, false);
        }
        //client took damage
        else 
        {
            playSound(dealDamageSound, false);
        }
    }

    //todo: may need some way to make opponent moves quieter than your own
}
