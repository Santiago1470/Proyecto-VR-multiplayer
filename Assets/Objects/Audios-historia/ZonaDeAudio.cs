using UnityEngine;

public class ZonaDeAudio : MonoBehaviour
{
    public AudioSource audioSource;
    private bool haComenzado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!audioSource.isPlaying)
            {
                if (!haComenzado)
                {
                    // Primera vez que se entra: reproducir desde el principio
                    audioSource.Play();
                    haComenzado = true;
                }
                else
                {
                    // Ya había comenzado: reanudar desde donde se pausó
                    audioSource.UnPause();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }
}

