using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine.UI;
using Unity.Collections;

public class ScreenRecorderV2 : MonoBehaviour
{
    [Header("Cameras")]
    public Camera captureCamera;
    public Camera displayCamera;

    [Header("Canvas (Screen Space - Camera)")]
    public Canvas targetCanvas;

    [Header("Capture Settings")]
    //public int width = 1500;   // CHANGED
    //public int height = 200;   // CHANGED
    public int width = 1920;   // CHANGED
    public int height = 178;   // CHANGED
    public int fps = 25;

    [Header("FFmpeg Settings")]
    public string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
    public string outputFileName = "capture_1920x178.mp4"; // optional rename

    [Header("Recording Duration (seconds)")]
    public float recordingDuration = 10f;
    public float generalCountdown;

    [Header("External Script")]
    public DebugTool myDebugTool;
    

    // =========================
    // Internal
    // =========================
    private RenderTexture captureRT;
    private Process ffmpeg;
    private bool recording = false;

    private Queue<NativeArray<byte>> frameQueue = new Queue<NativeArray<byte>>();
    private NativeArray<byte>[] nativePool;
    private int poolIndex = 0;

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

        // Force RenderTexture to 1920x178
        captureRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        captureRT.Create();
        captureCamera.targetTexture = captureRT;

        UnityEngine.Debug.Log("ScreenRecorder ready. CaptureRT: " + width + "x" + height);
    }

    public void ButtonStartRecording()
    {
        //myDebugTool.countdown = recordingDuration;
        GeneralCountdown();
        if (!recording)
            
           
        StartCoroutine(StartRecordingCoroutine());

    }

    public void ButtonStopRecording()
    {
        if (recording)
            StopRecording();
    }

    private IEnumerator StartRecordingCoroutine()
    {
        recording = true;

        

        if (targetCanvas != null)
            targetCanvas.worldCamera = captureCamera;

        totalFrames = Mathf.CeilToInt(recordingDuration * fps);
        framesWritten = 0;

        int bytesPerFrame = width * height * 4;
        nativePool = new NativeArray<byte>[Mathf.Min(4, totalFrames)];

        for (int i = 0; i < nativePool.Length; i++)
        {
            nativePool[i] = new NativeArray<byte>(bytesPerFrame, Allocator.Persistent);
        }

        poolIndex = 0;

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
        UnityEngine.Debug.Log($"Recording resolution: {width}x{height}");

        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        int capturedFrames = 0;

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
                //myDebugTool.countdown =
                //    Mathf.Max(0, recordingDuration - ((float)capturedFrames / fps)); //DEBUG COUNTDOWN IMPORTANT
            });

            while (frameQueue.Count > 0)
            {
                var frame = frameQueue.Dequeue();
                ffmpeg.StandardInput.BaseStream.Write(frame.ToArray(), 0, frame.Length);
                framesWritten++;
                UnityEngine.Debug.Log($"Frame written: {framesWritten}/{totalFrames}");
            }
        }

        StopRecording();
    }

    private void StopRecording()
    {
        if (!recording) return;
        recording = false;

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

        if (nativePool != null)
        {
            foreach (var arr in nativePool)
                if (arr.IsCreated)
                    arr.Dispose();
        }

        UnityEngine.Debug.Log("Recording stopped and video saved.");
    }

    private void GeneralCountdown()
    {
        generalCountdown= 15f;
        

        // 1. Countdown from 10 seconds
        if (generalCountdown > 0)
        {
            generalCountdown -= Time.deltaTime;
            UnityEngine.Debug.Log("GeneralCountdown:"+ generalCountdown);
            myDebugTool.countdown = generalCountdown;
        }
        else
        {
            generalCountdown = 0;
            
        }
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
