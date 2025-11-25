using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public HamsterSpawner hamsterSpawner;
    public Button button;
    public TextMeshProUGUI PlayText;
    public TextMeshProUGUI StopText;
    public PlacementSystem placementSystem;
    public UIPanel uIPanel;
    
    void Start()
    {
        StopText.gameObject.SetActive(false);
        PlayText.gameObject.SetActive(true);
        button.onClick.AddListener(StartPlayMode);
    }

    private void StartPlayMode()
    {
        // Disable editing
        placementSystem.DisableEditing();
        var buttons = uIPanel.GetComponentsInChildren<Button>();
        foreach (var button in buttons) {
            button.interactable = false;
        }
        hamsterSpawner.SpawnHamsters();
        
        // Update UI
        PlayText.gameObject.SetActive(false);
        StopText.gameObject.SetActive(true);
        
        // Disable button interaction
        button.interactable = false;
    }
}
