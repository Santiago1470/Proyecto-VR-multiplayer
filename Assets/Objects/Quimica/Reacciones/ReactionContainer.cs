using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ReactionContainer : MonoBehaviour
{
    [Header("Visual Elements")]
    public GameObject liquidVisual;           // Visual representation of the liquid
    public float maxLiquidHeight = 0.5f;      // Maximum height the liquid can reach
    public float elementHeightIncrement = 0.1f; // How much each element increases liquid height
    
    [Header("Reaction Display")]
    public Text reactionText;                 // UI Text to display current formula
    
    [Header("Clear Button")]
    public Button clearButton;                // Button to remove elements
    
    // List to store all poured elements
    private List<ChemicalTube.ChemicalElement> elements = new List<ChemicalTube.ChemicalElement>();
    
    // Dictionary to store chemical formulas and their corresponding colors
    private Dictionary<string, Color> chemicalCompounds = new Dictionary<string, Color>() {
        {"H2O", new Color(0f, 0.6f, 1f, 0.8f)},        // Water - Light blue
        {"CO2", new Color(0.5f, 0.5f, 0.5f, 0.8f)},    // Carbon dioxide - Gray
        {"NH3", new Color(0.6f, 0.6f, 1f, 0.8f)},      // Ammonia - Pale blue
        {"CH4", new Color(0.2f, 0.2f, 0.2f, 0.8f)},    // Methane - Dark gray
        {"H2SO4", new Color(0.9f, 0.7f, 0.1f, 0.8f)},  // Sulfuric acid - Amber
        {"HCl", new Color(0.6f, 0.9f, 0.6f, 0.8f)},    // Hydrochloric acid - Pale green
        {"NaCl", Color.white}                          // Salt - White (placeholder, not in your elements)
    };
    
    // Dictionary for default element colors (matching your tube colors)
    private Dictionary<ChemicalTube.ChemicalElement, Color> elementColors = new Dictionary<ChemicalTube.ChemicalElement, Color>() {
        {ChemicalTube.ChemicalElement.Hydrogen, new Color(0.5f, 0.8f, 1f, 0.8f)},  // Light blue
        {ChemicalTube.ChemicalElement.Oxygen, new Color(1f, 0.2f, 0.2f, 0.8f)},    // Red
        {ChemicalTube.ChemicalElement.Carbon, new Color(0.2f, 0.2f, 0.2f, 0.8f)},  // Black
        {ChemicalTube.ChemicalElement.Nitrogen, new Color(1f, 1f, 0.2f, 0.8f)},    // Yellow
        {ChemicalTube.ChemicalElement.Chlorine, new Color(0.2f, 0.8f, 0.2f, 0.8f)},// Green
        {ChemicalTube.ChemicalElement.Sulfur, new Color(0.9f, 0.8f, 0.2f, 0.8f)}   // Mustard yellow
    };
    
    private Material liquidMaterial;
    
    void Start()
    {
        // Set up liquid visual
        if (liquidVisual != null)
        {
            liquidMaterial = liquidVisual.GetComponent<Renderer>().material;
            UpdateLiquidVisual(0); // Initialize with zero height
        }
        
        // Set up clear button
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(RemoveLastElement);
        }
        
        // Initialize empty container
        UpdateFormula();
    }
    
    public void RegisterPour(ChemicalTube.ChemicalElement element)
    {
        // Add the element to our list
        elements.Add(element);
        
        // Update the liquid display
        UpdateLiquidVisual(elements.Count * elementHeightIncrement);
        
        // Update the formula text
        UpdateFormula();
        
        // Debug log the current elements
        Debug.Log($"Added {element}. Current elements: {string.Join(", ", elements)}");
    }
    
    private void UpdateLiquidVisual(float height)
    {
        if (liquidVisual == null || liquidMaterial == null) return;
        
        // Clamp height to max
        height = Mathf.Min(height, maxLiquidHeight);
        
        // Scale the liquid visual
        Vector3 scale = liquidVisual.transform.localScale;
        scale.y = height;
        liquidVisual.transform.localScale = scale;
        
        // Position the liquid at half its height from the bottom
        Vector3 position = liquidVisual.transform.localPosition;
        position.y = height / 2;
        liquidVisual.transform.localPosition = position;
        
        // Update the color based on the current formula
        string formula = GetCurrentFormula();
        if (chemicalCompounds.ContainsKey(formula))
        {
            liquidMaterial.color = chemicalCompounds[formula];
        }
        else if (elements.Count > 0)
        {
            // If no specific compound found, use the color of the last added element
            liquidMaterial.color = elementColors[elements.Last()];
        }
        else
        {
            // Default color for empty container
            liquidMaterial.color = new Color(0.9f, 0.9f, 0.9f, 0.4f); // Transparent white
        }
    }
    
    private void UpdateFormula()
    {
        if (reactionText == null) return;
        
        string formula = GetCurrentFormula();
        reactionText.text = formula;
    }
    
    private string GetCurrentFormula()
    {
        // Count occurrences of each element
        Dictionary<ChemicalTube.ChemicalElement, int> elementCounts = 
            new Dictionary<ChemicalTube.ChemicalElement, int>();
            
        foreach (var element in elements)
        {
            if (elementCounts.ContainsKey(element))
                elementCounts[element]++;
            else
                elementCounts[element] = 1;
        }
        
        // First check for known compounds
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Hydrogen, 2, ChemicalTube.ChemicalElement.Oxygen, 1))
            return "H2O"; // Water
            
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Carbon, 1, ChemicalTube.ChemicalElement.Oxygen, 2))
            return "CO2"; // Carbon dioxide
            
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Nitrogen, 1, ChemicalTube.ChemicalElement.Hydrogen, 3))
            return "NH3"; // Ammonia
            
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Carbon, 1, ChemicalTube.ChemicalElement.Hydrogen, 4))
            return "CH4"; // Methane
            
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Hydrogen, 2, ChemicalTube.ChemicalElement.Sulfur, 1, 
                       ChemicalTube.ChemicalElement.Oxygen, 4))
            return "H2SO4"; // Sulfuric acid
            
        if (IsCompound(elementCounts, ChemicalTube.ChemicalElement.Hydrogen, 1, ChemicalTube.ChemicalElement.Chlorine, 1))
            return "HCl"; // Hydrochloric acid
            
        // If no known compound is found, build a generic formula
        string formula = "";
        foreach (var element in elementCounts.Keys.OrderBy(GetElementPriority))
        {
            formula += GetElementSymbol(element);
            if (elementCounts[element] > 1)
                formula += elementCounts[element].ToString();
        }
        
        return formula;
    }
    
    // Helper method to determine if elements match a specific compound
    private bool IsCompound(Dictionary<ChemicalTube.ChemicalElement, int> elements, 
                           params object[] elementCounts)
    {
        if (elements.Count * 2 != elementCounts.Length) return false;
        
        for (int i = 0; i < elementCounts.Length; i += 2)
        {
            ChemicalTube.ChemicalElement element = (ChemicalTube.ChemicalElement)elementCounts[i];
            int count = (int)elementCounts[i + 1];
            
            if (!elements.ContainsKey(element) || elements[element] != count)
                return false;
        }
        
        return true;
    }
    
    // Get priority for ordering elements in formula (C, H, O, N, etc. standard order)
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
    
    // Get chemical symbol for element
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
    
    public void RemoveLastElement()
    {
        if (elements.Count == 0) return;
        
        // Remove the last element added
        elements.RemoveAt(elements.Count - 1);
        
        // Update visuals
        UpdateLiquidVisual(elements.Count * elementHeightIncrement);
        UpdateFormula();
        
        Debug.Log("Removed last element. Remaining: " + string.Join(", ", elements));
    }
    
    public void ClearAllElements()
    {
        elements.Clear();
        UpdateLiquidVisual(0);
        UpdateFormula();
        Debug.Log("Cleared all elements");
    }
}