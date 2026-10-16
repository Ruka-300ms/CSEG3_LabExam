using UnityEngine;

/// <summary>
/// Enemy 1: Vision Cone Enemy.
/// Roams around the field autonomously.
/// Only chases the player when the player is inside its front vision cone.
/// Can only damage the player in melee range.
/// Independent entity, separated from GameManager.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class VisionConeEnemy : EnemyBase
{
    public enum State
    {
        Roaming,
        Chasing
    }

    [Header("Vision Cone Settings")]
    [Tooltip("Total field of view angle in degrees for the vision cone.")]
    [Range(20f, 160f)]
    public float visionAngle = 75f;

    [Tooltip("Maximum sight distance of the vision cone.")]
    public float visionDistance = 18f;

    [Header("Movement Speeds")]
    [Tooltip("Movement speed while roaming the field.")]
    public float roamSpeed = 2.5f;

    [Tooltip("Movement speed while actively chasing the player.")]
    public float chaseSpeed = 4.5f;

    [Tooltip("Turn rotation speed.")]
    public float turnSpeed = 6f;

    [Header("Roaming Settings")]
    [Tooltip("Radius around origin where waypoints are randomly generated.")]
    public float roamRadius = 16f;

    [Tooltip("Seconds to wait at a waypoint before selecting the next one.")]
    public float waypointWaitDuration = 1.5f;

    [Tooltip("Max seconds allowed to reach a waypoint before picking another (prevents getting stuck).")]
    public float maxWaypointTravelTime = 6f;

    // Runtime state
    [Header("Runtime Status")]
    public State currentState = State.Roaming;
    public bool isPlayerInVision = false;

    private Vector3 currentWaypoint;
    private float waitTimer = 0f;
    private float travelTimer = 0f;
    private bool isWaitingAtWaypoint = false;

    protected override void Start()
    {
        base.Start();
        PickNewWaypoint();
    }

    void Update()
    {
        EnsurePlayerReference();
        CheckVisionCone();
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Roaming:
                HandleRoaming();
                break;

            case State.Chasing:
                HandleChasing();
                break;
        }
    }

    /// <summary>
    /// Checks if the player is currently inside the forward vision cone.
    /// Requires: Within distance + within FOV angle + unobstructed line of sight.
    /// </summary>
    private void CheckVisionCone()
    {
        if (player == null)
        {
            isPlayerInVision = false;
            currentState = State.Roaming;
            return;
        }

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerCenter = player.position + Vector3.up * 1.0f;
        Vector3 toPlayer = playerCenter - eyePos;

        // 1. Distance check
        float distance = toPlayer.magnitude;
        if (distance > visionDistance)
        {
            SetPlayerInVision(false);
            return;
        }

        // 2. Horizontal Angle check (vision cone)
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        float angleToPlayer = Vector3.Angle(flatForward, flatToPlayer);

        if (angleToPlayer > visionAngle * 0.5f)
        {
            SetPlayerInVision(false);
            return;
        }

        // 3. Unobstructed Line of Sight check (raycast)
        bool clearSight = HasLineOfSight(playerCenter, visionDistance);
        SetPlayerInVision(clearSight);
    }

    private void SetPlayerInVision(bool inVision)
    {
        isPlayerInVision = inVision;

        if (inVision)
        {
            // Only chases when in sight!
            currentState = State.Chasing;
        }
        else
        {
            // When player escapes sight, returns to roaming
            if (currentState == State.Chasing)
            {
                currentState = State.Roaming;
                PickNewWaypoint();
            }
        }
    }

    /// <summary>
    /// Roaming behavior: wanders between waypoints across the field.
    /// </summary>
    private void HandleRoaming()
    {
        if (isWaitingAtWaypoint)
        {
            StopHorizontalMovement();
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f)
            {
                isWaitingAtWaypoint = false;
                PickNewWaypoint();
            }
            return;
        }

        travelTimer += Time.fixedDeltaTime;

        // Vector to waypoint
        Vector3 toWaypoint = currentWaypoint - transform.position;
        toWaypoint.y = 0;
        float distance = toWaypoint.magnitude;

        // Reached waypoint or timeout
        if (distance <= 1.2f || travelTimer >= maxWaypointTravelTime)
        {
            isWaitingAtWaypoint = true;
            waitTimer = waypointWaitDuration;
            travelTimer = 0f;
            StopHorizontalMovement();
            return;
        }

        // Obstacle ahead check: avoid walking into walls
        if (Physics.Raycast(transform.position + Vector3.up * eyeHeight, transform.forward, 1.2f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            PickNewWaypoint();
            return;
        }

        // Move towards waypoint
        RotateTowards(currentWaypoint, turnSpeed);
        MoveForward(roamSpeed);
    }

    /// <summary>
    /// Chasing behavior: moves toward player and attacks in melee range only.
    /// </summary>
    private void HandleChasing()
    {
        if (player == null)
        {
            currentState = State.Roaming;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Turn towards player
        RotateTowards(player.position, turnSpeed * 1.5f);

        if (distanceToPlayer <= meleeRange)
        {
            // In melee range: stop moving and attack
            StopHorizontalMovement();
            TryMeleeAttack();
        }
        else
        {
            // Pursue player
            MoveForward(chaseSpeed);
        }
    }

    /// <summary>
    /// Picks a random destination on the field within roamRadius.
    /// </summary>
    private void PickNewWaypoint()
    {
        travelTimer = 0f;

        for (int i = 0; i < 15; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Ground check via raycast down
            if (Physics.Raycast(candidate + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                currentWaypoint = hit.point + Vector3.up * 0.5f;
                return;
            }
        }

        // Fallback
        currentWaypoint = spawnPosition + transform.forward * 4f;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        // Draw vision cone lines
        Gizmos.color = isPlayerInVision ? Color.red : new Color(1f, 0.6f, 0f, 0.8f);

        Vector3 leftBoundary = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawRay(eyePos, leftBoundary * visionDistance);
        Gizmos.DrawRay(eyePos, rightBoundary * visionDistance);
        Gizmos.DrawRay(eyePos, transform.forward * visionDistance);

        // Connect outer arc with segments
        int segments = 16;
        Vector3 prevPoint = eyePos + leftBoundary * visionDistance;
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -visionAngle * 0.5f + (visionAngle / segments) * i;
            Vector3 dir = Quaternion.Euler(0f, currentAngle, 0f) * transform.forward;
            Vector3 nextPoint = eyePos + dir * visionDistance;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // Draw melee range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Draw current waypoint
        if (currentState == State.Roaming)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
    }
}
