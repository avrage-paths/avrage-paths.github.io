using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/// <summary>
/// Fade the screen to black when the player's head collides with an object.
/// </summary>
public class FadeToBlack : MonoBehaviour
{
    public float transitionTime = 0.5f;
    private int collisionCount = 0;
    private Color clearColor = Color.clear;
    private Color fadeColor = Color.black;

    void OnCollisionEnter(Collision collision)
    {
        collisionCount++;

        if (collisionCount > 0)
            SteamVR_Fade.Start(fadeColor, transitionTime);
    }

    void OnCollisionExit(Collision collision)
    {
        collisionCount--;

        if (collisionCount <= 0)
            SteamVR_Fade.Start(clearColor, transitionTime);
    }
}
