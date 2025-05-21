using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyItemCompletionUI : MonoBehaviour
{
    [Header("Key Item Settings")]
    public Transform[] keyItemSockets; // Transforms donde deben colocarse los objetos
    public string[] validKeyTags; // Tags válidas
    public float detectionRadius = 0.1f;

    [Header("UI Settings")]
    public GameObject completionCanvas; // Canvas que contiene el mensaje

    [Header("Particle Effects")]
    public ParticleSystem particleEffect1; // Primer sistema de partículas
    public ParticleSystem particleEffect2; // Segundo sistema de partículas

    private bool isCompleted = false;

    private void Start()
    {
        if (completionCanvas != null)
            completionCanvas.SetActive(false);

        if (particleEffect1 != null)
            particleEffect1.Stop();

        if (particleEffect2 != null)
            particleEffect2.Stop();

        InvokeRepeating("CheckKeyItems", 0f, 0.5f); // Verificación periódica
    }

    private void CheckKeyItems()
    {
        bool allSocketsHaveItems = true;

        foreach (Transform socket in keyItemSockets)
        {
            bool socketHasValidItem = false;

            Collider[] nearbyObjects = Physics.OverlapSphere(socket.position, detectionRadius);
            foreach (Collider col in nearbyObjects)
            {
                if (IsValidKeyItem(col.gameObject))
                {
                    socketHasValidItem = true;
                    break;
                }
            }

            if (!socketHasValidItem)
            {
                allSocketsHaveItems = false;
                break;
            }
        }

        if (allSocketsHaveItems && !isCompleted)
        {
            ShowCompletionCanvas();
            isCompleted = true;
        }
        else if (!allSocketsHaveItems && isCompleted)
        {
            HideCompletionCanvas();
            isCompleted = false;
        }
    }

    private bool IsValidKeyItem(GameObject item)
    {
        foreach (string tag in validKeyTags)
        {
            if (item.CompareTag(tag))
                return true;
        }
        return false;
    }

    private void ShowCompletionCanvas()
    {
        if (completionCanvas != null)
            completionCanvas.SetActive(true);

        if (particleEffect1 != null && !particleEffect1.isPlaying)
            particleEffect1.Play();

        if (particleEffect2 != null && !particleEffect2.isPlaying)
            particleEffect2.Play();
    }

    private void HideCompletionCanvas()
    {
        if (completionCanvas != null)
            completionCanvas.SetActive(false);

        if (particleEffect1 != null && particleEffect1.isPlaying)
            particleEffect1.Stop();

        if (particleEffect2 != null && particleEffect2.isPlaying)
            particleEffect2.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (keyItemSockets != null)
        {
            foreach (Transform socket in keyItemSockets)
            {
                if (socket != null)
                    Gizmos.DrawWireSphere(socket.position, detectionRadius);
            }
        }
    }
}
