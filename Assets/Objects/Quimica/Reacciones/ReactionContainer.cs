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
    [Header("Visual Elements")]
    [SerializeField] private Material liquidMaterial;
    [SerializeField] private Color emptyColor = new Color(0.9f, 0.9f, 0.9f, 0.4f);

    [Header("Reaction Display")]
    [SerializeField] private TextMeshProUGUI reactionText;
    [SerializeField] private TextMeshProUGUI reactionNameText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI completedReactionsText;

    [Header("Final Reward")]
    [SerializeField] private GameObject finalRewardPrefab;
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private float rewardDropHeight = 5f;
    [SerializeField] private float rewardDropForce = 0.5f;
    
    [Header("Objectives")]
    [SerializeField] private List<string> objectiveCompounds = new List<string>() { 
        "H2O", "CO2", "NH3", "CH4", "H2SO4", "Cl2", "C2H5OH", "HCl" 
    };
    [SerializeField] private bool autoClearOnReactionComplete = true;
    
    [Header("VR Controls")]
    [SerializeField] private XRSimpleInteractable deleteLastButton;
    [SerializeField] private XRSimpleInteractable clearAllButton;

    // Constantes para el shader
    private const float MIN_FILL_AMOUNT = 0.002f;
    private const float FILL_AMOUNT_PER_ELEMENT = 0.001f;
    private const float MAX_FILL_AMOUNT = 0.01f;

    // Shader Property IDs
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

    // Estado interno
    private List<ChemicalTube.ChemicalElement> elements = new List<ChemicalTube.ChemicalElement>();
    private List<string> completedReactions = new List<string>();
    private int currentObjectiveIndex = 0;
    private bool finalRewardGiven = false;
    private bool deleteLastInteractable = false;
    private bool clearAllInteractable = false;

    // Diccionarios para mapeo de elementos y compuestos
    private readonly Dictionary<string, Color> chemicalCompounds = new Dictionary<string, Color>() {
        {"H2O", new Color(0f, 0.6f, 1f, 0.8f)},
        {"CO2", new Color(0.7f, 0.7f, 0.7f, 0.8f)},
        {"NH3", new Color(0.6f, 0.8f, 0.2f, 0.8f)},
        {"CH4", new Color(0.3f, 0.5f, 0.9f, 0.8f)},
        {"H2SO4", new Color(1f, 0.4f, 0.1f, 0.8f)},
        {"Cl2", new Color(0.2f, 0.9f, 0.2f, 0.8f)},
        {"C2H5OH", new Color(0.7f, 0.3f, 0.7f, 0.8f)},
        {"HCl", new Color(0.9f, 0.9f, 0.2f, 0.8f)}
    };

    private readonly Dictionary<string, string> compoundNames = new Dictionary<string, string>() {
        {"H2O", "Agua"},
        {"CO2", "Dióxido de Carbono"},
        {"NH3", "Amoníaco"},
        {"CH4", "Metano"},
        {"H2SO4", "Ácido Sulfúrico"},
        {"Cl2", "Cloro Molecular"},
        {"C2H5OH", "Etanol"},
        {"HCl", "Ácido Clorhídrico"}
    };

    private readonly Dictionary<ChemicalTube.ChemicalElement, Color> elementColors = new Dictionary<ChemicalTube.ChemicalElement, Color>() {
        {ChemicalTube.ChemicalElement.Hydrogen, new Color(0.5f, 0.8f, 1f, 0.8f)},
        {ChemicalTube.ChemicalElement.Oxygen, new Color(1f, 0.2f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Carbon, new Color(0.2f, 0.2f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Nitrogen, new Color(1f, 1f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Chlorine, new Color(0.2f, 0.8f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Sulfur, new Color(0.9f, 0.8f, 0.2f, 0.8f)}
    };

    private void Awake()
    {
        ValidateLiquidMaterial();
    }

    private void Start()
    {
        InitializeComponents();
        SetupVRButtons();
        UpdateUI();
    }

    #region Initialization Methods
    
    private void ValidateLiquidMaterial()
    {
        if (liquidMaterial == null)
        {
            Debug.LogError("Material del líquido no asignado. Asigna el material en el Inspector.");
            return;
        }
        
        if (!liquidMaterial.HasProperty(FillAmountId))
        {
            Debug.LogWarning("El material no tiene la propiedad _FillAmount. Verifica el nombre en el shader.");
        }
    }

    private void InitializeComponents()
    {
        // Inicializar líquido
        if (liquidMaterial != null)
        {
            SetLiquidColor(emptyColor);
            UpdateLiquidVisual(MIN_FILL_AMOUNT);
        }

        // Inicializar punto de spawn para recompensas
        InitializeRewardSpawnPoint();
        
        // Verificar prefab de recompensa
        if (finalRewardPrefab == null)
        {
            Debug.LogWarning("Prefab de recompensa final no asignado. No se entregará recompensa al completar los objetivos.");
        }
    }

    private void InitializeRewardSpawnPoint()
    {
        if (rewardSpawnPoint == null)
        {
            GameObject spawnPointObj = new GameObject("RewardSpawnPoint");
            rewardSpawnPoint = spawnPointObj.transform;
            rewardSpawnPoint.position = new Vector3(transform.position.x, transform.position.y + rewardDropHeight, transform.position.z);
            Debug.LogWarning("Punto de spawn para recompensas no asignado. Se ha creado uno predeterminado.");
        }
    }

    private void SetupVRButtons()
    {
        if (deleteLastButton != null)
        {
            deleteLastButton.selectEntered.AddListener(DeleteLastButtonPressed);
        }
        else
        {
            Debug.LogWarning("Botón VR de eliminar último elemento no asignado");
        }

        if (clearAllButton != null)
        {
            clearAllButton.selectEntered.AddListener(ClearAllButtonPressed);
        }
        else
        {
            Debug.LogWarning("Botón VR de limpiar todos los elementos no asignado");
        }
    }

    private void UpdateUI()
    {
        UpdateFormula();
        UpdateButtonStates();
        UpdateObjectiveText();
        UpdateCompletedReactionsText();
    }
    
    #endregion

    #region Button Handlers
    
    private void DeleteLastButtonPressed(SelectEnterEventArgs args)
    {
        if (deleteLastInteractable)
        {
            RemoveLastElement();
        }
    }

    private void ClearAllButtonPressed(SelectEnterEventArgs args)
    {
        if (clearAllInteractable)
        {
            ClearAllElements();
        }
    }
    
    #endregion

    #region Public Methods
    
    public void RegisterPour(ChemicalTube.ChemicalElement element)
    {
        elements.Add(element);
        float newLevel = CalculateLiquidLevel();
        UpdateLiquidVisual(newLevel);
        UpdateFormula();
        UpdateButtonStates();
    }

    public void RemoveLastElement()
    {
        if (elements.Count > 0)
        {
            elements.RemoveAt(elements.Count - 1);
            float newLevel = CalculateLiquidLevel();
            UpdateLiquidVisual(newLevel);
            UpdateFormula();
            UpdateButtonStates();
        }
    }

    public void ClearAllElements()
    {
        elements.Clear();
        UpdateLiquidVisual(MIN_FILL_AMOUNT);
        UpdateFormula();
        UpdateButtonStates();
    }
    
    #endregion

    #region Liquid Visualization
    
    private float CalculateLiquidLevel()
    {
        return elements.Count > 0 ? 
            Mathf.Min(elements.Count * FILL_AMOUNT_PER_ELEMENT, MAX_FILL_AMOUNT) : 
            MIN_FILL_AMOUNT;
    }
    
    private void UpdateLiquidVisual(float level)
    {
        if (liquidMaterial == null) return;

        level = Mathf.Clamp(level, MIN_FILL_AMOUNT, MAX_FILL_AMOUNT);
        liquidMaterial.SetFloat(FillAmountId, level);
        
        // Actualizar color según fórmula
        string formula = GetCurrentFormula();
        Color newColor = GetColorForFormula(formula);
        SetLiquidColor(newColor);
        
        // Verificar si se completó alguna reacción
        if (chemicalCompounds.ContainsKey(formula))
        {
            CheckForCompletedReaction(formula);
        }
    }

    private Color GetColorForFormula(string formula)
    {
        if (chemicalCompounds.ContainsKey(formula))
        {
            return chemicalCompounds[formula];
        }
        else if (elements.Count > 0)
        {
            return elementColors[elements.Last()];
        }
        else
        {
            return emptyColor;
        }
    }

    private void SetLiquidColor(Color color)
    {
        if (liquidMaterial == null) return;
        
        // Asegurar transparencia adecuada
        if (color.a > 0.95f)
            color.a = 0.8f;
            
        liquidMaterial.SetColor(BaseColorId, color);
        liquidMaterial.SetColor("_BaseColor", color);
        liquidMaterial.SetColor("_Color", color);
    }
    
    #endregion

    #region Chemical Formula Processing
    
    private string GetCurrentFormula()
    {
        Dictionary<ChemicalTube.ChemicalElement, int> elementCounts = CountElements();

        // Compuestos conocidos
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Hydrogen, 2, ChemicalTube.ChemicalElement.Oxygen, 1))
            return "H2O";
        
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Carbon, 1, ChemicalTube.ChemicalElement.Oxygen, 2))
            return "CO2";
        
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Nitrogen, 1, ChemicalTube.ChemicalElement.Hydrogen, 3))
            return "NH3";
        
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Carbon, 1, ChemicalTube.ChemicalElement.Hydrogen, 4))
            return "CH4";
        
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Hydrogen, 2, ChemicalTube.ChemicalElement.Sulfur, 1, ChemicalTube.ChemicalElement.Oxygen, 4))
            return "H2SO4";
        
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Chlorine, 2))
            return "Cl2";
        
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Carbon, 2, ChemicalTube.ChemicalElement.Hydrogen, 5, ChemicalTube.ChemicalElement.Oxygen, 1, ChemicalTube.ChemicalElement.Hydrogen, 1))
            return "C2H5OH";
        
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Hydrogen, 1, ChemicalTube.ChemicalElement.Chlorine, 1))
            return "HCl";
        
        // Construir fórmula genérica si no coincide con compuestos conocidos
        return BuildGenericFormula(elementCounts);
    }

    private Dictionary<ChemicalTube.ChemicalElement, int> CountElements()
    {
        var elementCounts = new Dictionary<ChemicalTube.ChemicalElement, int>();
        
        foreach (var element in elements)
        {
            if (elementCounts.ContainsKey(element))
                elementCounts[element]++;
            else
                elementCounts[element] = 1;
        }
        
        return elementCounts;
    }

    private bool IsCompound(Dictionary<ChemicalTube.ChemicalElement, int> containerElements, params object[] requiredElements)
    {
        // Special case for C2H5OH which has H twice
        if (requiredElements.Length == 8)
        {
            // The standard compound check won't work for ethanol because H appears twice
            // Manual check for C2H5OH
            ChemicalTube.ChemicalElement c = (ChemicalTube.ChemicalElement)requiredElements[0];
            ChemicalTube.ChemicalElement h1 = (ChemicalTube.ChemicalElement)requiredElements[2];
            ChemicalTube.ChemicalElement o = (ChemicalTube.ChemicalElement)requiredElements[4];
            ChemicalTube.ChemicalElement h2 = (ChemicalTube.ChemicalElement)requiredElements[6];
            
            int cCount = (int)requiredElements[1];
            int hTotal = (int)requiredElements[3] + (int)requiredElements[7];
            int oCount = (int)requiredElements[5];
            
            return containerElements.ContainsKey(c) && containerElements[c] == cCount &&
                   containerElements.ContainsKey(h1) && containerElements[h1] == hTotal &&
                   containerElements.ContainsKey(o) && containerElements[o] == oCount;
        }
        
        // Regular check for other compounds
        Dictionary<ChemicalTube.ChemicalElement, int> requiredCounts = new Dictionary<ChemicalTube.ChemicalElement, int>();
        
        for (int i = 0; i < requiredElements.Length; i += 2)
        {
            ChemicalTube.ChemicalElement element = (ChemicalTube.ChemicalElement)requiredElements[i];
            int count = (int)requiredElements[i + 1];
            
            if (requiredCounts.ContainsKey(element))
                requiredCounts[element] += count;
            else
                requiredCounts[element] = count;
        }
        
        if (containerElements.Count != requiredCounts.Count)
            return false;
            
        foreach (var kvp in requiredCounts)
        {
            if (!containerElements.ContainsKey(kvp.Key) || containerElements[kvp.Key] != kvp.Value)
                return false;
        }
        
        return true;
    }

    private string BuildGenericFormula(Dictionary<ChemicalTube.ChemicalElement, int> elementCounts)
    {
        string formula = "";
        
        foreach (var element in elementCounts.Keys.OrderBy(GetElementPriority))
        {
            formula += GetElementSymbol(element);
            if (elementCounts[element] > 1)
                formula += elementCounts[element].ToString();
        }
        
        return formula;
    }

    private int GetElementPriority(ChemicalTube.ChemicalElement element)
    {
        switch (element)
        {
            case ChemicalTube.ChemicalElement.Carbon: return 0;
            case ChemicalTube.ChemicalElement.Hydrogen: return 1;
            case ChemicalTube.ChemicalElement.Oxygen: return 2;
            case ChemicalTube.ChemicalElement.Nitrogen: return 3;
            case ChemicalTube.ChemicalElement.Sulfur: return 4;
            case ChemicalTube.ChemicalElement.Chlorine: return 5;
            default: return 99;
        }
    }

    private string GetElementSymbol(ChemicalTube.ChemicalElement element)
    {
        switch (element)
        {
            case ChemicalTube.ChemicalElement.Hydrogen: return "H";
            case ChemicalTube.ChemicalElement.Oxygen: return "O";
            case ChemicalTube.ChemicalElement.Carbon: return "C";
            case ChemicalTube.ChemicalElement.Nitrogen: return "N";
            case ChemicalTube.ChemicalElement.Chlorine: return "Cl";
            case ChemicalTube.ChemicalElement.Sulfur: return "S";
            default: return "?";
        }
    }
    
    #endregion

    #region Reaction Completion
    
    private void CheckForCompletedReaction(string formula)
    {
        if (!objectiveCompounds.Contains(formula) || completedReactions.Contains(formula)) 
            return;
            
        // Verificar si es el objetivo actual
        if (formula == objectiveCompounds[currentObjectiveIndex])
        {
            StartCoroutine(CelebrateReaction(formula));
        }
        else
        {
            // Es un compuesto válido pero no el objetivo actual
            AddCompletedReaction(formula, false);
        }
    }

    private IEnumerator CelebrateReaction(string formula)
    {
        yield return PlayCelebrationAnimation(formula);
        
        // Registrar como completado
        AddCompletedReaction(formula, true);
        
        // Verificar si se han completado todos los objetivos
        bool allCompleted = CheckAllObjectivesCompleted();
        
        if (allCompleted && !finalRewardGiven)
        {
            yield return ShowCompletionMessage();
            SpawnFinalReward();
            finalRewardGiven = true;
        }
        
        // Vaciar automáticamente los elementos si está habilitado
        if (autoClearOnReactionComplete)
        {
            yield return new WaitForSeconds(1.0f);
            ClearAllElements();
        }
        
        // Avanzar al siguiente objetivo si está disponible
        if (currentObjectiveIndex < objectiveCompounds.Count - 1)
        {
            currentObjectiveIndex++;
            UpdateObjectiveText();
        }
    }

    private IEnumerator PlayCelebrationAnimation(string formula)
    {
        if (reactionNameText != null)
        {
            reactionNameText.text = $"¡{compoundNames[formula]} completado!";
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
    }

    private IEnumerator ShowCompletionMessage()
    {
        yield return new WaitForSeconds(1.5f);
        
        if (reactionNameText != null)
        {
            reactionNameText.text = "¡TODOS LOS OBJETIVOS COMPLETADOS!";
            reactionNameText.color = new Color(1f, 0.84f, 0f); // Color dorado
        }
        
        if (objectiveText != null)
        {
            objectiveText.text = "<b>¡Todos los objetivos completados!</b>";
            objectiveText.color = Color.green;
        }
        
        yield return new WaitForSeconds(1.0f);
    }

    private void AddCompletedReaction(string formula, bool isObjective)
    {
        if (!completedReactions.Contains(formula))
        {
            completedReactions.Add(formula);
            UpdateCompletedReactionsText();
            UpdateObjectiveText();
            Debug.Log($"Reacción completada: {formula} (Es objetivo: {isObjective})");
        }
    }

    private bool CheckAllObjectivesCompleted()
    {
        foreach (string objective in objectiveCompounds)
        {
            if (!completedReactions.Contains(objective))
            {
                return false;
            }
        }
        return true;
    }
    
    #endregion

    #region UI Updates
    
    private void UpdateFormula()
    {
        string formula = GetCurrentFormula();
        
        // Actualizar texto de fórmula
        if (reactionText != null)
        {
            reactionText.text = FormatFormulaWithSubscripts(formula);
        }
        
        // Actualizar nombre de reacción
        UpdateReactionName(formula);
    }

    private void UpdateReactionName(string formula)
    {
        if (reactionNameText == null) return;
        
        if (compoundNames.ContainsKey(formula))
        {
            reactionNameText.text = compoundNames[formula];
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

    private string FormatFormulaWithSubscripts(string formula)
    {
        string result = "";
        
        for (int i = 0; i < formula.Length; i++)
        {
            if (i < formula.Length - 1 && char.IsDigit(formula[i+1]))
            {
                result += formula[i];
                result += "<sub>" + formula[i+1] + "</sub>";
                i++; // Saltar el número ya procesado
            }
            else
            {
                result += formula[i];
            }
        }
        
        return result;
    }

    private void UpdateCompletedReactionsText()
    {
        if (completedReactionsText == null) return;
        
        if (completedReactions.Count > 0)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>Reacciones completadas:</b>");
            
            // Primero objetivos completados
            foreach (string formula in completedReactions.Where(f => objectiveCompounds.Contains(f)))
            {
                sb.AppendLine($"<color=#00FF00>✓ {compoundNames[formula]}</color>");
            }
            
            // Luego otras reacciones válidas
            foreach (string formula in completedReactions.Where(f => !objectiveCompounds.Contains(f)))
            {
                sb.AppendLine($"✓ {compoundNames[formula]}");
            }
            
            completedReactionsText.text = sb.ToString();
        }
        else
        {
            completedReactionsText.text = "<b>Reacciones completadas:</b>\nNinguna todavía";
        }
    }

    private void UpdateObjectiveText()
    {
        if (objectiveText == null || objectiveCompounds.Count == 0) return;
        
        bool allCompleted = CheckAllObjectivesCompleted();
        
        if (allCompleted)
        {
            objectiveText.text = "<b>¡Todos los objetivos completados!</b>";
            objectiveText.color = Color.green;
        }
        else
        {
            // Buscar el primer objetivo no completado
            int nextObjectiveIndex = objectiveCompounds.FindIndex(c => !completedReactions.Contains(c));
            
            if (nextObjectiveIndex != -1)
            {
                currentObjectiveIndex = nextObjectiveIndex;
                string currentObjective = objectiveCompounds[currentObjectiveIndex];
                objectiveText.text = $"<b>Objetivo:</b> Crear {compoundNames[currentObjective]}";
                objectiveText.color = Color.white;
            }
        }
    }

    private void UpdateButtonStates()
    {
        deleteLastInteractable = (elements.Count > 0);
        clearAllInteractable = (elements.Count > 0);
    }
    
    #endregion

    #region Reward Handling
    
    private void SpawnFinalReward()
    {
        if (finalRewardPrefab == null)
        {
            Debug.LogError("ERROR: Prefab de recompensa final no asignado. No se puede entregar recompensa final.");
            return;
        }

        // Posición de spawn
        Vector3 spawnPosition = rewardSpawnPoint != null ? 
            rewardSpawnPoint.position : 
            new Vector3(transform.position.x, transform.position.y + rewardDropHeight, transform.position.z);

        // Instanciar recompensa
        GameObject reward = Instantiate(finalRewardPrefab, spawnPosition, Random.rotation);
        
        if (reward == null)
        {
            Debug.LogError("ERROR: Falló la instanciación de la recompensa");
            return;
        }
        
        // Configurar físicas
        ConfigureRewardPhysics(reward);
        
        Debug.Log("¡Recompensa final generada por completar todos los objetivos!");
    }

    private void ConfigureRewardPhysics(GameObject reward)
    {
        Rigidbody rb = reward.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = reward.AddComponent<Rigidbody>();
        }
        
        // Configurar para caída lenta
        rb.linearDamping = 3.0f;
        rb.angularDamping = 2.0f;
        
        // Fuerza para caída lenta
        rb.AddForce(new Vector3(
            Random.Range(-0.1f, 0.1f), 
            -rewardDropForce, 
            Random.Range(-0.1f, 0.1f)
        ), ForceMode.Impulse);
        
        // Torque para rotación suave
        rb.AddTorque(new Vector3(
            Random.Range(-0.2f, 0.2f),
            Random.Range(-0.2f, 0.2f),
            Random.Range(-0.2f, 0.2f)
        ), ForceMode.Impulse);
    }
    
    #endregion
}