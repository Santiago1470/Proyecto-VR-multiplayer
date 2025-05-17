using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarroRepair : MonoBehaviour
{
    [Header("Partes separadas del carro")]
    public List<Transform> partesSeparadas;

    private int indiceParteActual = 0;

    [Header("Configuración de reparación")]
    public Vector3 posicionObjetivoLocal = Vector3.zero;
    public Vector3 rotacionObjetivoLocal = new Vector3(-90f, 0f, 0f);

    [Header("Animación")]
    public float duracionAnimacion = 0.5f; // Tiempo en segundos que tarda en acomodarse

    public SlidingDoorCar puertaAutomatica;  // Arrastra aquí el objeto con el script SlidingDoor

    public void Reparar()
    {
        if (indiceParteActual < partesSeparadas.Count)
        {
            Transform parte = partesSeparadas[indiceParteActual];
            StartCoroutine(AnimarReparacion(parte));
            indiceParteActual++;

            if (indiceParteActual >= partesSeparadas.Count && puertaAutomatica != null)
            {
                puertaAutomatica.AbrirPuertas();
            }

        }
        else
        {
            Debug.Log("¡Todas las partes ya están reparadas!");
        }
    }

    private IEnumerator AnimarReparacion(Transform parte)
    {
        Vector3 posicionInicial = parte.localPosition;
        Quaternion rotacionInicial = parte.localRotation;

        Vector3 posicionFinal = posicionObjetivoLocal;
        Quaternion rotacionFinal = Quaternion.Euler(rotacionObjetivoLocal);

        float tiempo = 0f;

        while (tiempo < duracionAnimacion)
        {
            float t = tiempo / duracionAnimacion;

            parte.localPosition = Vector3.Lerp(posicionInicial, posicionFinal, t);
            parte.localRotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, t);

            tiempo += Time.deltaTime;
            yield return null;
        }

        // Asegurarse que al final quede bien colocado
        parte.localPosition = posicionFinal;
        parte.localRotation = rotacionFinal;

        Debug.Log($"Parte animada: {parte.name}");
    }
}
