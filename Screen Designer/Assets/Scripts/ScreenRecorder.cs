using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine.UI;
using Unity.Collections;

public class ScreenRecorder : MonoBehaviour
{
    [Header("Cameras")]
    public Camera captureCamera;
    public Camera displayCamera;

    [Header("Canvas (Screen Space - Camera)")]
    public Canvas targetCanvas;

    [Header("Capture Settings")]
    public int width = 1920;
    public int height = 1080;
    public int fps = 30;

    [Header("FFmpeg Settings")]
    public string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
    public string outputFileName = "capture.mp4";

    [Header("Recording Duration (seconds)")]
    public float recordingDuration = 10f;

    [Header("External Script")]
    public DebugTool myDebugTool;

    // =========================
    // Internal
    // =========================
    private RenderTexture captureRT;
    private Process ffmpeg;
    private bool recording = false;

    // Queue of frames ready to write
    private Queue<NativeArray<byte>> frameQueue = new Queue<NativeArray<byte>>();

    // Pool of reusable NativeArrays
    private NativeArray<byte>[] nativePool;
    private int poolIndex = 0;

    // AsyncGPUReadback control
    private bool readbackInProgress = false;
    private int totalFrames;
    private int framesWritten = 0;

    void Start()
    {
        if (captureCamera == null)
        {
            UnityEngine.Debug.LogError("Capture Camera not assigned.");
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

        UnityEngine.Debug.Log("ScreenRecorder ready. CaptureRT: " + captureRT.name);
    }

    // =========================
    // PUBLIC BUTTONS
    // =========================
    public void ButtonStartRecording()
    {
        if (!recording)
            StartCoroutine(StartRecordingCoroutine());
    }

    public void ButtonStopRecording()
    {
        if (recording)
            StopRecording();
    }

    // =========================
    // RECORDING FLOW
    // =========================
    private IEnumerator StartRecordingCoroutine()
    {
        recording = true;

        // Setup countdown
        myDebugTool.countdown = recordingDuration;

        // Assign capture camera to canvas
        if (targetCanvas != null)
            targetCanvas.worldCamera = captureCamera;

        totalFrames = Mathf.CeilToInt(recordingDuration * fps);
        framesWritten = 0;

        // =========================
        // Preallocate NativeArray pool
        // =========================
        int bytesPerFrame = width * height * 4; // RGBA32
        nativePool = new NativeArray<byte>[Mathf.Min(4, totalFrames)]; // small pool of 4
        for (int i = 0; i < nativePool.Length; i++)
        {
            nativePool[i] = new NativeArray<byte>(bytesPerFrame, Allocator.Persistent);
        }
        poolIndex = 0;

        // =========================
        // Start FFmpeg
        // =========================
        string outputPath = Path.Combine(Application.dataPath, "..", outputFileName);
        outputPath = Path.GetFullPath(outputPath);

        string args =
            $"-y -f rawvideo -pix_fmt rgba -s {width}x{height} -r {fps} -i - " +
            $"-vf vflip -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{outputPath}\"";

        ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = ffmpegPath;
        ffmpeg.StartInfo.Arguments = args;
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.RedirectStandardInput = true;
        ffmpeg.StartInfo.CreateNoWindow = true;
        ffmpeg.Start();

        UnityEngine.Debug.Log($"Recording started for {recordingDuration}s at {fps} FPS.");

        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        int capturedFrames = 0;

        // =========================
        // Capture loop
        // =========================
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

                if (!recording)
                    return;

                NativeArray<byte> frameData = nativePool[poolIndex];
                poolIndex = (poolIndex + 1) % nativePool.Length;

                if (req.hasError)
                {
                    UnityEngine.Debug.LogWarning($"GPU readback error at frame {frameIndex + 1}");
                }
                else
                {
                    NativeArray<byte> data = req.GetData<byte>();
                    data.CopyTo(frameData);
                    frameQueue.Enqueue(frameData);
                }

                capturedFrames++;
                myDebugTool.countdown = Mathf.Max(0, recordingDuration - ((float)capturedFrames / fps));
            });

            // Write queued frames to FFmpeg
            while (frameQueue.Count > 0)
            {
                var frame = frameQueue.Dequeue();
                ffmpeg.StandardInput.BaseStream.Write(frame.ToArray(), 0, frame.Length);
                framesWritten++;
                UnityEngine.Debug.Log($"Frame written: {framesWritten}/{totalFrames}");
            }
        }

        // Auto stop
        StopRecording();
    }

    private void StopRecording()
    {
        if (!recording) return;
        recording = false;

        // Restore canvas to display camera
        if (targetCanvas != null && displayCamera != null)
            targetCanvas.worldCamera = displayCamera;

        StartCoroutine(FinalizeFFmpeg());
    }

    private IEnumerator FinalizeFFmpeg()
    {
        yield return new WaitForSeconds(0.2f);

        if (ffmpeg != null)
        {
            ffmpeg.StandardInput.Close();
            ffmpeg.WaitForExit();
            ffmpeg.Dispose();
        }

        // Dispose native arrays
        if (nativePool != null)
        {
            foreach (var arr in nativePool)
                if (arr.IsCreated)
                    arr.Dispose();
        }

        UnityEngine.Debug.Log("Recording stopped and video saved.");
    }

    void OnDestroy()
    {
        recording = false;

        if (ffmpeg != null)
        {
            ffmpeg.Kill();
            ffmpeg.Dispose();
        }

        if (nativePool != null)
        {
            foreach (var arr in nativePool)
                if (arr.IsCreated)
                    arr.Dispose();
        }
    }
}
