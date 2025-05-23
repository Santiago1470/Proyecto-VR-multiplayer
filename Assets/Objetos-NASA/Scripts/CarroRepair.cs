using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class CarroRepair : NetworkBehaviour
{
    [Header("Partes separadas del carro")]
    public List<Transform> partesSeparadas;

    // Variable de red para sincronizar el índice de la parte actual (solo se usa en multijugador)
    private NetworkVariable<int> netIndiceParteActual = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Variable local para singleplayer
    private int indiceParteActualLocal = 0;

    [Header("Configuración de reparación")]
    public Vector3 posicionObjetivoLocal = Vector3.zero;
    public Vector3 rotacionObjetivoLocal = new Vector3(-90f, 0f, 0f);
    public bool usarPosicionesRelativas = true; // Si false, las partes se quedan donde están
    public bool desactivarFisicasAlReparar = true; // Desactiva Rigidbody y Colliders al reparar

    [Header("Animación")]
    public float duracionAnimacion = 0.5f;
    public AnimationCurve curvaAnimacion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Puerta automática")]
    public SlidingDoorCar puertaAutomatica;

    [Header("UI")]
    public TMP_Text textoEstado; // TextMeshPro

    // Propiedad que devuelve el índice actual según el modo
    private int IndiceParteActual
    {
        get
        {
            return EsModoRed() ? netIndiceParteActual.Value : indiceParteActualLocal;
        }
    }

    private void Start()
    {
        ActualizarTextoEstado(); // Mostrar estado inicial
    }

    // Método para verificar si estamos en modo red activo
    private bool EsModoRed()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               IsSpawned;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse al evento de cambio del índice solo en modo red
        if (EsModoRed())
        {
            netIndiceParteActual.OnValueChanged += OnIndiceParteActualChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Desuscribirse del evento
        if (EsModoRed())
        {
            netIndiceParteActual.OnValueChanged -= OnIndiceParteActualChanged;
        }

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

    // Método principal de reparación que funciona en ambos modos
    public void Reparar()
    {
        if (EsModoRed())
        {
            // Modo multijugador: usar ServerRpc
            RepararServerRpc();
        }
        else
        {
            // Modo singleplayer: ejecutar directamente
            RepararLocal();
        }
    }

    // Método para reparación en singleplayer
    private void RepararLocal()
    {
        if (indiceParteActualLocal < partesSeparadas.Count)
        {
            // Animar la parte actual
            Transform parte = partesSeparadas[indiceParteActualLocal];
            StartCoroutine(AnimarReparacion(parte));

            // Incrementar el índice
            indiceParteActualLocal++;

            // Actualizar UI
            ActualizarTextoEstado();

            // Si es la última parte, abrir las puertas
            if (indiceParteActualLocal >= partesSeparadas.Count && puertaAutomatica != null)
            {
                puertaAutomatica.AbrirPuertas();
            }

            Debug.Log($"Parte reparada en singleplayer: {parte.name}");
        }
        else
        {
            Debug.Log("¡Todas las partes ya están reparadas!");
        }
    }

    // Este método se llama desde el martillo y se ejecuta en el servidor (solo multijugador)
    [ServerRpc(RequireOwnership = false)]
    public void RepararServerRpc()
    {
        // Solo funciona en modo red y si somos el servidor
        if (!EsModoRed() || !IsServer)
        {
            Debug.LogError("[CarroRepair] RepararServerRpc llamado pero no estamos en servidor!");
            return;
        }

        int indiceActual = netIndiceParteActual.Value;
        Debug.Log($"[Servidor CarroRepair] Reparando parte {indiceActual} de {partesSeparadas.Count}");

        if (indiceActual < partesSeparadas.Count)
        {
            // Incrementar el índice de parte (esto disparará el evento OnValueChanged en todos los clientes)
            netIndiceParteActual.Value++;

            Debug.Log($"[Servidor CarroRepair] Parte reparada, nuevo índice: {netIndiceParteActual.Value}");

            // Si es la última parte, abrir las puertas
            if (netIndiceParteActual.Value >= partesSeparadas.Count && puertaAutomatica != null)
            {
                Debug.Log("[Servidor CarroRepair] Todas las partes reparadas, abriendo puertas");
                puertaAutomatica.AbrirPuertas();
            }
        }
        else
        {
            Debug.Log("[Servidor CarroRepair] ¡Todas las partes ya están reparadas!");
        }
    }

    private void ActualizarTextoEstado()
    {
        if (textoEstado == null) return;

        int indiceActual = IndiceParteActual;

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
        // Guardar componentes físicos originales
        Rigidbody parteRb = parte.GetComponent<Rigidbody>();
        Collider[] collidersOriginales = parte.GetComponentsInChildren<Collider>();
        bool rbEraKinematic = false;

        if (parteRb != null)
        {
            rbEraKinematic = parteRb.isKinematic;
        }

        // Desactivar físicas durante la animación si está configurado
        if (desactivarFisicasAlReparar)
        {
            if (parteRb != null)
            {
                parteRb.isKinematic = true;
                parteRb.linearVelocity = Vector3.zero;
                parteRb.angularVelocity = Vector3.zero;
            }

            // Desactivar colliders temporalmente para evitar interferencias
            foreach (Collider col in collidersOriginales)
            {
                if (col != null && !col.isTrigger)
                {
                    col.enabled = false;
                }
            }
        }

        if (usarPosicionesRelativas)
        {
            // Animación con movimiento a posición objetivo
            Vector3 posicionInicial = parte.localPosition;
            Quaternion rotacionInicial = parte.localRotation;
            Vector3 posicionFinal = posicionObjetivoLocal;
            Quaternion rotacionFinal = Quaternion.Euler(rotacionObjetivoLocal);

            float tiempo = 0f;
            while (tiempo < duracionAnimacion)
            {
                float t = tiempo / duracionAnimacion;
                float curveValue = curvaAnimacion.Evaluate(t);

                parte.localPosition = Vector3.Lerp(posicionInicial, posicionFinal, curveValue);
                parte.localRotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, curveValue);
                tiempo += Time.deltaTime;
                yield return null;
            }

            parte.localPosition = posicionFinal;
            parte.localRotation = rotacionFinal;
        }
        else
        {
            // Solo animación visual sin movimiento (efecto de "reparado")
            Vector3 escalaOriginal = parte.localScale;
            Vector3 posicionOriginal = parte.localPosition;

            // Pequeña animación de "pop" para indicar que se reparó
            float tiempo = 0f;
            while (tiempo < duracionAnimacion)
            {
                float t = tiempo / duracionAnimacion;
                float curveValue = curvaAnimacion.Evaluate(t);

                // Efecto de escala y pequeño movimiento vertical
                float scaleMultiplier = 1f + (Mathf.Sin(curveValue * Mathf.PI) * 0.1f);
                parte.localScale = escalaOriginal * scaleMultiplier;

                // Pequeño bounce vertical
                Vector3 offset = Vector3.up * (Mathf.Sin(curveValue * Mathf.PI) * 0.05f);
                parte.localPosition = posicionOriginal + offset;

                tiempo += Time.deltaTime;
                yield return null;
            }

            // Restaurar valores originales
            parte.localScale = escalaOriginal;
            parte.localPosition = posicionOriginal;
        }

        // Reactivar físicas después de la animación
        if (desactivarFisicasAlReparar)
        {
            // Esperar un frame para asegurar que la posición se haya establecido
            yield return null;

            if (parteRb != null)
            {
                parteRb.isKinematic = rbEraKinematic;
            }

            // Reactivar colliders
            foreach (Collider col in collidersOriginales)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }

        Debug.Log($"Parte animada: {parte.name}");
    }

    // Método público para reiniciar la reparación (útil para testing)
    public void ReiniciarReparacion()
    {
        if (EsModoRed())
        {
            if (IsServer)
            {
                netIndiceParteActual.Value = 0;
            }
        }
        else
        {
            indiceParteActualLocal = 0;
            ActualizarTextoEstado();
        }
    }

    // Método para obtener el progreso de reparación (0-1)
    public float ObtenerProgreso()
    {
        if (partesSeparadas.Count == 0) return 1f;
        return (float)IndiceParteActual / partesSeparadas.Count;
    }

    // Método para configurar el comportamiento de reparación
    public void ConfigurarComportamiento(bool moverPartes, bool desactivarFisicas)
    {
        usarPosicionesRelativas = moverPartes;
        desactivarFisicasAlReparar = desactivarFisicas;
    }

    // Método para verificar si una parte específica está reparada
    public bool EstaParteReparada(int indiceParte)
    {
        return indiceParte < IndiceParteActual;
    }

    // Método para obtener la parte que se debe reparar siguiente
    public Transform ObtenerSiguienteParte()
    {
        int indice = IndiceParteActual;
        if (indice < partesSeparadas.Count)
        {
            return partesSeparadas[indice];
        }
        return null;
    }
}