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
    
    void Start()
    {
        StopText.gameObject.SetActive(false);
        PlayText.gameObject.SetActive(true);
        button.onClick.AddListener(StartPlayMode);
    }

    private void StartPlayMode()
    {
        // todo spawn 3 hamsters at spawn position
    }
}
