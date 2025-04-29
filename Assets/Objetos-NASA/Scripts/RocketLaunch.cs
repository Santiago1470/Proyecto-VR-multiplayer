using UnityEngine;

public class RocketLaunch : MonoBehaviour
{
    public float launchSpeed = 10f;
    private bool launching = false;

    void Update()
    {
        if (launching)
            transform.Translate(Vector3.up * launchSpeed * Time.deltaTime);
    }

    public void StartLaunch()
    {
        launching = true;
    }
}
