using UnityEngine;

public class Fall: MonoBehaviour
{
    public float gravedadPersonalizada = -2f; // Valor negativo, más cerca de 0 = caída más lenta

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Desactiva la gravedad predeterminada de Unity
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        // Aplica una gravedad personalizada hacia abajo
        rb.AddForce(Vector3.up * gravedadPersonalizada, ForceMode.Acceleration);
    }
}
