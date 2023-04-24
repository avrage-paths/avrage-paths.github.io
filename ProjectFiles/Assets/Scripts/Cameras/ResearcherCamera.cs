using UnityEngine;

[ExecuteInEditMode]
public class ResearcherCamera : MonoBehaviour
{
    [SerializeField]
    private Shader replacementShader; // the shader you want to use with this camera
    [SerializeField]
    private GameObject userGameObject;

    private Vector3 targetPos = new Vector3();
    public float cameraMoveSpeed = 5;
    private float cameraY;

    /// <summary>
    /// Color that the research view sees when pieces overlap
    /// </summary>
    [Header("Researcher View")]
    public Color impossibleSpaceColor = Color.cyan;
    [Range(0, 50), SerializeField]
    private float fadeStart = 25;
    [Range(0, 100), SerializeField]
    private float fadeEnd = 40;
    [Range(0, 1), SerializeField]
    private float opacity = 1;

    void OnEnable()
    {
        Shader.SetGlobalFloat("_FadeStart", fadeStart);
        Shader.SetGlobalFloat("_FadeEnd", fadeEnd);
        Shader.SetGlobalFloat("_Opacity", opacity);
        Shader.SetGlobalColor("_ImpossibleSpaceColor", impossibleSpaceColor);
        Shader.SetGlobalFloat("_ImpossibleSpace", 0.0f);
        Shader.SetGlobalFloat("_ImpossiblePieces", 1.0f);

        if (replacementShader != null)
            GetComponent<Camera>().SetReplacementShader(replacementShader, "");
    }

    private void OnDisable()
    {
        GetComponent<Camera>().ResetReplacementShader();
    }

    public void SetupCamera(GameObject player)
    {
        if (userGameObject == null || !userGameObject.activeSelf)
        {
            userGameObject = player;
            if (userGameObject == null)
            {
                return;
            }
        }

        cameraY = transform.position.y;
    }

    private void LateUpdate()
    {
        if (userGameObject == null) return;

        // lerp camera to follow player
        targetPos = userGameObject.transform.position;
        targetPos.y = cameraY;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);
    }
}
