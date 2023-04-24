using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// DEPRECATED: Player motion directed by joystick and teleportation.
/// </summary>
/// <remarks> This script is not fully functional and contains some unintended bugs. Do not use, or use at your own risk. </remarks>
public class JoystickTeleportHybrid : MonoBehaviour
{
    public CharacterController player;
    public GameObject MovementDirectionObject;
    public SteamVR_ActionSet actionSet;
    public SteamVR_Action_Vector2 joystickInput;
    public SteamVR_Action_Boolean teleportInput;

    public float moveSensitivity = 3.5f;
    public float moveSpeed = 10.0f;
    public float maxSpeed = 30.0f;
    public float gravityStrength = 9.81f;
    private float teleportStart;

    public Teleport Teleporting;

    public void Start()
    {
        SteamVR_Actions._default.Activate(priority: 0, disableAllOtherActionSets: true);
        actionSet.Activate(priority: 0);
    }

    public void Update()
    {
        // Moving joystick forward
        if (joystickInput.axis.y > 0)
        {
            // Direction to move in is the direction the headset is facing
            Vector3 moveDirection = MovementDirectionObject.transform.TransformDirection(
                new Vector3(0, 0, joystickInput.axis.y)
            );

            // Clamp speed to [0, maxSpeed]
            moveSpeed = Mathf.Clamp(joystickInput.axis.y * moveSensitivity, 0, maxSpeed);

            // Projected moveDirection onto horizontal plane (to ensure player stays on ground)
            float originalMagnitude = moveDirection.magnitude;
            Vector3 flattenedDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);

            // Ensure move speed is consistent regardless of headset angle (to horizon)
            flattenedDirection = Vector3.Normalize(flattenedDirection) * originalMagnitude;

            player.Move((moveSpeed * flattenedDirection) * Time.deltaTime);
        }

        // SteamVR's teleport mechanism moves the world space rather than the player, so if you
        // move the CharacterController and teleport at the same time, the teleport does not produce any movement
        // from the player's perspective.
        // So, we pause gravity until the teleport is finished.

        // Teleport action initiated
        if (teleportInput.stateUp)
            teleportStart = Time.time;

        float elapsedSinceTeleport = Time.time - teleportStart;

        // Let the teleport action happen
        if (elapsedSinceTeleport <= Teleporting.teleportFadeTime + 0.02f)
            return;

        player.Move(Vector3.down * gravityStrength * Time.deltaTime);
    }
}
