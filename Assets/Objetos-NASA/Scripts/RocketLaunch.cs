using UnityEngine;
using TMPro;
using System.Collections;

public class RocketLaunch : MonoBehaviour
{
    public float launchSpeed = 10f;
    private bool launching = false;

    public float alturaMaxima = 20f; // Altura para reiniciar
    private bool yaReinicio = false;

    public ParticleSystem launchEffect;
    public TextMeshProUGUI countdownText;
    public GameObject launchButton;
    public GameObject panelInstruccion;
    public TechoMover techo;
    public GameObject firstButton;

    public Vector3 posicionInicio = new Vector3(-0.52f, -4.516f, 0.46f); // Puedes cambiarla desde el editor

    private Coroutine resetCoroutine;

    public void StartLaunch()
    {
        StartCoroutine(CountdownAndPrepareLaunch());
    }

    private IEnumerator CountdownAndPrepareLaunch()
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

        resetCoroutine = StartCoroutine(ResetIfNotLaunched());
    }

    public void LaunchRocket()
    {
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        if (panelInstruccion != null)
            panelInstruccion.SetActive(false);

        if (launchEffect != null)
            launchEffect.Play();

        launching = true;
    }

    private IEnumerator ResetIfNotLaunched()
    {
        yield return new WaitForSeconds(5f);

        if (countdownText != null)
            countdownText.text = "";

        if (panelInstruccion != null)
            panelInstruccion.SetActive(false);

        if (launchButton != null)
            launchButton.SetActive(false);

        if (firstButton != null)
            firstButton.SetActive(true);

        if (techo != null)
            techo.CerrarTecho();

        transform.localPosition = posicionInicio;
        launching = false;
        yaReinicio = false;
    }

    private IEnumerator ResetAfterLaunch()
    {
        yield return new WaitForSeconds(2f);

        if (countdownText != null)
            countdownText.text = "";

        if (panelInstruccion != null)
            panelInstruccion.SetActive(false);

        if (launchButton != null)
            launchButton.SetActive(false);

        if (firstButton != null)
            firstButton.SetActive(true);

        if (techo != null)
            techo.CerrarTecho();

        transform.localPosition = posicionInicio;
        launching = false;
        yaReinicio = false;
    }

    void Start()
    {
        // Establece la posición inicial al iniciar
        transform.localPosition = posicionInicio;
    }

    void Update()
    {
        if (launching)
        {
            transform.Translate(Vector3.up * launchSpeed * Time.deltaTime);

            if (transform.position.y >= alturaMaxima && !yaReinicio)
            {
                yaReinicio = true;
                StartCoroutine(ResetAfterLaunch());
            }
        }
    }
}
