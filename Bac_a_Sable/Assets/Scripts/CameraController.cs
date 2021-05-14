using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class CameraController : MonoBehaviour
{
    Camera cam;
    public Transform target;
    public float followSpeed = 10;
    public float rotationSpeed = 10;
    public float lookForwardAmount = 5;
    public Vector3 offsetPosition;



    void Start()
    {
        cam = Camera.main;
    }


    private void FixedUpdate()
    {
        Vector3 targetPosition = target.position + (target.forward * offsetPosition.z) + (target.up * offsetPosition.y) + (target.right * offsetPosition.x);

        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPosition, followSpeed * Time.deltaTime);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, target.rotation, rotationSpeed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        cam.transform.LookAt(target.position + target.forward * lookForwardAmount);
    }

}
