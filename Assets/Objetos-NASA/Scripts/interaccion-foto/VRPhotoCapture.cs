using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VRPhotoCapture : MonoBehaviour
{
    public Camera phoneCamera; // Cámara del celular
    public GameObject cuadroTexto; // Texto que se muestra al tomar la foto
    public AudioSource sonidoCamara; // Opcional

    public GameObject objetoObjetivo; // Objeto que será revelado al verlo con la cámara
    public string nuevaCapa = "Default"; // Capa a cambiar al ser visible
    private bool yaRevelado = false;

    void Update()
    {
        DetectarObjetoEnCamara();
    }

    void DetectarObjetoEnCamara()
    {
        if (yaRevelado || phoneCamera == null || objetoObjetivo == null)
            return;

        Renderer[] renderers = objetoObjetivo.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(phoneCamera);
        bool enVista = false;

        foreach (Renderer rend in renderers)
        {
            if (GeometryUtility.TestPlanesAABB(planes, rend.bounds))
            {
                enVista = true;
                break;
            }
        }

        if (enVista)
        {
            CambiarCapaRecursivamente(objetoObjetivo.transform, LayerMask.NameToLayer(nuevaCapa));
            yaRevelado = true;
            Debug.Log("Objeto revelado al estar dentro de la vista de la cámara del celular.");
        }
    }

    void CambiarCapaRecursivamente(Transform obj, int capa)
    {
        obj.gameObject.layer = capa;
        foreach (Transform hijo in obj)
        {
            CambiarCapaRecursivamente(hijo, capa);
        }
    }

    // Esta parte aún puede usarse si decides permitir tomar fotos
    public void TomarFoto()
    {
        StartCoroutine(CapturarFoto());
    }

    IEnumerator CapturarFoto()
    {
        yield return new WaitForEndOfFrame();

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = phoneCamera.targetTexture;

        Texture2D image = new Texture2D(phoneCamera.targetTexture.width, phoneCamera.targetTexture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();

        RenderTexture.active = currentRT;

        if (cuadroTexto != null)
            cuadroTexto.SetActive(true);

        if (sonidoCamara != null)
            sonidoCamara.Play();
    }
}
