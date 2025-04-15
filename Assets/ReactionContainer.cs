using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReactionContainer : MonoBehaviour
{
    public Material liquidMaterial;
    public Transform liquidLevel;
    public TextMeshProUGUI formulaText;
    public TextMeshProUGUI resultText;
    public ParticleSystem reactionEffect;
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip pourSound;
    
    private Dictionary<ChemicalTube.ChemicalElement, Color> elementColors = new Dictionary<ChemicalTube.ChemicalElement, Color>();
    private List<ChemicalTube.ChemicalElement> containedElements = new List<ChemicalTube.ChemicalElement>();
    private Dictionary<string, ReactionInfo> reactionDatabase = new Dictionary<string, ReactionInfo>();
    // Puedes iniciar mixedColor con un color base o dejarlo en clear y forzar la opacidad en UpdateVisuals
    private Color mixedColor = Color.clear;
    private float fillAmount = 0f;

    [System.Serializable]
    public class ReactionInfo
    {
        public string resultName;
        public string formula;
        public Color resultColor;
        public ParticleSystem specialEffect;
    }

    void Start()
    {
        // Instanciamos el material para evitar modificar el asset original.
        if (liquidMaterial != null)
            liquidMaterial = new Material(liquidMaterial);
        
        InitializeElementColors();
        InitializeReactionDatabase();
        UpdateVisuals();
    }

    void InitializeElementColors()
    {
        elementColors[ChemicalTube.ChemicalElement.Hydrogen] = new Color(0.7f, 0.9f, 1f, 1f);     // Azul claro
        elementColors[ChemicalTube.ChemicalElement.Oxygen] = new Color(1f, 0.2f, 0.2f, 1f);       // Rojo
        elementColors[ChemicalTube.ChemicalElement.Carbon] = new Color(0.2f, 0.2f, 0.2f, 1f);       // Negro
        elementColors[ChemicalTube.ChemicalElement.Nitrogen] = new Color(1f, 0.9f, 0.2f, 1f);       // Amarillo
        elementColors[ChemicalTube.ChemicalElement.Chlorine] = new Color(0.2f, 0.8f, 0.2f, 1f);     // Verde
        elementColors[ChemicalTube.ChemicalElement.Sulfur] = new Color(0.8f, 0.8f, 0.0f, 1f);       // Amarillo mostaza
    }

    void InitializeReactionDatabase()
    {
        // Reacciones con azufre
        AddReaction(new[] { 
            ChemicalTube.ChemicalElement.Hydrogen, 
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Sulfur
        }, "Ácido Sulfhídrico", "2H + S → H₂S", new Color(0.6f, 0.6f, 0.3f, 1f));

        AddReaction(new[] {
            ChemicalTube.ChemicalElement.Sulfur,
            ChemicalTube.ChemicalElement.Oxygen,
            ChemicalTube.ChemicalElement.Oxygen
        }, "Dióxido de Azufre", "S + O₂ → SO₂", new Color(0.7f, 0.5f, 0.2f, 1f));

        // Agua (H₂O)
        AddReaction(new[] {
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Oxygen
        }, "Agua", "2H + O → H₂O", new Color(0.7f, 0.7f, 1f, 1f));

        // Dióxido de Carbono (CO₂)
        AddReaction(new[] {
            ChemicalTube.ChemicalElement.Carbon,
            ChemicalTube.ChemicalElement.Oxygen,
            ChemicalTube.ChemicalElement.Oxygen
        }, "Dióxido de Carbono", "C + O₂ → CO₂", new Color(0.5f, 0.5f, 0.5f, 1f));

        // Metano (CH₄)
        AddReaction(new[] {
            ChemicalTube.ChemicalElement.Carbon,
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Hydrogen
        }, "Metano", "C + 4H → CH₄", new Color(0.4f, 0.7f, 0.4f, 1f));

        // Amoniaco (NH₃)
        AddReaction(new[] {
            ChemicalTube.ChemicalElement.Nitrogen,
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Hydrogen
        }, "Amoniaco", "N + 3H → NH₃", new Color(0.8f, 0.8f, 0.4f, 1f));

        // Ácido Clorhídrico (HCl)
        AddReaction(new[] {
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Chlorine
        }, "Ácido Clorhídrico", "H + Cl → HCl", new Color(0.5f, 0.9f, 0.5f, 1f));

        // Ácido Sulfúrico (H₂SO₄)
        AddReaction(new[] {
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Hydrogen,
            ChemicalTube.ChemicalElement.Sulfur,
            ChemicalTube.ChemicalElement.Oxygen,
            ChemicalTube.ChemicalElement.Oxygen,
            ChemicalTube.ChemicalElement.Oxygen,
            ChemicalTube.ChemicalElement.Oxygen
        }, "Ácido Sulfúrico", "2H + S + 4O → H₂SO₄", new Color(0.6f, 0.5f, 0.1f, 1f));
    }

    public void RegisterPour(ChemicalTube.ChemicalElement element)
    {
        containedElements.Add(element);
        if (audioSource && pourSound)
            audioSource.PlayOneShot(pourSound);
        
        // Mezcla de colores: se fuerza la opacidad en cada color
        if (elementColors.TryGetValue(element, out Color newColor))
        {
            newColor.a = 1f;
            if (mixedColor == Color.clear)
            {
                mixedColor = newColor;
            }
            else
            {
                mixedColor = Color.Lerp(mixedColor, newColor, 0.5f);
                mixedColor.a = 1f; // Forzamos la opacidad después de la mezcla
            }
        }

        fillAmount = Mathf.Clamp01(fillAmount + 0.2f);
        CheckReaction();
        UpdateVisuals();
    }

    void CheckReaction()
    {
        var sortedElements = new List<ChemicalTube.ChemicalElement>(containedElements);
        sortedElements.Sort();
        
        string key = string.Join(",", sortedElements);
        
        if (reactionDatabase.TryGetValue(key, out ReactionInfo reaction)) 
        {
            formulaText.text = reaction.formula;
            resultText.text = reaction.resultName;
            mixedColor = reaction.resultColor;
            mixedColor.a = 1f;  // Aseguramos que el color del resultado es opaco
            
            if (reactionEffect)
                reactionEffect.Play();
                
            if (audioSource && successSound)
                audioSource.PlayOneShot(successSound);
        }
        else
        {
            formulaText.text = GetCurrentFormula();
            resultText.text = "Mezcla desconocida";
        }
    }

    string GetCurrentFormula()
    {
        var counts = new Dictionary<ChemicalTube.ChemicalElement, int>();
        foreach (var element in containedElements)
        {
            if (counts.ContainsKey(element))
                counts[element]++;
            else
                counts[element] = 1;
        }
        
        string formula = "";
        foreach (var pair in counts)
        {
            formula += $"{GetElementSymbol(pair.Key)}{(pair.Value > 1 ? pair.Value.ToString() : "")} + ";
        }
        return formula.TrimEnd(' ', '+');
    }

    string GetElementSymbol(ChemicalTube.ChemicalElement element)
    {
        switch (element)
        {
            case ChemicalTube.ChemicalElement.Sulfur: return "S";
            case ChemicalTube.ChemicalElement.Hydrogen: return "H";
            case ChemicalTube.ChemicalElement.Oxygen: return "O";
            case ChemicalTube.ChemicalElement.Carbon: return "C";
            case ChemicalTube.ChemicalElement.Nitrogen: return "N";
            case ChemicalTube.ChemicalElement.Chlorine: return "Cl";
            default: return "?";
        }
    }

    void UpdateVisuals()
    {
        if (liquidMaterial)
        {
            // Si mixedColor está en clear, podemos asignar un color base (por ejemplo blanco)
            Color finalColor = mixedColor != Color.clear ? mixedColor : new Color(1f, 1f, 1f, 1f);
            finalColor.a = 1f; // Forzamos la opacidad
            liquidMaterial.color = finalColor;
        }
            
        if (liquidLevel)
        {
            liquidLevel.localScale = new Vector3(1, fillAmount, 1);
            liquidLevel.localPosition = new Vector3(0, fillAmount * 0.5f - 0.5f, 0);
        }
    }

    public void Reset()
    {
        containedElements.Clear();
        mixedColor = Color.clear;
        fillAmount = 0f;
        
        if (formulaText)
            formulaText.text = "";
        if (resultText)
            resultText.text = "";
        
        UpdateVisuals();
    }

    void AddReaction(ChemicalTube.ChemicalElement[] elements, string name, string formula, Color color)
    {
        var sorted = new List<ChemicalTube.ChemicalElement>(elements);
        sorted.Sort();
        reactionDatabase[string.Join(",", sorted)] = new ReactionInfo {
            resultName = name,
            formula = formula,
            resultColor = color
        };
    }
}
