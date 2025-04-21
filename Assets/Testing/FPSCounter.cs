using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;
    private float timer = 0.0f;
    private float refreshRate = 0.15f; // Tiempo entre actualizaciones, en segundos

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timer += Time.unscaledDeltaTime;

        if (timer >= refreshRate)
        {
            float fps = 1.0f / deltaTime;
            fpsText.text = $"FPS: {Mathf.CeilToInt(fps)}";
            timer = 0f;
        }
    }
}

