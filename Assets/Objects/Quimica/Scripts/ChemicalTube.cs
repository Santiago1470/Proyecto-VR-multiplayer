using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class ChemicalTube : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor homeSocket;
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
    if (!grabInteractable.isSelected) return;

    // Calcula el ángulo firmado entre el up del objeto y el up del mundo,
    // usando como eje de rotación el forward del objeto (inclinación izquierda/derecha)
    float sideAngle = Vector3.SignedAngle(transform.up, Vector3.up, transform.forward);

    // Si el ángulo está en el rango [min,max] a la derecha...
    bool rightTilt = sideAngle >= pourAngleMin && sideAngle <= pourAngleMax;
    // ...o en el rango [-max,-min] a la izquierda
    bool leftTilt  = sideAngle <= -pourAngleMin && sideAngle >= -pourAngleMax;

    if ((rightTilt || leftTilt) && !isPouring)
    {
        StartPouring();
    }
    else if (!(rightTilt || leftTilt) && isPouring)
    {
        StopPouring();
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

    public void ResetToInitial(bool snapToSocket = true)
{
    // 1) Reposicionar en world space
    transform.position = initialPosition;
    transform.rotation = initialRotation;

    // 2) Limpiar cualquier agarre activo
    var mgr = grabInteractable.interactionManager;
    var interactor = grabInteractable.firstInteractorSelecting;
    if (mgr != null && interactor != null)
        mgr.SelectExit(interactor, grabInteractable);

    // 3) Reseteo de física
    var rb = GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // 4) (Opcional) Snap al socket original
    if (snapToSocket && homeSocket != null)
    {
        // Si hay algo en el socket, lo “liberamos”
        if (homeSocket.hasSelection)
            homeSocket.EndManualInteraction();  // ¡sin argumentos!

        // Ahora iniciamos la interacción manual con nuestro tubo
        var interactable = grabInteractable as UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable;
        homeSocket.StartManualInteraction(interactable);
    }
}
}