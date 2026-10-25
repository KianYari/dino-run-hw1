using UnityEngine;

public class AirObstacle : Obstacle
{
    [SerializeField] Sprite[] flapFrames;
    [SerializeField] float flapInterval = 0.15f;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        FitCollider(sr.sprite);
    }

    protected override void Update()
    {
        base.Update();
        if (flapFrames == null || flapFrames.Length == 0) return;
        int frame = (int)(Time.time / flapInterval) % flapFrames.Length;
        sr.sprite = flapFrames[frame];
    }
}
