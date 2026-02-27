using UnityEngine;

public class Cube_RandomRotation : MonoBehaviour
{
    // The speed at which the cube will rotate
    public float rotationSpeed = 30f;

    // Stores the random rotation direction
    private Vector3 randomDirection;

    void Start()
    {
        // Generate a random rotation axis (normalized vector)
        randomDirection = Random.onUnitSphere;
    }

    void Update()
    {
        // Rotate the object around the randomDirection axis every frame
        // Time.deltaTime ensures the rotation is smooth and frame-rate independent
        transform.Rotate(randomDirection * rotationSpeed * Time.deltaTime);
    }
}
