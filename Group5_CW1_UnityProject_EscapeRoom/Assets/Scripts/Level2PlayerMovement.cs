// AI-assisted change: fixed overhead camera and matching input for the Escape Room.
using UnityEngine;

public class Level2PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Camera playerCamera = GetComponentInChildren<Camera>();
        playerCamera.transform.SetParent(null);
        playerCamera.transform.SetPositionAndRotation(new Vector3(0, 8, -.5f), Quaternion.Euler(90, 0, 0));
        playerCamera.orthographic = true;
        playerCamera.orthographicSize = Mathf.Max(7.5f, 5.5f / playerCamera.aspect);

        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Player marker";
        marker.GetComponent<Collider>().enabled = false;
        marker.GetComponent<Renderer>().material.color = Color.cyan;
        marker.transform.SetParent(transform, false);
        marker.transform.localScale = new Vector3(.45f, .25f, .45f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical);

        if (movement.magnitude > 1f)
            movement.Normalize();

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;

        controller.Move(movement * moveSpeed * Time.deltaTime);

    }
}
