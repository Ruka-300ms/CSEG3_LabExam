using UnityEngine;
using System.Collections; 

public class PressurePlateEvent : MonoBehaviour
{
    // Drag your Directional Light into this slot in the Inspector
    public Light mainLight; 
    
    // Type the exact Tag of the objects you want to change (e.g., "ColorProp")
    public string targetTag = "ColorProp"; 
    
    private bool isTriggered = false; 

    void OnCollisionEnter(Collision collision)
    {
        // Only trigger if it's the Player and the trap hasn't sprung yet
        if (collision.gameObject.CompareTag("Player") && !isTriggered)
        {
            StartCoroutine(LightAndColorSequence());
        }
    }

    IEnumerator LightAndColorSequence()
    {
        isTriggered = true; // Lock the plate so it only happens once

        // 1. Turn OFF the light
        mainLight.enabled = false;

        // 2. Pause the sequence for 2 seconds (the dark dramatic pause)
        yield return new WaitForSeconds(3f);

        // 3. Turn ON the light
        mainLight.enabled = true;

        // 4. Find every object in the game that has our target tag and put them in an Array
        GameObject[] allProps = GameObject.FindGameObjectsWithTag(targetTag);

        // 5. Loop through that array and change their colors
        foreach (GameObject prop in allProps)
        {
            // Safely grab the renderer of the current object in the loop
            Renderer propRenderer = prop.GetComponent<Renderer>();
            
            // Just in case we tagged an invisible object by mistake, we check if the renderer actually exists
            if (propRenderer != null) 
            {
                propRenderer.material.color = Random.ColorHSV();
            }
        }
        isTriggered = false;
    }
}