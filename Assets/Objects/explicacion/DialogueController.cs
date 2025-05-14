using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using System.Collections;

[System.Serializable]
public struct DialogueStep
{
    public string text;
    public Texture image;
}

public class DialogueController : MonoBehaviour
{
    [Header("Componentes UI")]
    public TextMeshProUGUI dialogueText;
    public RawImage dialogueImage;
    public Button nextButton;
    public TextMeshProUGUI buttonText;
    public CanvasGroup dialogueCanvasGroup; 

    [Header("Contenido del diálogo")]
    public DialogueStep[] steps;

    [Header("Paredes invisibles a desactivar")]
    public GameObject[] wallsToDisable;

    // Referencias a proveedores de locomoción XR
    private ActionBasedContinuousMoveProvider continuousMove;
    private ActionBasedSnapTurnProvider snapTurn;
    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportProvider;

    private int currentIndex = 0;
    private float fadeDuration = 1f; // Duración del fade

    void Awake()
    {
        continuousMove = FindObjectOfType<ActionBasedContinuousMoveProvider>();
        snapTurn = FindObjectOfType<ActionBasedSnapTurnProvider>();
        teleportProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
    }

    void Start()
    {
        SetLocomotionEnabled(false);
        nextButton.onClick.AddListener(OnNextClicked);
        ShowStep(currentIndex);
        // Asegurar estado inicial de alpha a 1
        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 1f;
    }

    void ShowStep(int idx)
    {
        dialogueText.text = steps[idx].text;

        if (steps[idx].image != null)
        {
            dialogueImage.texture = steps[idx].image;
            dialogueImage.gameObject.SetActive(true);
        }
        else
        {
            dialogueImage.gameObject.SetActive(false);
        }

        bool isLast = (idx == steps.Length - 1);
        buttonText.text = isLast ? "Cerrar" : "Siguiente";
    }

    void OnNextClicked()
    {
        if (currentIndex < steps.Length - 1)
        {
            currentIndex++;
            ShowStep(currentIndex);
        }
        else
        {
            // Iniciar fade out y luego cerrar
            if (dialogueCanvasGroup != null)
                StartCoroutine(FadeAndClose());
            else
            {
                // Si no hay CanvasGroup, cerrar inmediatamente
                CloseDialogue();
            }
        }
    }

    private IEnumerator FadeAndClose()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            dialogueCanvasGroup.alpha = alpha;
            yield return null;
        }
        dialogueCanvasGroup.alpha = 0f;

        CloseDialogue();
    }

    private void CloseDialogue()
    {
        // Desactivar paredes invisibles
        foreach (var wall in wallsToDisable)
        {
            if (wall != null)
                wall.SetActive(false);
        }
        // Desactivar el diálogo
        gameObject.SetActive(false);
        // Reactivar locomoción
        SetLocomotionEnabled(true);
    }

    private void SetLocomotionEnabled(bool enabled)
    {
        if (continuousMove != null)
            continuousMove.enabled = enabled;
        if (snapTurn != null)
            snapTurn.enabled = enabled;
        if (teleportProvider != null)
            teleportProvider.enabled = enabled;
    }
}
