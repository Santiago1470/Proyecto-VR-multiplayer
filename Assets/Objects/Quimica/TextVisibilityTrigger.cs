using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RobustUIElementsWithPagination : MonoBehaviour
{
    [Header("UI Referencias")]
    public TextMeshProUGUI textToControl; // El texto UI 
    public GameObject panelToControl; // El panel UI que contiene el texto
    
    [Header("Referencias de Paginación")]
    public Button nextPageButton; // Botón para ir a la siguiente página
    public Button previousPageButton; // Botón para ir a la página anterior
    public TextMeshProUGUI pageIndicator; // Texto opcional para mostrar "Página X de Y"
    
    [Header("Contenido de Páginas")]
    [TextArea(3, 10)]
    public List<string> pageContents = new List<string>(); // Contenido de texto para cada página
    
    [Header("Configuración de Visibilidad")]
    public Transform playerHead; // La cámara o cabeza del jugador VR
    public float fadeDuration = 0.3f;
    public bool useDistanceCheck = true; // Activar comprobación de distancia adicional
    public float triggerDistance = 2.0f; // Distancia que corresponde al tamaño del trigger

    private Color originalTextColor;
    private CanvasGroup canvasGroup; // Para manejar la transparencia del panel
    private Coroutine fadeCoroutine;
    private bool isVisible = false;
    private Collider triggerCollider;
    private int currentPageIndex = 0;

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
                canvasGroup = canvasGroup = panelToControl.AddComponent<CanvasGroup>();
            }
        }
        
        // Configurar los botones de paginación
        SetupPaginationButtons();
        
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
        
        // Mostrar la primera página
        ShowCurrentPage();
    }

    private void SetupPaginationButtons()
    {
        // Configurar el botón de siguiente página
        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(NextPage);
        }
        
        // Configurar el botón de página anterior
        if (previousPageButton != null)
        {
            previousPageButton.onClick.AddListener(PreviousPage);
        }
        
        // Actualizar el estado inicial de los botones
        UpdatePaginationButtonsState();
    }

    public void NextPage()
    {
        if (currentPageIndex < pageContents.Count - 1)
        {
            currentPageIndex++;
            ShowCurrentPage();
            UpdatePaginationButtonsState();
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowCurrentPage();
            UpdatePaginationButtonsState();
        }
    }

    private void ShowCurrentPage()
    {
        // Actualizar el texto con el contenido de la página actual
        if (textToControl != null && pageContents.Count > 0 && currentPageIndex < pageContents.Count)
        {
            textToControl.text = pageContents[currentPageIndex];
        }
        
        // Actualizar el indicador de página si existe
        UpdatePageIndicator();
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicator != null && pageContents.Count > 0)
        {
            pageIndicator.text = "Página " + (currentPageIndex + 1) + " de " + pageContents.Count;
        }
    }

    private void UpdatePaginationButtonsState()
    {
        // Habilitar/deshabilitar botones según la posición actual
        if (previousPageButton != null)
        {
            previousPageButton.interactable = (currentPageIndex > 0);
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.interactable = (currentPageIndex < pageContents.Count - 1);
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
        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(true);
        }
        if (previousPageButton != null)
        {
            previousPageButton.gameObject.SetActive(true);
        }
        if (pageIndicator != null)
        {
            pageIndicator.gameObject.SetActive(true);
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
            // Los botones y el indicador de página ya son desactivados en SetUIVisibility
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
        
        // Controlar la visibilidad de los botones de navegación
        if (nextPageButton != null)
        {
            CanvasGroup nextButtonCanvasGroup = nextPageButton.GetComponent<CanvasGroup>();
            if (nextButtonCanvasGroup == null)
            {
                nextButtonCanvasGroup = nextPageButton.gameObject.AddComponent<CanvasGroup>();
            }
            nextButtonCanvasGroup.alpha = alpha;
            nextButtonCanvasGroup.interactable = alpha > 0;
            nextButtonCanvasGroup.blocksRaycasts = alpha > 0;
            
            // Desactivar completamente el objeto si alpha es 0
            if (alpha <= 0)
            {
                nextPageButton.gameObject.SetActive(false);
            }
            else if (!nextPageButton.gameObject.activeSelf)
            {
                nextPageButton.gameObject.SetActive(true);
            }
        }
        
        if (previousPageButton != null)
        {
            CanvasGroup prevButtonCanvasGroup = previousPageButton.GetComponent<CanvasGroup>();
            if (prevButtonCanvasGroup == null)
            {
                prevButtonCanvasGroup = previousPageButton.gameObject.AddComponent<CanvasGroup>();
            }
            prevButtonCanvasGroup.alpha = alpha;
            prevButtonCanvasGroup.interactable = alpha > 0;
            prevButtonCanvasGroup.blocksRaycasts = alpha > 0;
            
            // Desactivar completamente el objeto si alpha es 0
            if (alpha <= 0)
            {
                previousPageButton.gameObject.SetActive(false);
            }
            else if (!previousPageButton.gameObject.activeSelf)
            {
                previousPageButton.gameObject.SetActive(true);
            }
        }
        
        // Controlar la visibilidad del indicador de página
        if (pageIndicator != null)
        {
            Color originalIndicatorColor = pageIndicator.color;
            pageIndicator.color = new Color(originalIndicatorColor.r, originalIndicatorColor.g, originalIndicatorColor.b, alpha);
            
            if (alpha <= 0)
            {
                pageIndicator.gameObject.SetActive(false);
            }
            else if (!pageIndicator.gameObject.activeSelf)
            {
                pageIndicator.gameObject.SetActive(true);
            }
        }
    }
}