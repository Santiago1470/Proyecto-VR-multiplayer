using UnityEngine;
using TMPro;
using System.Collections;

public class RocketLaunch : MonoBehaviour
{
    public float launchSpeed = 10f;
    private bool launching = false;

    public ParticleSystem launchEffect;
    public TextMeshProUGUI countdownText;
    public GameObject launchButton; // Segundo botón para lanzar el cohete
    public TextMeshProUGUI instructionText; // Texto que dice "Presiona este botón..."
    public TechoMover techo; // Referencia al script que controla el techo
    public GameObject firstButton; // Primer botón

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

        // Mostrar texto de instrucción
        if (instructionText != null)
            instructionText.text = "Presiona el botón para iniciar el despegue";

        // Mostrar el segundo botón
        if (launchButton != null)
            launchButton.SetActive(true);

        // Detener el reinicio automático
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);
    }

    public void LaunchRocket()
    {
        // Si ya se presionó el botón, ocultamos la instrucción y reproducimos el efecto visual
        if (instructionText != null)
            instructionText.text = ""; // Quitar mensaje de instrucción

        if (launchEffect != null)
            launchEffect.Play();

        launching = true; // El cohete comienza a moverse
    }

    void Update()
    {
        if (launching)
            transform.Translate(Vector3.up * launchSpeed * Time.deltaTime);
    }
}
