using UnityEngine;

/// <summary>
/// Enemy 3: Line of Sight (LoS) Enemy.
/// Detects player with 360-degree unobstructed line of sight.
/// Chases the player whenever they are in sight.
/// Can only damage the player in melee range.
/// Independent entity, separated from GameManager.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class LineOfSightEnemy : EnemyBase
{
    public enum State
    {
        IdleLooking,
        Chasing
    }

    [Header("Line of Sight Settings")]
    [Tooltip("Maximum sight range for detecting the player.")]
    public float sightRange = 25f;

    [Header("Movement Speeds")]
    [Tooltip("Speed when actively chasing the player.")]
    public float chaseSpeed = 4.5f;

    [Tooltip("Rotation speed to turn towards the player.")]
    public float turnSpeed = 7f;

    [Tooltip("Speed at which enemy turns while searching/idle.")]
    public float idleTurnSpeed = 25f;

    // Runtime state
    [Header("Runtime Status")]
    public State currentState = State.IdleLooking;
    public bool isPlayerInSight = false;

    private Vector3 lastSeenPosition;
    private bool hasLastSeenPosition = false;

    void Update()
    {
        EnsurePlayerReference();
        CheckLineOfSight();
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Chasing:
                HandleChasing();
                break;

            case State.IdleLooking:
                HandleIdle();
                break;
        }
    }

    /// <summary>
    /// Checks 360-degree unobstructed line of sight to the player within sightRange.
    /// </summary>
    private void CheckLineOfSight()
    {
        if (player == null)
        {
            isPlayerInSight = false;
            currentState = State.IdleLooking;
            return;
        }

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerCenter = player.position + Vector3.up * 1.0f;
        float distance = Vector3.Distance(eyePos, playerCenter);

        if (distance <= sightRange)
        {
            // Raycast check for unobstructed view
            bool clearSight = HasLineOfSight(playerCenter, sightRange);

            if (clearSight)
            {
                isPlayerInSight = true;
                lastSeenPosition = player.position;
                hasLastSeenPosition = true;
                currentState = State.Chasing;
                return;
            }
        }

        // Sight broken or out of range
        isPlayerInSight = false;
        if (currentState == State.Chasing)
        {
            currentState = State.IdleLooking;
        }
    }

    /// <summary>
    /// Chases the player while in line of sight, and attacks only in melee range.
    /// </summary>
    private void HandleChasing()
    {
        if (player == null)
        {
            currentState = State.IdleLooking;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Turn towards player
        RotateTowards(player.position, turnSpeed);

        if (distance <= meleeRange)
        {
            // In melee range: stop and execute melee attack
            StopHorizontalMovement();
            TryMeleeAttack();
        }
        else
        {
            // Chase towards player
            MoveForward(chaseSpeed);
        }
    }

    /// <summary>
    /// When player breaks line of sight, stop moving and look in the last known direction.
    /// </summary>
    private void HandleIdle()
    {
        StopHorizontalMovement();

        if (hasLastSeenPosition)
        {
            // Turn to face last known position
            RotateTowards(lastSeenPosition, turnSpeed * 0.5f);
        }
        else
        {
            // Gentle continuous scan rotation
            transform.Rotate(0f, idleTurnSpeed * Time.fixedDeltaTime, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        // Draw sight range sphere
        Gizmos.color = isPlayerInSight 
            ? new Color(1f, 0f, 0f, 0.4f) 
            : new Color(0.7f, 0.2f, 0.2f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Draw ray to player
        if (player != null)
        {
            Vector3 playerCenter = player.position + Vector3.up * 1.0f;
            Gizmos.color = isPlayerInSight ? Color.red : Color.gray;
            Gizmos.DrawLine(eyePos, playerCenter);
        }

        // Draw melee range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
