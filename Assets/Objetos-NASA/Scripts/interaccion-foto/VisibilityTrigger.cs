using UnityEngine;

public class VisibilityTrigger : MonoBehaviour
{
    public Camera phoneCamera; // Cámara del celular
    public GameObject objetoObjetivo; // Objeto que cambiará de capa
    public string nuevaCapa = "Default"; // Capa a la que se cambiará

    private bool yaActivado = false;

    void Update()
    {
        if (yaActivado || phoneCamera == null || objetoObjetivo == null)
            return;

        // Usa GetComponentInChildren por si el Renderer está en un hijo o nieto
        Renderer rend = objetoObjetivo.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("No se encontró Renderer en el objeto o sus hijos.");
            return;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(phoneCamera);

        if (GeometryUtility.TestPlanesAABB(planes, rend.bounds))
        {
            objetoObjetivo.layer = LayerMask.NameToLayer(nuevaCapa);
            yaActivado = true;
            Debug.Log("Objeto visto por la cámara. Se cambió de capa.");
        }
    }
}
