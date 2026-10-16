using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The Enemy Prefab to spawn")]
    public GameObject enemyPrefab;

    [Tooltip("How often to spawn an enemy in seconds")]
    public float spawnInterval = 5f;

    [Header("Spawn Area")]
    public Vector3 spawnAreaCenter = Vector3.zero;
    public Vector2 spawnAreaSize = new Vector2(20f, 20f);

    private float timer = 0f;

    void Update()
    {
        if (enemyPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        float x = spawnAreaCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float z = spawnAreaCenter.z + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        
        Vector3 rayOrigin = new Vector3(x, 20f, z);
        Vector3 spawnPos = new Vector3(x, 1f, z);
        
        // Find the ground to spawn on
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point + Vector3.up * 1f; // 1 unit up so they don't clip into the floor
        }

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        newEnemy.SetActive(true);
    }
}
