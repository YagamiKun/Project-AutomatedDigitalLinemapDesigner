using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using Unity.Collections;
using System.IO;

public class ScreenRecorderOptimization : MonoBehaviour
{
    [Header("FFmpeg Settings")]
    public string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
    public string outputFileName = "capture.mp4";
    public int width = 1920;
    public int height = 1080;
    public int fps = 30;

    private Process ffmpeg;
    private Queue<NativeArray<byte>> frameQueue = new Queue<NativeArray<byte>>();
    private NativeArray<byte>[] nativePool;
    private int poolIndex = 0;

    public void ProcessFrame(NativeArray<byte> frameData, int w, int h)
    {
        // Initialize FFmpeg and pool on first frame
        if (ffmpeg == null)
        {
            StartFFmpeg();
            InitNativePool();
        }

        // Copy frame to pool and enqueue
        frameData.CopyTo(nativePool[poolIndex]);
        frameQueue.Enqueue(nativePool[poolIndex]);
        poolIndex = (poolIndex + 1) % nativePool.Length;

        // Write queued frames
        while (frameQueue.Count > 0)
        {
            var frame = frameQueue.Dequeue();
            ffmpeg.StandardInput.BaseStream.Write(frame.ToArray(), 0, frame.Length);
        }
    }

    private void StartFFmpeg()
    {
        string outputPath = Path.Combine(Application.dataPath, "..", outputFileName);
        outputPath = Path.GetFullPath(outputPath);

        string args = $"-y -f rawvideo -pix_fmt rgba -s {width}x{height} -r {fps} -i - " +
                      $"-vf vflip -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{outputPath}\"";

        ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = ffmpegPath;
        ffmpeg.StartInfo.Arguments = args;
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.RedirectStandardInput = true;
        ffmpeg.StartInfo.CreateNoWindow = true;
        ffmpeg.Start();
    }

    private void InitNativePool()
    {
        int bytesPerFrame = width * height * 4;
        nativePool = new NativeArray<byte>[4]; // small pool
        for (int i = 0; i < nativePool.Length; i++)
            nativePool[i] = new NativeArray<byte>(bytesPerFrame, Allocator.Persistent);

        poolIndex = 0;
    }

    public void FinalizeRecording()
    {
        // Close FFmpeg
        if (ffmpeg != null)
        {
            ffmpeg.StandardInput.Close();
            ffmpeg.WaitForExit();
            ffmpeg.Dispose();
            ffmpeg = null;
        }

        // Dispose pool
        if (nativePool != null)
        {
            foreach (var arr in nativePool)
                if (arr.IsCreated)
                    arr.Dispose();
        }

        frameQueue.Clear();
        UnityEngine.Debug.Log("Optimization complete. Video saved.");
    }

    private void OnDestroy()
    {
        FinalizeRecording();
    }
}
