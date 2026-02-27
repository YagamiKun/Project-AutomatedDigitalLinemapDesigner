//using UnityEngine;
//using UnityEngine.Rendering;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using Unity.Collections;
//using System;

//public class ScreenRecorderV3 : MonoBehaviour
//{
//    [Header("Cameras")]
//    public Camera captureCamera;
//    public Camera displayCamera;

//    [Header("Canvas (Screen Space - Camera)")]
//    public Canvas targetCanvas;

//    [Header("Capture Settings")]
//    public int width = 1920;
//    public int height = 178;
//    public int fps = 25;

//    [Header("FFmpeg Settings")]
//    public string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
//    public string outputFileName = "capture_1920x178.mp4";

//    [Header("Recording Duration (seconds)")]
//    public float recordingDuration = 10f;

//    [Header("FPS Lock")]
//    public bool fpsLockEnabled = true; // Toggle for testing FPS lock

//    [Header("External Script")]
//    public DebugTool myDebugTool;

//    // Internal
//    private RenderTexture captureRT;
//    private bool recording = false;
//    private int framesCaptured = 0;

//    private string frameFolderPath;

//    void Start()
//    {
//        if (captureCamera == null)
//        {
//            UnityEngine.Debug.LogError("Capture Camera not assigned.");
//            return;
//        }

//        // Force RenderTexture
//        captureRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
//        captureRT.Create();
//        captureCamera.targetTexture = captureRT;

//        if (fpsLockEnabled)
//        {
//            Application.targetFrameRate = fps;
//            myDebugTool.lockFPS.text = "Lock FPS" + fps.ToString();
//            UnityEngine.Debug.Log($"FPS lock enabled: {fps} FPS");
//        }

//        UnityEngine.Debug.Log($"ScreenRecorderV3 ready. CaptureRT: {width}x{height}");
//    }

//    public void ButtonStartRecording()
//    {
//        if (!recording)
//            StartCoroutine(StartRecordingCoroutine());
//    }

//    public void ButtonStopRecording()
//    {
//        if (recording)
//            StopRecording();
//    }

//    private IEnumerator StartRecordingCoroutine()
//    {
//        recording = true;
//        framesCaptured = 0;

//        if (targetCanvas != null)
//            targetCanvas.worldCamera = captureCamera;

//        // Create folder for PNGs
//        string sessionFolder = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
//        frameFolderPath = Path.Combine(Application.dataPath, "..", "CaptureFrames", sessionFolder);
//        Directory.CreateDirectory(frameFolderPath);
//        UnityEngine.Debug.Log($"Frame folder created: {frameFolderPath}");

//        WaitForEndOfFrame wait = new WaitForEndOfFrame();
//        float startTime = Time.realtimeSinceStartup;

//        while (recording)
//        {
//            yield return wait;

//            float wallClock = Time.realtimeSinceStartup - startTime;
//            if (wallClock >= recordingDuration)
//                break;

//            // Capture frame
//            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
//            RenderTexture.active = captureRT;
//            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//            tex.Apply();

//            string framePath = Path.Combine(frameFolderPath, $"frame_{framesCaptured:D4}.png");
//            File.WriteAllBytes(framePath, tex.EncodeToPNG());
//            UnityEngine.Object.Destroy(tex);

//            framesCaptured++;
//            UnityEngine.Debug.Log($"Frame captured: {framesCaptured} | Wall clock: {wallClock:F2}s / {recordingDuration}s");
//        }

//        UnityEngine.Debug.Log($"Total frames captured: {framesCaptured}, starting encoding...");

//        // Start encoding
//        StartCoroutine(EncodeFramesToVideo());
//    }

//    private IEnumerator EncodeFramesToVideo()
//    {
//        string framePattern = Path.Combine(frameFolderPath, "frame_%04d.png");
//        string outputPath = Path.Combine(Application.dataPath, "..", outputFileName);
//        outputPath = Path.GetFullPath(outputPath);

//        string args =
//            $"-y -framerate {fps} -i \"{framePattern}\" -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{outputPath}\"";

//        Process ffmpeg = new Process();
//        ffmpeg.StartInfo.FileName = ffmpegPath;
//        ffmpeg.StartInfo.Arguments = args;
//        ffmpeg.StartInfo.UseShellExecute = false;
//        ffmpeg.StartInfo.RedirectStandardOutput = true;
//        ffmpeg.StartInfo.RedirectStandardError = true;
//        ffmpeg.StartInfo.CreateNoWindow = true;
//        ffmpeg.Start();

