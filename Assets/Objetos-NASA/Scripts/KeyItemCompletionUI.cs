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

    private bool isCompleted = false;

    private void Start()
    {
        if (completionCanvas != null)
            completionCanvas.SetActive(false); // Ocultar al inicio

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
    }

    private void HideCompletionCanvas()
    {
        if (completionCanvas != null)
            completionCanvas.SetActive(false);
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
