using UnityEngine;
using UnityEngine.Video;

public class VideoAutoPause : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Transform vrCamera;
    public float viewAngleThreshold = 60f; // ángulo en grados

    void Update()
    {
        Vector3 toScreen = (transform.position - vrCamera.position).normalized;
        float angle = Vector3.Angle(vrCamera.forward, toScreen);

        if (angle < viewAngleThreshold)
        {
            if (!videoPlayer.isPlaying)
                videoPlayer.Play();
        }
        else
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
        }
    }
}
