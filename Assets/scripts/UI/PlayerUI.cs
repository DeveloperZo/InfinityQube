using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI playerLevel;
    [SerializeField] public PlayerManager playerManager;

    void Start()
    {
        playerManager = FindFirstObjectByType<PlayerManager>();


        UpdateDisplay();
    }

    void Update()
    {
        UpdateDisplay();
    }
    private void UpdateDisplay()
    {
       
    }
}