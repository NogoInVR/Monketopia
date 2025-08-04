using UnityEngine;

public class PlaySoundOnTag : MonoBehaviour
{
    [Header("Tag to detect")]
    public string tagToDetect = "Player"; // Editable in Inspector

    [Header("Sound settings")]
    public AudioClip soundToPlay;
    public AudioSource audioSource;

    void Start()
    {
        // If no AudioSource is set, try to find one on this GameObject
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource found on object " + gameObject.name);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(tagToDetect))
        {
            PlaySound();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagToDetect))
        {
            PlaySound();
        }
    }

    void PlaySound()
    {
        if (audioSource != null && soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
}
