using UnityEngine;

public class Filler : MonoBehaviour
{
    [Header("Configuración")]
    public ParticleSystem coffeeParticles;
    public LiquidFiller targetCup;    // Taza con el script LiquidFiller

    void Start()
    {
        if (coffeeParticles == null)
        {
            coffeeParticles = GetComponent<ParticleSystem>();
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (targetCup != null && other.gameObject == targetCup.gameObject)
        {
            targetCup.AddLiquid(1f); // Ajusta este valor para más/menos flujo
        }
    }
}