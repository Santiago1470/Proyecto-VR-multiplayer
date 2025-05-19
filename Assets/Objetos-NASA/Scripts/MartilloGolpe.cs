using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using System.Collections;

public class MartilloGolpe : MonoBehaviour
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
    public TMP_Text textoGolpeDebil; // Asigna aquí el TextMeshPro “Golpea más fuerte”
    public float duracionMensaje = 2f; // Tiempo que se mostrará el mensaje

    private Coroutine mensajeCoroutine;
    private AudioSource audioSource;

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
        Debug.Log("Martillo agarrado");
    }

    private void OnSoltado(SelectExitEventArgs args)
    {
        estaEnMano = false;
        Debug.Log("Martillo soltado");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!estaEnMano) return;

        if (collision.gameObject.CompareTag("Carro"))
        {
            float fuerzaImpacto = collision.relativeVelocity.magnitude;
            Debug.Log("Fuerza de impacto: " + fuerzaImpacto);

            if (fuerzaImpacto >= fuerzaMinimaReparar)
            {
                // Reproducir efectos
                ReproducirEfectos(collision.contacts[0].point, collision.contacts[0].normal);

                // Reparar el carro
                CarroRepair reparador = collision.gameObject.GetComponent<CarroRepair>();
                if (reparador != null)
                {
                    reparador.Reparar();
                }
            }
            else
            {
                Debug.Log("Golpe demasiado débil, no se repara.");
                MostrarMensajeGolpeDebil();
            }
        }
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
        estaEnMano = enMano;
    }
}
