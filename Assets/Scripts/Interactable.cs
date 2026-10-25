using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] float despawnX = -12f;
    [SerializeField] float bobAmplitude;
    [SerializeField] float bobSpeed = 3f;

    float baseY;
    float bobPhase;

    protected virtual void Start()
    {
        baseY = transform.position.y;
        bobPhase = Random.value * Mathf.PI * 2f;
    }

    protected virtual void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        Vector3 pos = transform.position;
        pos.x -= gm.ScrollSpeed * Time.deltaTime;
        if (bobAmplitude > 0f)
            pos.y = baseY + Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;
        transform.position = pos;

        if (pos.x < despawnX) Destroy(gameObject);
    }

    public abstract void OnPlayerContact(PlayerController player);
}
