using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTest : MonoBehaviour
{
    public AudioClip music1; 
    public AudioClip music2; 
    public AudioClip music3; 
    public AudioClip music4; 
    public AudioClip music5; 
    public AudioClip music6; 
    public AudioClip music7; 
    public AudioClip music8; 
    public AudioClip music9;
    public AudioClip music10;
    public AudioClip music11;
    private AudioSource player;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<AudioSource>();
        player.clip = music1;
        player.clip = music2;
        player.clip = music3;
        player.clip = music4;
        player.clip = music5;
        player.clip = music6;
        player.clip = music7;
        player.clip = music8;
        player.clip = music9;
        player.volume = 0.5f;
        player.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.playOnAwake = true;
            if(player.isPlaying)
                {
                player.Pause ();
            }
            else
            {
                player.UnPause();
            }
        }
    }
}
