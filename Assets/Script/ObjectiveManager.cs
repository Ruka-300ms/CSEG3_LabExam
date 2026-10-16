using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Objective
{
    public GameObject targetObject;
    public Color requiredColor;
    public bool isCompleted = false;
}

public class ObjectiveManager : MonoBehaviour
{
    public List<Objective> objectives = new List<Objective>();

    public int CorrectColors { get; private set; } = 0;
    public int IncorrectColors { get; private set; } = 0;

    public int TotalObjectives => objectiveMap.Count > 0 ? objectiveMap.Count : (objectives != null ? objectives.Count : 0);

    public event Action OnAllObjectivesCompleted;

    private Dictionary<GameObject, Objective> objectiveMap = new Dictionary<GameObject, Objective>();
    private Dictionary<GameObject, Color> paintedObjectsMap = new Dictionary<GameObject, Color>();

    public void InitializeObjectives()
    {
        CorrectColors = 0;
        IncorrectColors = 0;
        objectiveMap.Clear();
        paintedObjectsMap.Clear();

        if (objectives != null)
        {
            objectives.RemoveAll(o => o == null || o.targetObject == null);

            // 1. Register Inspector assigned objectives
            foreach (var obj in objectives)
            {
                if (obj != null && obj.targetObject != null)
                {
                    obj.isCompleted = false;
                    objectiveMap[obj.targetObject] = obj;
                }
            }
        }

        // 2. Auto-discover all scene target objects by tag and component
        AutoPopulateSceneObjectives();
    }

    public void AutoPopulateSceneObjectives()
    {
        if (objectives == null) objectives = new List<Objective>();

        string[] tagsToFind = new string[] { "Cabinet", "Lamp", "Chair", "Table", "Green", "Yellow", "Red", "Blue" };
        foreach (string tag in tagsToFind)
        {
            try
            {
                GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject go in objs)
                {
                    if (go == null) continue;

                    GameObject rootGo = (go.transform.parent != null && IsColorTargetTag(go.transform.parent.gameObject)) ? go.transform.parent.gameObject : go;

                    if (!objectiveMap.ContainsKey(rootGo))
                    {
                        Color req = GetTagRequiredColor(rootGo, Color.white);
                        Objective newObj = new Objective
                        {
                            targetObject = rootGo,
                            requiredColor = req,
                            isCompleted = false
                        };
                        objectives.Add(newObj);
                        objectiveMap[rootGo] = newObj;
                    }
                }
            }
            catch (UnityException)
            {
                // Tag not registered in Tag Manager yet
            }
        }

