using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class FadingPlatform : MonoBehaviour
{
    [SerializeField] private int zoneIndex = -1;
    [SerializeField] private float armDelay = 0.45f;
    [SerializeField] private float goneDuration = 1.1f;
    [SerializeField] private float recoverDuration = 1.4f;
    [SerializeField] private float fastArmDelay = 0.18f;
    [SerializeField] private float fastGoneDuration = 0.55f;

    private Collider2D solid;
    private SpriteRenderer sprite;
    private float armedTime;
    private float stateTime;
    private int state;

    public void Configure(int zone)
    {
        zoneIndex = zone;
    }

    private void Awake()
    {
        solid = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (TarotZoneQuery.HasEffect(zoneIndex, TarotEffectId.E12))
        {
            SetSolid(true, 1f);
            armedTime = 0f;
            state = 0;
            return;
        }

        bool fast = TarotZoneQuery.HasEffect(zoneIndex, TarotEffectId.E18);
        float arm = fast ? fastArmDelay : armDelay;
        float gone = fast ? fastGoneDuration : goneDuration;

        if (state == 0)
        {
            if (armedTime > 0f)
            {
                armedTime += Time.deltaTime;
                if (sprite != null)
                    sprite.color = new Color(1f, 0.85f, 0.2f, 1f);
                if (armedTime >= arm)
                {
                    state = 1;
                    stateTime = 0f;
                    SetSolid(false, 0.15f);
                }
            }
            return;
        }

        stateTime += Time.deltaTime;
        if (state == 1 && stateTime >= gone)
        {
            state = 2;
            stateTime = 0f;
        }
        else if (state == 2 && stateTime >= recoverDuration)
        {
            state = 0;
            armedTime = 0f;
            SetSolid(true, 1f);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (state != 0)
            return;
        if (collision.collider.GetComponentInParent<PlayerController>() == null)
            return;
        if (armedTime <= 0f)
            armedTime = 0.0001f;
    }

    private void SetSolid(bool on, float alpha)
    {
        if (solid != null)
            solid.enabled = on;
        if (sprite != null)
        {
            Color color = sprite.color;
            color.a = alpha;
            if (on && alpha >= 1f)
                color = Color.white;
            sprite.color = color;
        }
    }
}
