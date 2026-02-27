using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Header("Fixed Frame Rate Settings")]
    public bool useFixedFPS = false;      // Toggle on/off
    public int targetFPS = 25;            // Desired FPS when fixed
    private int originalVSync;

    void Awake()
    {
        // Store original vSyncCount to restore later
        originalVSync = QualitySettings.vSyncCount;
    }

    void Update()
    {
        if (useFixedFPS)
        {
            // Disable VSync and apply target frame rate
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
        }
        else
        {
            // Restore default (VSync enabled or uncapped)
            QualitySettings.vSyncCount = originalVSync;
            Application.targetFrameRate = -1; // uncapped
        }

        // Optional: debug info
        ////if (Input.GetKeyDown(KeyCode.F))
        ////{
            
        ////}

    }

    public void ViewFPS()
    {
        useFixedFPS = !useFixedFPS;
        Debug.Log($"Fixed FPS {(useFixedFPS ? "enabled" : "disabled")}, target: {targetFPS}");
    }
}
