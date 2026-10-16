using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("Speed of the bullet when initialized")]
    public float speed = 120f;
    [Tooltip("Time in seconds before bullet is automatically destroyed")]
    public float lifeTime = 5f;
    [Tooltip("Damage dealt to enemies on impact")]
    public float damage = 20f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false; // Disable gravity completely so bullet flies perfectly straight
            rb.linearDamping = 0f;  // Prevent loss of momentum/speed in flight
            rb.angularDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    void Start()
    {
        // Ensure bullet flies straight forward if velocity wasn't already assigned by the shooter script
        if (rb != null && rb.linearVelocity.sqrMagnitude < 0.1f)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        // Ignore physical collision between bullet and player
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider != null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                Collider[] playerColliders = pc.GetComponentsInChildren<Collider>();
                foreach (Collider pCol in playerColliders)
                {
                    if (pCol != null) Physics.IgnoreCollision(bulletCollider, pCol);
                }
            }
        }

        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Color color)
    {
        // Coloring feature removed
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = transform.forward * speed;
        }
    }

    public void SetColor(Color color)
    {
        // Coloring feature removed
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Bullet"))
        {
            return;
        }

        GameObject hitObject = collision.gameObject;

        // Damage enemies if present (check self, parent, and children)
        EnemyHealth enemy = hitObject.GetComponentInParent<EnemyHealth>();
        if (enemy == null)
        {
            enemy = hitObject.GetComponentInChildren<EnemyHealth>();
        }

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Bullet")) return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null)
        {
            enemy = other.GetComponentInChildren<EnemyHealth>();
        }

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}