using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class ScreenRecorderCore : MonoBehaviour
{
    [Header("Cameras")]
    public Camera captureCamera;
    public Camera displayCamera;

    [Header("Canvas")]
    public Canvas targetCanvas;

    [Header("Recording Settings")]
    public int width = 1920;
    public int height = 1080;
    public int fps = 30;
    public float recordingDuration = 10f;

    [Header("Optimization Handler")]
    public ScreenRecorderOptimization optimizationHandler;

    [Header("UI / Debug")]
    public DebugTool debugTool;

    private RenderTexture captureRT;
    private bool recording = false;
    private bool readbackInProgress = false;
    private int totalFrames;
    private int capturedFrames = 0;

    void Start()
    {
        if (captureCamera == null)
        {
            Debug.LogError("Capture camera not assigned!");
            return;
        }

        if (captureCamera.targetTexture == null)
        {
            captureRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            captureRT.Create();
            captureCamera.targetTexture = captureRT;
        }
        else
        {
            captureRT = captureCamera.targetTexture;
        }

        Debug.Log("ScreenRecorderCore ready.");
    }

    public void ButtonStartRecording()
    {
        if (!recording)
            StartCoroutine(CaptureRoutine());
    }

    public void ButtonStopRecording()
    {
        StopRecording();
    }

    private IEnumerator CaptureRoutine()
    {
        recording = true;
        capturedFrames = 0;
        totalFrames = Mathf.CeilToInt(recordingDuration * fps);

        // Assign canvas to capture camera
        if (targetCanvas != null)
            targetCanvas.worldCamera = captureCamera;

        debugTool.countdown = recordingDuration;

        WaitForEndOfFrame wait = new WaitForEndOfFrame();

        while (recording && capturedFrames < totalFrames)
        {
            yield return wait;

            if (readbackInProgress)
                continue;

            readbackInProgress = true;
            int frameIndex = capturedFrames;

            AsyncGPUReadback.Request(captureRT, 0, TextureFormat.RGBA32, req =>
            {
                readbackInProgress = false;

                if (!recording) return;

                if (req.hasError)
                {
                    Debug.LogWarning($"GPU readback error at frame {frameIndex + 1}");
                }
                else
                {
                    optimizationHandler?.ProcessFrame(req.GetData<byte>(), width, height);
                }

                capturedFrames++;
                debugTool.countdown = Mathf.Max(0f, recordingDuration - ((float)capturedFrames / fps));
            });
        }

        StopRecording();
    }

    private void StopRecording()
    {
        if (!recording) return;

        recording = false;

        if (targetCanvas != null && displayCamera != null)
            targetCanvas.worldCamera = displayCamera;

        optimizationHandler?.FinalizeRecording();
        Debug.Log($"Recording stopped. Frames captured: {capturedFrames}");
    }
}
