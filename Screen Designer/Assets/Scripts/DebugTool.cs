using TMPro;
using UnityEngine;

public class DebugTool : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    public float countdown = 0f;
    private float fps = 0f;
    private string resolution = "";
    public TMP_Text lockFPS;
    public GameObject myProgressScreen;

    [Header("External Script")]    
    public CloneInfo myCloneInfo;


    void Start()
    {
        // Cache resolution once
        resolution = $"{Screen.currentResolution.width} x {Screen.currentResolution.height}";
    }

    void Update()
    {
        

        // 1. Countdown Logic
        if (countdown > 0)
        {
            countdown -= Time.deltaTime;
            myCloneInfo.clonedText.text = countdown.ToString();
        }
        else
        {
            countdown = 0; // Lock at 0
        }

        // 2. FPS Calculation
        fps = 1.0f / Time.smoothDeltaTime;

        // 3. Update Text
        if (debugText != null)
        {
            debugText.text = string.Format("Timer: {0:00}s | FPS: {1:0} | Res: {2}",
                Mathf.Ceil(countdown), fps, resolution);
            
        }
        
    }
}
