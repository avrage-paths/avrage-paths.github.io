using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Player motion directed by a joystick.
/// </summary>
public class JoystickMotion : MonoBehaviour
{
    public CharacterController player;
    /// <summary>
    /// The GameObject whose orientation is the direction the player will travel in.
    /// </summary>
    public GameObject MovementDirectionObject;
    public SteamVR_ActionSet joystickActionSet;
    public SteamVR_Action_Vector2 joystickInput;
    /// <summary>
    /// Boolean value indicating if the player should be effected by gravity or not. 
    /// </summary>
    public bool joystick3D = false;

    public float moveSensitivity = 2.5f;
    private float maxSpeed = 30.0f;
    private float gravityStrength = 9.81f;

    void OnEnable()
    {
        // We overwrite the default action set, but activate it for hand posing
        SteamVR_Actions._default.Activate(priority: 0, disableAllOtherActionSets: true);
        joystickActionSet.Activate(priority: 1);
    }

    public void Update()
    {
        // Move in direction of the MovementDirectionObject
        Vector3 moveDirection = MovementDirectionObject.transform.TransformDirection(
            new Vector3(joystickInput.axis.x, 0, joystickInput.axis.y)
        );

        // Clamp the speed to maxSpeed
        float speed = moveDirection.magnitude * moveSensitivity;
        float moveSpeed = Mathf.Clamp(speed, -maxSpeed, maxSpeed);

        if (joystick3D)
        {
            player.Move(moveSpeed * moveDirection * Time.deltaTime);
            return;
        }

        // Projected moveDirection onto horizontal plane (to ensure player stays on ground)
        float originalMagnitude = moveDirection.magnitude;
        Vector3 flattenedDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);

        // Ensure the speed is consistent regardless of MovementDirectionObject's angle to horizon
        flattenedDirection = Vector3.Normalize(flattenedDirection) * originalMagnitude;

        // Movement due to joystick and gravity
        Vector3 movement = (moveSpeed * flattenedDirection) + (Vector3.down * gravityStrength);

        player.Move(movement * Time.deltaTime);
    }
}
