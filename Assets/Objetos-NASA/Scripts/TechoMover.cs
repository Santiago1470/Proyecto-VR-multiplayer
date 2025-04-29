using UnityEngine;

public class TechoMover : MonoBehaviour
{
    public Vector3 destino; // Posición destino al abrir el techo
    public float duracion = 2f; // Duración del movimiento

    private Vector3 posicionInicial;
    private Vector3 posicionFinal;
    private float tiempo = 0f;
    private bool moviendo = false;

    public void AbrirTecho()
    {
        posicionInicial = transform.position;
        posicionFinal = new Vector3(destino.x, posicionInicial.y, destino.z); // Cambia la X al valor deseado (ej. -2)
        tiempo = 0f;
        moviendo = true;
    }

    public void CerrarTecho()
    {
        posicionInicial = transform.position;
        posicionFinal = new Vector3(0f, posicionInicial.y, posicionInicial.z); // Regresa al valor inicial (ej. 0)
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
                moviendo = false; // Detiene el movimiento una vez alcanzada la posición final
        }
    }
}

