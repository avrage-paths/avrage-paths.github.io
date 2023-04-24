using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class to track telemetry data for the player
/// </summary>
public class PlayerTelemetry : MonoBehaviour
{
    public Transform leftHand, rightHand, head;
    // Start is called before the first frame update
    void Start()
    {
        StartPieceScript.startPieceEntered += StartTracking;
    }

    void OnDestroy()
    {
        StartPieceScript.startPieceEntered -= StartTracking;
    }

    public void StartTracking(GameObject o)
    {
        if (TelemetryManager.instance != null)
            TelemetryManager.instance.AddPoller(GiveVRTransform, 1 / 10f);
        else
            Debug.LogError("Can't add poller because Telemetry manager is null!");
    }

    /// <summary>
    /// The transform of tracked VR objects, both in local and world space
    /// </summary>
    public class VRTransform : TelemetryManager.DataContainer
    {
        public float timestamp;
        #region Local Space
        public float localLeftHandPosX, localLeftHandPosY, localLeftHandPosZ;
        public float localRightHandPosX, localRightHandPosY, localRightHandPosZ;
        public float localHeadPosX, localHeadPosY, localHeadPosZ;

        public float localLeftHandRotX, localLeftHandRotY, localLeftHandRotZ, localLeftHandRotW;
        public float localRightHandRotX, localRightHandRotY, localRightHandRotZ, localRightHandRotW;
        public float localHeadRotX, localHeadRotY, localHeadRotZ, localHeadRotW;
        #endregion

        #region World Space
        public float worldLeftHandPosX, worldLeftHandPosY, worldLeftHandPosZ;
        public float worldRightHandPosX, worldRightHandPosY, worldRightHandPosZ;
        public float worldHeadPosX, worldHeadPosY, worldHeadPosZ;

        public float worldLeftHandRotX, worldLeftHandRotY, worldLeftHandRotZ, worldLeftHandRotW;
        public float worldRightHandRotX, worldRightHandRotY, worldRightHandRotZ, worldRightHandRotW;
        public float worldHeadRotX, worldHeadRotY, worldHeadRotZ, worldHeadRotW;

        #endregion
    }

    public VRTransform GiveVRTransform()
    {
        VRTransform data = new VRTransform();

        data.timestamp = MazeGenerator.instance.GetElapsedTime();

        data.localLeftHandPosX = leftHand.localPosition.x;
        data.localLeftHandPosY = leftHand.localPosition.y;
        data.localLeftHandPosZ = leftHand.localPosition.z;

        data.localRightHandPosX = rightHand.localPosition.x;
        data.localRightHandPosY = rightHand.localPosition.y;
        data.localRightHandPosZ = rightHand.localPosition.z;

        data.localHeadPosX = head.localPosition.x;
        data.localHeadPosY = head.localPosition.y;
        data.localHeadPosZ = head.localPosition.z;

        data.localLeftHandRotX = leftHand.localRotation.x;
        data.localLeftHandRotY = leftHand.localRotation.y;
        data.localLeftHandRotZ = leftHand.localRotation.z;
        data.localLeftHandRotW = leftHand.localRotation.w;

        data.localRightHandRotX = rightHand.localRotation.x;
        data.localRightHandRotY = rightHand.localRotation.y;
        data.localRightHandRotZ = rightHand.localRotation.z;
        data.localRightHandRotW = rightHand.localRotation.w;

        data.localHeadRotX = head.localRotation.x;
        data.localHeadRotY = head.localRotation.y;
        data.localHeadRotZ = head.localRotation.z;
        data.localHeadRotW = head.localRotation.w;

        data.worldLeftHandPosX = leftHand.position.x;
        data.worldLeftHandPosY = leftHand.position.y;
        data.worldLeftHandPosZ = leftHand.position.z;

        data.worldRightHandPosX = rightHand.position.x;
        data.worldRightHandPosY = rightHand.position.y;
        data.worldRightHandPosZ = rightHand.position.z;

        data.worldHeadPosX = head.position.x;
        data.worldHeadPosY = head.position.y;
        data.worldHeadPosZ = head.position.z;

        data.worldLeftHandRotX = leftHand.rotation.x;
        data.worldLeftHandRotY = leftHand.rotation.y;
        data.worldLeftHandRotZ = leftHand.rotation.z;
        data.worldLeftHandRotW = leftHand.rotation.w;

        data.worldRightHandRotX = rightHand.rotation.x;
        data.worldRightHandRotY = rightHand.rotation.y;
        data.worldRightHandRotZ = rightHand.rotation.z;
        data.worldRightHandRotW = rightHand.rotation.w;

        data.worldHeadRotX = head.rotation.x;
        data.worldHeadRotY = head.rotation.y;
        data.worldHeadRotZ = head.rotation.z;
        data.worldHeadRotW = head.rotation.w;

        return data;
    }
}
