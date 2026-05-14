using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class generateCube : MonoBehaviour
{
    // Call this method to create a cube
    public void addCube(string x)
    {
        // Create a cube primitive
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Set the position of the cube (optional)
        cube.transform.position = new Vector3(0, 5, 0);

        // Add a Rigidbody component to enable physics
        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;

        if (x.Equals(""))
            cube.transform.localScale = new Vector3(0.1f, 1, 0.1f);
        if (x.Equals("big"))
            cube.transform.localScale = new Vector3(2, 2, 2);
        if (x.Equals("small"))
            cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }
}
