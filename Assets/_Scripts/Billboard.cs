using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera camera;

    private void Update()
    {
        if (camera == null)
        {
            camera = FindObjectOfType<Camera>();
        }
        
        if(camera == null)
        {
            return;
        }

        transform.LookAt(camera.transform);
        transform.Rotate(Vector3.up * 180);
    }
}
