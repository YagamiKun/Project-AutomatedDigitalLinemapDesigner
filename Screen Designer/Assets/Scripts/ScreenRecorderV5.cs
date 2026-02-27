using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System;

public class ScreenRecorderV5 : MonoBehaviour
{
    [Header("Cameras")]
    public Camera captureCamera;
    public Camera displayCamera;

    [Header("Canvas (Screen Space - Camera)")]
    public Canvas targetCanvas;
    public GameObject progressScreen;

    [Header("Video Player")]
    public UnityEngine.Video.VideoPlayer myVideoPlayer;

    [Header("Capture Settings")]
    public int width = 1920;
    public int height = 178;
    public int fps = 25;

    [Header("Recording Duration (seconds)")]
    public float recordingDuration = 10f;

    [Header("FPS Lock")]
    public bool fpsLockEnabled = true;

    [Header("External Script")]
    public DebugTool myDebugTool;

    // -----------------------------
    // Internal
    // -----------------------------
    private RenderTexture captureRT;
    private bool recording = false;
    private int framesCaptured = 0;
    private string frameFolderPath;

    private string ffmpegPath;

    // -----------------------------
    // Initialization
    // -----------------------------
    void Awake()
    {
        // Auto-detect FFmpeg for Windows
#if UNITY_STANDALONE_WIN
        ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg/Windows/ffmpeg.exe");
#else
            Debug.LogWarning("[ScreenRecorder] FFmpeg path not configured for this platform.");
#endif

        if (!File.Exists(ffmpegPath))
            UnityEngine.Debug.LogError($"[ScreenRecorder] FFmpeg executable not found at: {ffmpegPath}");
    }

    void Start()
    {
        if (captureCamera == null)
        {
            UnityEngine.Debug.LogError("[ScreenRecorder] Capture Camera not assigned.");
            return;
        }

        // Create forced RenderTexture for capture
        captureRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        captureRT.Create();
        captureCamera.targetTexture = captureRT;

        if (fpsLockEnabled)
        {
            Application.targetFrameRate = fps;
            if (myDebugTool != null && myDebugTool.lockFPS != null)
                myDebugTool.lockFPS.text = "Lock FPS " + fps;
            UnityEngine.Debug.Log($"[ScreenRecorder] FPS lock enabled: {fps} FPS");
        }

        UnityEngine.Debug.Log($"[ScreenRecorder] Ready. CaptureRT: {width}x{height}");
    }

    // -----------------------------
    // Button Handlers
    // -----------------------------
    public void ButtonStartRecording()
    {
        if (recording)
            return;

        // Fail-safe: ensure FFmpeg exists
        if (!File.Exists(ffmpegPath))
        {
            UnityEngine.Debug.LogError("[ScreenRecorder] Cannot start recording: FFmpeg missing.");
            UpdateStatus("FFmpeg missing. Recording disabled.");
            return;
        }

        if (myVideoPlayer != null)
            myVideoPlayer.enabled = true;
            progressScreen.SetActive(true);

        if (myDebugTool != null && myDebugTool.myProgressScreen != null)
            myDebugTool.myProgressScreen.SetActive(true);

        StartCoroutine(StartRecordingCoroutine());
    }

    public void ButtonStopRecording()
    {
        recording = false;
    }

    // -----------------------------
    // Recording Coroutine
    // -----------------------------
    private IEnumerator StartRecordingCoroutine()
    {
        recording = true;
        framesCaptured = 0;

        if (targetCanvas != null)
            targetCanvas.worldCamera = captureCamera;

        // Create folder for PNG frames
        string sessionFolder = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
        frameFolderPath = Path.Combine(Application.dataPath, "..", "CaptureFrames", sessionFolder);
        Directory.CreateDirectory(frameFolderPath);
        UnityEngine.Debug.Log($"[ScreenRecorder] Frame folder: {frameFolderPath}");

        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        float startTime = Time.realtimeSinceStartup;
        float nextCaptureTime = 0f;

        while (recording)
        {
            yield return wait;

            float wallClock = Time.realtimeSinceStartup - startTime;
            if (wallClock >= recordingDuration)
                break;

            if (wallClock < nextCaptureTime)
                continue; // wait until next frame based on target FPS

            // Capture frame
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture.active = captureRT;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            string framePath = Path.Combine(frameFolderPath, $"frame_{framesCaptured:D4}.png");
            File.WriteAllBytes(framePath, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);

            framesCaptured++;
            nextCaptureTime += 1f / fps;
            UnityEngine.Debug.Log($"[ScreenRecorder] Frame {framesCaptured} | Time: {wallClock:F2}s / {recordingDuration}s");
        }

        UnityEngine.Debug.Log($"[ScreenRecorder] Total frames captured: {framesCaptured}. Starting encoding...");

        StartCoroutine(EncodeFramesToVideo());
    }

    // -----------------------------
    // Encode Frames to MP4
    // -----------------------------
    private IEnumerator EncodeFramesToVideo()
    {
        string framePattern = Path.Combine(frameFolderPath, "frame_%04d.png");
        string outputPath = Path.Combine(Application.dataPath, "..", "capture.mp4");
        outputPath = Path.GetFullPath(outputPath);

        string args = $"-y -framerate {fps} -i \"{framePattern}\" -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{outputPath}\"";

        Process ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = ffmpegPath;
        ffmpeg.StartInfo.Arguments = args;
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.RedirectStandardOutput = true;
        ffmpeg.StartInfo.RedirectStandardError = true;
        ffmpeg.StartInfo.CreateNoWindow = true;
        ffmpeg.Start();

        while (!ffmpeg.HasExited)
        {
            string line = ffmpeg.StandardError.ReadLine();
            if (!string.IsNullOrEmpty(line))
                UnityEngine.Debug.Log("[FFmpeg] " + line);
            yield return null;
        }

        ffmpeg.WaitForExit();
        ffmpeg.Dispose();

        UnityEngine.Debug.Log($"[ScreenRecorder] Encoding finished. Video saved at: {outputPath}");
        UpdateStatus("Export Successful!");
        progressScreen.SetActive(false);

        if (myDebugTool != null && myDebugTool.myProgressScreen != null)
            myDebugTool.myProgressScreen.SetActive(false);

        recording = false;

        if (targetCanvas != null && displayCamera != null)
            targetCanvas.worldCamera = displayCamera;
    }

    // -----------------------------
    // Helper
    // -----------------------------
    private void UpdateStatus(string message)
    {
        if (myDebugTool != null && myDebugTool.lockFPS != null)
            myDebugTool.lockFPS.text = message;
    }

    void OnDestroy()
    {
        recording = false;
    }
}