//        // Optional: print ffmpeg logs
//        while (!ffmpeg.HasExited)
//        {
//            string line = ffmpeg.StandardError.ReadLine();
//            if (!string.IsNullOrEmpty(line))
//                UnityEngine.Debug.Log("[FFmpeg] " + line);
//            yield return null;
//        }

//        ffmpeg.WaitForExit();
//        ffmpeg.Dispose();

//        UnityEngine.Debug.Log($"Encoding finished. Video saved at: {outputPath}");
//        recording = false;
//        myDebugTool.lockFPS.text = "Recording Finished.";
//    }

//    private void StopRecording()
//    {
//        if (!recording) return;
//        recording = false;

//        if (targetCanvas != null && displayCamera != null)
//            targetCanvas.worldCamera = displayCamera;

//        UnityEngine.Debug.Log("Recording stopped.");

//    }

//    void OnDestroy()
//    {
//        recording = false;
//    }
//}
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using Unity.Collections;
using UnityEngine.Video;

public class ScreenRecorderV3 : MonoBehaviour
{
    [Header("Cameras")]
    public Camera captureCamera;
    public Camera displayCamera;

    [Header("Video Player")]
    public VideoPlayer myVideoPlayer;

    [Header("Canvas (Screen Space - Camera)")]
    public Canvas targetCanvas;

    [Header("Capture Settings")]
    public int width = 1920;
    public int height = 178;
    public int fps = 25;

    [Header("FFmpeg Settings")]
    public string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
    public string outputFileName = "capture_1920x178.mp4";

    [Header("Recording Duration (seconds)")]
    public float recordingDuration = 10f;

    [Header("FPS Lock")]
    public bool fpsLockEnabled = true;

    [Header("External Script")]
    public DebugTool myDebugTool;

    // Internal
    private RenderTexture captureRT;
    private bool recording = false;
    private int framesCaptured = 0;
    private string frameFolderPath;

    void Start()
    {
        if (captureCamera == null)
        {
            UnityEngine.Debug.LogError("Capture Camera not assigned.");
            return;
        }

        // Force RenderTexture
        captureRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        captureRT.Create();
        captureCamera.targetTexture = captureRT;

        if (fpsLockEnabled)
        {
            Application.targetFrameRate = fps;
            UnityEngine.Debug.Log($"FPS lock enabled: {fps} FPS");
        }

        UnityEngine.Debug.Log($"ScreenRecorderV3 ready. CaptureRT: {width}x{height}");
    }

    public void ButtonStartRecording()
    {

        if (!recording)
            myVideoPlayer.enabled = true;
            myDebugTool.myProgressScreen.SetActive(true);

            StartCoroutine(StartRecordingCoroutine());
    }

    public void ButtonStopRecording()
    {

        if (recording)
            recording = false;
    }

    private IEnumerator StartRecordingCoroutine()
    {
        
        recording = true;
        framesCaptured = 0;
        

        if (targetCanvas != null)
            targetCanvas.worldCamera = captureCamera;

        // Create folder for PNGs
        string sessionFolder = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
        frameFolderPath = Path.Combine(Application.dataPath, "..", "CaptureFrames", sessionFolder);
        Directory.CreateDirectory(frameFolderPath);
        UnityEngine.Debug.Log($"Frame folder created: {frameFolderPath}");

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
            UnityEngine.Debug.Log($"Frame captured: {framesCaptured} | Wall clock: {wallClock:F2}s / {recordingDuration}s");
        }

        UnityEngine.Debug.Log($"Total frames captured: {framesCaptured}. Starting encoding...");

        StartCoroutine(EncodeFramesToVideo());
    }

    private IEnumerator EncodeFramesToVideo()
    {
        string framePattern = Path.Combine(frameFolderPath, "frame_%04d.png");
        string outputPath = Path.Combine(Application.dataPath, "..", outputFileName);
        outputPath = Path.GetFullPath(outputPath);

        string args =
            $"-y -framerate {fps} -i \"{framePattern}\" -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{outputPath}\"";

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

        UnityEngine.Debug.Log($"Encoding finished. Video saved at: {outputPath}");
        myDebugTool.lockFPS.text = "Export Successful!";
        myDebugTool.myProgressScreen.SetActive(false);
        recording = false;
        if (targetCanvas != null)
            targetCanvas.worldCamera = displayCamera;
    }

    void OnDestroy()
    {
        recording = false;
    }
    private void OnApplicationQuit()
    {
        Application.Quit();
    }
    public void QuitApplication()
    {
        OnApplicationQuit();
    }
}