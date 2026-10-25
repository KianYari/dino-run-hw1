using UnityEngine;

public class GroundObstacle : Obstacle
{
    [SerializeField] Sprite[] variants;

    void Awake()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (variants != null && variants.Length > 0)
            sr.sprite = variants[Random.Range(0, variants.Length)];
        FitCollider(sr.sprite);
    }
}
