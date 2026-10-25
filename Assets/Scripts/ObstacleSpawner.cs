using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject groundObstaclePrefab;
    [SerializeField] GameObject airObstaclePrefab;
    [SerializeField] GameObject starPrefab;

    [Header("Placement")]
    [SerializeField] float spawnX = 11f;
    [SerializeField] float groundY = -3f;
    [SerializeField] float airHeight = 2f;
    [SerializeField] float lowStarHeight = 0.6f;
    [SerializeField] float highStarHeight = 2.7f;

    [Header("Timing")]
    [SerializeField] float minObstacleInterval = 1.0f;
    [SerializeField] float maxObstacleInterval = 2.0f;
    [SerializeField] float minStarInterval = 4f;
    [SerializeField] float maxStarInterval = 8f;
    [SerializeField, Range(0f, 1f)] float airChance = 0.3f;

    float obstacleTimer = 1.5f;
    float starTimer;

    void Start()
    {
        starTimer = Random.Range(minStarInterval, maxStarInterval);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        obstacleTimer -= Time.deltaTime;
        if (obstacleTimer <= 0f)
        {
            bool air = Random.value < airChance;
            Spawn(air ? airObstaclePrefab : groundObstaclePrefab, air ? groundY + airHeight : groundY);

            // gets a bit tighter as speed goes up
            float tighten = Mathf.Lerp(1f, 0.7f, gm.Difficulty01);
            obstacleTimer = Random.Range(minObstacleInterval, maxObstacleInterval) * tighten;
        }

        starTimer -= Time.deltaTime;
        if (starTimer <= 0f)
        {
            float height = Random.value < 0.5f ? lowStarHeight : highStarHeight;
            Spawn(starPrefab, groundY + height);
            starTimer = Random.Range(minStarInterval, maxStarInterval);
        }
    }

    void Spawn(GameObject prefab, float y)
    {
        Instantiate(prefab, new Vector3(spawnX, y, 0f), Quaternion.identity);
    }
}
