using UnityEngine;

public class Turn_Move : MonoBehaviour {
    public int TurnX;
    public int TurnY;
    public int TurnZ;

    public int MoveX;
    public int MoveY;
    public int MoveZ;

    public bool World;
    
    // Variable para activar la animación
    private bool shouldAnimate = false;

    // Se activa la animación mediante el trigger
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            shouldAnimate = true;
        }
    }

    // Update se ejecuta cada frame
    void Update () {
        if (shouldAnimate) {
            if (World) {
                transform.Rotate(TurnX * Time.deltaTime, TurnY * Time.deltaTime, TurnZ * Time.deltaTime, Space.World);
                transform.Translate(MoveX * Time.deltaTime, MoveY * Time.deltaTime, MoveZ * Time.deltaTime, Space.World);
            } else {
                transform.Rotate(TurnX * Time.deltaTime, TurnY * Time.deltaTime, TurnZ * Time.deltaTime, Space.Self);
                transform.Translate(MoveX * Time.deltaTime, MoveY * Time.deltaTime, MoveZ * Time.deltaTime, Space.Self);
            }
        }
    }
}
