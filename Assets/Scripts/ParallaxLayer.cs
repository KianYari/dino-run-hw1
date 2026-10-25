using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] float speedFactor = 1f;
    [SerializeField] float tileWidth = 20f;
    [SerializeField] float drift;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        Vector3 pos = transform.position;
        pos.x -= (gm.ScrollSpeed * speedFactor + drift) * Time.deltaTime;
        if (pos.x <= -tileWidth) pos.x += tileWidth;
        transform.position = pos;
    }
}
