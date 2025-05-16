using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using System;

public class ReactionContainer : MonoBehaviour
{
    [SerializeField] private Material liquidMaterial;
    [SerializeField] private Color emptyColor = new Color(0.9f, 0.9f, 0.9f, 0.4f);
    [SerializeField] private TextMeshProUGUI reactionText, reactionNameText, objectiveText, completedReactionsText;
    [SerializeField] private GameObject finalRewardPrefab;
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private float rewardDropHeight = 5f, rewardDropForce = 0.5f;
    [SerializeField] private bool autoClearOnReactionComplete = true;
    [SerializeField] private XRSimpleInteractable deleteLastButton, clearAllButton;
    [SerializeField] private ParticleSystem reactionParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip reactionCompleteSound, allObjectivesCompleteSound;
    [SerializeField] private float reactionVolume = 0.7f, completionVolume = 1.0f;
    
    // Delegado para el evento de reacción completada
    public event Action<string> OnReactionCompleted;
    
    // Componente auxiliar que maneja los datos y lógica de química
    private ChemistryManager chemManager;
    
    // Estado interno
    private List<ChemicalTube.ChemicalElement> elements = new List<ChemicalTube.ChemicalElement>();
    private List<string> completedReactions = new List<string>();
    private int currentObjectiveIndex = 0;
    private bool finalRewardGiven = false;
    private bool reactionProcessing = false;
    
    // Control para evitar duplicar notificaciones en multiplayer
    private HashSet<string> notifiedCompletedReactions = new HashSet<string>();

    private void Start()
    {
        // Inicializar el gestor de química
        chemManager = gameObject.AddComponent<ChemistryManager>();
        
        // Configurar el material del líquido
        UpdateLiquidVisual();
        
        // Configurar el AudioSource si es necesario
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }

        // Crear punto de spawn para recompensas si no existe
        if (rewardSpawnPoint == null)
        {
            rewardSpawnPoint = new GameObject("RewardSpawnPoint").transform;
            rewardSpawnPoint.position = transform.position + Vector3.up * rewardDropHeight;
        }
        
        // Configurar botones VR
        if (deleteLastButton != null)
            deleteLastButton.selectEntered.AddListener((_) => { if (elements.Count > 0 && !reactionProcessing) RemoveLastElement(); });
        
        if (clearAllButton != null)
            clearAllButton.selectEntered.AddListener((_) => { if (elements.Count > 0 && !reactionProcessing) ClearAllElements(); });
        
        // Actualizar el primer objetivo
        UpdateCurrentObjectiveText();
        
