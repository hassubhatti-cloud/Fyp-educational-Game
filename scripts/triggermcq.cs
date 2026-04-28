using UnityEngine;
using System.Collections;
using TMPro;

public class CarTriggerHandler : MonoBehaviour
{
    private MCQManager mcqManager;
    public Collider col;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public float pickupVolume = 0.7f;

    private bool hasTriggered = false;

    void Start()
    {
        mcqManager = FindObjectOfType<MCQManager>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Car") && CompareTag("PytPoints"))
        {
            if (mcqManager == null)
            {
                Debug.LogWarning("MCQManager not found!");
                return;
            }

            hasTriggered = true;

            // Play pickup audio
            PlayPickupAudio();

            // Show MCQ
            mcqManager.TriggerMCQs();

            // Start cleanup AFTER short delay
            StartCoroutine(HidePickupObjectafterpickup());
        }
    }

    void PlayPickupAudio()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource not assigned in Inspector!");
            return;
        }
        
        if (pickupSound == null)
        {
            Debug.LogWarning("Pickup sound not assigned in Inspector!");
            return;
        }
        
        audioSource.PlayOneShot(pickupSound, pickupVolume);
        Debug.Log("Pickup audio played");
    }

    IEnumerator HidePickupObjectafterpickup()
    {
        // Prevent retrigger
        if (col != null)
            col.enabled = false;

        // Wait a moment so MCQ panel appears first
        yield return new WaitForSeconds(0.01f);

        Transform parent = col != null ? col.transform.parent : transform;

        // Disable all visuals
        foreach (MeshRenderer r in parent.GetComponentsInChildren<MeshRenderer>())
            r.enabled = false;

        foreach (TextMeshPro t in parent.GetComponentsInChildren<TextMeshPro>())
            t.enabled = false;

        foreach (TextMeshProUGUI t in parent.GetComponentsInChildren<TextMeshProUGUI>())
            t.enabled = false;
    }
}