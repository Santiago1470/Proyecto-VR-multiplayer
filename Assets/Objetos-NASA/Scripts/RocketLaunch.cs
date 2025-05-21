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

    // Variables para singleplayer
    private bool launching = false;
    private bool yaReinicio = false;

    // Network Variables para multijugador
    private NetworkVariable<bool> isLaunching = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> yaReincioNetwork = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isCountingDown = new NetworkVariable<bool>(false);

    private Coroutine resetCoroutine;

    // Propiedad para detectar si estamos en multijugador
    private bool IsMultiplayer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Solo suscribirse si estamos en multijugador
        if (IsMultiplayer)
        {
            isLaunching.OnValueChanged += OnLaunchingStateChanged;
            yaReincioNetwork.OnValueChanged += OnResetStateChanged;
            isCountingDown.OnValueChanged += OnCountdownStateChanged;
        }

        // Inicializar posición
        transform.localPosition = posicionInicio;

        Debug.Log($"RocketLaunch: NetworkSpawn completado. IsServer: {IsServer}, IsClient: {IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        if (IsMultiplayer)
        {
            if (isLaunching != null)
                isLaunching.OnValueChanged -= OnLaunchingStateChanged;
            if (yaReincioNetwork != null)
                yaReincioNetwork.OnValueChanged -= OnResetStateChanged;
            if (isCountingDown != null)
                isCountingDown.OnValueChanged -= OnCountdownStateChanged;
        }

        base.OnNetworkDespawn();
    }

    void Start()
    {
        transform.localPosition = posicionInicio;
    }

    private void OnLaunchingStateChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"RocketLaunch: Estado de lanzamiento cambió de {previousValue} a {newValue}");

        // Sincronizar efectos visuales y audio en todos los clientes
        if (newValue && !previousValue)
        {
            PlayLaunchEffects();
        }
    }

    private void OnResetStateChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"RocketLaunch: Estado de reset cambió de {previousValue} a {newValue}");
    }

    private void OnCountdownStateChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"RocketLaunch: Estado de countdown cambió de {previousValue} a {newValue}");
    }

    private void PlayLaunchEffects()
    {
        Debug.Log("RocketLaunch: Reproduciendo efectos de lanzamiento!");

        if (launchEffect != null)
        {
            launchEffect.Play();
            Debug.Log("RocketLaunch: Efecto de partículas reproducido!");
        }

        if (audioDespegue != null)
        {
            audioDespegue.Play();
            Debug.Log("RocketLaunch: Audio de despegue reproducido!");
        }
    }

    // Método principal para iniciar lanzamiento (funciona en ambos modos)
    public void StartLaunch()
    {
        if (IsMultiplayer)
        {
            // Verificar que estamos spawneados correctamente
            if (!IsSpawned)
            {
                Debug.LogError("RocketLaunch: No se puede iniciar lanzamiento - objeto no spawneado");
                return;
            }

            // Modo multijugador
            StartLaunchServerRpc();
        }
        else
        {
            // Modo singleplayer
            StartLaunchSingleplayer();
        }
    }

    // Método principal para lanzar cohete (funciona en ambos modos)
    public void LaunchRocket()
    {
        if (IsMultiplayer)
        {
            // Verificar que estamos spawneados correctamente
            if (!IsSpawned)
            {
                Debug.LogError("RocketLaunch: No se puede lanzar cohete - objeto no spawneado");
                return;
            }

            // Modo multijugador
            LaunchRocketServerRpc();
        }
        else
        {
            // Modo singleplayer
            LaunchRocketSingleplayer();
        }
    }

    // ==================== MULTIJUGADOR ====================
    [ServerRpc(RequireOwnership = false)]
    public void StartLaunchServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // Verificar que no estamos ya en proceso
        if (isCountingDown.Value || isLaunching.Value)
        {
            Debug.Log("RocketLaunch: Ya hay un proceso de lanzamiento en curso");
            return;
        }

        isCountingDown.Value = true;
        StartCoroutine(CountdownAndPrepareLaunchMultiplayer());
    }

    [ServerRpc(RequireOwnership = false)]
    public void LaunchRocketServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("RocketLaunch: LaunchRocketServerRpc llamado!");

        if (!IsServer) return;

        Debug.Log("RocketLaunch: Ejecutando lanzamiento en servidor!");

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            Debug.Log("RocketLaunch: Corrutina de reset detenida!");
        }

        HideLaunchUIClientRpc();
        isLaunching.Value = true;
        isCountingDown.Value = false;
        Debug.Log("RocketLaunch: isLaunching establecido en true!");
    }

    private IEnumerator CountdownAndPrepareLaunchMultiplayer()
    {
        PlayCountdownAudioClientRpc();
        HidePanelInstruccionInicialClientRpc();

        string[] countdownSteps = { "5", "4", "3", "2", "1", "¡Despegue!" };

        foreach (string step in countdownSteps)
        {
            UpdateCountdownTextClientRpc(step);
            yield return new WaitForSeconds(1f);
        }

        ShowLaunchUIClientRpc();
        resetCoroutine = StartCoroutine(ResetIfNotLaunchedMultiplayer());
    }

    private IEnumerator ResetIfNotLaunchedMultiplayer()
    {
        yield return new WaitForSeconds(5f);

        if (!isLaunching.Value)
        {
            UpdateCountdownTextClientRpc("¡Vuelo cancelado!");
            yield return new WaitForSeconds(2f);
            ResetRocketClientRpc();
        }
    }

    private IEnumerator ResetAfterLaunchMultiplayer()
    {
        yield return new WaitForSeconds(2f);
        ResetRocketClientRpc();
    }

    // ==================== SINGLEPLAYER ====================
    private void StartLaunchSingleplayer()
    {
        if (audioConteo != null)
            audioConteo.Play();

        if (panelInstruccionInicial != null)
            panelInstruccionInicial.SetActive(false);

        StartCoroutine(CountdownAndPrepareLaunchSingleplayer());
    }

    private void LaunchRocketSingleplayer()
    {
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        if (panelInstruccion != null)
            panelInstruccion.SetActive(false);

        PlayLaunchEffects();
        launching = true;
    }

    private IEnumerator CountdownAndPrepareLaunchSingleplayer()
    {
        string[] countdownSteps = { "5", "4", "3", "2", "1", "¡Despegue!" };

        foreach (string step in countdownSteps)
        {
            if (countdownText != null)
                countdownText.text = step;

            yield return new WaitForSeconds(1f);
        }

        if (panelInstruccion != null)
            panelInstruccion.SetActive(true);

        if (launchButton != null)
            launchButton.SetActive(true);

        resetCoroutine = StartCoroutine(ResetIfNotLaunchedSingleplayer());
    }

    private IEnumerator ResetIfNotLaunchedSingleplayer()
    {
        yield return new WaitForSeconds(5f);

        if (!launching)
        {
            if (countdownText != null)
                countdownText.text = "¡Vuelo cancelado!";

            yield return new WaitForSeconds(2f);
        }

        ResetRocketSingleplayer();
    }

    private IEnumerator ResetAfterLaunchSingleplayer()
    {
        yield return new WaitForSeconds(2f);
        ResetRocketSingleplayer();
    }

    private void ResetRocketSingleplayer()
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
        launching = false;
        yaReinicio = false;
    }

    // ==================== CLIENT RPCs (Solo multijugador) ====================
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
            yaReincioNetwork.Value = false;
            isCountingDown.Value = false;
        }
    }

    void Update()
    {
        if (IsMultiplayer)
        {
            // Modo multijugador: solo el servidor mueve el cohete
            if (!IsServer || !IsSpawned) return;

            if (isLaunching.Value)
            {
                transform.Translate(Vector3.up * launchSpeed * Time.deltaTime);

                if (transform.position.y >= alturaMaxima && !yaReincioNetwork.Value)
                {
                    yaReincioNetwork.Value = true;
                    StartCoroutine(ResetAfterLaunchMultiplayer());
                }
            }
        }
        else
        {
            // Modo singleplayer: mueve el cohete directamente
            if (launching)
            {
                transform.Translate(Vector3.up * launchSpeed * Time.deltaTime);

                if (transform.position.y >= alturaMaxima && !yaReinicio)
                {
                    yaReinicio = true;
                    StartCoroutine(ResetAfterLaunchSingleplayer());
                }
            }
        }
    }
}