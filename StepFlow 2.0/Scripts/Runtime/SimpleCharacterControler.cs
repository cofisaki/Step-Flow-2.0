using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCharacterControler : MonoBehaviour
{
    [SerializeField]
    private CharacterController Controller;
    [SerializeField]
    private float MovementSpeed;
    [SerializeField]
    private float SprintMultiplier;
    [SerializeField]
    private float Acceleration;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private bool rotate;

    private Vector2 MovementDir;
    private Vector2 Move;
    private float verticalSpeed;

    // Update is called once per frame
    private void Awake()
    {
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }


    void Update()
    {
        if (Controller.isGrounded)
        {
            verticalSpeed = -5f;
        }
        else
        {
            verticalSpeed -= 9.81f * Time.deltaTime;
        }
        MovementDir = Vector2.zero;
        MovementDir += v3To2(cam.transform.forward) * Input.GetAxis("Vertical");
        MovementDir += v3To2(cam.transform.right) * Input.GetAxis("Horizontal");
        MovementDir.Normalize();
        if (Input.GetKey(KeyCode.LeftShift))
        {
            MovementDir *= SprintMultiplier;
        }
        Move = Vector2.Lerp(Move, MovementDir * MovementSpeed, Acceleration * Time.deltaTime);
        Controller.Move(v2T3(Move) * Time.deltaTime + Vector3.up * verticalSpeed * Time.deltaTime);
        if (MovementDir.magnitude > 0 && rotate && !Input.GetMouseButton(0))
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(v2T3(Move), Vector3.up), Time.deltaTime * 3);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }

        if (Input.GetButtonDown("Jump"))
        {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

    }

    private Vector3 v2T3(Vector2 vector)
    {
        return new Vector3(vector.x, 0, vector.y);
    }
    private Vector2 v3To2(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }
}
