using UnityEngine;

public class RotateAroundYAxis : MonoBehaviour
{
    public float rotationSpeed = 45.0f; // Rotation speed in degrees per second

    void Update()
    {
        // Rotate the GameObject around its Y-axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}