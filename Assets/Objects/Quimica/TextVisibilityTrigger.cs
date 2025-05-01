using UnityEngine;
using TMPro;
using System.Collections;

public class RobustUIElementsVisibility : MonoBehaviour
{
    public TextMeshProUGUI textToControl; // El texto UI 
    public GameObject panelToControl; // El panel UI que contiene el texto
    public Transform playerHead; // La cámara o cabeza del jugador VR
    public float fadeDuration = 0.3f;
    public bool useDistanceCheck = true; // Activar comprobación de distancia adicional
    public float triggerDistance = 2.0f; // Distancia que corresponde al tamaño del trigger

    private Color originalTextColor;
    private CanvasGroup canvasGroup; // Para manejar la transparencia del panel
    private Coroutine fadeCoroutine;
    private bool isVisible = false;
    private Collider triggerCollider;

    private void Start()
    {
        // Obtener el collider de este objeto
        triggerCollider = GetComponent<Collider>();
        
        // Configurar el texto
        if (textToControl != null)
        {
            originalTextColor = textToControl.color;
        }
        
        // Configurar el panel
        if (panelToControl != null)
        {
            // Intentar obtener un CanvasGroup o añadir uno nuevo
            canvasGroup = panelToControl.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelToControl.AddComponent<CanvasGroup>();
            }
        }
        
        // Ocultar los elementos UI al inicio
        SetUIVisibility(0);
        
        // Si no se asignó manualmente, buscar la cámara principal
        if (playerHead == null)
        {
            if (Camera.main != null)
            {
                playerHead = Camera.main.transform;
            }
        }
    }

    private void Update()
    {
        if (useDistanceCheck && playerHead != null)
        {
            bool shouldBeVisible;
            
            if (triggerCollider != null)
            {
                // Verificar si el punto más cercano en el collider está dentro de la distancia
                Vector3 closestPoint = triggerCollider.ClosestPoint(playerHead.position);
                float distance = Vector3.Distance(closestPoint, playerHead.position);
                
                // Si el punto más cercano es muy cercano o el jugador está dentro del collider
                shouldBeVisible = distance < 0.1f || triggerCollider.bounds.Contains(playerHead.position);
            }
            else
            {
                // Fallback a simple comprobación de distancia si no hay collider
                float distance = Vector3.Distance(transform.position, playerHead.position);
                shouldBeVisible = distance <= triggerDistance;
            }
            
            // Cambiar la visibilidad solo si ha cambiado el estado
            if (shouldBeVisible != isVisible)
            {
                if (shouldBeVisible)
                {
                    FadeInUI();
                }
                else
                {
                    FadeOutUI();
                }
            }
        }
    }

    // Eventos de trigger para redundancia
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            FadeInUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            FadeOutUI();
        }
    }

    private void FadeInUI()
    {
        isVisible = true;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeUICoroutine(0, 1));
    }

    private void FadeOutUI()
    {
        isVisible = false;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeUICoroutine(1, 0));
    }

    private IEnumerator FadeUICoroutine(float startAlpha, float targetAlpha)
    {
        // Activar los elementos UI para el fade
        if (textToControl != null)
        {
            textToControl.enabled = true;
        }
        if (panelToControl != null)
        {
            panelToControl.SetActive(true);
        }
        
        // Establecer alpha inicial
        SetUIVisibility(startAlpha);
        
        float elapsedTime = 0;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            
            SetUIVisibility(newAlpha);
            
            yield return null;
        }
        
        // Asegurar el valor final
        SetUIVisibility(targetAlpha);
        
        // Si el fade fue a transparente, desactivar los elementos UI completamente
        if (targetAlpha <= 0)
        {
            if (textToControl != null)
            {
                textToControl.enabled = false;
            }
            if (panelToControl != null)
            {
                panelToControl.SetActive(false);
            }
        }
    }

    private void SetUIVisibility(float alpha)
    {
        // Ajustar alpha del texto
        if (textToControl != null)
        {
            textToControl.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
        }
        
        // Ajustar alpha del panel usando CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            
            // Opcional: también deshabilitar interacciones cuando está invisible
            if (alpha <= 0)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }
}