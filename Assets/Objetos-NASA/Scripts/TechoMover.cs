using UnityEngine;

public class TechoMover : MonoBehaviour
{
    [Header("Posiciones del techo")]
    public Vector3 posicionCerrado;  // Posición final al cerrar
    public Vector3 posicionAbierto;  // Posición final al abrir
    public float duracion = 2f;      // Tiempo que tarda en moverse

    private Vector3 posicionInicial;
    private Vector3 posicionFinal;
    private float tiempo = 0f;
    private bool moviendo = false;

    public void AbrirTecho()
    {
        posicionInicial = transform.position;
        posicionFinal = posicionAbierto;
        tiempo = 0f;
        moviendo = true;
    }

    public void CerrarTecho()
    {
        posicionInicial = transform.position;
        posicionFinal = posicionCerrado;
        tiempo = 0f;
        moviendo = true;
    }

    void Update()
    {
        if (moviendo)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            transform.position = Vector3.Lerp(posicionInicial, posicionFinal, t);

            if (t >= 1f)
                moviendo = false;
        }
    }
}


