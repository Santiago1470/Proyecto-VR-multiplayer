using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    private AudioSource audioSource;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // Agregamos automáticamente un AudioSource si no hay
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
            }
        }
    }

    private void ReproducirEfectos(Vector3 posicionImpacto, Vector3 normalImpacto)
    {
        // Reproducir sonido
        if (sonidoGolpe != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoGolpe);
        }

        // Instanciar partículas
        if (efectoChispas != null)
        {
            ParticleSystem particulas = Instantiate(efectoChispas, posicionImpacto, Quaternion.LookRotation(normalImpacto));
            particulas.Play();
            Destroy(particulas.gameObject, 2f); // destruir las partículas después de 2 segundos
        }
    }

    // Método opcional manual
    public void SetMartilloEnMano(bool enMano)
    {
        estaEnMano = enMano;
    }
}
