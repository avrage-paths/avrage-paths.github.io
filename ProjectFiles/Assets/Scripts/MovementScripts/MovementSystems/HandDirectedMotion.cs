using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class HandDirectedMotion : MonoBehaviour
{
    public CharacterController player;
    public GameObject leftHand;
    public GameObject rightHand;

    public SteamVR_ActionSet handDirectedMotionActionSet;
    public SteamVR_Action_Vector2 leftJoystick;
    public SteamVR_Action_Vector2 rightJoystick;

    public float moveSensitivity = 2.5f;
    private float maxSpeed = 2.5f;
    private float gravityStrength = 9.81f;

    public void OnEnable()
    {
        SteamVR_Actions._default.Activate(priority: 0, disableAllOtherActionSets: true);
        handDirectedMotionActionSet.Activate(priority: 1);
    }
    public void Update()
    {
        Vector3 moveDirection = Vector3.zero;
        int contributingHands = 0;

        if (leftJoystick.axis.y > 0)
        {
            moveDirection += leftHand.transform.TransformDirection(
                new Vector3(0, 0, leftJoystick.axis.y)
            );
            contributingHands++;
        }

        if (rightJoystick.axis.y > 0)
        {
            moveDirection += rightHand.transform.TransformDirection(
                new Vector3(0, 0, rightJoystick.axis.y)
            );
            contributingHands++;
        }

        if (contributingHands != 0)
            moveDirection /= contributingHands;

        // Clamp the speed to maxSpeed
        float speed = moveDirection.magnitude * moveSensitivity;
        float moveSpeed = Mathf.Clamp(speed, 0, maxSpeed);

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
