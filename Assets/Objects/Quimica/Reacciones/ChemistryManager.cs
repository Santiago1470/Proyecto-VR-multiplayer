using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChemistryManager : MonoBehaviour
{
    [HideInInspector] public List<string> objectiveCompounds;

    private List<string> allCompounds = new List<string>() {
        "H2O", "CO2", "NH3", "CH4", "H2SO4", "Cl2", "C2H5OH", "HCl"
    };

    private void Awake()
    {
        objectiveCompounds = allCompounds.OrderBy(_ => Random.value).Take(3).ToList();
    }
    // Diccionario de colores para compuestos químicos
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

    // Diccionario de nombres para compuestos
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

    // Diccionario de colores para elementos químicos
    private readonly Dictionary<ChemicalTube.ChemicalElement, Color> elementColors = new Dictionary<ChemicalTube.ChemicalElement, Color>() {
        {ChemicalTube.ChemicalElement.Hydrogen, new Color(0.5f, 0.8f, 1f, 0.8f)},
        {ChemicalTube.ChemicalElement.Oxygen, new Color(1f, 0.2f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Carbon, new Color(0.2f, 0.2f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Nitrogen, new Color(1f, 1f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Chlorine, new Color(0.2f, 0.8f, 0.2f, 0.8f)},
        {ChemicalTube.ChemicalElement.Sulfur, new Color(0.9f, 0.8f, 0.2f, 0.8f)}
    };

    // Métodos públicos para acceder a los datos
    public bool IsKnownCompound(string formula) => chemicalCompounds.ContainsKey(formula);
    
    public string GetCompoundName(string formula) => 
        compoundNames.TryGetValue(formula, out var name) ? name : formula;
    
    public Color GetColorForFormula(string formula, List<ChemicalTube.ChemicalElement> elements, Color emptyColor)
    {
        if (chemicalCompounds.TryGetValue(formula, out var color))
            return color;
        else if (elements != null && elements.Count > 0 && elementColors.TryGetValue(elements.Last(), out var elemColor))
            return elemColor;
        else
            return emptyColor;
    }

    // Obtener fórmula a partir de una lista de elementos
    public string GetFormulaFromElements(List<ChemicalTube.ChemicalElement> elements)
    {
        if (elements == null || elements.Count == 0)
            return "";
            
        var elementCounts = CountElements(elements);
        
        // Comprobar compuestos específicos
        if (IsWater(elementCounts)) return "H2O";
        if (IsCarbonDioxide(elementCounts)) return "CO2";
        if (IsAmmonia(elementCounts)) return "NH3";
        if (IsMethane(elementCounts)) return "CH4";
        if (IsSulfuricAcid(elementCounts)) return "H2SO4";
        if (IsChlorine(elementCounts)) return "Cl2";
        if (IsEthanol(elementCounts)) return "C2H5OH";
        if (IsHydrochloricAcid(elementCounts)) return "HCl";
        
        // Si no coincide con ningún compuesto conocido, construir fórmula genérica
        return BuildGenericFormula(elementCounts);
    }
    
    // Métodos para verificar compuestos específicos
    private bool IsWater(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Hydrogen, out int h) && h == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Oxygen, out int o) && o == 1;
        
    private bool IsCarbonDioxide(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Carbon, out int c) && c == 1 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Oxygen, out int o) && o == 2;
        
    private bool IsAmmonia(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Nitrogen, out int n) && n == 1 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Hydrogen, out int h) && h == 3;
        
    private bool IsMethane(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Carbon, out int c) && c == 1 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Hydrogen, out int h) && h == 4;
        
    private bool IsSulfuricAcid(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 3 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Hydrogen, out int h) && h == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Sulfur, out int s) && s == 1 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Oxygen, out int o) && o == 4;
        
    private bool IsChlorine(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 1 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Chlorine, out int cl) && cl == 2;
        
    private bool IsEthanol(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 3 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Carbon, out int c) && c == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Hydrogen, out int h) && h == 6 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Oxygen, out int o) && o == 1;
        
    private bool IsHydrochloricAcid(Dictionary<ChemicalTube.ChemicalElement, int> elements) =>
        elements.Count == 2 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Hydrogen, out int h) && h == 1 &&
        elements.TryGetValue(ChemicalTube.ChemicalElement.Chlorine, out int cl) && cl == 1;

    // Contar elementos en la lista
    private Dictionary<ChemicalTube.ChemicalElement, int> CountElements(List<ChemicalTube.ChemicalElement> elements)
    {
        var counts = new Dictionary<ChemicalTube.ChemicalElement, int>();
        
        foreach (var element in elements)
        {
            if (counts.TryGetValue(element, out int count))
                counts[element] = count + 1;
            else
                counts[element] = 1;
        }
        
        return counts;
    }

    // Construir fórmula química en formato texto
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

    // Determinar prioridad de un elemento en la fórmula
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

    // Obtener símbolo de un elemento
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
    
    // Formatear fórmula con subíndices HTML
    public string FormatFormulaWithSubscripts(string formula)
    {
        string result = "";
        
        for (int i = 0; i < formula.Length; i++)
        {
            if (i < formula.Length - 1 && char.IsDigit(formula[i+1]))
            {
                result += formula[i];
                result += "<sub>" + formula[i+1] + "</sub>";
                i++;
            }
            else
            {
                result += formula[i];
            }
        }
        
        return result;
    }
}