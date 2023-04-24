using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Player motion directed by teleportation.
/// SteamVR already handles the logic for teleportation, so as long as we activate the approriate SteamVR action set and prefab for it it will be functional.
/// </summary>

public class TeleportMotion : MonoBehaviour
{
    public GameObject Teleporting;

    void OnEnable()
    {
        SteamVR_Actions._default.Activate(priority: 0, disableAllOtherActionSets: true);
        Teleporting.SetActive(true);
    }
    void OnDisable()
    {
        Teleporting.SetActive(false);
    }

}
