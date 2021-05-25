using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testController : MonoBehaviour
{
    public float speed = 2;
    public float acceleration = 3;
    public float rotationSpeed = 2;
    float rotX;
    float rotY;

    float vertical;
    float horizontal;
    Vector3 TargetDir;

    void Start()
    {
        
    }

    void Update()
    {
        Movement();
        rotation();
    }

    void Movement()
    {
        vertical = Input.GetAxis("Vertical");
        horizontal = Input.GetAxis("Horizontal");

        TargetDir = transform.position + (transform.forward * vertical * speed) + (transform.right * horizontal * speed);
        TargetDir.y = transform.position.y;

        transform.position = Vector3.Lerp(transform.position, TargetDir, Time.deltaTime * acceleration);
    }

    void rotation()
    {
        rotX += rotationSpeed * Input.GetAxis("Mouse X");
        rotY += rotationSpeed * -Input.GetAxis("Mouse Y");
        transform.eulerAngles = new Vector3(rotY, rotX, 0);
    }
}
