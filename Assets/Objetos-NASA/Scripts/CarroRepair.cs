using UnityEngine;
using System.Collections.Generic;

public class CarroRepair : MonoBehaviour
{
    [Header("Partes separadas del carro")]
    public List<Transform> partesSeparadas; // Lista de las partes que deben acomodarse

    private int indiceParteActual = 0; // Para llevar control de qué parte acomodar

    [Header("Configuración de reparación")]
    public Vector3 posicionReparada = Vector3.zero; // Posición final relativa
    public Vector3 rotacionReparada = new Vector3(-90f, 0f, 0f); // Rotación final relativa

    public void Reparar()
    {
        if (indiceParteActual < partesSeparadas.Count)
        {
            Transform parteAReparar = partesSeparadas[indiceParteActual];

            // Reparar la posición y rotación local
            parteAReparar.localPosition = posicionReparada;
            parteAReparar.localRotation = Quaternion.Euler(rotacionReparada);

            Debug.Log($"Parte reparada: {parteAReparar.name}");

            indiceParteActual++;
        }
        else
        {
            Debug.Log("¡Todas las partes del carro ya están reparadas!");
        }
    }
}
