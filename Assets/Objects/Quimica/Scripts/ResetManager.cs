using UnityEngine;
using Unity.Netcode;

public class ResetManager : NetworkBehaviour
{
    private ChemicalTube[] allTubes;

    void Awake()
    {
        // Buscar tubos al inicializar
        RefreshTubesList();
    }

    public override void OnNetworkSpawn()
    {
        // Refrescar la lista cuando se spawne en la red
        RefreshTubesList();
    }

    private void RefreshTubesList()
    {
        allTubes = FindObjectsOfType<ChemicalTube>();
    }

    // Método público que se llama localmente
    public void RequestResetAllTubes()
    {
        if (IsServer)
        {
            // Si somos el servidor, ejecutar directamente
            ResetAllTubes();
        }
        else
        {
            // Si somos cliente, solicitar al servidor
            RequestResetServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestResetServerRpc()
    {
        ResetAllTubes();
    }

    private void ResetAllTubes()
    {
        // Refrescar lista por si han cambiado los objetos
        RefreshTubesList();
        
        foreach (var tube in allTubes)
        {
            if (tube != null)
            {
                // true = que use StartManualInteraction para snappear al socket
                tube.ResetToInitial(snapToSocket: true);
            }
        }

        // Notificar a todos los clientes que se ha hecho el reset
        NotifyResetClientRpc();
    }

    [ClientRpc]
    private void NotifyResetClientRpc()
    {
        // Aquí puedes agregar efectos visuales, sonidos, etc.
        Debug.Log("Todos los tubos han sido reseteados");
    }
}