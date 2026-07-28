using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CameraTargetFollow : MonoBehaviour
{
    public Transform player;

    public float minX = 1f;
    public float minY = -5f;

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 pos = player.position;
        pos.y = Mathf.Max(pos.y, minY);
        pos.x = Mathf.Max(pos.x, minX);

        if ((transform.position - pos).sqrMagnitude > 0.000001f)
        {
            transform.position = pos;
        }
    }
}
