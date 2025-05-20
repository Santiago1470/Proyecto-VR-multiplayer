using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SlidingDoorCar : NetworkBehaviour
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
    public float doorSpeed = 2f;
    public AudioClip sonidoApertura;

    // Variable de red para sincronizar el estado de las puertas
    private NetworkVariable<bool> netIsOpen = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && sonidoApertura != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse al evento de cambio del estado de las puertas
        netIsOpen.OnValueChanged += OnDoorStateChanged;

        // Si se conecta a una sesión donde las puertas ya están abiertas
        if (netIsOpen.Value)
        {
            // Colocar las puertas en posición abierta inmediatamente
            if (leftDoor != null)
                leftDoor.localPosition = leftOpenPosition;

            if (rightDoor != null)
                rightDoor.localPosition = rightOpenPosition;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Desuscribirse del evento
        netIsOpen.OnValueChanged -= OnDoorStateChanged;

        base.OnNetworkDespawn();
    }

    private void OnDoorStateChanged(bool oldValue, bool newValue)
    {
        // Cuando el estado de la puerta cambia en la red, animar localmente
        StopAllCoroutines();

        if (newValue) // Si se abrieron las puertas
        {
            // Reproducir sonido de apertura
            if (audioSource != null && sonidoApertura != null)
            {
                audioSource.PlayOneShot(sonidoApertura);
            }

            // Animar la apertura
            if (leftDoor != null)
                StartCoroutine(MoveDoor(leftDoor, leftOpenPosition));

            if (rightDoor != null)
                StartCoroutine(MoveDoor(rightDoor, rightOpenPosition));
        }
        else // Si se cerraron las puertas
        {
            // Animar el cierre
            if (leftDoor != null)
                StartCoroutine(MoveDoor(leftDoor, leftClosedPosition));

            if (rightDoor != null)
                StartCoroutine(MoveDoor(rightDoor, rightClosedPosition));
        }
    }

    // Movimiento suave de las puertas (se mantiene igual)
    private IEnumerator MoveDoor(Transform door, Vector3 targetPosition)
    {
        while (Vector3.Distance(door.localPosition, targetPosition) > 0.01f)
        {
            door.localPosition = Vector3.Lerp(door.localPosition, targetPosition, Time.deltaTime * doorSpeed);
            yield return null;
        }
        door.localPosition = targetPosition;
    }

    // Método público para abrir las puertas
    public void AbrirPuertas()
    {
        if (!netIsOpen.Value)
        {
            // Si somos el servidor, podemos cambiar el estado directamente
            if (IsServer)
            {
                netIsOpen.Value = true;
            }
            else
            {
                // Si no somos servidor, pedimos cambio a través de RPC
                AbrirPuertasServerRpc();
            }
        }
    }

    // Método público para cerrar las puertas (opcional)
    public void CerrarPuertas()
    {
        if (netIsOpen.Value)
        {
            if (IsServer)
            {
                netIsOpen.Value = false;
            }
            else
            {
                CerrarPuertasServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AbrirPuertasServerRpc()
    {
        if (!netIsOpen.Value)
        {
            netIsOpen.Value = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CerrarPuertasServerRpc()
    {
        if (netIsOpen.Value)
        {
            netIsOpen.Value = false;
        }
    }

    // Método para consultar si las puertas están abiertas
    public bool AreDoorsOpen()
    {
        return netIsOpen.Value;
    }

    // Los métodos OnTriggerEnter/Exit se mantienen comentados como en tu versión original
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Player") && !isOpen)
    //    {
    //        StopAllCoroutines();
    //        StartCoroutine(MoveDoor(leftDoor, leftOpenPosition));
    //        StartCoroutine(MoveDoor(rightDoor, rightOpenPosition));
    //        isOpen = true;
    //    }
    //}
    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("Player") && isOpen)
    //    {
    //        StopAllCoroutines();
    //        StartCoroutine(MoveDoor(leftDoor, leftClosedPosition));
    //        StartCoroutine(MoveDoor(rightDoor, rightClosedPosition));
    //        isOpen = false;
    //    }
    //}
}