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

    private Vector3 lastCameraPosition;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            enabled = false;
            return;
        }

        lastCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        float moveX = enableX ? cameraDelta.x * parallaxFactorX : 0f;
        float moveY = enableY ? cameraDelta.y * parallaxFactorY : 0f;

        transform.position += new Vector3(moveX, moveY, 0f);

        lastCameraPosition = cameraTransform.position;
    }
}