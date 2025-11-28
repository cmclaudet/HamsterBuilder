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
        var hamsterSpawners = FindObjectsByType<HamsterSpawner>(FindObjectsSortMode.None);
        var gridManager = FindObjectsByType<GridManager>(FindObjectsSortMode.None)[0];

        if (!isPlayMode) {
            // Disable editing
            placementSystem.DisableEditing();
            DeregisterSpawnerFromGrid(gridManager, hamsterSpawners);
            var buttons = uIPanel.GetComponentsInChildren<Button>();
            foreach (var button in buttons) {
                button.interactable = false;
            }
            foreach (var spawner in hamsterSpawners) {
                spawner.SpawnHamsters();
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
            foreach (var spawner in hamsterSpawners) {
                spawner.DespawnHamsters();
            }
            HamsterManager.Instance.ResetSpawnedHamsters();

            // Update UI
            PlayText.gameObject.SetActive(true);
            StopText.gameObject.SetActive(false);
            isPlayMode = false;
        }
        
    }

    private void DeregisterSpawnerFromGrid(GridManager gridManager, HamsterSpawner[] hamsterSpawners)
    {
        foreach (var spawner in hamsterSpawners)
        {
            var placedData = spawner.GetComponent<PlacementSystem.PlacedObjectData>();
            gridManager.FreeCells(placedData.gridPosition, placedData.gridSize);
        }
    }
    
    private void RegisterSpawnerToGrid(GridManager gridManager, HamsterSpawner[] hamsterSpawners)
    {
        foreach (var spawner in hamsterSpawners)
        {
            var placedData = spawner.GetComponent<PlacementSystem.PlacedObjectData>();
            gridManager.OccupyCells(placedData.gridPosition, placedData.gridSize);
        }
    }
}
