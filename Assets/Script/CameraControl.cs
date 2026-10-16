using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Target & Body Alignment")]
    [Tooltip("Drag the Player GameObject transform here")]
    public Transform playerBody;
    [Tooltip("Height offset for the camera pivot point (e.g., 1.4 for neck/head level)")]
    public float pivotHeight = 1.4f;

    [Header("3rd Person Camera Position (Modifiable in Inspector)")]
    [Tooltip("Distance behind the player model (Lower value = closer camera)")]
    public float distanceBack = 1.8f;
    [Tooltip("Height of camera relative to pivot point")]
    public float heightOffset = 0.2f;
    [Tooltip("Shoulder offset: Positive for right shoulder, Negative for left, 0 for center")]
    public float shoulderOffset = 0.35f;
    [Tooltip("Combined Vector3 offset (Auto-calculated from distance, height, and shoulder offset)")]
    public Vector3 thirdPersonOffset = new Vector3(0.35f, 0.2f, -1.8f);

    [Header("1st Person / Aiming Position")]
    [Tooltip("Camera offset relative to player head when aiming down sights (RMB)")]
    public Vector3 firstPersonOffset = new Vector3(0f, 0.1f, 0.3f);

    [Header("Camera Angles & Mouse Sensitivity")]
    [Tooltip("Mouse look sensitivity")]
    public float mouseSensitivity = 200f;
    [Tooltip("Initial vertical tilt angle when game starts")]
    public float initialPitchAngle = 10f;
    [Tooltip("Minimum vertical angle look down limit")]
    [Range(-89f, 0f)] public float minPitch = -60f;
    [Tooltip("Maximum vertical angle look up limit")]
    [Range(0f, 89f)] public float maxPitch = 70f;
    [Tooltip("Invert vertical mouse movement")]
    public bool invertY = false;

    [Header("Field of View (FOV)")]
    [Tooltip("Standard Field of View angle")]
    public float normalFOV = 60f;
    [Tooltip("Field of View angle when aiming down sights")]
    public float zoomedFOV = 35f;
    [Tooltip("Speed of FOV zooming transition")]
    public float fovTransitionSpeed = 12f;

    [Header("Camera Motion & Smoothing")]
    [Tooltip("Speed of camera position following smoothing")]
    public float positionSmoothSpeed = 15f;
    [Tooltip("Speed of camera rotation smoothing")]
    public float rotationSmoothSpeed = 20f;

    [Header("Wall Collision Settings")]
    [Tooltip("Enable camera pulling closer when backed against walls")]
    public bool enableCameraCollision = true;
    [Tooltip("Layers the camera collides with to prevent clipping through walls")]
    public LayerMask obstacleLayers = ~0;
    [Tooltip("Sphere cast radius for wall detection")]
    public float cameraCollisionRadius = 0.2f;

    [Header("UI Crosshair")]
    [Tooltip("Crosshair UI GameObject reference")]
    public GameObject crosshairUI;
    [Tooltip("If true, crosshair only appears while aiming down sights")]
    public bool showCrosshairOnlyWhenAiming = false;

    private float pitch = 0f;
    private float yaw = 0f;
    private Camera cam;
    private bool isAiming = false;

    void OnValidate()
    {
        // Keep thirdPersonOffset in sync when tweaking individual sliders in Inspector
        thirdPersonOffset = new Vector3(shoulderOffset, heightOffset, -distanceBack);
        if (minPitch > maxPitch) minPitch = maxPitch - 1f;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Unparent camera so model motion doesn't fling the camera
        transform.SetParent(null);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        pitch = initialPitchAngle;

        if (playerBody != null)
        {
            yaw = playerBody.eulerAngles.y;
        }

        // Auto-find crosshair UI by name if unassigned
        if (crosshairUI == null)
        {
            GameObject found = GameObject.Find("Crosshair");
            if (found != null) crosshairUI = found;
        }

        UpdateCrosshairVisibility(false);
    }

    void LateUpdate()
    {
        if (playerBody == null) return;

        // Mouse input processing
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Rotate player body horizontally with camera yaw (rotate root object if playerBody is a child)
        Transform rootTransform = (playerBody != null && playerBody.root != null) ? playerBody.root : playerBody;
        rootTransform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Check if right mouse button is held for aiming
        isAiming = Input.GetMouseButton(1);

        // Active offset & target FOV depending on aiming state
        thirdPersonOffset = new Vector3(shoulderOffset, heightOffset, -distanceBack);
        Vector3 desiredLocalOffset = isAiming ? firstPersonOffset : thirdPersonOffset;
        float targetFOV = isAiming ? zoomedFOV : normalFOV;

        // Smoothly adjust Field of View
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
        }

        // Calculate camera rotation and pivot point
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivotPosition = playerBody.position + Vector3.up * pivotHeight;

        // Compute target camera position in world space
        Vector3 targetCameraPosition = pivotPosition + cameraRotation * desiredLocalOffset;

        // Wall collision check in 3rd person mode to prevent clipping through terrain/walls
        if (!isAiming && enableCameraCollision)
        {
            Vector3 rayDirection = targetCameraPosition - pivotPosition;
            float rayDistance = rayDirection.magnitude;

            if (rayDistance > 0.05f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(pivotPosition, cameraCollisionRadius, rayDirection.normalized, rayDistance, obstacleLayers, QueryTriggerInteraction.Ignore);
                float minHitDistance = rayDistance;

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || hit.collider.isTrigger || hit.collider.CompareTag("Bullet"))
                        continue;

                    // Ignore any colliders that belong to the player (root object, child mesh, or armature)
                    bool isPlayerCollider = hit.collider.CompareTag("Player") ||
                                            hit.collider.transform.root == playerBody.root ||
                                            hit.collider.transform == playerBody ||
                                            hit.collider.transform == rootTransform ||
                                            hit.collider.transform.IsChildOf(playerBody) ||
                                            hit.collider.transform.IsChildOf(rootTransform) ||
                                            playerBody.IsChildOf(hit.collider.transform);

                    if (!isPlayerCollider)
                    {
                        if (hit.distance < minHitDistance)
                        {
                            minHitDistance = hit.distance;
                        }
                    }
                }

                if (minHitDistance < rayDistance)
                {
                    targetCameraPosition = pivotPosition + rayDirection.normalized * Mathf.Max(0.3f, minHitDistance - cameraCollisionRadius);
                }
            }
        }

        // Apply smooth position and rotation tracking
        transform.position = Vector3.Lerp(transform.position, targetCameraPosition, Time.deltaTime * positionSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, Time.deltaTime * rotationSmoothSpeed);

        // Manage Crosshair UI
        UpdateCrosshairVisibility(isAiming);
    }

    void UpdateCrosshairVisibility(bool aiming)
    {
        if (crosshairUI == null) return;

        if (showCrosshairOnlyWhenAiming)
        {
            crosshairUI.SetActive(aiming);
        }
        else
        {
            crosshairUI.SetActive(true);
        }
    }
}

