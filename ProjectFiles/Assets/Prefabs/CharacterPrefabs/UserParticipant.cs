using UnityEngine;

public class UserParticipant : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Camera researchCam = null; 
        foreach(Camera camera in Camera.allCameras)
        {
            if (camera.tag == "playbackCamera")
                researchCam = camera;
        }

        if (researchCam != null)
            researchCam.GetComponent<ResearcherCamera>().SetupCamera(gameObject);
        else
            Debug.LogError("No research camera with the tag playbackCamera was found");
    }
}
