using UnityEngine;

public class AmmoSpawner : MonoBehaviour
{
    [Header("Spawn Timing")]
    [Tooltip("Seconds between ammo spawn attempts")]
    public float spawnInterval = 15f;

    [Header("Spawn Distance Near Player")]
    [Tooltip("Minimum distance from player to spawn ammo pickup")]
    public float minSpawnDistance = 3f;
    [Tooltip("Maximum distance from player to spawn ammo pickup")]
    public float maxSpawnDistance = 8f;

    [Header("Pickup Settings")]
    [Tooltip("Bullets added to reserve when picked up")]
    public int ammoPerPickup = 25;
    public Color pickupColor = new Color(1f, 0.85f, 0.2f); // Golden yellow

    private float spawnTimer;
    private GameObject currentPickup; // Only one allowed on the map

    void Start()
    {
        spawnTimer = 0f;
    }

    void Update()
    {
        if (!IsGameActive()) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            // Only spawn if no pickup is currently on the map
            if (currentPickup == null)
            {
                SpawnAmmoPickupNearPlayer();
            }
        }
    }

    private void SpawnAmmoPickupNearPlayer()
    {
        Transform playerTr = FindPlayerTransform();
        Vector3 spawnPos = Vector3.zero;

        if (playerTr != null)
        {
            Vector3 playerPos = playerTr.position;

            for (int attempt = 0; attempt < 25; attempt++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector3 candidate = playerPos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;

                // Raycast downwards from slightly above player's elevation
                Vector3 rayOrigin = new Vector3(candidate.x, playerPos.y + 5f, candidate.z);
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Ensure floor surface is near player's vertical level
                    if (Mathf.Abs(hit.point.y - playerPos.y) <= 2.5f)
                    {
                        Vector3 testPos = hit.point + Vector3.up * 0.4f;

                        // Ensure not inside obstacles
                        if (!Physics.CheckSphere(testPos, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            // Ensure line of sight from player to pickup location (not behind a wall)
                            if (!Physics.Linecast(playerPos + Vector3.up * 0.5f, testPos, ~0, QueryTriggerInteraction.Ignore))
                            {
                                spawnPos = testPos;
                                break;
                            }
                        }
                    }
                }
            }

            // Fallback: spawn on the ground in front of player
            if (spawnPos == Vector3.zero)
            {
                Vector3 forwardTarget = playerPos + playerTr.forward * 3f;
                Vector3 fallbackRay = new Vector3(forwardTarget.x, playerPos.y + 3f, forwardTarget.z);
                if (Physics.Raycast(fallbackRay, Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
                {
                    spawnPos = hit.point + Vector3.up * 0.4f;
                }
                else
                {
                    spawnPos = playerPos + playerTr.forward * 2.5f + Vector3.up * 0.3f;
                }
            }
        }
        else
        {
            // Fallback if player transform is not found
            spawnPos = new Vector3(0f, 0.4f, 0f);
        }

        CreatePickup(spawnPos);
    }

    private void CreatePickup(Vector3 position)
    {
        // Small golden cube
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickup.name = "AmmoPickup";
        pickup.transform.position = position;
        pickup.transform.localScale = Vector3.one * 0.6f;

        // Visual setup
        Renderer rend = pickup.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = pickupColor;
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", pickupColor);
            if (rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", pickupColor * 0.4f);
            }
        }

        // Attach collider as trigger
        Collider col = pickup.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Attach behaviour
        AmmoPickup ap = pickup.AddComponent<AmmoPickup>();
        ap.ammoAmount = ammoPerPickup;

        currentPickup = pickup;
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

    private bool IsGameActive()
    {
        if (Time.timeScale <= 0f) return false;

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && gm.isActiveAndEnabled)
        {
            return gm.CurrentState == GameState.Playing;
        }

        return true;
    }

    // ── Gizmos ──
    void OnDrawGizmosSelected()
    {
        Transform p = FindPlayerTransform();
        Vector3 center = p != null ? p.position : Vector3.zero;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(center, maxSpawnDistance);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(center, minSpawnDistance);
    }
}
