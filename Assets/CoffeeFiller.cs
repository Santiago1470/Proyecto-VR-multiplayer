using UnityEngine;

public class LiquidFiller : MonoBehaviour
{
    [Header("Referencias")]
    public Transform liquidSurface;       
    public float maxFillHeight = 0.3f;    
    public float fillSpeed = 0.05f;        

    [Header("Ajustes Visuales")]
    public bool scaleWidth = true;       
    public float minWidth = 0.6f;         
    public float maxWidth = 0.7f;         

    private float currentFill = 0f;

    void Update()
    {
        UpdateLiquidVisual();
    }

    public void AddLiquid(float amount)
    {
        currentFill += amount * fillSpeed * Time.deltaTime;
        currentFill = Mathf.Clamp(currentFill, 0f, maxFillHeight);
    }

    private void UpdateLiquidVisual()
    {
        // Posición del líquido
        liquidSurface.localPosition = new Vector3(0, currentFill, 0);

        // Escala progresiva (para tazas que se ensanchan)
        if (scaleWidth)
        {
            float width = Mathf.Lerp(minWidth, maxWidth, currentFill / maxFillHeight);
            liquidSurface.localScale = new Vector3(width, 0.01f, width);
        }
    }

    // Resetear líquido (opcional)
    public void ResetLiquid()
    {
        currentFill = 0f;
        UpdateLiquidVisual();
    }
}