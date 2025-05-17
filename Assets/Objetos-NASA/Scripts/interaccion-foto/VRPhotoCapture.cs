using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VRPhotoCapture : MonoBehaviour
{
    public Camera phoneCamera;
    public GameObject cuadroTexto;
    public AudioSource sonidoCamara;

    public GameObject objetoObjetivo;
    public GameObject objetoParaMostrar;
    public string nuevaCapa = "Default";

    private bool objetivoRevelado = false;
    private bool mostrarRevelado = false;

    void Update()
    {
        DetectarYRevelarObjeto(objetoObjetivo, ref objetivoRevelado);
        DetectarYRevelarObjeto(objetoParaMostrar, ref mostrarRevelado);
    }

    void DetectarYRevelarObjeto(GameObject obj, ref bool yaRevelado)
    {
        if (yaRevelado || phoneCamera == null || obj == null)
            return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(phoneCamera);
        foreach (Renderer rend in renderers)
        {
            if (GeometryUtility.TestPlanesAABB(planes, rend.bounds))
            {
                CambiarCapaRecursivamente(obj.transform, LayerMask.NameToLayer(nuevaCapa));
                obj.SetActive(true);
                yaRevelado = true;
                Debug.Log($"Objeto {obj.name} revelado al estar en vista.");
                break;
            }
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

