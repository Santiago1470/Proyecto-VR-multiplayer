using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class CarroRepair : NetworkBehaviour
{
    [Header("Partes separadas del carro")]
    public List<Transform> partesSeparadas;

    // Variable de red para sincronizar el índice de la parte actual
    private NetworkVariable<int> netIndiceParteActual = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Configuración de reparación")]
    public Vector3 posicionObjetivoLocal = Vector3.zero;
    public Vector3 rotacionObjetivoLocal = new Vector3(-90f, 0f, 0f);

    [Header("Animación")]
    public float duracionAnimacion = 0.5f;

    [Header("Puerta automática")]
    public SlidingDoorCar puertaAutomatica;

    [Header("UI")]
    public TMP_Text textoEstado; // TextMeshPro

    private void Start()
    {
        ActualizarTextoEstado(); // Mostrar estado inicial
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse al evento de cambio del índice
        netIndiceParteActual.OnValueChanged += OnIndiceParteActualChanged;
    }

    public override void OnNetworkDespawn()
    {
        // Desuscribirse del evento
        netIndiceParteActual.OnValueChanged -= OnIndiceParteActualChanged;

        base.OnNetworkDespawn();
    }

    private void OnIndiceParteActualChanged(int previousIndex, int newIndex)
    {
        // Si el índice cambió en la red, ejecutar la animación local
        if (newIndex > previousIndex && newIndex <= partesSeparadas.Count)
        {
            // El -1 es porque el índice de red ya fue incrementado, pero necesitamos animar la parte anterior
            Transform parte = partesSeparadas[newIndex - 1];
            StartCoroutine(AnimarReparacion(parte));

            // Actualizar UI
            ActualizarTextoEstado();

            // Verificar si es la última parte para abrir puertas
            if (newIndex >= partesSeparadas.Count && puertaAutomatica != null)
            {
                if (IsServer)
                {
                    puertaAutomatica.AbrirPuertas();
                }
            }
        }
    }

    // Se mantiene el método Reparar() para compatibilidad, pero ahora llama al ServerRpc
    public void Reparar()
    {
        RepararServerRpc();
    }

    // Este método se llama desde el martillo y se ejecuta en el servidor
    [ServerRpc(RequireOwnership = false)]
    public void RepararServerRpc()
    {
        // Verifica que estamos en el servidor antes de modificar la variable de red
        if (IsServer)
        {
            int indiceActual = netIndiceParteActual.Value;

            if (indiceActual < partesSeparadas.Count)
            {
                // Incrementar el índice de parte (esto disparará el evento OnValueChanged en todos los clientes)
                netIndiceParteActual.Value++;

                // Si es la última parte, abrir las puertas
                if (netIndiceParteActual.Value >= partesSeparadas.Count && puertaAutomatica != null)
                {
                    puertaAutomatica.AbrirPuertas();
                }
            }
            else
            {
                Debug.Log("¡Todas las partes ya están reparadas!");
            }
        }
    }

    private void ActualizarTextoEstado()
    {
        if (textoEstado == null) return;

        int indiceActual = netIndiceParteActual.Value;

        if (indiceActual < partesSeparadas.Count)
        {
            textoEstado.text = $"Vehículo en reparación:\nParte {indiceActual} de {partesSeparadas.Count}";
        }
        else
        {
            textoEstado.text = "Vehículo reparado completamente";
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

        parte.localPosition = posicionFinal;
        parte.localRotation = rotacionFinal;
        Debug.Log($"Parte animada: {parte.name}");
    }
}