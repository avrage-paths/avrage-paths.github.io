using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Strategies {
    PushPlayer,
    FadeToBlack,
    None,
};

/// <summary> Places the selected Collision Resolution strategy onto the VR Player's HeadCollider. </summary>
/// <remarks> The HeadCollider object must be present on the VR Player. </remarks>
public class CollisionResolutionManager : MonoBehaviour
{
    public Strategies strategy;

    void Start()
    {
        GameObject HeadCollider = GameObject.Find("HeadCollider");

        switch (strategy)
        {
            case Strategies.PushPlayer:
                HeadCollider.AddComponent<PushPlayer>();
                break; 

            case Strategies.FadeToBlack:
                HeadCollider.AddComponent<FadeToBlack>();
                break;
            
            default:
                break;
        }
    }
}
