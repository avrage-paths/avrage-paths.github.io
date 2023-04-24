using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary> Provides an ability to activate a Control System or enable the debug mode. </summary>
public class ControlSystemManager : MonoBehaviour
{
    public Camera debugCamera;
    public GameObject debugPlayer;
    public GameObject KeyboardMotion;
    public GameObject ControlSystems;
    public GameObject [] controlSystems;

    /// <summary>
    /// Enables the given control system, disables all others.
    /// </summary>
    /// <param name="movementType"> The enum of type MovementTypes indicating whing control system to activate.</param>
    public void enableControlSystem(MovementTypes movementType)
    {
        // Put player at the start
        this.transform.position = new Vector3(0, 0.5f, 0);
        // Reset Collision Resolution Manager

        foreach (MovementTypes mType in System.Enum.GetValues(typeof(MovementTypes)))
            controlSystems[(int) mType].SetActive(mType == movementType);
    }

    void setDebugTo(bool state)
    {
        // Toggle overhead camera
        debugCamera.gameObject.SetActive(state);
        debugCamera.enabled = state;

        // Toggle debug player capsule
        debugPlayer.SetActive(state);

        // Toggle keyboard controls
        KeyboardMotion.SetActive(state);
    }
}

