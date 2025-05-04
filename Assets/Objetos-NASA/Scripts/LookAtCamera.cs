using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            Vector3 direction = Camera.main.transform.position - transform.position;
            transform.rotation = Quaternion.LookRotation(direction);
            transform.Rotate(0, 180f, 0); // Corrige el texto que se ve al revés
        }
    }
}
