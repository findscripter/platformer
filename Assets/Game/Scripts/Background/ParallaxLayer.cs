using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax")]
    [SerializeField] private float parallaxFactorX = 0.2f;
    [SerializeField] private float parallaxFactorY = 0.1f;

    [Header("Axis")]
    [SerializeField] private bool enableX = true;
    [SerializeField] private bool enableY = false;

    [Header("Startup")]
    [Tooltip("启动时把本层 X 对齐到相机 X（用于需要始终盖满画面的全屏背景层，如 sky）")]
    [SerializeField] private bool snapToCameraOnStart = false;

    private Vector3 lastCameraPosition;
    // Cinemachine 开播时会把相机从场景摆放位置瞬移到跟随目标，
    // 这次瞬移与本脚本的 LateUpdate 帧内先后顺序不定，
    // 预热两帧再开始累计位移，保证瞬移不会被当作相机移动拖走视差层
    private int warmupFrames = 2;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (warmupFrames > 0)
        {
            warmupFrames--;
            lastCameraPosition = cameraTransform.position;
            if (warmupFrames == 0 && snapToCameraOnStart)
            {
                Vector3 p = transform.position;
                transform.position = new Vector3(cameraTransform.position.x, p.y, p.z);
            }
            return;
        }

        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        float moveX = enableX ? cameraDelta.x * parallaxFactorX : 0f;
        float moveY = enableY ? cameraDelta.y * parallaxFactorY : 0f;

        transform.position += new Vector3(moveX, moveY, 0f);

        lastCameraPosition = cameraTransform.position;
    }
}
