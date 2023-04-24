using System.Collections;
using UnityEngine;

/// <summary>
/// Allows the camera to be rotated and zoomed around the CharacterController.
/// </summary>
public class DebugMouseLook : MonoBehaviour
{
    public float sensitivity = 1.0f;
    public CharacterController player;

    void Start()
    {
        // Place the camera at a comfortable position behind the player
        this.transform.position = player.transform.position
                                + 2 * Vector3.up
                                + 5 * Vector3.back;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Mouse X");
        float vertical = Input.GetAxis("Mouse Y");

        // Rotate camera around player when clicking and dragging
        if (Input.GetKey(KeyCode.Mouse1))
        {
            transform.RotateAround(player.transform.position, Vector3.down, horizontal * sensitivity);
            transform.RotateAround(player.transform.position, transform.right, vertical * sensitivity);
        }

        // Vector from player to camera
        Vector3 playerToCamera = this.transform.position - player.transform.position;

        // Scale the vector based on scroll wheel
        Vector3 scaledPlayerToCamera = (Input.mouseScrollDelta.y * -0.1f) * playerToCamera;

        // Zoom in/out
        this.transform.position += scaledPlayerToCamera;
    }
}