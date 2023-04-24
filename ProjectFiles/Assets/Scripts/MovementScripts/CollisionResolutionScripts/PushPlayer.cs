using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Push the player away from the object their head is colliding with.
/// </summary>
public class PushPlayer : MonoBehaviour
{
    public float maxPushSize = 0.015f;
    public int pushIncrements = 10;
    private CharacterController player;
    private int collisionCount = 0;
    private Vector3 pushDirection = Vector3.zero;
    private Vector3 lerped = Vector3.zero;

    public void Start()
    {
        player = GameObject.Find("Player").GetComponent<CharacterController>();
        StartCoroutine(PushPlayerOverTime());
    }

    /// <summary>
    /// On collision, pushes the player in the direction from their head to their CharacterController,
    /// but doing so over time as to make the push smoother.
    /// </summary>
    private void Update()
    {
        // Don't adjust if no collisions
        if (collisionCount <= 0)
        {
            pushDirection = Vector3.zero;
            return;
        }

        // Vector from current location to character controller
        Vector3 headToCharacter = this.transform.position - player.transform.position;

        // Flattened and negated
        pushDirection = -1 * Vector3.ProjectOnPlane(headToCharacter, Vector3.up);

        // Clamp the distance so the movement is smooth
        float desiredMagnitude = Mathf.Min(maxPushSize, Mathf.Abs(pushDirection.magnitude));
        pushDirection = pushDirection.normalized * desiredMagnitude;
    }

    IEnumerator PushPlayerOverTime()
    {
        while (true)
        {
            lerped = pushDirection / pushIncrements;

            if (collisionCount <= 0)
                lerped = Vector3.zero;

            for (int i = 0; i < pushIncrements; i++)
            {
                player.Move(lerped);
                yield return new WaitForFixedUpdate();
                pushDirection -= lerped;
            }
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        collisionCount++;
    }
    public void OnCollisionExit(Collision collision)
    {
        collisionCount--;
    }
}