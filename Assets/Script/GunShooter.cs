using UnityEngine;
using UnityEngine.UI;

public class GunShooter : MonoBehaviour
{
    [Header("Gun Setup")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float fireRate = 0.2f;

    [Header("Zoom Scope Setup")]
    public Camera playerCamera;
    public float normalFOV = 60f;
    public float zoomedFOV = 30f;
    public float zoomSpeed = 10f;

    [Header("UI References")]
    public Image bulletColorUI;
    public Image gunHUDImage; // 2D Sprite for the Gun in the HUD
    public Image scopeOverlayUI; // Optional Scope overlay graphic when zoomed

    [Header("Color Palette Options")]
    public Color[] availableColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta
    };

    private int currentColorIndex = 0;
    private float nextFireTime = 0f;
    private Color currentColor;
    private bool isZoomed = false;

    void Start()
    {
        // Auto-find main camera if unassigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }

        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
            if (normalFOV <= 0f) normalFOV = 60f;
        }

        // Hide scope overlay by default
        if (scopeOverlayUI != null)
        {
            scopeOverlayUI.gameObject.SetActive(false);
        }

        SetColor(0);
    }

    void Update()
    {
        // Key-based Color Selection (Keys 1 - 6)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetColor(0); // Red
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetColor(1); // Blue
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetColor(2); // Green
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetColor(3); // Yellow
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetColor(4); // Cyan
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetColor(5); // Magenta

        // Mouse Wheel Color Cycle
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) CycleColor(1);
        else if (scroll < 0f) CycleColor(-1);

        // Left Click: Shoot
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            if (LevelManager.Instance != null && !LevelManager.Instance.CanShoot())
            {
                return; // Cannot shoot if out of ammo or game inactive
            }

            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        // Right Click: Zoom Scope / ADS
        HandleZoom();
    }

    public void SetColor(int index)
    {
        if (availableColors == null || availableColors.Length == 0) return;

        currentColorIndex = Mathf.Clamp(index, 0, availableColors.Length - 1);
        currentColor = availableColors[currentColorIndex];

        UpdateColorUI();
    }

    public void CycleColor(int direction)
    {
        if (availableColors == null || availableColors.Length == 0) return;

        currentColorIndex = (currentColorIndex + direction + availableColors.Length) % availableColors.Length;
        currentColor = availableColors[currentColorIndex];

        UpdateColorUI();
    }

    void UpdateColorUI()
    {
        if (bulletColorUI != null)
        {
            bulletColorUI.gameObject.SetActive(true);
            bulletColorUI.enabled = true;
            bulletColorUI.color = currentColor;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.UpdateAmmoColorPreview(currentColor);
        }
    }

    void HandleZoom()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }

        isZoomed = Input.GetMouseButton(1);

        if (playerCamera != null)
        {
            float targetFOV = isZoomed ? zoomedFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }

        if (scopeOverlayUI != null)
        {
            scopeOverlayUI.gameObject.SetActive(isZoomed);
        }
    }

    void Shoot()
    {
        if (firePoint == null || bulletPrefab == null)
        {
            Debug.LogWarning("GunShooter: FirePoint or BulletPrefab is missing!");
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }

        // Raycast from camera center (crosshair) to find target point in 3D world (ignoring player body)
        Vector3 targetPoint = Vector3.zero;
        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundTarget = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.transform.IsChildOf(transform.root))
                {
                    targetPoint = hit.point;
                    foundTarget = true;
                    break;
                }
            }

            if (!foundTarget)
            {
                targetPoint = ray.GetPoint(500f);
            }
        }
        else
        {
            targetPoint = firePoint.position + firePoint.forward * 100f;
        }

        // Calculate direction vector towards the crosshair target point
        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);

        // Instantiate bullet facing exact target direction
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.Initialize(currentColor);
        }

        // Use ammo in LevelManager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.UseAmmo();
        }

        // Optional Gun HUD Recoil Punch
        if (gunHUDImage != null)
        {
            StopAllCoroutines();
            StartCoroutine(GunRecoilEffect());
        }
    }

    System.Collections.IEnumerator GunRecoilEffect()
    {
        Vector3 origScale = Vector3.one;
        gunHUDImage.transform.localScale = origScale * 1.15f;
        yield return new WaitForSeconds(0.08f);
        gunHUDImage.transform.localScale = origScale;
    }
}




