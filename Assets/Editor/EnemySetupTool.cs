using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class EnemySetupTool : EditorWindow
{
    private const string PREFAB_DIR = "Assets/Prefab";
    private const string MATERIAL_DIR = "Assets/Materials";

    [MenuItem("Tools/Enemies/1. Generate 3 Enemy Prefabs (Cubes)")]
    public static void GenerateEnemyPrefabs()
    {
        EnsureDirectories();

        Material visionConeMat = GetOrCreateMaterial("VisionConeEnemy_Mat", new Color(1f, 0.55f, 0f, 1f)); // Bright Amber / Orange
        Material proximityMat = GetOrCreateMaterial("ProximityEnemy_Mat", new Color(0.6f, 0.15f, 0.95f, 1f)); // Deep Violet / Purple
        Material losMat = GetOrCreateMaterial("LineOfSightEnemy_Mat", new Color(0.9f, 0.12f, 0.15f, 1f)); // Crimson / Blood Red
        Material visorMat = GetOrCreateMaterial("EnemyVisor_Mat", new Color(0f, 0.9f, 1f, 1f)); // Cyan Glow Visor
        Material nozzleMat = GetOrCreateMaterial("EnemyNozzle_Mat", new Color(0.2f, 0.2f, 0.25f, 1f)); // Dark Metallic

        CreateVisionConePrefab(visionConeMat, visorMat);
        CreateProximityPrefab(proximityMat, nozzleMat);
        CreateLineOfSightPrefab(losMat, visorMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green><b>[EnemySetupTool]</b> Successfully generated all 3 Cube Enemy Prefabs in Assets/Prefab/!</color>");
    }

    [MenuItem("Tools/Enemies/2. Spawn All 3 Types In Active Scene")]
    public static void SpawnEnemiesInScene()
    {
        GenerateEnemyPrefabs();

        GameObject visionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/VisionConeEnemy.prefab");
        GameObject proxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/ProximityEnemy.prefab");
        GameObject losPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/LineOfSightEnemy.prefab");

        if (visionPrefab == null || proxPrefab == null || losPrefab == null)
        {
            Debug.LogError("Could not find generated prefabs in " + PREFAB_DIR);
            return;
        }

        // Find parent container in scene or create one
        GameObject enemiesParent = GameObject.Find("Enemies");
        if (enemiesParent == null)
        {
            enemiesParent = new GameObject("Enemies");
            Undo.RegisterCreatedObjectUndo(enemiesParent, "Create Enemies Root");
        }

        // Determine ground level based on existing enemies or player
        Vector3 basePos = Vector3.zero;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            basePos = playerObj.transform.position;
        }
        else if (enemiesParent.transform.childCount > 0)
        {
            basePos = enemiesParent.transform.GetChild(0).position;
        }

        // Spawn Vision Cone Enemy
        Vector3 vPos = basePos + new Vector3(8f, 0.6f, 12f);
        GameObject vEnemy = (GameObject)PrefabUtility.InstantiatePrefab(visionPrefab, enemiesParent.transform);
        vEnemy.transform.position = vPos;
        vEnemy.name = "VisionConeEnemy_Cube";
        Undo.RegisterCreatedObjectUndo(vEnemy, "Spawn Vision Cone Enemy");

        // Spawn Proximity Enemy
        Vector3 pPos = basePos + new Vector3(-10f, 0.6f, 8f);
        GameObject pEnemy = (GameObject)PrefabUtility.InstantiatePrefab(proxPrefab, enemiesParent.transform);
        pEnemy.transform.position = pPos;
        pEnemy.name = "ProximityEnemy_Cube";
        Undo.RegisterCreatedObjectUndo(pEnemy, "Spawn Proximity Enemy");

        // Spawn Line of Sight Enemy
        Vector3 lPos = basePos + new Vector3(0f, 0.6f, 16f);
        GameObject lEnemy = (GameObject)PrefabUtility.InstantiatePrefab(losPrefab, enemiesParent.transform);
        lEnemy.transform.position = lPos;
        lEnemy.name = "LineOfSightEnemy_Cube";
        Undo.RegisterCreatedObjectUndo(lEnemy, "Spawn Line Of Sight Enemy");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("<color=green><b>[EnemySetupTool]</b> Spawned Vision Cone, Proximity, and Line of Sight Cube Enemies into active scene!</color>");
    }

    [MenuItem("Tools/Enemies/3. Replace Scene Enemies With 3 Types")]
    public static void ReplaceSceneEnemies()
    {
        GenerateEnemyPrefabs();

        GameObject visionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/VisionConeEnemy.prefab");
        GameObject proxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/ProximityEnemy.prefab");
        GameObject losPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/LineOfSightEnemy.prefab");

        GameObject enemiesFolder = GameObject.Find("Enemies");
        if (enemiesFolder == null)
        {
            SpawnEnemiesInScene();
            return;
        }

        // Store positions of existing enemies
        Transform[] children = new Transform[enemiesFolder.transform.childCount];
        for (int i = 0; i < enemiesFolder.transform.childCount; i++)
        {
            children[i] = enemiesFolder.transform.GetChild(i);
        }

        GameObject[] prefabsToCycle = new GameObject[] { visionPrefab, proxPrefab, losPrefab };

        int prefabIdx = 0;
        foreach (Transform child in children)
        {
            if (child == null) continue;
            Vector3 worldPos = child.position;
            Quaternion rot = child.rotation;

            // Remove old enemy
            Undo.DestroyObjectImmediate(child.gameObject);

            // Replace with one of the 3 new AI cube enemies
            GameObject chosenPrefab = prefabsToCycle[prefabIdx % prefabsToCycle.Length];
            GameObject newEnemy = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, enemiesFolder.transform);
            newEnemy.transform.position = worldPos;
            newEnemy.transform.rotation = rot;
            Undo.RegisterCreatedObjectUndo(newEnemy, "Replace Enemy");

            prefabIdx++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("<color=green><b>[EnemySetupTool]</b> Replaced all existing scene enemies with the 3 new AI Cube Enemies!</color>");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
        if (!Directory.Exists(MATERIAL_DIR)) Directory.CreateDirectory(MATERIAL_DIR);
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"{MATERIAL_DIR}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static void CreateVisionConePrefab(Material bodyMat, Material visorMat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "VisionConeEnemy";
        cube.tag = "Enemy";
        cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        cube.GetComponent<Renderer>().sharedMaterial = bodyMat;

        // Front visor cube so user can see facing direction
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor_LookIndicator";
        visor.transform.SetParent(cube.transform, false);
        visor.transform.localPosition = new Vector3(0f, 0.2f, 0.52f);
        visor.transform.localScale = new Vector3(0.7f, 0.25f, 0.1f);
        visor.GetComponent<Renderer>().sharedMaterial = visorMat;
        Object.DestroyImmediate(visor.GetComponent<Collider>());

        // Configure Components
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BoxCollider col = cube.GetComponent<BoxCollider>();
        col.size = Vector3.one;

        cube.AddComponent<EnemyHealth>();
        VisionConeEnemy ai = cube.AddComponent<VisionConeEnemy>();
        ai.visionAngle = 75f;
        ai.visionDistance = 18f;
        ai.roamSpeed = 2.5f;
        ai.chaseSpeed = 4.5f;
        ai.meleeRange = 1.8f;
        ai.meleeDamage = 15f;
        ai.attackCooldown = 1.0f;

        string prefabPath = $"{PREFAB_DIR}/VisionConeEnemy.prefab";
        PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
        Object.DestroyImmediate(cube);
    }

    private static void CreateProximityPrefab(Material bodyMat, Material nozzleMat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "ProximityEnemy";
        cube.tag = "Enemy";
        cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        cube.GetComponent<Renderer>().sharedMaterial = bodyMat;

        // Front turret nozzle
        GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nozzle.name = "TurretNozzle";
        nozzle.transform.SetParent(cube.transform, false);
        nozzle.transform.localPosition = new Vector3(0f, 0.1f, 0.6f);
        nozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        nozzle.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
        nozzle.GetComponent<Renderer>().sharedMaterial = nozzleMat;
        Object.DestroyImmediate(nozzle.GetComponent<Collider>());

        // Dedicated Fire Point
        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(cube.transform, false);
        fp.transform.localPosition = new Vector3(0f, 0.1f, 0.85f);

        // Configure Components
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Stationary!
        rb.freezeRotation = true;

        BoxCollider col = cube.GetComponent<BoxCollider>();
        col.size = Vector3.one;

        cube.AddComponent<EnemyHealth>();
        ProximityEnemy ai = cube.AddComponent<ProximityEnemy>();
        ai.proximityRange = 16f;
        ai.fireRate = 1.5f;
        ai.bulletDamage = 15f;
        ai.bulletSpeed = 22f;
        ai.firePoint = fp.transform;

        string prefabPath = $"{PREFAB_DIR}/ProximityEnemy.prefab";
        PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
        Object.DestroyImmediate(cube);
    }

    private static void CreateLineOfSightPrefab(Material bodyMat, Material visorMat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "LineOfSightEnemy";
        cube.tag = "Enemy";
        cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        cube.GetComponent<Renderer>().sharedMaterial = bodyMat;

        // Eye core accent
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "AllSeeingEye";
        eye.transform.SetParent(cube.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.2f, 0.5f);
        eye.transform.localScale = new Vector3(0.45f, 0.45f, 0.2f);
        eye.GetComponent<Renderer>().sharedMaterial = visorMat;
        Object.DestroyImmediate(eye.GetComponent<Collider>());

        // Configure Components
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BoxCollider col = cube.GetComponent<BoxCollider>();
        col.size = Vector3.one;

        cube.AddComponent<EnemyHealth>();
        LineOfSightEnemy ai = cube.AddComponent<LineOfSightEnemy>();
        ai.sightRange = 25f;
        ai.chaseSpeed = 4.5f;
        ai.meleeRange = 1.8f;
        ai.meleeDamage = 15f;
        ai.attackCooldown = 1.0f;

        string prefabPath = $"{PREFAB_DIR}/LineOfSightEnemy.prefab";
        PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
        Object.DestroyImmediate(cube);
    }
}