        // Also discover objects with ColorTargetObject component attached
        ColorTargetObject[] colorTargets = FindObjectsOfType<ColorTargetObject>();
        foreach (ColorTargetObject cto in colorTargets)
        {
            if (cto == null || cto.gameObject == null) continue;
            GameObject go = cto.gameObject;

            if (!objectiveMap.ContainsKey(go))
            {
                Color req = GetTagRequiredColor(go, cto.requiredColor);
                Objective newObj = new Objective
                {
                    targetObject = go,
                    requiredColor = req,
                    isCompleted = false
                };
                objectives.Add(newObj);
                objectiveMap[go] = newObj;
            }
        }
    }

    void Awake()
    {
        if (FindObjectOfType<TrainingLevelManager>() != null)
        {
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (FindObjectOfType<TrainingLevelManager>() != null)
        {
            enabled = false;
            return;
        }

        InitializeObjectives();
        RecalculateScores();
    }

    [Header("Tag Checker Settings")]
    [Tooltip("Tags recognized as valid colorable targets for objectives")]
    public string[] targetTags = new string[] { "Cabinet", "Lamp", "Chair", "Table", "Green", "Yellow", "Red", "Blue", "ColorTarget", "ColorProp", "Obstacle", "Objective", "Target" };

    /// <summary>
    /// Returns designated color required for specific object or color tags:
    /// Cabinet/Green -> Green, Lamp/Yellow -> Yellow, Chair/Red -> Red, Table/Blue -> Blue
    /// </summary>
    public static Color GetTagRequiredColor(GameObject obj, Color defaultRequired)
    {
        if (obj == null) return defaultRequired;

        // Traverse up hierarchy to find assigned tag if child mesh is Untagged
        string tag = obj.tag;
        Transform current = obj.transform;
        while (current != null)
        {
            if (!string.IsNullOrEmpty(current.tag) && current.tag != "Untagged")
            {
                tag = current.tag;
                break;
            }
            current = current.parent;
        }

        if (string.IsNullOrEmpty(tag) || tag == "Untagged") return defaultRequired;

        // Cabinet or Green tag -> Green
        if (tag.Equals("Cabinet", StringComparison.OrdinalIgnoreCase) || tag.Equals("Green", StringComparison.OrdinalIgnoreCase))
        {
            return Color.green;
        }

        // Lamp or Yellow tag -> Yellow
        if (tag.Equals("Lamp", StringComparison.OrdinalIgnoreCase) || tag.Equals("Yellow", StringComparison.OrdinalIgnoreCase))
        {
            return Color.yellow;
        }

        // Chair or Red tag -> Red
        if (tag.Equals("Chair", StringComparison.OrdinalIgnoreCase) || tag.Equals("Red", StringComparison.OrdinalIgnoreCase))
        {
            return Color.red;
        }

        // Table or Blue tag -> Blue
        if (tag.Equals("Table", StringComparison.OrdinalIgnoreCase) || tag.Equals("Blue", StringComparison.OrdinalIgnoreCase))
        {
            return Color.blue;
        }

        return defaultRequired;
    }

    /// <summary>
    /// Tag Checker: Safely checks if a GameObject or its parent matches any color target tag.
    /// </summary>
    public static bool IsColorTargetTag(GameObject obj, string[] tags = null)
    {
        if (obj == null) return false;

        string[] checkTags = tags ?? new string[] { "Cabinet", "Lamp", "Chair", "Table", "Green", "Yellow", "Red", "Blue", "ColorTarget", "ColorProp", "Obstacle", "Objective", "Target" };

        string objTag = obj.tag;
        string parentTag = (obj.transform.parent != null) ? obj.transform.parent.tag : "";

        foreach (string t in checkTags)
        {
            if (string.IsNullOrEmpty(t)) continue;
            if (string.Equals(objTag, t, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(parentTag, t, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Fallback: Check if ColorTargetObject component exists on obj or parent
        return obj.GetComponentInParent<ColorTargetObject>() != null;
    }

    public static bool IsColorMatch(Color c1, Color c2, float tolerance = 0.15f)
    {
        return Mathf.Abs(c1.r - c2.r) < tolerance &&
               Mathf.Abs(c1.g - c2.g) < tolerance &&
               Mathf.Abs(c1.b - c2.b) < tolerance;
    }

    public void RecalculateScores()
    {
        int correct = 0;
        int incorrect = 0;
        HashSet<GameObject> countedObjectives = new HashSet<GameObject>();

        // 1. Process Objective Targets
        foreach (var kvp in objectiveMap)
        {
            Objective obj = kvp.Value;
            if (obj == null) continue;

            GameObject target = kvp.Key != null ? kvp.Key : obj.targetObject;
            if (target == null) continue;

            countedObjectives.Add(target);

            Color reqColor = GetTagRequiredColor(target, obj.requiredColor);
            obj.requiredColor = reqColor;

            // Check if player has painted target OR any child transform of target
            bool foundPaint = false;
            Color appliedColor = Color.white;

            if (paintedObjectsMap.TryGetValue(target, out appliedColor))
            {
                foundPaint = true;
            }
            else
            {
                foreach (var pKvp in paintedObjectsMap)
                {
                    if (pKvp.Key != null && (pKvp.Key.transform.IsChildOf(target.transform) || target.transform.IsChildOf(pKvp.Key.transform)))
                    {
                        appliedColor = pKvp.Value;
                        foundPaint = true;
                        countedObjectives.Add(pKvp.Key);
                        break;
                    }
                }
            }

            if (foundPaint)
            {
                if (IsColorMatch(appliedColor, reqColor))
                {
                    obj.isCompleted = true;
                    correct++;
                }
                else
                {
                    obj.isCompleted = false;
                    incorrect++;
                }
            }
            else
            {
                obj.isCompleted = false;
            }
        }

        // 2. Process non-objective obstacle objects painted by player
        foreach (var kvp in paintedObjectsMap)
        {
            GameObject paintedObj = kvp.Key;
            if (paintedObj == null) continue;

            bool isObjectivePart = false;
            foreach (GameObject objTarget in countedObjectives)
            {
                if (objTarget != null && (paintedObj == objTarget || paintedObj.transform.IsChildOf(objTarget.transform) || objTarget.transform.IsChildOf(paintedObj.transform)))
                {
                    isObjectivePart = true;
                    break;
                }
            }

            if (!isObjectivePart)
            {
                // Non-objective obstacle painted by player -> penalty
                incorrect++;
            }
        }

        CorrectColors = correct;
        IncorrectColors = incorrect;
    }

    public void OnObjectColored(GameObject hitObject, Color colorApplied)
    {
        if (hitObject == null) return;

        // Resolve hitObject to target key in objectiveMap or top-most parent
        GameObject resolvedTarget = hitObject;
        foreach (var kvp in objectiveMap)
        {
            GameObject objKey = kvp.Key;
            if (objKey == null) continue;

            if (hitObject == objKey || hitObject.transform.IsChildOf(objKey.transform) || objKey.transform.IsChildOf(hitObject.transform))
            {
                resolvedTarget = objKey;
                break;
            }
        }

        if (!objectiveMap.ContainsKey(resolvedTarget) && hitObject.transform.parent != null)
        {
            resolvedTarget = hitObject.transform.root.gameObject;
        }

        // Record paint action on resolved target
        paintedObjectsMap[resolvedTarget] = colorApplied;

        RecalculateScores();
        CheckWinCondition();
    }



    private void CheckWinCondition()
    {
        int totalTargets = TotalObjectives;

        // Win condition: Game ends ONLY when ALL active objectives are correctly colored
        if (totalTargets > 0 && CorrectColors >= totalTargets)
        {
            OnAllObjectivesCompleted?.Invoke();
        }
    }

    public static string GetColorNameString(Color c)
    {
        if (IsColorMatch(c, Color.green)) return "Green";
        if (IsColorMatch(c, Color.yellow)) return "Yellow";
        if (IsColorMatch(c, Color.red)) return "Red";
        if (IsColorMatch(c, Color.blue)) return "Blue";
        if (IsColorMatch(c, Color.cyan)) return "Cyan";
        if (IsColorMatch(c, Color.magenta)) return "Magenta";
        return "Target Color";
    }

    public string GetObjectivesText()
    {
        RecalculateScores();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>OBJECTIVES:</b>");

        foreach (var kvp in objectiveMap)
        {
            Objective obj = kvp.Value;
            if (obj == null) continue;

            GameObject targetObj = kvp.Key != null ? kvp.Key : obj.targetObject;
            string objName = targetObj != null ? targetObj.name : "Target Object";
            
            if (objName.Contains("(Clone)")) objName = objName.Substring(0, objName.IndexOf("(Clone)"));
            if (objName.Contains("_Clone")) objName = objName.Substring(0, objName.IndexOf("_Clone"));

            Color reqColor = GetTagRequiredColor(targetObj, obj.requiredColor);
            string colorHex = ColorUtility.ToHtmlStringRGB(reqColor);
            string colorName = GetColorNameString(reqColor);
            string status = obj.isCompleted ? "<color=#4CAF50>[✓]</color>" : "<color=#FF5252>[X]</color>";
            
            sb.AppendLine($"{objName} -> <color=#{colorHex}><b>{colorName}</b></color> {status}");
        }
        return sb.ToString();
    }
}
