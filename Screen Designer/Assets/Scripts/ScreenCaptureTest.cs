using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public class ScreenCaptureTest : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera captureCamera;       // Camera to capture
    public int width = 1920;
    public int height = 1080;

    [Header("Optional UI Preview")]
    public UnityEngine.UI.RawImage previewUI;

    private RenderTexture captureRT;

    void Start()
    {
        if (captureCamera == null) captureCamera = Camera.main;

        // Create RenderTexture for capture
        captureRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        captureRT.Create();

        // Force camera to render into RT
        captureCamera.targetTexture = captureRT;

        // Optional preview
        if (previewUI != null)
            previewUI.texture = captureRT;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // Normal screen output
        Graphics.Blit(src, dest);

        // AsyncGPUReadback from dedicated RT
        AsyncGPUReadback.Request(captureRT, 0, TextureFormat.RGBA32, OnFrameReady);
    }

    private void OnFrameReady(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogWarning("GPU readback error");
            return;
        }

        NativeArray<byte> data = request.GetData<byte>();

        Debug.Log($"Frame captured: {data.Length} bytes");

        // Optional: flip vertically
        FlipVerticalInPlace(data, width, height);
    }

    // Flip RGBA frame vertically (in-place)
    static void FlipVerticalInPlace(NativeArray<byte> data, int width, int height)
    {
        int rowSize = width * 4;
        byte[] temp = new byte[rowSize];

        for (int y = 0; y < height / 2; y++)
        {
            int top = y * rowSize;
            int bottom = (height - y - 1) * rowSize;

            // Copy top row → temp
            for (int i = 0; i < rowSize; i++)
                temp[i] = data[top + i];

            // Copy bottom row → top
            for (int i = 0; i < rowSize; i++)
                data[top + i] = data[bottom + i];

            // Copy temp → bottom
            for (int i = 0; i < rowSize; i++)
                data[bottom + i] = temp[i];
        }
    }
}
