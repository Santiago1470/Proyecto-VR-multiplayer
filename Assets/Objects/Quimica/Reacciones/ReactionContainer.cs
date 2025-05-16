// ReactionContainer.cs - Script principal para gestionar las reacciones químicas
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

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
    
    // Componente auxiliar que maneja los datos y lógica de química
    private ChemistryManager chemManager;
    
    // Estado interno
    private List<ChemicalTube.ChemicalElement> elements = new List<ChemicalTube.ChemicalElement>();
    private List<string> completedReactions = new List<string>();
    private int currentObjectiveIndex = 0;
    private bool finalRewardGiven = false;
    private bool reactionProcessing = false;


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
        
        // Verificar si es el objetivo actual
        if (formula == chemManager.objectiveCompounds[currentObjectiveIndex])
            yield return CelebrateReaction(formula);
        else if (chemManager.objectiveCompounds.Contains(formula))
            AddCompletedReaction(formula);
        else
            AddCompletedReaction(formula);
        
        // Verificar si se han completado todos los objetivos
        bool allCompleted = CheckAllObjectivesCompleted();
        
        if (allCompleted && !finalRewardGiven)
        {
            yield return ShowCompletionMessage();
            SpawnFinalReward();
            finalRewardGiven = true;
        }
        
        yield return new WaitForSeconds(1.0f);
        
        if (autoClearOnReactionComplete)
        {
            elements.Clear();
            UpdateLiquidVisual();
            UpdateFormula();
        }
        
        if (!allCompleted)
            UpdateNextObjective();
        
        reactionProcessing = false;
    }

    private IEnumerator CelebrateReaction(string formula)
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
            reactionNameText.text = $"¡{chemManager.GetCompoundName(formula)} completado!";
            reactionNameText.color = Color.green;
            
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
        
        AddCompletedReaction(formula);
    }

    private IEnumerator StopParticlesAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (reactionParticles != null && reactionParticles.isPlaying)
            reactionParticles.Stop();
    }

    private IEnumerator ShowCompletionMessage()
    {
        yield return new WaitForSeconds(1.5f);
        
        if (audioSource != null && allObjectivesCompleteSound != null)
        {
            audioSource.volume = completionVolume;
            audioSource.PlayOneShot(allObjectivesCompleteSound);
        }
        
        if (reactionNameText != null)
        {
            reactionNameText.text = "¡TODOS LOS OBJETIVOS COMPLETADOS!";
            reactionNameText.color = new Color(1f, 0.84f, 0f);
        }
        
        if (objectiveText != null)
        {
            objectiveText.text = "<b>¡Todos los objetivos completados!</b>";
            objectiveText.color = Color.green;
        }
    }

    private void AddCompletedReaction(string formula)
    {
        if (!completedReactions.Contains(formula))
        {
            completedReactions.Add(formula);
            UpdateCompletedReactionsText();
            UpdateObjectiveText();
        }
    }

    private bool CheckAllObjectivesCompleted()
    {
        return chemManager.objectiveCompounds.All(obj => completedReactions.Contains(obj));
    }

    private void UpdateNextObjective()
    {
        for (int i = 0; i < chemManager.objectiveCompounds.Count; i++)
        {
            if (!completedReactions.Contains(chemManager.objectiveCompounds[i]))
            {
                currentObjectiveIndex = i;
                UpdateObjectiveText();
                break;
            }
        }
    }
    
    private void UpdateUI()
    {
        UpdateFormula();
        UpdateObjectiveText();
        UpdateCompletedReactionsText();
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

    private void UpdateObjectiveText()
    {
        if (objectiveText == null || chemManager.objectiveCompounds.Count == 0) return;
        
        bool allCompleted = CheckAllObjectivesCompleted();
        
        objectiveText.text = allCompleted 
            ? "<b>¡Todos los objetivos completados!</b>" 
            : $"<b>Objetivo:</b> Crear {chemManager.GetCompoundName(chemManager.objectiveCompounds[currentObjectiveIndex])}";
        
        objectiveText.color = allCompleted ? Color.green : Color.white;
    }
    
    private void SpawnFinalReward()
    {
        if (finalRewardPrefab == null) return;

        Vector3 spawnPosition = rewardSpawnPoint != null 
            ? rewardSpawnPoint.position 
            : transform.position + Vector3.up * rewardDropHeight;

        GameObject reward = Instantiate(finalRewardPrefab, spawnPosition, Random.rotation);
        Rigidbody rb = reward.GetComponent<Rigidbody>() ?? reward.AddComponent<Rigidbody>();
        
        rb.linearDamping = 3.0f;
        rb.angularDamping = 2.0f;
        
        rb.AddForce(new Vector3(Random.Range(-0.1f, 0.1f), -rewardDropForce, Random.Range(-0.1f, 0.1f)), 
                    ForceMode.Impulse);
        
        rb.AddTorque(new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f)), 
                     ForceMode.Impulse);
    }
}