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
    private bool isAnimating = false; // Para evitar animaciones conflictivas

    // Estado local para singleplayer
    private bool isOpenLocal = false;

    // Método para verificar si estamos en modo red activo
    private bool EsModoRed()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               IsSpawned;
    }

    private void Start()
    {
        // Inicializar componentes
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && sonidoApertura != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configurar posiciones iniciales siempre
        ConfigurarPosicionesIniciales();

        // En singleplayer, inicializar inmediatamente
        if (!EsModoRedPotencial())
        {
            Debug.Log("[SlidingDoorCar] Modo Singleplayer detectado en Start");
        }
    }

    // Método adicional para detectar si hay potencial de red (NetworkManager existe)
    private bool EsModoRedPotencial()
    {
        return NetworkManager.Singleton != null;
    }

    private void ConfigurarPosicionesIniciales()
    {
        // Asegurar que las puertas estén en posición cerrada al inicio
        if (leftDoor != null)
            leftDoor.localPosition = leftClosedPosition;

        if (rightDoor != null)
            rightDoor.localPosition = rightClosedPosition;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[SlidingDoorCar] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, Modo Red Activo: {EsModoRed()}");

        // Solo suscribirse si realmente estamos en modo red activo
        if (EsModoRed())
        {
            netIsOpen.OnValueChanged += OnDoorStateChanged;

            Debug.Log($"[SlidingDoorCar] Estado inicial de puertas en red: {netIsOpen.Value}");

            // Sincronizar estado inicial
            if (netIsOpen.Value)
            {
                // Colocar las puertas en posición abierta inmediatamente
                if (leftDoor != null)
                    leftDoor.localPosition = leftOpenPosition;

                if (rightDoor != null)
                    rightDoor.localPosition = rightOpenPosition;

                isOpenLocal = true; // Sincronizar estado local también
            }
            else
            {
                ConfigurarPosicionesIniciales();
                isOpenLocal = false;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        // Desuscribirse del evento
        if (EsModoRed())
        {
            netIsOpen.OnValueChanged -= OnDoorStateChanged;
        }

        base.OnNetworkDespawn();
    }

    private void OnDoorStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[SlidingDoorCar] Estado de puertas cambió en red: {oldValue} -> {newValue}");

        // Sincronizar estado local
        isOpenLocal = newValue;

        // Animar según el nuevo estado
        StopAllCoroutines();
        isAnimating = false;

        if (newValue) // Si se abrieron las puertas
        {
            Debug.Log("[SlidingDoorCar] Abriendo puertas desde red...");
            AbrirPuertasLocal();
        }
        else // Si se cerraron las puertas
        {
            Debug.Log("[SlidingDoorCar] Cerrando puertas desde red...");
            CerrarPuertasLocal();
        }
    }

    // Método interno para abrir puertas localmente
    private void AbrirPuertasLocal()
    {
        if (isAnimating) return;

        isAnimating = true;

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

    // Método interno para cerrar puertas localmente
    private void CerrarPuertasLocal()
    {
        if (isAnimating) return;

        isAnimating = true;

        // Animar el cierre
        if (leftDoor != null)
            StartCoroutine(MoveDoor(leftDoor, leftClosedPosition));

        if (rightDoor != null)
            StartCoroutine(MoveDoor(rightDoor, rightClosedPosition));
    }

    // Movimiento suave de las puertas (mejorado)
    private IEnumerator MoveDoor(Transform door, Vector3 targetPosition)
    {
        if (door == null)
        {
            yield break;
        }

        Vector3 startPosition = door.localPosition;
        float elapsedTime = 0f;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);

        if (journeyLength < 0.01f)
        {
            isAnimating = false;
            yield break;
        }

        while (elapsedTime < 1f / doorSpeed)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = (elapsedTime * doorSpeed);

            door.localPosition = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            yield return null;
        }

        door.localPosition = targetPosition;
        isAnimating = false;

        Debug.Log($"[SlidingDoorCar] Puerta {door.name} movida a posición: {targetPosition}");
    }

    // Método público para abrir las puertas (funciona en ambos modos)
    public void AbrirPuertas()
    {
        Debug.Log($"[SlidingDoorCar] AbrirPuertas llamado - EsModoRed: {EsModoRed()}, NetworkManager existe: {NetworkManager.Singleton != null}");

        if (EsModoRed())
        {
            // Modo multijugador activo
            bool estadoActual = netIsOpen.Value;
            Debug.Log($"[SlidingDoorCar] Estado actual en red: {estadoActual}");

            if (!estadoActual)
            {
                if (IsServer)
                {
                    Debug.Log("[SlidingDoorCar] Servidor abriendo puertas directamente");
                    netIsOpen.Value = true;
                }
                else
                {
                    Debug.Log("[SlidingDoorCar] Cliente solicitando apertura al servidor");
                    AbrirPuertasServerRpc();
                }
            }
            else
            {
                Debug.Log("[SlidingDoorCar] Las puertas ya están abiertas en red");
            }
        }
        else
        {
            // Modo singleplayer o red no activa
            Debug.Log($"[SlidingDoorCar] Modo singleplayer - Estado actual: {isOpenLocal}");

            if (!isOpenLocal)
            {
                Debug.Log("[SlidingDoorCar] Abriendo puertas en singleplayer");
                isOpenLocal = true;
                AbrirPuertasLocal();
            }
            else
            {
                Debug.Log("[SlidingDoorCar] Las puertas ya están abiertas en singleplayer");
            }
        }
    }

    // Método público para cerrar las puertas (funciona en ambos modos)
    public void CerrarPuertas()
    {
        Debug.Log($"[SlidingDoorCar] CerrarPuertas llamado - EsModoRed: {EsModoRed()}");

        if (EsModoRed())
        {
            // Modo multijugador activo
            if (netIsOpen.Value)
            {
                if (IsServer)
                {
                    Debug.Log("[SlidingDoorCar] Servidor cerrando puertas directamente");
                    netIsOpen.Value = false;
                }
                else
                {
                    Debug.Log("[SlidingDoorCar] Cliente solicitando cierre al servidor");
                    CerrarPuertasServerRpc();
                }
            }
        }
        else
        {
            // Modo singleplayer o red no activa
            if (isOpenLocal)
            {
                Debug.Log("[SlidingDoorCar] Cerrando puertas en singleplayer");
                isOpenLocal = false;
                CerrarPuertasLocal();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AbrirPuertasServerRpc()
    {
        Debug.Log("[SlidingDoorCar] AbrirPuertasServerRpc recibido en servidor");

        if (!EsModoRed() || !IsServer)
        {
            Debug.LogError("[SlidingDoorCar] AbrirPuertasServerRpc llamado pero no estamos en servidor!");
            return;
        }

        if (!netIsOpen.Value)
        {
            Debug.Log("[SlidingDoorCar] Servidor estableciendo puertas como abiertas");
            netIsOpen.Value = true;
        }
        else
        {
            Debug.Log("[SlidingDoorCar] Las puertas ya estaban abiertas en el servidor");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CerrarPuertasServerRpc()
    {
        Debug.Log("[SlidingDoorCar] CerrarPuertasServerRpc recibido en servidor");

        if (!EsModoRed() || !IsServer)
        {
            Debug.LogError("[SlidingDoorCar] CerrarPuertasServerRpc llamado pero no estamos en servidor!");
            return;
        }

        if (netIsOpen.Value)
        {
            Debug.Log("[SlidingDoorCar] Servidor estableciendo puertas como cerradas");
            netIsOpen.Value = false;
        }
    }

    // Método para consultar si las puertas están abiertas
    public bool AreDoorsOpen()
    {
        if (EsModoRed())
        {
            return netIsOpen.Value;
        }
        else
        {
            // En singleplayer, usar estado local
            return isOpenLocal;
        }
    }

    // Método de debug para verificar el estado
    public void DebugEstado()
    {
        Debug.Log($"[SlidingDoorCar Debug] EsModoRed: {EsModoRed()}, IsServer: {(EsModoRed() ? IsServer.ToString() : "N/A")}, IsSpawned: {(EsModoRed() ? IsSpawned.ToString() : "N/A")}, PuertasAbiertas: {AreDoorsOpen()}, IsAnimating: {isAnimating}");

        if (leftDoor != null)
            Debug.Log($"Puerta izquierda posición: {leftDoor.localPosition}");
        if (rightDoor != null)
            Debug.Log($"Puerta derecha posición: {rightDoor.localPosition}");
    }

    // Método para forzar apertura (útil para testing)
    [ServerRpc(RequireOwnership = false)]
    public void ForzarAperturaServerRpc()
    {
        if (!EsModoRed() || !IsServer) return;

        Debug.Log("[SlidingDoorCar] Forzando apertura de puertas");
        netIsOpen.Value = true;
    }

    // Método para resetear puertas (útil para testing)
    public void ResetearPuertas()
    {
        if (EsModoRed())
        {
            if (IsServer)
            {
                netIsOpen.Value = false;
            }
        }
        else
        {
            StopAllCoroutines();
            isOpenLocal = false;
            ConfigurarPosicionesIniciales();
        }
    }

    // Método adicional para testing en ambos modos
    public void ForzarApertura()
    {
        if (EsModoRed())
        {
            if (IsServer)
            {
                netIsOpen.Value = true;
            }
            else
            {
                ForzarAperturaServerRpc();
            }
        }
        else
        {
            Debug.Log("[SlidingDoorCar] Forzando apertura en singleplayer");
            isOpenLocal = true;
            AbrirPuertasLocal();
        }
    }

    // Método para alternar estado (útil para testing)
    public void AlternarPuertas()
    {
        if (AreDoorsOpen())
        {
            CerrarPuertas();
        }
        else
        {
            AbrirPuertas();
        }
    }
}