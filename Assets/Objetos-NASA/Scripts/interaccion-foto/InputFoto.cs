using UnityEngine;
using UnityEngine.XR;

public class InputFoto : MonoBehaviour
{
    public VRPhotoCapture captura;

    private InputDevice dispositivoDerecho;

    void Start()
    {
        dispositivoDerecho = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (dispositivoDerecho.isValid)
        {
            Debug.Log("Controlador derecho detectado: " + dispositivoDerecho.name);
        }
        else
        {
            Debug.LogWarning("No se detectó el controlador derecho. Asegúrate de que el XR esté activo.");
        }
    }

    void Update()
    {
        // Si no es válido, intenta nuevamente obtener el dispositivo
        if (!dispositivoDerecho.isValid)
        {
            dispositivoDerecho = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            return;
        }

        // Verifica si se presiona el trigger
        if (dispositivoDerecho.TryGetFeatureValue(CommonUsages.triggerButton, out bool presionado) && presionado)
        {
            Debug.Log("¡Trigger presionado!");
            captura.TomarFoto();
        }
    }
}

