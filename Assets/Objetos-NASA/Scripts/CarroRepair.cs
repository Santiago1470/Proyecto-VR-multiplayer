using UnityEngine;

public class CarroRepair : MonoBehaviour
{
    public Transform[] partesSeparadas;  // piezas que quieres mover
    private bool reparado = false;

    public void Reparar()
    {
        if (reparado) return;

        foreach (Transform parte in partesSeparadas)
        {
            parte.localPosition = Vector3.zero;
            parte.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        reparado = true;
    }
}