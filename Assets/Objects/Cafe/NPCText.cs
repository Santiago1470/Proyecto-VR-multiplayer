using UnityEngine;
using TMPro;

/// <summary>
/// Hace que un objeto de texto (TextMeshProUGUI) siempre mire hacia la cámara del jugador
/// manteniendo su posición fija en el espacio.
/// </summary>
public class TextBillboard : MonoBehaviour
{
    [Tooltip("Referencia a la cámara principal. Si está vacío, se usará Camera.main")]
    public Camera targetCamera;
    
    [Tooltip("Mantiene la rotación en el eje Y solamente (útil para carteles)")]
    public bool lockYAxis = false;
    
    [Tooltip("Offset de rotación en grados (si necesitas ajustar la orientación)")]
    public Vector3 rotationOffset = Vector3.zero;
    
    private void Start()
    {
        // Si no se asignó una cámara, buscar la cámara principal
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("No se encontró ninguna cámara principal. Asigna una cámara al script TextBillboard.");
                enabled = false;
                return;
            }
        }
        
        // Verificar que el objeto tiene un componente TextMeshPro
        if (GetComponent<TextMeshPro>() == null && GetComponent<TextMeshProUGUI>() == null)
        {
            Debug.LogWarning("Este script está diseñado para objetos TextMeshPro/TextMeshProUGUI, pero no se encontró ninguno en este GameObject.");
        }
    }
    
    private void LateUpdate()
    {
        if (targetCamera == null) return;
        
        Vector3 directionToCamera = targetCamera.transform.position - transform.position;
        
        // Si queremos bloquear el eje Y, eliminamos la componente Y del vector
        if (lockYAxis)
        {
            directionToCamera.y = 0;
        }
        
        // Solo actualizamos si hay una distancia suficiente para evitar problemas de precisión
        if (directionToCamera.magnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(-directionToCamera); // Usamos negativo para que mire hacia la cámara
            
            // Aplicamos el offset de rotación
            lookRotation *= Quaternion.Euler(rotationOffset);
            
            transform.rotation = lookRotation;
        }
    }
}