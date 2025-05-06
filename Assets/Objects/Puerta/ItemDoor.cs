using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ItemDoor : MonoBehaviour
{
    [Header("Left Door Settings")]
    public Transform leftDoor;
    public Vector3 leftOpenPosition;
    public Vector3 leftClosedPosition;

    [Header("Right Door Settings")]
    public Transform rightDoor;
    public Vector3 rightOpenPosition;
    public Vector3 rightClosedPosition;

    [Header("General Settings")]
    public float doorSpeed = 2f;
    
    [Header("Key Items Settings")]
    public Transform[] keyItemSockets; // Transform de los puntos donde se deben colocar los objetos
    public string[] validKeyTags; // Tags de los objetos válidos para abrir la puerta
    public float detectionRadius = 0.1f; // Radio para detectar si el objeto está cerca del socket
    
    private bool isOpen = false;
    private bool canOpenDoor = false;
    private List<GameObject> detectedKeyItems = new List<GameObject>();

    private void Start()
    {
        // Verificar estado inicial
        InvokeRepeating("CheckKeyItems", 0f, 0.5f); // Verificar periódicamente
    }

    private void CheckKeyItems()
    {
        // Limpiar la lista actual
        detectedKeyItems.Clear();
        
        // Si no hay sockets configurados, permitir apertura por defecto
        if (keyItemSockets == null || keyItemSockets.Length == 0)
        {
            canOpenDoor = true;
            return;
        }

        // Verificar cada socket
        bool allSocketsHaveItems = true;
        
        foreach (Transform socket in keyItemSockets)
        {
            bool socketHasValidItem = false;
            
            // Buscar objetos cercanos con los tags válidos
            Collider[] nearbyObjects = Physics.OverlapSphere(socket.position, detectionRadius);
            foreach (Collider col in nearbyObjects)
            {
                if (IsValidKeyItem(col.gameObject))
                {
                    socketHasValidItem = true;
                    detectedKeyItems.Add(col.gameObject);
                    break;
                }
            }
            
            if (!socketHasValidItem)
            {
                allSocketsHaveItems = false;
                break;
            }
        }
        
        // Actualizar el estado
        bool previousState = canOpenDoor;
        canOpenDoor = allSocketsHaveItems;
        
        // Si el estado cambió
        if (previousState != canOpenDoor)
        {
            if (!canOpenDoor && isOpen)
            {
                // Si ya no se puede mantener la puerta abierta, cerrarla
                CloseDoorsImmediate();
            }
        }
    }

    private bool IsValidKeyItem(GameObject item)
    {
        // Verificar si el objeto tiene alguno de los tags válidos
        if (validKeyTags != null && validKeyTags.Length > 0)
        {
            foreach (string tag in validKeyTags)
            {
                if (item.CompareTag(tag))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void CloseDoorsImmediate()
    {
        StopAllCoroutines();
        StartCoroutine(MoveDoor(leftDoor, leftClosedPosition));
        StartCoroutine(MoveDoor(rightDoor, rightClosedPosition));
        isOpen = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo abrir si el jugador se acerca Y se pueden abrir las puertas
        if (other.CompareTag("Player") && !isOpen && canOpenDoor)
        {
            StopAllCoroutines();
            StartCoroutine(MoveDoor(leftDoor, leftOpenPosition));
            StartCoroutine(MoveDoor(rightDoor, rightOpenPosition));
            isOpen = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isOpen)
        {
            StopAllCoroutines();
            StartCoroutine(MoveDoor(leftDoor, leftClosedPosition));
            StartCoroutine(MoveDoor(rightDoor, rightClosedPosition));
            isOpen = false;
        }
    }

    private IEnumerator MoveDoor(Transform door, Vector3 targetPosition)
    {
        while (Vector3.Distance(door.localPosition, targetPosition) > 0.01f)
        {
            door.localPosition = Vector3.Lerp(door.localPosition, targetPosition, Time.deltaTime * doorSpeed);
            yield return null;
        }
        door.localPosition = targetPosition;
    }

    // Método auxiliar para visualizar el radio de detección en el editor
    private void OnDrawGizmosSelected()
    {
        if (keyItemSockets != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform socket in keyItemSockets)
            {
                if (socket != null)
                {
                    Gizmos.DrawWireSphere(socket.position, detectionRadius);
                }
            }
        }
    }

    public bool CanOpenDoor()
    {
        return canOpenDoor;
    }

}