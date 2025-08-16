using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using Vuforia;
using System.IO;
using System.Collections;

public class PlayOnTracking : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;
    public string videoFileName = "output.mp4"; // Must be inside StreamingAssets

    private ObserverBehaviour observerBehaviour;
    private bool isVideoReady = false;

    void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        StartCoroutine(PrepareVideoPath());
    }

    IEnumerator PrepareVideoPath()
    {
#if UNITY_ANDROID
        string sourcePath = Path.Combine(Application.streamingAssetsPath, videoFileName);
        string destPath = Path.Combine(Application.persistentDataPath, videoFileName);

        if (!File.Exists(destPath))
        {
            UnityWebRequest request = UnityWebRequest.Get(sourcePath);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(destPath, request.downloadHandler.data);
            }
            else
            {
                Debug.LogError("Failed to copy video from StreamingAssets: " + request.error);
                yield break;
            }
        }

        if (videoPlayer != null)
            videoPlayer.url = destPath;
#else
        if (videoPlayer != null)
            videoPlayer.url = "file://" + Path.Combine(Application.streamingAssetsPath, videoFileName);
#endif

        // Prepare the video and wait until it's ready
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        Debug.Log("Video is ready to play.");
        isVideoReady = true;
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (!isVideoReady) return; // Avoid playing if not prepared

        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            if (videoPlayer != null && !videoPlayer.isPlaying)
                videoPlayer.Play();
            if (audioSource != null && !audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.Pause();
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Pause();
        }
    }
}
