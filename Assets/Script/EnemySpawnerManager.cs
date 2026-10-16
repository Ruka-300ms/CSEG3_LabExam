using UnityEngine;

/// <summary>
/// Autonomous spawner and manager for the 3 AI Enemy Types (Vision Cone, Proximity, Line of Sight).
/// Completely separated from GameManager.
/// Can spawn the 3 cube enemy types procedurally or from prefabs at start or on intervals.
/// </summary>
public class EnemySpawnerManager : MonoBehaviour
{
    [Header("Enemy Prefab References (Optional)")]
    [Tooltip("Prefab for Vision Cone enemy (roams, chases when in sight, melee only)")]
    public GameObject visionConePrefab;

    [Tooltip("Prefab for Proximity enemy (stationary, shoots bullets in proximity)")]
    public GameObject proximityPrefab;

    [Tooltip("Prefab for Line of Sight enemy (chases when in sight, melee only)")]
    public GameObject lineOfSightPrefab;

    [Header("Spawn Counts At Start")]
    public int initialVisionConeEnemies = 2;
    public int initialProximityEnemies = 2;
    public int initialLineOfSightEnemies = 2;

    [Header("Spawn Area")]
    public Vector3 spawnCenter = Vector3.zero;
    public Vector2 spawnAreaSize = new Vector2(25f, 25f);
    public float minDistanceFromPlayer = 8f;

    [Header("Materials (Optional fallback for procedural creation)")]
    public Material visionConeMaterial;
    public Material proximityMaterial;
    public Material lineOfSightMaterial;

    [Header("Old Enemy Replacement")]
    [Tooltip("If true, disables legacy EnemyAI enemies so only the 3 new AI cube enemies are active.")]
    public bool replaceOldEnemies = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSpawnerInScene()
    {
        // Don't spawn inside MainMenu
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu") return;

        if (FindObjectOfType<EnemySpawnerManager>() == null)
        {
            GameObject spawnerObj = new GameObject("EnemySpawnerManager");
            spawnerObj.AddComponent<EnemySpawnerManager>();
        }
    }

    void Awake()
    {
        // Try to load prefabs from Resources or Assets if unassigned
        LoadPrefabsIfNeeded();
    }

    void Start()
    {
        // Disable old legacy enemies if configured
        if (replaceOldEnemies)
        {
            EnemyAI[] legacyEnemies = FindObjectsOfType<EnemyAI>(true);
            foreach (EnemyAI old in legacyEnemies)
            {
                if (old != null &&
                    old.GetComponent<VisionConeEnemy>() == null &&
                    old.GetComponent<ProximityEnemy>() == null &&
                    old.GetComponent<LineOfSightEnemy>() == null)
                {
                    old.gameObject.SetActive(false);
                }
            }
        }

        DetectArenaFloor();

        // Check if scene already has these enemies
        int existingVision = FindObjectsOfType<VisionConeEnemy>().Length;
        int existingProx = FindObjectsOfType<ProximityEnemy>().Length;
        int existingLos = FindObjectsOfType<LineOfSightEnemy>().Length;

        // If none of a type exists, spawn the initial count
        int spawnVision = Mathf.Max(0, initialVisionConeEnemies - existingVision);
        int spawnProx = Mathf.Max(0, initialProximityEnemies - existingProx);
        int spawnLos = Mathf.Max(0, initialLineOfSightEnemies - existingLos);

        for (int i = 0; i < spawnVision; i++)
        {
            SpawnEnemy(EnemyType.VisionCone);
        }

        for (int i = 0; i < spawnProx; i++)
        {
            SpawnEnemy(EnemyType.Proximity);
        }

        for (int i = 0; i < spawnLos; i++)
        {
            SpawnEnemy(EnemyType.LineOfSight);
        }
    }

    public enum EnemyType
    {
        VisionCone,
        Proximity,
        LineOfSight
    }

    public GameObject SpawnEnemy(EnemyType type)
    {
        Vector3 spawnPos = GetRandomGroundPosition();
        return SpawnEnemy(type, spawnPos);
    }

    public GameObject SpawnEnemy(EnemyType type, Vector3 position)
    {
        GameObject enemyObj = null;

        switch (type)
        {
            case EnemyType.VisionCone:
                if (visionConePrefab != null)
                {
                    enemyObj = Instantiate(visionConePrefab, position, Quaternion.identity, transform);
                }
                else
                {
                    enemyObj = CreateProceduralVisionConeEnemy(position);
                }
                enemyObj.name = "VisionConeEnemy_Cube";
                break;

            case EnemyType.Proximity:
                if (proximityPrefab != null)
                {
                    enemyObj = Instantiate(proximityPrefab, position, Quaternion.identity, transform);
                }
                else
                {
                    enemyObj = CreateProceduralProximityEnemy(position);
                }
                enemyObj.name = "ProximityEnemy_Cube";
                break;

            case EnemyType.LineOfSight:
                if (lineOfSightPrefab != null)
                {
                    enemyObj = Instantiate(lineOfSightPrefab, position, Quaternion.identity, transform);
                }
                else
                {
                    enemyObj = CreateProceduralLineOfSightEnemy(position);
                }
                enemyObj.name = "LineOfSightEnemy_Cube";
                break;
        }

        if (enemyObj != null)
        {
            enemyObj.tag = "Enemy";
            enemyObj.SetActive(true);
        }

        return enemyObj;
    }

