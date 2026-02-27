using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GreenTrail : MonoBehaviour
{
    
    public RectTransform arrow;      // The Sprite GameObject
    public RectTransform pointA;     // Start position object
    public RectTransform pointB;     // End position object
    public RectTransform pointFade; // Extended fade point for Next Station
    public RectTransform tempStart;
    public RectTransform tempA;
    public RectTransform tempB;
    public RectTransform tempC;


    public float speed = 5f;     // Units per second

    void Start()
    {
        // Optional: Start the arrow exactly at Point A
        if (arrow != null && pointA != null)
            arrow.position = pointA.position;
        //IntializeTrailingEffects();
    }

    
    void Update()
    {
        if (arrow == null || pointA == null || pointB == null) return;

        // 1. Move towards Point B
        arrow.position = Vector3.MoveTowards(
            arrow.position,
            pointB.position,
            speed * Time.deltaTime
        );

        // 2. Check if we reached Point B
        // We use a small distance check (0.01) because float math is rarely perfect
        if (Vector3.Distance(arrow.position, pointB.position) < 0.01f)
        {
            // 3. Reset back to Point A instantly
            arrow.position = pointA.position;
        }
    }

    public void PathA()
    {
        pointA = tempStart;
        pointB = tempA;
    }
    public void PathB()
    {
        pointA = tempStart;
        pointB = tempB;
    }
    public void PathC()
    {
        pointA = tempStart;
        pointB = tempC;
    }
}
