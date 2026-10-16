using UnityEngine;

public class ColorTargetObject : MonoBehaviour
{
    [Header("Objective Settings")]
    public string objectName = "Target Object";
    public Color requiredColor = Color.red;
    
    [Header("Current Status")]
    public Color currentColor = Color.white;
    public bool isCorrectlyColored = false;

    private Renderer objRenderer;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            currentColor = objRenderer.material.color;
        }

        CheckColorMatch();
    }

    public void ApplyColor(Color newColor)
    {
        currentColor = newColor;

        if (objRenderer != null)
        {
            objRenderer.material.color = currentColor;
        }

        CheckColorMatch();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnObjectColored(this);
        }
    }

    private void CheckColorMatch()
    {
        // Compare RGB values with a small tolerance float difference
        float rDiff = Mathf.Abs(currentColor.r - requiredColor.r);
        float gDiff = Mathf.Abs(currentColor.g - requiredColor.g);
        float bDiff = Mathf.Abs(currentColor.b - requiredColor.b);

        isCorrectlyColored = (rDiff < 0.15f && gDiff < 0.15f && bDiff < 0.15f);
    }
}