        UpdateUI();
    }
    
    public void RegisterPour(ChemicalTube.ChemicalElement element)
    {
        if (reactionProcessing) return;
        
        elements.Add(element);
        UpdateLiquidVisual();
        UpdateFormula();
        
        // Verificar si se ha formado un compuesto conocido
        string formula = chemManager.GetFormulaFromElements(elements);
        if (chemManager.IsKnownCompound(formula) && !completedReactions.Contains(formula))
            StartCoroutine(ProcessCompletedReaction(formula));
    }

    public void RemoveLastElement()
    {
        if (elements.Count == 0 || reactionProcessing) return;
        
        elements.RemoveAt(elements.Count - 1);
        UpdateLiquidVisual();
        UpdateFormula();
    }

    public void ClearAllElements()
    {
        if (reactionProcessing) return;
        
        elements.Clear();
        UpdateLiquidVisual();
        UpdateFormula();
    }
    
    private void UpdateLiquidVisual()
    {
        if (liquidMaterial == null) return;
        
        string formula = chemManager.GetFormulaFromElements(elements);
        Color color = chemManager.GetColorForFormula(formula, elements, emptyColor);
        
        // Asegurar transparencia adecuada
        if (color.a > 0.95f) color.a = 0.8f;
        
        liquidMaterial.SetColor("_BaseColor", color);
        liquidMaterial.SetColor("_Color", color);
    }
    
    private void UpdateFormula()
    {
        string formula = chemManager.GetFormulaFromElements(elements);
        
        // Actualizar texto de fórmula
        if (reactionText != null)
            reactionText.text = chemManager.FormatFormulaWithSubscripts(formula);
        
        // Actualizar nombre de reacción
        UpdateReactionName(formula);
    }
    
    private void UpdateReactionName(string formula)
    {
        if (reactionNameText == null) return;
        
        if (chemManager.IsKnownCompound(formula))
        {
            reactionNameText.text = chemManager.GetCompoundName(formula);
            reactionNameText.color = Color.white;
        }
        else if (elements.Count > 0)
        {
            reactionNameText.text = "Mezclando...";
            reactionNameText.color = new Color(0.8f, 0.8f, 0.2f);
        }
        else
        {
            reactionNameText.text = "Contenedor vacío";
            reactionNameText.color = Color.gray;
        }
    }
    
    private IEnumerator ProcessCompletedReaction(string formula)
    {
        reactionProcessing = true;
        
        // Evitar procesamiento del mismo compuesto más de una vez
        if (notifiedCompletedReactions.Contains(formula))
        {
            reactionProcessing = false;
            yield break;
        }
        
        notifiedCompletedReactions.Add(formula);
        
        // Verificar si es un compuesto objetivo
        bool isObjective = chemManager.objectiveCompounds.Contains(formula);
        bool isCurrentObjective = isObjective && 
            currentObjectiveIndex < chemManager.objectiveCompounds.Count && 
            formula == chemManager.objectiveCompounds[currentObjectiveIndex];
        
        // Celebración visual de la reacción creada
        yield return CelebrateReaction(formula, isObjective);
        
        // Añadir a la lista de completados localmente
        AddCompletedReaction(formula);
        
        // Avanzar al siguiente objetivo si se completó el actual
        if (isCurrentObjective)
        {
            yield return RevealNextObjective();
        }
        
        // Notificar al MultiplayerChemistryManager
        OnReactionCompleted?.Invoke(formula);
        
        yield return new WaitForSeconds(1.0f);
        
        if (autoClearOnReactionComplete)
        {
            elements.Clear();
            UpdateLiquidVisual();
            UpdateFormula();
        }
        
        reactionProcessing = false;
    }

    private IEnumerator CelebrateReaction(string formula, bool isObjective)
    {
        if (reactionParticles != null)
        {
            reactionParticles.Clear();
            reactionParticles.Play();
            StartCoroutine(StopParticlesAfterDelay(2.0f));
        }
        
        if (audioSource != null && reactionCompleteSound != null)
        {
            audioSource.volume = reactionVolume;
            audioSource.PlayOneShot(reactionCompleteSound);
        }
        
        if (reactionNameText != null)
        {
            string displayText = isObjective ? 
                $"¡{chemManager.GetCompoundName(formula)} completado (Objetivo)!" : 
                $"¡{chemManager.GetCompoundName(formula)} completado!";
                
            reactionNameText.text = displayText;
            reactionNameText.color = isObjective ? Color.green : new Color(0.3f, 0.6f, 1f);
            
            float duration = 2.0f;
            float startTime = Time.time;
            
            while (Time.time - startTime < duration)
            {
                float pulseValue = Mathf.PingPong((Time.time - startTime) * 2, 1);
                reactionNameText.transform.localScale = Vector3.one * (1 + pulseValue * 0.2f);
                yield return null;
            }
            
            reactionNameText.transform.localScale = Vector3.one;
            reactionNameText.color = Color.white;
        }
    }

    private IEnumerator StopParticlesAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (reactionParticles != null && reactionParticles.isPlaying)
            reactionParticles.Stop();
    }

    private void AddCompletedReaction(string formula)
    {
        if (!completedReactions.Contains(formula))
        {
            completedReactions.Add(formula);
            UpdateCompletedReactionsText();
            
            // Si es el objetivo actual, actualizar el índice
            if (currentObjectiveIndex < chemManager.objectiveCompounds.Count && 
                formula == chemManager.objectiveCompounds[currentObjectiveIndex])
            {
                currentObjectiveIndex++;
                UpdateCurrentObjectiveText();
            }
        }
    }
    
    private IEnumerator RevealNextObjective()
    {
        // Efectos visuales para revelar el siguiente objetivo
        if (objectiveText != null)
        {
            // Hacer desaparecer el texto actual
            float fadeDuration = 0.5f;
            float startTime = Time.time;
            Color startColor = objectiveText.color;
            Color transparentColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            
            while (Time.time - startTime < fadeDuration)
            {
                float t = (Time.time - startTime) / fadeDuration;
                objectiveText.color = Color.Lerp(startColor, transparentColor, t);
                yield return null;
            }
            
            // Actualizar al siguiente objetivo
            UpdateCurrentObjectiveText();
            
            // Hacer aparecer el nuevo texto
            startTime = Time.time;
            while (Time.time - startTime < fadeDuration)
            {
                float t = (Time.time - startTime) / fadeDuration;
                objectiveText.color = Color.Lerp(transparentColor, startColor, t);
                yield return null;
            }
            
            objectiveText.color = startColor;
        }
        
        // Si hemos completado todos los objetivos
        if (currentObjectiveIndex >= chemManager.objectiveCompounds.Count && !finalRewardGiven)
        {
            yield return CelebrateAllObjectivesCompleted();
        }
    }
    
    private IEnumerator CelebrateAllObjectivesCompleted()
    {
        if (audioSource != null && allObjectivesCompleteSound != null)
        {
            audioSource.volume = completionVolume;
            audioSource.PlayOneShot(allObjectivesCompleteSound);
        }
        
        // Mostrar mensaje de felicitación
        if (objectiveText != null)
        {
            objectiveText.text = "<color=#FFD700><b>¡TODOS LOS OBJETIVOS COMPLETADOS!</b></color>";
            
            // Efecto de pulsación
            float duration = 3.0f;
            float startTime = Time.time;
            
            while (Time.time - startTime < duration)
            {
                float scale = 1.0f + 0.2f * Mathf.Sin((Time.time - startTime) * 5f);
                objectiveText.transform.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }
            
            objectiveText.transform.localScale = Vector3.one;
        }
        
        // Generar recompensa si está configurada
        if (finalRewardPrefab != null && rewardSpawnPoint != null && !finalRewardGiven)
        {
            GameObject reward = Instantiate(finalRewardPrefab, rewardSpawnPoint.position, Quaternion.identity);
            if (reward.GetComponent<Rigidbody>() != null)
            {
                reward.GetComponent<Rigidbody>().AddForce(UnityEngine.Random.insideUnitSphere * rewardDropForce, ForceMode.Impulse);
                reward.GetComponent<Rigidbody>().AddTorque(UnityEngine.Random.insideUnitSphere * rewardDropForce, ForceMode.Impulse);
            }
            
            finalRewardGiven = true;
        }
    }
    
    private void UpdateUI()
    {
        UpdateFormula();
        UpdateCompletedReactionsText();
        UpdateCurrentObjectiveText();
    }

    private void UpdateCompletedReactionsText()
    {
        if (completedReactionsText == null) return;
        
        if (completedReactions.Count > 0)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>Reacciones completadas:</b>");
            
            foreach (string formula in completedReactions.Where(f => chemManager.objectiveCompounds.Contains(f)))
                sb.AppendLine($"<color=#00FF00>✓ {chemManager.GetCompoundName(formula)}</color>");
            
            foreach (string formula in completedReactions.Where(f => !chemManager.objectiveCompounds.Contains(f)))
                sb.AppendLine($"✔{chemManager.GetCompoundName(formula)}");
            
            completedReactionsText.text = sb.ToString();
        }
        else
        {
            completedReactionsText.text = "<b>Reacciones completadas:</b>\nNinguna todavía";
        }
    }
    
    private void UpdateCurrentObjectiveText()
    {
        if (objectiveText == null) return;
        
        // Si se han completado todos los objetivos
        if (currentObjectiveIndex >= chemManager.objectiveCompounds.Count)
        {
            objectiveText.text = "<color=#FFD700><b>¡TODOS LOS OBJETIVOS COMPLETADOS!</b></color>";
            return;
        }
        
        // Mostrar sólo el objetivo actual
        string currentObjective = chemManager.objectiveCompounds[currentObjectiveIndex];
        string objectiveName = chemManager.GetCompoundName(currentObjective);
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Objetivo Actual:</b>");
        sb.AppendLine($"Crear: <color=#FFFFFF>{objectiveName}</color>");
        
        // Opcionalmente mostrar progreso
        if (currentObjectiveIndex > 0)
        {
            sb.AppendLine($"\n<size=80%>Progreso: {currentObjectiveIndex}/{chemManager.objectiveCompounds.Count}</size>");
        }
        
        objectiveText.text = sb.ToString();
    }
    
    // Método público para verificar si un compuesto ya ha sido completado
    public bool IsCompoundCompleted(string formula)
    {
        return completedReactions.Contains(formula);
    }

    // Método público para obtener la lista de reacciones completadas
    public List<string> GetCompletedReactions()
    {
        return new List<string>(completedReactions);
    }
}