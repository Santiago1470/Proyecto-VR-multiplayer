using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Netcode;
using TMPro;

public class VRSheetsCollectorNetwork : NetworkBehaviour
{
    [Header("Referencias VR")]
    [SerializeField] private List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor> sockets = new();
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private ParticleSystem completionEffect;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip socketFillSound;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Configuración")]
    [SerializeField] private string targetTag = "sheets";
    private const int SheetsPerPlayer = 3;
    private const int MaxPlayers = 2;

    // NetworkVariables para sincronizar estado
    private NetworkVariable<int> currentFilledSockets = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> requiredObjects = new(SheetsPerPlayer, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private AudioSource audioSource;
    private bool rewardDelivered = false;

    public override void OnNetworkSpawn()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        if (IsServer)
        {
            RecalculateRequired();
            NetworkManager.OnClientConnectedCallback += OnClientConnectedOrDisconnected;
            NetworkManager.OnClientDisconnectCallback += OnClientConnectedOrDisconnected;

            for (int i = 0; i < sockets.Count; i++)
            {
                int idx = i;
                sockets[i].selectEntered.AddListener(args => Server_OnSocketChanged(idx, +1, args.interactableObject));
                sockets[i].selectExited.AddListener(args => Server_OnSocketChanged(idx, -1, args.interactableObject));
            }
        }

        requiredObjects.OnValueChanged += (_, newVal) => UpdateProgressDisplay(currentFilledSockets.Value, newVal);
        currentFilledSockets.OnValueChanged += (_, newVal) =>
        {
            UpdateProgressDisplay(newVal, requiredObjects.Value);
            if (newVal >= requiredObjects.Value) Client_ShowCompletion();
        };

        UpdateProgressDisplay(currentFilledSockets.Value, requiredObjects.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedOrDisconnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientConnectedOrDisconnected;
        }
    }

    private void OnClientConnectedOrDisconnected(ulong _) => RecalculateRequired();

    private void RecalculateRequired()
    {
        int players = Mathf.Clamp(NetworkManager.ConnectedClientsList.Count, 1, MaxPlayers);
        requiredObjects.Value = players * SheetsPerPlayer;

        if (currentFilledSockets.Value > requiredObjects.Value)
            currentFilledSockets.Value = requiredObjects.Value;
    }

    private void Server_OnSocketChanged(int socketIndex, int delta, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable interactable)
    {
        if (interactable.transform.CompareTag(targetTag))
        {
            int newValue = currentFilledSockets.Value + delta;
            currentFilledSockets.Value = Mathf.Clamp(newValue, 0, requiredObjects.Value);
            PlaySocketSoundClientRpc();
        }
    }

    [ClientRpc]
    private void PlaySocketSoundClientRpc()
    {
        if (socketFillSound != null && audioSource != null)
            audioSource.PlayOneShot(socketFillSound);
    }

    private void Client_ShowCompletion()
    {
        if (rewardDelivered) return;
        rewardDelivered = true;

        if (completionEffect != null) completionEffect.Play();
        if (successSound != null && audioSource != null) audioSource.PlayOneShot(successSound);

        StartCoroutine(DeliverRewardClient());
    }

    private IEnumerator DeliverRewardClient()
    {
        yield return new WaitForSeconds(1.5f);

        Vector3 pos = spawnPoint != null
            ? spawnPoint.position + Vector3.up * 5f
            : transform.position + Vector3.up * 5f;

        GameObject reward = Instantiate(rewardPrefab, pos, Quaternion.identity);

        if (reward.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = Vector3.down * 2f;
    }

    private void UpdateProgressDisplay(int filled, int required)
    {
        if (progressText == null) return;

        if (filled >= required)
        {
            progressText.text = $"¡Completado! {filled}/{required} hojas.";
            progressText.color = Color.green;
        }
        else
        {
            int remaining = required - filled;
            progressText.text = remaining == 1
                ? "¡Solo falta 1 hoja!"
                : $"Hojas: {filled}/{required}\n¡Vamos bien!";
            progressText.color = Color.white;
        }
    }
}
