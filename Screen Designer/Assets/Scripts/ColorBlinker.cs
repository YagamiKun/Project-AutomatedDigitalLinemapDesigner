using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ColorBlinker : MonoBehaviour

{
    [SerializeField] private float changeInterval = 2f;
    private Image rend;
    public Image image;
    public bool dynamicColor=false;
    public bool singleColor = false;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void Start()
    {
        rend = GetComponent<Image>();
        if (dynamicColor)
        StartCoroutine(ChangeColorRoutine());
        if (singleColor)
        StartCoroutine(PingPongColor(Color.white, Color.green, .1f));
    }

    private IEnumerator ChangeColorRoutine()
    {
        while (true)
        {
            image.color = GetRandomColor();
            yield return new WaitForSeconds(changeInterval);
        }
    }

    private Color GetRandomColor()
    {
        return new Color(
            Random.value,
            Random.value,
            Random.value,
            1f // keep alpha fully opaque
        );
    }

    public IEnumerator PingPongColor(Color color1, Color color2, float duration)
    {
        float elapsed = 0f;
        float halfDuration = duration;
        //float halfDuration = duration / 2f;

        // Phase 1: Color 1 to Color 2
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            //rend.color = Color.Lerp(color1, color2, elapsed / halfDuration);
            rend.color = Color.Lerp(color1, color2, halfDuration);
            yield return null;
        }

        elapsed = 0f; // Reset timer for the way back

        // Phase 2: Color 2 back to Color 1
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            rend.color = Color.Lerp(color2, color1, elapsed / halfDuration);
            yield return null;
        }

        // Ensure it ends exactly on color1
        rend.color = color1;
    }

    public void ManualTrailCaller()
    {
        StartCoroutine(PingPongColor(Color.white, Color.green, 2.0f));
    }
}

