using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UIPanel : MonoBehaviour
{
    public Button ButtonPrefab;
    public PlacementSystem placementSystem;
    
    void Start()
    {
        LoadPlaceableObjects();
    }

    private void LoadPlaceableObjects()
    {
        // Load all PlaceableObject assets from Resources
        PlaceableObjectDefinition[] placeableObjects = Resources.LoadAll<PlaceableObjectDefinition>("PlaceableObjects");
        
        // Create a button for each placeable object
        foreach (PlaceableObjectDefinition placeableObject in placeableObjects)
        {
            CreateButtonForObject(placeableObject);
        }
    }
    
    private void CreateButtonForObject(PlaceableObjectDefinition placeableObject)
    {
        if (ButtonPrefab == null)
        {
            Debug.LogWarning("UIPanel: ButtonPrefab is not assigned.");
            return;
        }
        
        // Instantiate button
        Button button = Instantiate(ButtonPrefab, transform);
        
        // Set button text to the object's name
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = placeableObject.name;
        }
        else
        {
            // Fallback to standard Text component if TMP is not available
            Text legacyText = button.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.text = placeableObject.name;
            }
        }
        
        // Add click listener
        button.onClick.AddListener(() => OnPlaceableObjectButtonClicked(placeableObject));
    }
    
    private void OnPlaceableObjectButtonClicked(PlaceableObjectDefinition placeableObject)
    {
        if (placementSystem != null)
        {
            placementSystem.StartPlacement(placeableObject);
        }
        else
        {
            Debug.LogWarning("UIPanel: PlacementSystem is not assigned.");
        }
    }
}
