using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public AudioClip[] audioall;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void audioStart(int i)
    {
        try
        {
            audioSource.clip = audioall[i];
            audioSource.Play();
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void audioStop()
    {
        audioSource.Stop();
    }
}
