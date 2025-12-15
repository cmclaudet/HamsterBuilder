using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI PlayText;
    public TextMeshProUGUI StopText;
    public PlacementSystem placementSystem;
    public UIPanel uIPanel;
    private bool isPlayMode;
    
    void Start()
    {
        StopText.gameObject.SetActive(false);
        PlayText.gameObject.SetActive(true);
        button.onClick.AddListener(TogglePlayMode);
    }

    private void TogglePlayMode()
    {
        var gridManager = FindObjectsByType<GridManager>(FindObjectsSortMode.None)[0];
        GameObject[] hamsterSpawners = GameObject.FindGameObjectsWithTag("Spawner");

        if (!isPlayMode) {
            DeregisterSpawnerFromGrid(gridManager, hamsterSpawners);
            // Disable editing
            placementSystem.DisableEditing();
            // var hamsterSpawners = FindObjectsByType<HamsterSpawner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var buttons = uIPanel.GetComponentsInChildren<Button>();
            foreach (var button in buttons) {
                button.interactable = false;
            }
            foreach (var spawner in hamsterSpawners) {
                spawner.GetComponent<HamsterSpawner>().SpawnHamsters();
            }
            
            // Update UI
            PlayText.gameObject.SetActive(false);
            StopText.gameObject.SetActive(true);
            isPlayMode = true;
        } else {
            placementSystem.EnableEditing();
            RegisterSpawnerToGrid(gridManager, hamsterSpawners);
            var buttons = uIPanel.GetComponentsInChildren<Button>();
            foreach (var button in buttons) {
                button.interactable = true;
            }
            var allPlaceableObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);
            foreach (var placeableObject in allPlaceableObjects)
            {
                placeableObject.StopAllCoroutines();
            }
            foreach (var spawner in hamsterSpawners) {
                spawner.GetComponent<HamsterSpawner>().DespawnHamsters();
            }
            HamsterManager.Instance.ResetSpawnedHamsters();

            // Update UI
            PlayText.gameObject.SetActive(true);
            StopText.gameObject.SetActive(false);
            isPlayMode = false;
        }
        
    }

    private void DeregisterSpawnerFromGrid(GridManager gridManager, GameObject[] hamsterSpawners)
    {
        foreach (var spawner in hamsterSpawners)
        {
            if (spawner == placementSystem.PreviewObject)
            {
                continue;
            }
            var placedData = spawner.GetComponent<PlacementSystem.PlacedObjectData>();
            gridManager.FreeCells(placedData.gridPosition, placedData.gridSize);
        }

        // Also deregister Food objects so hamsters can walk over them
        Food[] foodObjects = FindObjectsByType<Food>(FindObjectsSortMode.None);
        foreach (var food in foodObjects)
        {
            if (food.gameObject == placementSystem.PreviewObject)
            {
                continue;
            }
            var placedData = food.GetComponent<PlacementSystem.PlacedObjectData>();
            if (placedData != null)
            {
                gridManager.FreeCells(placedData.gridPosition, placedData.gridSize);

                // Free entry point cells if they were blocking placement
                if (food.ShouldEntryPointsBlockPlacement)
                {
                    Vector3[] entryPoints = food.GetEntryPoints();
                    gridManager.FreePlacementBlockingCells(entryPoints);
                }
            }
        }
    }
    
    private void RegisterSpawnerToGrid(GridManager gridManager, GameObject[] hamsterSpawners)
    {
        foreach (var spawner in hamsterSpawners)
        {
            var placedData = spawner.GetComponent<PlacementSystem.PlacedObjectData>();
            gridManager.OccupyCells(placedData.gridPosition, placedData.gridSize);
        }

        // Also re-register Food objects
        Food[] foodObjects = FindObjectsByType<Food>(FindObjectsSortMode.None);
        foreach (var food in foodObjects)
        {
            var placedData = food.GetComponent<PlacementSystem.PlacedObjectData>();
            if (placedData != null)
            {
                gridManager.OccupyCells(placedData.gridPosition, placedData.gridSize);

                // Re-occupy entry point cells if they should block placement
                if (food.ShouldEntryPointsBlockPlacement)
                {
                    Vector3[] entryPoints = food.GetEntryPoints();
                    gridManager.OccupyPlacementBlockingCells(entryPoints);
                }
            }
        }
    }
}
