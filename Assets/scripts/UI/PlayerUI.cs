using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI playerLevel;
    [SerializeField] public PlayerManager playerManager;

    void Start()
    {
        playerManager = FindObjectOfType<PlayerManager>();


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