using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public Hamster hamsterPrefab;
    public GameObject hamsterSpawnPosition;
    public Button button;
    public TextMeshProUGUI PlayText;
    public TextMeshProUGUI StopText;
    public PlacementSystem placementSystem;
    
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
        
        // Spawn 3 hamsters at spawn position
        for (int i = 0; i < 3; i++)
        {
            Instantiate(hamsterPrefab, hamsterSpawnPosition.transform.position, Quaternion.identity);
        }
        
        // Update UI
        PlayText.gameObject.SetActive(false);
        StopText.gameObject.SetActive(true);
        
        // Disable button interaction
        button.interactable = false;
    }
}
