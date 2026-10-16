using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo")]
    [Tooltip("Bullets added to the player's reserve when picked up")]
    public int ammoAmount = 25;
    [Tooltip("Whether to consume the pickup if the player's reserve is already full")]
    public bool consumeWhenFull = false;

    [Header("Visual Animation")]
    public float rotationSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.25f;

    [Header("Pickup Detection")]
    [Tooltip("Distance at which the player automatically collects this ammo")]
    public float pickupRadius = 2.0f;

    private Vector3 startPosition;
    private Transform playerTransform;
    private bool isCollected = false;

    void Start()
    {
        startPosition = transform.position;

        // Ensure collider is configured as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = pickupRadius;
        }

        // Rigidbody is required for CharacterControllers to fire OnTriggerEnter
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // Cache player transform
        FindPlayer();
    }

    void Update()
    {
        if (isCollected) return;

        // Visual animation: slow spin
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Visual animation: gentle bob
        Vector3 pos = startPosition;
        pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = pos;

        // Proximity check fallback: ensures 100% reliable collection even if trigger events fail
        if (playerTransform == null)
        {
            FindPlayer();
        }

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= pickupRadius)
            {
                Collect();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // Check if the collider belongs to the player
        if (other.GetComponentInParent<PlayerShooter>() != null ||
            other.GetComponentInParent<PlayerController>() != null ||
            other.GetComponentInParent<CharacterController>() != null ||
            other.CompareTag("Player") ||
            (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
            other.name.ToLower().Contains("player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (isCollected) return;

        PlayerShooter shooter = FindObjectOfType<PlayerShooter>();
        if (shooter != null)
        {
            // Remove the capacity check and the Mathf.Min cap
            isCollected = true;
            shooter.totalAmmo += ammoAmount; 
        }
        else
        {
            isCollected = true;
        }

        if (TrainingLevelManager.Instance != null)
        {
            TrainingLevelManager.Instance.OnAmmoPickedUp();
        }

        Destroy(gameObject);
    }


    private void FindPlayer()
    {
        CharacterController cc = FindObjectOfType<CharacterController>();
        if (cc != null) { playerTransform = cc.transform; return; }

        PlayerShooter ps = FindObjectOfType<PlayerShooter>();
        if (ps != null) { playerTransform = ps.transform; return; }

        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) { playerTransform = pc.transform; return; }

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) { playerTransform = tagged.transform; return; }
    }
}
