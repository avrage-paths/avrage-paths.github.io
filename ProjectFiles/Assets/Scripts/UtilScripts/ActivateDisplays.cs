using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to control the various displays
/// </summary>
public class ActivateDisplays : MonoBehaviour
{
    /// <summary>
    /// The camera that is rendering to the headset
    /// </summary>
    public Camera vrCam;

    /// <summary>
    /// The camera that is rending to the desktop 
    /// </summary>
    public Camera desktopView;

    // Start is called before the first frame update
    void Start()
    {

        // Display.displays[0] is the primary, default display and is always ON, so start at index 1.
        // Check if additional displays are available and activate each.

        /* for (int i = 1; i < Display.displays.Length; i++)
         {
             Display.displays[i].Activate();
         }*/


        vrCam.depth = 0;
        desktopView.depth = 1;

        desktopView.SetTargetBuffers(Display.main.colorBuffer, Display.main.depthBuffer);

        vrCam.enabled = true;
        desktopView.enabled = true;
    }

}
