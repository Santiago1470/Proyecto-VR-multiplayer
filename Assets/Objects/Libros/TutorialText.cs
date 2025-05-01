using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TextImageSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class ContentPair
    {
        public string text;
        public Texture image;
    }

    // Referencias a los componentes UI
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private RawImage imageDisplay;
    
    // Arreglo con las 4 combinaciones de texto e imagen
    [SerializeField] private ContentPair[] contentPairs = new ContentPair[4];
    
    // Referencias a los botones de navegación
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    
    // Índice del contenido actualmente mostrado
    private int currentIndex = 0;

    private void Start()
    {
        // Configurar listeners para los botones
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousContent);
        
        if (nextButton != null)
            nextButton.onClick.AddListener(NextContent);
        
        // Mostrar el primer contenido al iniciar
        ShowContent(0);
    }

    // Método para mostrar el contenido según el índice
    public void ShowContent(int index)
    {
        if (index < 0 || index >= contentPairs.Length)
            return;
            
        // Actualizar el índice actual
        currentIndex = index;
        
        // Actualizar el texto y la imagen
        textDisplay.text = contentPairs[index].text;
        imageDisplay.texture = contentPairs[index].image;
    }

    // Métodos para navegar entre contenidos
    public void NextContent()
    {
        int nextIndex = (currentIndex + 1) % contentPairs.Length;
        ShowContent(nextIndex);
    }
    
    public void PreviousContent()
    {
        int prevIndex = (currentIndex - 1 + contentPairs.Length) % contentPairs.Length;
        ShowContent(prevIndex);
    }
}