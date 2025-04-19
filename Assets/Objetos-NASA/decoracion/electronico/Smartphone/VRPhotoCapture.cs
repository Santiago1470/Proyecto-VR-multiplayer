using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VRPhotoCapture : MonoBehaviour
{
    public Camera phoneCamera; // Asigna PhoneCamera
    public GameObject cuadroTexto; // Texto que se muestra al tomar la foto
    public AudioSource sonidoCamara; // Opcional

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

        // Mostrar cuadro de texto
        if (cuadroTexto != null)
            cuadroTexto.SetActive(true);

        // Sonido de cámara
        if (sonidoCamara != null)
            sonidoCamara.Play();

        // (Opcional) Guardar imagen en archivo o hacer algo más
    }
}