    // ──────────────── Procedural Creation Fallbacks ────────────────

    private GameObject CreateProceduralVisionConeEnemy(Vector3 position)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        cube.transform.SetParent(transform);

        // Amber/Orange color
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(1f, 0.55f, 0f);
        }

        // Visor to indicate forward facing direction
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor_LookIndicator";
        visor.transform.SetParent(cube.transform, false);
        visor.transform.localPosition = new Vector3(0f, 0.2f, 0.52f);
        visor.transform.localScale = new Vector3(0.7f, 0.25f, 0.1f);
        if (visor.GetComponent<Renderer>() != null) visor.GetComponent<Renderer>().material.color = Color.cyan;
        Destroy(visor.GetComponent<Collider>());

        // Physics & AI
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        cube.AddComponent<EnemyHealth>();
        VisionConeEnemy ai = cube.AddComponent<VisionConeEnemy>();
        ai.visionAngle = 75f;
        ai.visionDistance = 18f;
        ai.roamSpeed = 2.5f;
        ai.chaseSpeed = 4.5f;
        ai.meleeRange = 1.8f;
        ai.meleeDamage = 15f;
        ai.attackCooldown = 1.0f;

        return cube;
    }

    private GameObject CreateProceduralProximityEnemy(Vector3 position)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        cube.transform.SetParent(transform);

        // Purple color
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.6f, 0.15f, 0.95f);
        }

        // Turret cannon nozzle
        GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nozzle.name = "TurretNozzle";
        nozzle.transform.SetParent(cube.transform, false);
        nozzle.transform.localPosition = new Vector3(0f, 0.1f, 0.6f);
        nozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        nozzle.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
        if (nozzle.GetComponent<Renderer>() != null) nozzle.GetComponent<Renderer>().material.color = Color.black;
        Destroy(nozzle.GetComponent<Collider>());

        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(cube.transform, false);
        fp.transform.localPosition = new Vector3(0f, 0.1f, 0.85f);

        // Stationary physics & AI
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.freezeRotation = true;

        cube.AddComponent<EnemyHealth>();
        ProximityEnemy ai = cube.AddComponent<ProximityEnemy>();
        ai.proximityRange = 16f;
        ai.fireRate = 1.5f;
        ai.bulletDamage = 15f;
        ai.bulletSpeed = 22f;
        ai.firePoint = fp.transform;

        return cube;
    }

    private GameObject CreateProceduralLineOfSightEnemy(Vector3 position)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        cube.transform.SetParent(transform);

        // Crimson color
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.9f, 0.12f, 0.15f);
        }

        // All-seeing eye accent
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "AllSeeingEye";
        eye.transform.SetParent(cube.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.2f, 0.5f);
        eye.transform.localScale = new Vector3(0.45f, 0.45f, 0.2f);
        if (eye.GetComponent<Renderer>() != null) eye.GetComponent<Renderer>().material.color = Color.yellow;
        Destroy(eye.GetComponent<Collider>());

        // Physics & AI
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        cube.AddComponent<EnemyHealth>();
        LineOfSightEnemy ai = cube.AddComponent<LineOfSightEnemy>();
        ai.sightRange = 25f;
        ai.chaseSpeed = 4.5f;
        ai.meleeRange = 1.8f;
        ai.meleeDamage = 15f;
        ai.attackCooldown = 1.0f;

        return cube;
    }

    private void DetectArenaFloor()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            spawnCenter = new Vector3(playerObj.transform.position.x, playerObj.transform.position.y, playerObj.transform.position.z);
        }
    }

    private Vector3 GetRandomGroundPosition()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = playerObj != null ? playerObj.transform.position : spawnCenter;

        for (int i = 0; i < 30; i++)
        {
            float rx = Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f);
            float rz = Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f);
            Vector3 candidate = spawnCenter + new Vector3(rx, 0f, rz);

            // Ensure safe distance from player
            if (Vector3.Distance(new Vector3(candidate.x, 0, candidate.z), new Vector3(playerPos.x, 0, playerPos.z)) < minDistanceFromPlayer)
            {
                continue;
            }

            // Raycast down to find ground
            if (Physics.Raycast(candidate + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 0.6f;
            }
        }

        return spawnCenter + new Vector3(10f, 0.6f, 10f);
    }

    private void LoadPrefabsIfNeeded()
    {
#if UNITY_EDITOR
        if (visionConePrefab == null)
            visionConePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/VisionConeEnemy.prefab");
        if (proximityPrefab == null)
            proximityPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ProximityEnemy.prefab");
        if (lineOfSightPrefab == null)
            lineOfSightPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/LineOfSightEnemy.prefab");
#endif
    }
}
