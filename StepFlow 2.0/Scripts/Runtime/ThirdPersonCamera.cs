using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public float turnSpeed = 4.0f;
    public GameObject target;
    private float targetDistance;
    public float minTurnAngle = -90.0f;
    public float maxTurnAngle = 0.0f;
    private float rotX;
    [SerializeField]
    private float TargetD;
    Vector3 targetP;
    void Start()
    {
        targetDistance = Vector3.Distance(transform.position, target.transform.position);

        targetDistance = TargetD;
        // get the mouse inputs
        float y = Input.GetAxis("Mouse X") * turnSpeed;
        rotX += Input.GetAxis("Mouse Y") * turnSpeed;
        // clamp the vertical rotation
        rotX = Mathf.Clamp(rotX, minTurnAngle, maxTurnAngle);
        // rotate the camera
        transform.eulerAngles = new Vector3(-rotX, transform.eulerAngles.y + y, 0);
        // move the camera position
        transform.position = target.transform.position - (transform.forward * targetDistance);
    }
    void Update()
    {
        targetP = new Vector3(target.transform.position.x, Mathf.Lerp(targetP.y, target.transform.position.y, Time.deltaTime * 10), target.transform.position.z);
        if (Input.GetMouseButton(1) || true)
        {
            targetDistance = TargetD;
            // get the mouse inputs
            float y = Input.GetAxis("Mouse X") * turnSpeed;
            rotX += Input.GetAxis("Mouse Y") * turnSpeed;
            // clamp the vertical rotation
            rotX = Mathf.Clamp(rotX, minTurnAngle, maxTurnAngle);
            // rotate the camera
            transform.eulerAngles = new Vector3(-rotX, transform.eulerAngles.y + y, 0);
            // move the camera position
        }
        transform.position = targetP - (transform.forward * targetDistance);
    }
}
