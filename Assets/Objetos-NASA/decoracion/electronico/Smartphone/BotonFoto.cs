using UnityEngine;

public class BotonFoto : MonoBehaviour
{
    public GameObject ventanaFotoTomada; // Referencia a la ventana de texto
    public float duracionVentana = 2f;   // Segundos que se muestra

    private bool presionado = false;

    private void OnTriggerEnter(Collider other)
    {
        // Solo si el botón no está en uso
        if (!presionado)
        {
            presionado = true;
            MostrarVentana();
        }
    }

    void MostrarVentana()
    {
        if (ventanaFotoTomada != null)
        {
            ventanaFotoTomada.SetActive(true);
            Debug.Log("Foto tomada");

            // Ocultar después de un tiempo
            Invoke(nameof(OcultarVentana), duracionVentana);
        }
    }

    void OcultarVentana()
    {
        if (ventanaFotoTomada != null)
            ventanaFotoTomada.SetActive(false);

        presionado = false;
    }
}
