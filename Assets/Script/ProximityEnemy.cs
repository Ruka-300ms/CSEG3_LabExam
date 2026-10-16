using UnityEngine;

/// <summary>
/// Enemy 2: Proximity Enemy.
/// Does not move (stationary cube).
/// Detects player when within proximity range, turns towards them,
/// and shoots bullets that damage the player.
/// Independent entity, separated from GameManager.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ProximityEnemy : EnemyBase
{
    [Header("Proximity Settings")]
    [Tooltip("Radius within which the enemy detects the player and starts shooting.")]
    public float proximityRange = 15f;

    [Tooltip("Rotation speed to track the player while in proximity.")]
    public float aimRotationSpeed = 6f;

    [Header("Shooting Settings")]
    [Tooltip("Interval between bullet shots in seconds.")]
    public float fireRate = 1.5f;

    [Tooltip("Damage dealt by each bullet to the player.")]
    public float bulletDamage = 15f;

    [Tooltip("Velocity of the fired bullet.")]
    public float bulletSpeed = 22f;

    [Tooltip("Optional bullet prefab. If null, a procedural bullet is instantiated.")]
    public GameObject bulletPrefab;

    [Tooltip("Point where bullets are spawned. If null, automatically calculated from front face.")]
    public Transform firePoint;

    [Tooltip("Check unobstructed line of sight before shooting so bullets don't fire into walls.")]
    public bool requireLineOfSightToShoot = true;

    // Runtime state
    [Header("Runtime Status")]
    public bool isPlayerInProximity = false;
    private float nextFireTime = 0f;

    protected override void Awake()
    {
        base.Awake();

        // Stationary entity: set kinematic so physics never displaces it
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        EnsurePlayerReference();

        if (player == null)
        {
            isPlayerInProximity = false;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerInProximity = (distance <= proximityRange);

        if (isPlayerInProximity)
        {
            // Rotate towards player horizontally
            RotateTowards(player.position, aimRotationSpeed);

            // Shooting logic
            if (Time.time >= nextFireTime)
            {
                bool canShoot = true;
                if (requireLineOfSightToShoot)
                {
                    Vector3 playerCenter = player.position + Vector3.up * 1.0f;
                    canShoot = HasLineOfSight(playerCenter, proximityRange);
                }

                if (canShoot)
                {
                    ShootBullet();
                    nextFireTime = Time.time + fireRate;
                }
            }
        }
    }

    /// <summary>
    /// Spawns and fires a bullet aimed at the player's center.
    /// </summary>
    private void ShootBullet()
    {
        if (player == null) return;

        Vector3 spawnPos = firePoint != null 
            ? firePoint.position 
            : transform.position + Vector3.up * eyeHeight + transform.forward * 0.9f;

        Vector3 targetPos = player.position + Vector3.up * 1.0f;
        Vector3 shootDir = (targetPos - spawnPos).normalized;

        GameObject bullet;
        if (bulletPrefab != null)
        {
            bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(shootDir));
        }
        else
        {
            // Procedural projectile
            bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "EnemyProximityBullet";
            bullet.transform.position = spawnPos;
            bullet.transform.localScale = Vector3.one * 0.45f;

            Renderer bRenderer = bullet.GetComponent<Renderer>();
            if (bRenderer != null)
            {
                bRenderer.material.color = new Color(0.8f, 0.1f, 0.9f); // Magenta/purple glow
            }
        }

        // Configure EnemyBullet component
        EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
        if (eb == null) eb = bullet.AddComponent<EnemyBullet>();
        eb.damage = bulletDamage;

        // Ensure collider exists
        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bulletCol == null) bulletCol = bullet.AddComponent<SphereCollider>();

        // Ignore collision between bullet and this enemy's own collider
        if (boxCollider != null && bulletCol != null)
        {
            Physics.IgnoreCollision(bulletCol, boxCollider, true);
        }

        // Configure Rigidbody on bullet
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb == null) bulletRb = bullet.AddComponent<Rigidbody>();

        bulletRb.useGravity = false;
        bulletRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        bulletRb.linearVelocity = shootDir * bulletSpeed;

        // Visual flash feedback on shoot
        StartCoroutine(FlashAttackFeedback());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isPlayerInProximity 
            ? new Color(0.8f, 0f, 1f, 0.6f) 
            : new Color(0.5f, 0f, 0.8f, 0.25f);

        Gizmos.DrawWireSphere(transform.position, proximityRange);

        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(firePoint.position, 0.15f);
            Gizmos.DrawRay(firePoint.position, firePoint.forward * 2f);
        }
    }
}
