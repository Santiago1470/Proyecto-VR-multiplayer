using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class MartilloGolpe : NetworkBehaviour
{
    private bool estaEnMano = false;
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    [Header("Configuración de golpe")]
    public float fuerzaMinimaReparar = 1.0f;

    [Header("Efectos")]
    public AudioClip sonidoGolpe;
    public ParticleSystem efectoChispas;

    [Header("UI Feedback")]
    public TMP_Text textoGolpeDebil; // Asigna aquí el TextMeshPro "Golpea más fuerte"
    public float duracionMensaje = 2f; // Tiempo que se mostrará el mensaje

    private Coroutine mensajeCoroutine;
    private AudioSource audioSource;

    // Variable sincronizada para el estado del martillo (solo multijugador)
    private NetworkVariable<bool> netEstaEnMano = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnAgarrado);
            grabInteractable.selectExited.AddListener(OnSoltado);
        }

        if (textoGolpeDebil != null)
        {
            textoGolpeDebil.gameObject.SetActive(false); // Ocultar al inicio
        }
    }

    // Método para verificar si estamos en modo red activo
    private bool EsModoRed()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               IsSpawned;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse al evento de cambio de la variable de red solo en modo multijugador
        if (EsModoRed())
        {
            netEstaEnMano.OnValueChanged += OnEstaEnManoChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Desuscribirse del evento
        if (EsModoRed())
        {
            netEstaEnMano.OnValueChanged -= OnEstaEnManoChanged;
        }

        base.OnNetworkDespawn();
    }

    private void OnEstaEnManoChanged(bool previousValue, bool newValue)
    {
        // Actualizar la variable local cuando cambie el valor en la red
        estaEnMano = newValue;
        Debug.Log($"Estado del martillo sincronizado: {estaEnMano}");
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnAgarrado);
            grabInteractable.selectExited.RemoveListener(OnSoltado);
        }
    }

    private void OnAgarrado(SelectEnterEventArgs args)
    {
        estaEnMano = true;

        // Solo sincronizar en red si estamos en modo multijugador
        if (EsModoRed() && IsOwner)
        {
            netEstaEnMano.Value = true;
        }

        Debug.Log("Martillo agarrado");
    }

    private void OnSoltado(SelectExitEventArgs args)
    {
        estaEnMano = false;

        // Solo sincronizar en red si estamos en modo multijugador
        if (EsModoRed() && IsOwner)
        {
            netEstaEnMano.Value = false;
        }

        Debug.Log("Martillo soltado");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // En multijugador, solo el dueño procesa colisiones
        // En singleplayer, siempre procesamos si está en mano
        bool puedeProcesar = EsModoRed() ? (IsOwner && estaEnMano) : estaEnMano;

        if (!puedeProcesar) return;

        if (collision.gameObject.CompareTag("Carro"))
        {
            float fuerzaImpacto = collision.relativeVelocity.magnitude;
            Debug.Log($"[Cliente {(IsOwner ? "Owner" : "No Owner")}] Fuerza de impacto: {fuerzaImpacto}");

            if (fuerzaImpacto >= fuerzaMinimaReparar)
            {
                Vector3 puntoImpacto = collision.contacts[0].point;
                Vector3 normalImpacto = collision.contacts[0].normal;

                // Reproducir efectos localmente
                ReproducirEfectos(puntoImpacto, normalImpacto);

                if (EsModoRed())
                {
                    // Obtener el NetworkObject del carro para reparar
                    NetworkObject carroNetObj = collision.gameObject.GetComponent<NetworkObject>();
                    if (carroNetObj != null)
                    {
                        ulong carroNetworkId = carroNetObj.NetworkObjectId;

                        // Llamar al ServerRpc que maneja tanto efectos como reparación
                        ProcesarGolpeCarroServerRpc(carroNetworkId, puntoImpacto, normalImpacto, fuerzaImpacto);
                    }
                    else
                    {
                        Debug.LogError("El carro no tiene componente NetworkObject!");
                    }
                }
                else
                {
                    // Modo singleplayer: reparar directamente
                    CarroRepair reparador = collision.gameObject.GetComponent<CarroRepair>();
                    if (reparador != null)
                    {
                        reparador.Reparar();
                    }
                }
            }
            else
            {
                Debug.Log("Golpe demasiado débil, no se repara.");
                MostrarMensajeGolpeDebil();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ProcesarGolpeCarroServerRpc(ulong carroNetworkId, Vector3 posicionImpacto, Vector3 normalImpacto, float fuerza)
    {
        // Solo funciona en modo red y si somos el servidor
        if (!EsModoRed() || !IsServer) return;

        Debug.Log($"[Servidor] Procesando golpe en carro ID: {carroNetworkId}, Fuerza: {fuerza}");

        // Buscar el NetworkObject del carro por su ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(carroNetworkId, out NetworkObject carroNetObj))
        {
            // Reparar el carro
            CarroRepair reparador = carroNetObj.GetComponent<CarroRepair>();
            if (reparador != null)
            {
                Debug.Log("[Servidor] Reparando carro...");
                reparador.RepararServerRpc();
            }
            else
            {
                Debug.LogError("[Servidor] CarroRepair no encontrado en el carro!");
            }

            // Reproducir efectos en todos los clientes (excepto el que originó el golpe)
            ReproducirEfectosClientRpc(posicionImpacto, normalImpacto);
        }
        else
        {
            Debug.LogError($"[Servidor] No se encontró NetworkObject con ID: {carroNetworkId}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReproducirEfectosServerRpc(Vector3 posicionImpacto, Vector3 normalImpacto)
    {
        // Solo funciona en modo red y si somos el servidor
        if (!EsModoRed() || !IsServer) return;

        // El servidor recibe esta llamada y luego la propaga a todos los clientes
        ReproducirEfectosClientRpc(posicionImpacto, normalImpacto);
    }

    [ClientRpc]
    private void ReproducirEfectosClientRpc(Vector3 posicionImpacto, Vector3 normalImpacto)
    {
        // No reproducir efectos de nuevo en el cliente que originó la colisión
        if (!EsModoRed() || IsOwner) return;

        ReproducirEfectos(posicionImpacto, normalImpacto);
    }

    private void ReproducirEfectos(Vector3 posicionImpacto, Vector3 normalImpacto)
    {
        if (sonidoGolpe != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoGolpe);
        }

        if (efectoChispas != null)
        {
            ParticleSystem particulas = Instantiate(efectoChispas, posicionImpacto, Quaternion.LookRotation(normalImpacto));
            particulas.Play();
            Destroy(particulas.gameObject, 2f);
        }
    }

    private void MostrarMensajeGolpeDebil()
    {
        if (textoGolpeDebil == null) return;

        if (mensajeCoroutine != null)
            StopCoroutine(mensajeCoroutine);

        mensajeCoroutine = StartCoroutine(MostrarMensajeTemporal());
    }

    private IEnumerator MostrarMensajeTemporal()
    {
        textoGolpeDebil.gameObject.SetActive(true);
        yield return new WaitForSeconds(duracionMensaje);
        textoGolpeDebil.gameObject.SetActive(false);
    }

    // Método opcional manual que funciona en ambos modos
    public void SetMartilloEnMano(bool enMano)
    {
        estaEnMano = enMano;

        if (EsModoRed() && IsOwner)
        {
            netEstaEnMano.Value = enMano;
        }
    }

    // Método público para verificar si el martillo está en uso
    public bool EstaEnMano()
    {
        return estaEnMano;
    }

    // Método para obtener la fuerza mínima requerida (útil para UI)
    public float ObtenerFuerzaMinima()
    {
        return fuerzaMinimaReparar;
    }

    // Método de debug para verificar el estado del martillo
    public void DebugEstado()
    {
        Debug.Log($"[Martillo Debug] EsModoRed: {EsModoRed()}, IsOwner: {(EsModoRed() ? IsOwner.ToString() : "N/A")}, EstaEnMano: {estaEnMano}, IsSpawned: {(EsModoRed() ? IsSpawned.ToString() : "N/A")}");
    }

    // Método para forzar la sincronización del estado (útil para debugging)
    [ServerRpc(RequireOwnership = false)]
    public void ForzarSincronizacionServerRpc(bool nuevoEstado)
    {
        if (!EsModoRed() || !IsServer) return;

        Debug.Log($"[Servidor] Forzando sincronización del martillo: {nuevoEstado}");
        netEstaEnMano.Value = nuevoEstado;
    }
}