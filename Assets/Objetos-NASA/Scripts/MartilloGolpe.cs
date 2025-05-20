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

    // Variable sincronizada para el estado del martillo
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse al evento de cambio de la variable de red
        netEstaEnMano.OnValueChanged += OnEstaEnManoChanged;
    }

    public override void OnNetworkDespawn()
    {
        // Desuscribirse del evento
        netEstaEnMano.OnValueChanged -= OnEstaEnManoChanged;

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
        if (IsOwner)
        {
            estaEnMano = true;
            netEstaEnMano.Value = true;
            Debug.Log("Martillo agarrado");
        }
    }

    private void OnSoltado(SelectExitEventArgs args)
    {
        if (IsOwner)
        {
            estaEnMano = false;
            netEstaEnMano.Value = false;
            Debug.Log("Martillo soltado");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Solo el dueño del martillo procesa las colisiones
        if (!IsOwner || !estaEnMano) return;

        if (collision.gameObject.CompareTag("Carro"))
        {
            float fuerzaImpacto = collision.relativeVelocity.magnitude;
            Debug.Log("Fuerza de impacto: " + fuerzaImpacto);

            if (fuerzaImpacto >= fuerzaMinimaReparar)
            {
                // Reproducir efectos localmente
                Vector3 puntoImpacto = collision.contacts[0].point;
                Vector3 normalImpacto = collision.contacts[0].normal;
                ReproducirEfectos(puntoImpacto, normalImpacto);

                // Notificar a todos los clientes para reproducir efectos
                ReproducirEfectosServerRpc(puntoImpacto, normalImpacto);

                // Reparar el carro a través de su NetworkBehaviour
                NetworkObject carroNetObj = collision.gameObject.GetComponent<NetworkObject>();
                if (carroNetObj != null)
                {
                    CarroRepair reparador = collision.gameObject.GetComponent<CarroRepair>();
                    if (reparador != null)
                    {
                        // Llamar al ServerRpc del carro para repararlo
                        reparador.RepararServerRpc();
                    }
                }
                else
                {
                    Debug.LogError("El carro no tiene componente NetworkObject!");
                }
            }
            else
            {
                Debug.Log("Golpe demasiado débil, no se repara.");
                MostrarMensajeGolpeDebil();
            }
        }
    }

    [ServerRpc]
    private void ReproducirEfectosServerRpc(Vector3 posicionImpacto, Vector3 normalImpacto)
    {
        // El servidor recibe esta llamada y luego la propaga a todos los clientes
        ReproducirEfectosClientRpc(posicionImpacto, normalImpacto);
    }

    [ClientRpc]
    private void ReproducirEfectosClientRpc(Vector3 posicionImpacto, Vector3 normalImpacto)
    {
        // No reproducir efectos de nuevo en el cliente que originó la colisión
        if (IsOwner) return;

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

    // Método opcional manual
    public void SetMartilloEnMano(bool enMano)
    {
        if (IsOwner)
        {
            estaEnMano = enMano;
            netEstaEnMano.Value = enMano;
        }
    }
}
