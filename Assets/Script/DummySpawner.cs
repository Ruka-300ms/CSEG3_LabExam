using UnityEngine;
using UnityEngine.UI;

public class DummySpawner : MonoBehaviour
{
    [Header("Enemy Template (with HP Bar attachment)")]
    [Tooltip("The Enemy GameObject that has the HP bar mechanic")]
    public GameObject enemyTemplate;

    [Header("Spawn Timing")]
    [Tooltip("Seconds between each dummy spawn")]
    public float spawnInterval = 5f;

    [Header("Spawn Area (world space)")]
    public Vector3 spawnAreaCenter = Vector3.zero;
    public Vector3 spawnAreaSize = new Vector3(20f, 0f, 20f);

    [Header("Dummy Health")]
    [Tooltip("Max HP for each spawned dummy")]
    public float dummyHealth = 100f;

    private float spawnTimer;

    void Start()
    {
        EnsureEnemyTemplate();
        DetectArenaCenter();

        // Spawn first dummy after 1 second, then every 5 seconds
        spawnTimer = Mathf.Max(0f, spawnInterval - 1f);
    }

    void Update()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && gm.CurrentState != GameState.Playing) return;
        if (Time.timeScale <= 0f) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnDummy();
        }
    }

    private void EnsureEnemyTemplate()
    {
        if (enemyTemplate == null)
        {
            // 1. Look inside the "Enemies" parent in the scene
            GameObject enemiesParent = GameObject.Find("Enemies");
            if (enemiesParent != null && enemiesParent.transform.childCount > 0)
            {
                for (int i = 0; i < enemiesParent.transform.childCount; i++)
                {
                    Transform child = enemiesParent.transform.GetChild(i);
                    if (child.GetComponent<EnemyHealth>() != null)
                    {
                        enemyTemplate = child.gameObject;
                        break;
                    }
                }
                if (enemyTemplate == null)
                {
                    enemyTemplate = enemiesParent.transform.GetChild(0).gameObject;
                }
            }

            // 2. Fallbacks by name
            if (enemyTemplate == null) enemyTemplate = GameObject.Find("Enemy (1)");
            if (enemyTemplate == null) enemyTemplate = GameObject.Find("Enemy (5)");

            // 3. Fallback by tag
            if (enemyTemplate == null)
            {
                GameObject[] tagged = GameObject.FindGameObjectsWithTag("Enemy");
                if (tagged.Length > 0) enemyTemplate = tagged[0];
            }
        }

        if (enemyTemplate == null)
        {
            Debug.LogError("DummySpawner: No Enemy GameObject found in the scene to use as the dummy template!");
            return;
        }

        // Deactivate scene enemies so they do not roam or shoot; we will spawn stationary dummy clones
        GameObject enemiesFolder = GameObject.Find("Enemies");
        if (enemiesFolder != null)
        {
            for (int i = 0; i < enemiesFolder.transform.childCount; i++)
            {
                enemiesFolder.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        else
        {
            enemyTemplate.SetActive(false);
        }
    }

    private void SpawnDummy()
    {
        if (enemyTemplate == null)
        {
            EnsureEnemyTemplate();
            if (enemyTemplate == null) return;
        }

        Vector3 pos = GetRandomGroundPosition();
        CreateDummy(pos);
    }

    private Vector3 GetRandomGroundPosition()
    {
        // Arena plane is 30x30 centered near (0, 0, 0).
        // Keep spawns comfortably inside [-11, 11]
        float x = Mathf.Clamp(spawnAreaCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f), -11f, 11f);
        float z = Mathf.Clamp(spawnAreaCenter.z + Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f), -11f, 11f);

        Vector3 rayOrigin = new Vector3(x, 20f, z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Capsule collider has height 2 and center 0, so placing at hit.point + 1.0f puts feet on floor
            return hit.point + Vector3.up * 1.0f;
        }

        return new Vector3(x, 1.0f, z);
    }

    private void CreateDummy(Vector3 position)
    {
        // Clone the enemy GameObject that has the HP bar attachment
        GameObject dummy = Instantiate(enemyTemplate, position, Quaternion.identity);
        dummy.name = "TrainingDummy";
        dummy.tag = "Enemy";

        // Disable AI so the dummy is completely stationary (no moving, no shooting)
        EnemyAI ai = dummy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.canShoot = false;
            ai.enabled = false;
        }

        // Configure health and ensure the attached HP bar slider is wired up
        EnemyHealth health = dummy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.maxHealth = dummyHealth;
            health.currentHealth = dummyHealth;

            if (health.healthBar == null)
            {
                health.healthBar = dummy.GetComponentInChildren<Slider>(true);
            }

            if (health.healthBar != null)
            {
                health.healthBar.maxValue = dummyHealth;
                health.healthBar.value = dummyHealth;
            }

            health.enabled = true;
        }

        // Keep stationary: kinematic Rigidbody prevents falling over or glitching into the floor
        Rigidbody rb = dummy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Ensure collider is active
        Collider col = dummy.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        dummy.SetActive(true);
    }

    private void DetectArenaCenter()
    {
        Transform playerTr = FindPlayerTransform();
        if (playerTr != null)
        {
            // Arena center is roughly (0, 0, 0); keep center close to map center
            spawnAreaCenter = new Vector3(0f, playerTr.position.y, 0f);
        }
        else
        {
            spawnAreaCenter = Vector3.zero;
        }
    }

    private Transform FindPlayerTransform()
    {
        CharacterController cc = FindObjectOfType<CharacterController>();
        if (cc != null) return cc.transform;

        PlayerShooter ps = FindObjectOfType<PlayerShooter>();
        if (ps != null) return ps.transform;

        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) return pc.transform;

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;

        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawCube(spawnAreaCenter + Vector3.up, new Vector3(spawnAreaSize.x, 2f, spawnAreaSize.z));
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireCube(spawnAreaCenter + Vector3.up, new Vector3(spawnAreaSize.x, 2f, spawnAreaSize.z));
    }
}
