using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class ChemicalTube : MonoBehaviour
{
    public enum ChemicalElement
    {
        Hydrogen,    // Color: Azul claro
        Oxygen,      // Color: Rojo
        Carbon,      // Color: Negro
        Nitrogen,    // Color: Amarillo
        Chlorine,    // Color: Verde
        Sulfur       // Color: Amarillo mostaza (nuevo elemento)
    }
    public ChemicalElement element;
    public Transform pourPoint;  // Punto de origen para el vertido (asegúrate de orientarlo correctamente en el editor)
    public ParticleSystem pourEffect;

    [Header("Configuración de Vertido")]
    [Range(0, 45)]
    public float pourAngleMin = 0f;
    [Range(0, 45)]
    public float pourAngleMax = 45f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isPouring = false;
    private Coroutine pourRoutine = null;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void Start()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void Update()
    {
        if (grabInteractable.isSelected)
        {
            // Calculamos el ángulo entre la dirección hacia arriba del objeto y el Vector3.up (positivo siempre)
            float angle = Vector3.Angle(transform.up, Vector3.up);

            // Si el ángulo está dentro del rango y no estamos ya vertiendo...
            if (angle >= pourAngleMin && angle <= pourAngleMax && !isPouring)
            {
                StartPouring();
            }
            // Si se sale del rango y se estaba vertiendo...
            else if ((angle < pourAngleMin || angle > pourAngleMax) && isPouring)
            {
                StopPouring();
            }
        }
    }

    void StartPouring()
    {
        isPouring = true;

        if (pourEffect != null)
        {
            // Configuramos posición y orientación del efecto de partículas en el punto de vertido
            pourEffect.transform.position = pourPoint.position;
            Quaternion pourRotation = pourPoint.rotation;
            pourRotation *= Quaternion.Euler(0f, 0f, 0f);
            pourEffect.transform.rotation = pourRotation;
            pourEffect.Play();
        }

        // Iniciamos la coroutine que registrará el vertido (únicamente una vez)
        if (pourRoutine == null)
            pourRoutine = StartCoroutine(PourRoutine());
    }

    IEnumerator PourRoutine()
    {
        // Una única llamada para registrar el elemento al comenzar el vertido:
        if (Physics.Raycast(pourPoint.position, pourPoint.forward, out RaycastHit hit, 2f))
        {
            ReactionContainer targetContainer = hit.collider.GetComponent<ReactionContainer>();
            if (targetContainer != null)
            {
                targetContainer.RegisterPour(element);
            }
        }

        // Esperamos mientras se mantenga el estado de vertido.
        // Esta espera evita que se acumulen más elementos durante un solo evento.
        while(isPouring)
        {
            yield return null;
        }
        pourRoutine = null;
    }

    void StopPouring()
    {
        isPouring = false;

        if (pourEffect != null)
            pourEffect.Stop();

        if (pourRoutine != null)
        {
            StopCoroutine(pourRoutine);
            pourRoutine = null;
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // Lógica opcional al agarrar el objeto
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // Si se deja de verter, se detiene el vertido activo.
        if (isPouring)
            StopPouring();
    }


}
