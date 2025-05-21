using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class RocketLaunch : NetworkBehaviour
{
    [Header("Rocket Settings")]
    public float launchSpeed = 10f;
    public float alturaMaxima = 20f;
    public Vector3 posicionInicio = new Vector3(-0.52f, -4.516f, 0.46f);

    [Header("UI References")]
    public TextMeshProUGUI countdownText;
    public GameObject launchButton;
    public GameObject panelInstruccion;
    public GameObject panelInstruccionInicial;
    public GameObject firstButton;

    [Header("Effects")]
    public ParticleSystem launchEffect;
    public AudioSource audioConteo;
    public AudioSource audioDespegue;

    [Header("Other References")]
    public TechoMover techo;

    // Network Variables para sincronizar el estado
    private NetworkVariable<bool> isLaunching = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> yaReinicio = new NetworkVariable<bool>(false);

    private Coroutine resetCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse a cambios en las variables de red
        isLaunching.OnValueChanged += OnLaunchingStateChanged;

        // Inicializar posición
        transform.localPosition = posicionInicio;
    }

    public override void OnNetworkDespawn()
    {
        if (isLaunching != null)
            isLaunching.OnValueChanged -= OnLaunchingStateChanged;

        base.OnNetworkDespawn();
    }

    private void OnLaunchingStateChanged(bool previousValue, bool newValue)
    {
        // Sincronizar efectos visuales y audio en todos los clientes
        if (newValue && !previousValue)
        {
            if (launchEffect != null)
                launchEffect.Play();

            if (audioDespegue != null)
                audioDespegue.Play();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartLaunchServerRpc()
    {
        // Solo el servidor ejecuta la lógica principal
        if (!IsServer) return;

        StartCoroutine(CountdownAndPrepareLaunch());
    }

    private IEnumerator CountdownAndPrepareLaunch()
    {
        // Reproducir audio de conteo en todos los clientes
        PlayCountdownAudioClientRpc();

        // Ocultar panel inicial en todos los clientes
        HidePanelInstruccionInicialClientRpc();

        string[] countdownSteps = { "5", "4", "3", "2", "1", "¡Despegue!" };

        foreach (string step in countdownSteps)
        {
            UpdateCountdownTextClientRpc(step);
            yield return new WaitForSeconds(1f);
        }

        // Mostrar UI de lanzamiento en todos los clientes
        ShowLaunchUIClientRpc();

        resetCoroutine = StartCoroutine(ResetIfNotLaunched());
    }

    [ServerRpc(RequireOwnership = false)]
    public void LaunchRocketServerRpc()
    {
        if (!IsServer) return;

        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        // Ocultar UI en todos los clientes
        HideLaunchUIClientRpc();

        // Actualizar estado de lanzamiento
        isLaunching.Value = true;
    }

    private IEnumerator ResetIfNotLaunched()
    {
        yield return new WaitForSeconds(5f);

        if (!isLaunching.Value)
        {
            UpdateCountdownTextClientRpc("¡Vuelo cancelado!");
            yield return new WaitForSeconds(2f);
        }

        ResetRocketClientRpc();
    }

    private IEnumerator ResetAfterLaunch()
    {
        yield return new WaitForSeconds(2f);
        ResetRocketClientRpc();
    }

    // ClientRpcs para sincronizar UI y efectos
    [ClientRpc]
    private void PlayCountdownAudioClientRpc()
    {
        if (audioConteo != null)
            audioConteo.Play();
    }

    [ClientRpc]
    private void HidePanelInstruccionInicialClientRpc()
    {
        if (panelInstruccionInicial != null)
            panelInstruccionInicial.SetActive(false);
    }

    [ClientRpc]
    private void UpdateCountdownTextClientRpc(string text)
    {
        if (countdownText != null)
            countdownText.text = text;
    }

    [ClientRpc]
    private void ShowLaunchUIClientRpc()
    {
        if (panelInstruccion != null)
            panelInstruccion.SetActive(true);

        if (launchButton != null)
            launchButton.SetActive(true);
    }

    [ClientRpc]
    private void HideLaunchUIClientRpc()
    {
        if (panelInstruccion != null)
            panelInstruccion.SetActive(false);
    }

    [ClientRpc]
    private void ResetRocketClientRpc()
    {
        if (countdownText != null)
            countdownText.text = "";

        if (panelInstruccion != null)
            panelInstruccion.SetActive(false);

        if (launchButton != null)
            launchButton.SetActive(false);

        if (firstButton != null)
            firstButton.SetActive(true);

        if (panelInstruccionInicial != null)
            panelInstruccionInicial.SetActive(true);

        if (techo != null)
            techo.CerrarTecho();

        transform.localPosition = posicionInicio;

        if (IsServer)
        {
            isLaunching.Value = false;
            yaReinicio.Value = false;
        }
    }

    void Update()
    {
        // Solo el servidor mueve el cohete, los clientes se sincronizan automáticamente
        if (!IsServer) return;

        if (isLaunching.Value)
        {
            transform.Translate(Vector3.up * launchSpeed * Time.deltaTime);

            if (transform.position.y >= alturaMaxima && !yaReinicio.Value)
            {
                yaReinicio.Value = true;
                StartCoroutine(ResetAfterLaunch());
            }
        }
    }
}
