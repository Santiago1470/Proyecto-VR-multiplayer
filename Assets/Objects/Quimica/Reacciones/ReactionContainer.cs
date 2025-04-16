using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ReactionContainer : MonoBehaviour
{
    [Header("Visual Elements")]
    // Asigna manualmente el material del líquido en el Inspector
    public Material liquidMaterial;
    public float maxLiquidLevel = 0.01f;           // Nivel máximo del líquido (valor que espera el shader)
    public float elementLevelIncrement = 0.001f;  // Incremento de nivel por cada elemento añadido
    public Color emptyColor = new Color(0.9f, 0.9f, 0.9f, 0.4f); // Color cuando está vacío

    [Header("Reaction Display")]
    public Text reactionText;                   // Texto de UI para mostrar la fórmula actual

    // Lista de elementos vertidos
    private List<ChemicalTube.ChemicalElement> elements = new List<ChemicalTube.ChemicalElement>();

    // Diccionario para compuestos y sus colores
    private Dictionary<string, Color> chemicalCompounds = new Dictionary<string, Color>() {
        {"H2O", new Color(0f, 0.6f, 1f, 0.8f)},
        {"CO2", new Color(0.5f, 0.5f, 0.5f, 0.8f)},
        {"NH3", new Color(0.6f, 0.6f, 1f, 0.8f)},
        {"CH4", new Color(0.2f, 0.2f, 0.2f, 0.8f)},
        {"H2SO4", new Color(0.9f, 0.7f, 0.1f, 0.8f)},
        {"HCl", new Color(0.6f, 0.9f, 0.6f, 0.8f)},
        {"NaCl", Color.white}
    };

    // Diccionario para colores según el último elemento añadido
    private Dictionary<ChemicalTube.ChemicalElement, Color> elementColors = new Dictionary<ChemicalTube.ChemicalElement, Color>() {
        {ChemicalTube.ChemicalElement.Hydrogen, new Color(0.5f, 0.8f, 1f, 0.8f)},
        {ChemicalTube.ChemicalElement.Oxygen, new Color(1f, 0.2f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Carbon, new Color(0.2f, 0.2f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Nitrogen, new Color(1f, 1f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Chlorine, new Color(0.2f, 0.8f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Sulfur, new Color(0.9f, 0.8f, 0.2f, 0.8f)}
    };

    // Identificadores de propiedades en el shader
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

    void Awake()
    {
        if (liquidMaterial == null)
        {
            Debug.LogError("Material del líquido no asignado. Asigna el material en el Inspector.");
        }
        else
        {
            // Verifica que el material tenga la propiedad _FillAmount
            if (!liquidMaterial.HasProperty(FillAmountId))
            {
                Debug.LogWarning("El material no tiene la propiedad _FillAmount. Verifica el nombre en el shader.");
            }
            Debug.Log("Material del líquido asignado correctamente.");
        }
    }

    void Start()
    {
        if (liquidMaterial != null)
        {
            SetLiquidColor(emptyColor);
            // Se establece el nivel inicial del líquido mediante la propiedad del shader
            UpdateLiquidVisual(0.002f);
            Debug.Log("Nivel y color iniciales del líquido establecidos.");
        }
        else
        {
            Debug.LogError("No se pudo inicializar el material del líquido.");
        }

        UpdateFormula();
    }
    
    // Actualiza la propiedad de nivel del líquido en el material (sin modificar transformaciones)
    private void UpdateLiquidVisual(float level)
    {
        if (liquidMaterial == null)
        {
            Debug.LogError("Material del líquido no asignado");
            return;
        }

        level = Mathf.Clamp(level, 0.002f, maxLiquidLevel);
        liquidMaterial.SetFloat(FillAmountId, level);
        Debug.Log($"Liquid visual updated with level: {level}");
        
        // Actualiza también el color en función de la fórmula actual
        string formula = GetCurrentFormula();
        Debug.Log($"Current formula: {formula}");

        Color newColor;
        if (chemicalCompounds.ContainsKey(formula))
        {
            newColor = chemicalCompounds[formula];
            Debug.Log($"Using compound color for {formula}: {newColor}");
            Debug.Log($"Reacción formada: {formula}");
        }
        else if (elements.Count > 0)
        {
            newColor = elementColors[elements.Last()];
            Debug.Log($"Using element color: {newColor}");
        }
        else
        {
            newColor = emptyColor;
            Debug.Log("Using empty color");
        }

        // Asegura una transparencia adecuada
        if (newColor.a > 0.95f)
            newColor.a = 0.8f;

        SetLiquidColor(newColor);
    }

    // Asigna el color al material del líquido
    private void SetLiquidColor(Color color)
    {
        if (liquidMaterial != null)
        {
            liquidMaterial.SetColor(BaseColorId, color);
            liquidMaterial.SetColor("_BaseColor", color);
            liquidMaterial.SetColor("_Color", color);
            Debug.Log($"Liquid color set to: {color}");
        }
    }
    
    // Registra un nuevo elemento y actualiza el nivel y la fórmula
    public void RegisterPour(ChemicalTube.ChemicalElement element)
    {
        Debug.Log($"RegisterPour called with element: {element}");
        elements.Add(element);
        float newLevel = Mathf.Max(0.002f, elements.Count * elementLevelIncrement);
        UpdateLiquidVisual(newLevel);
        UpdateFormula();
        Debug.Log($"Elemento agregado. Total: {elements.Count}, Nivel: {newLevel}");
    }

    // Elimina el último elemento agregado y actualiza la visualización
    public void RemoveLastElement()
    {
        if (elements.Count > 0)
        {
            elements.RemoveAt(elements.Count - 1);
            float newLevel = (elements.Count > 0) ? elements.Count * elementLevelIncrement : 0.002f;
            UpdateLiquidVisual(newLevel);
            UpdateFormula();
            Debug.Log($"Removed last element. Remaining count: {elements.Count}");
        }
        else
        {
            Debug.Log("No hay elementos para eliminar.");
        }
    }

    // Limpia todos los elementos y reinicia el nivel y la fórmula
    public void ClearAllElements()
    {
        elements.Clear();
        UpdateLiquidVisual(0.002f);
        UpdateFormula();
        Debug.Log("All elements cleared");
    }

    // Actualiza el texto de la fórmula en la UI
    private void UpdateFormula()
    {
        if (reactionText == null)
            return;
        
        string formula = GetCurrentFormula();
        reactionText.text = formula;
        Debug.Log($"Formula text updated to: {formula}");
    }

    // Construye la fórmula química a partir de los elementos agregados
    private string GetCurrentFormula()
    {
        Dictionary<ChemicalTube.ChemicalElement, int> elementCounts = new Dictionary<ChemicalTube.ChemicalElement, int>();
        foreach (var element in elements)
        {
            if (elementCounts.ContainsKey(element))
                elementCounts[element]++;
            else
                elementCounts[element] = 1;
        }

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
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Hydrogen, 1, ChemicalTube.ChemicalElement.Chlorine, 1))
            return "HCl";
        
        // Construye una fórmula genérica si no coincide con ninguna conocida
        string formula = "";
        foreach (var element in elementCounts.Keys.OrderBy(GetElementPriority))
        {
            formula += GetElementSymbol(element);
            if (elementCounts[element] > 1)
                formula += elementCounts[element].ToString();
        }
        return formula;
    }

    // Verifica si la combinación de elementos coincide exactamente con un compuesto
    private bool IsCompound(Dictionary<ChemicalTube.ChemicalElement, int> containerElements, params object[] requiredElements)
    {
        if (containerElements.Count * 2 != requiredElements.Length)
            return false;
        
        for (int i = 0; i < requiredElements.Length; i += 2)
        {
            ChemicalTube.ChemicalElement element = (ChemicalTube.ChemicalElement)requiredElements[i];
            int count = (int)requiredElements[i + 1];
            if (!containerElements.ContainsKey(element) || containerElements[element] != count)
                return false;
        }
        return true;
    }

    // Define la prioridad para ordenar los elementos al construir la fórmula
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

    // Devuelve el símbolo del elemento
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
}
