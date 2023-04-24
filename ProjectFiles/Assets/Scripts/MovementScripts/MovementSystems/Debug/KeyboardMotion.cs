using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug WASD keyboard controls.
/// </summary>
public class KeyboardMotion : MonoBehaviour
{
    public CharacterController player;
    public float moveSpeed = 8.0f;
    public Camera debugCamera;

    public void Update()
    {
        Vector3 move = new Vector3(0, 0, 0);

        float _moveSpeed = moveSpeed;

        // Fly faster
        if (Input.GetKey(KeyCode.LeftShift))
            _moveSpeed = 2 * moveSpeed;

        // This is only for debug mode, so we hardcode the bindings to WASD
        if (Input.GetKey(KeyCode.W))
            move += Vector3.forward * _moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.A))
            move += Vector3.left * _moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S))
            move += Vector3.back * _moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.D))
            move += Vector3.right * _moveSpeed * Time.deltaTime;

        Vector3 camTransformed = debugCamera.transform.TransformDirection(move);

        // Transforming the direction to match the cameras orientation can introduce some non-zero
        // vertical component to the move vector. We remove that here so that only Shift and Space can move up/down.
        camTransformed.y = 0;

        Vector3 upMove = new Vector3(0, 0, 0);

        // Fly up
        if (Input.GetKey(KeyCode.Space))
            upMove += Vector3.up * _moveSpeed * Time.deltaTime;

        // FLy down
        if (Input.GetKey(KeyCode.LeftControl))
            upMove += Vector3.down * _moveSpeed * Time.deltaTime;

        player.Move(camTransformed + upMove);
    }
}
