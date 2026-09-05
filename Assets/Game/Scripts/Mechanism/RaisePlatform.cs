using UnityEngine;

public sealed class RaisePlatform : MonoBehaviour
{
    [SerializeField] private int zoneIndex = -1;
    [SerializeField] private Vector3 restPosition;
    [SerializeField] private Vector3 highPosition;
    [SerializeField] private float speed = 4f;

    public void Configure(int zone, Vector3 rest, Vector3 high)
    {
        zoneIndex = zone;
        restPosition = rest;
        highPosition = high;
    }

    private void Awake()
    {
        if (restPosition == Vector3.zero)
            restPosition = transform.position;
        if (highPosition == Vector3.zero)
            highPosition = restPosition + Vector3.up * 1.6f;
    }

    private void Update()
    {
        Vector3 target = TarotZoneQuery.HasEffect(zoneIndex, TarotEffectId.E15) ? highPosition : restPosition;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }
}
