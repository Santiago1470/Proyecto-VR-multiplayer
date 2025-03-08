using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Left Door Settings")]
    public Transform leftDoor;            // Puerta izquierda
    public Vector3 leftOpenPosition;      // Posición abierta de la puerta izquierda
    public Vector3 leftClosedPosition;    // Posición cerrada de la puerta izquierda

    [Header("Right Door Settings")]
    public Transform rightDoor;           // Puerta derecha
    public Vector3 rightOpenPosition;     // Posición abierta de la puerta derecha
    public Vector3 rightClosedPosition;   // Posición cerrada de la puerta derecha

    [Header("General Settings")]
    public float doorSpeed = 2f;          // Velocidad de movimiento de las puertas

    private bool isOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpen)
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

    // Movimiento suave de las puertas
    private IEnumerator MoveDoor(Transform door, Vector3 targetPosition)
    {
        while (Vector3.Distance(door.localPosition, targetPosition) > 0.01f)
        {
            door.localPosition = Vector3.Lerp(door.localPosition, targetPosition, Time.deltaTime * doorSpeed);
            yield return null;
        }
        door.localPosition = targetPosition;
    }
}

