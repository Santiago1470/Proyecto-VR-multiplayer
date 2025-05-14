using UnityEngine;

public class ResetManager : MonoBehaviour
{
    private ChemicalTube[] allTubes;

    void Awake()
    {
        allTubes = FindObjectsOfType<ChemicalTube>();
    }

    public void ResetAllTubes()
    {
        foreach (var tube in allTubes)
        {
            // true = que use StartManualInteraction para snappear al socket
            tube.ResetToInitial(snapToSocket: true);
        }
    }
}
