using TMPro;
using UnityEngine;
using static Enumerations;

public class StageInfoUI : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI StageNumber;
    [SerializeField] public TextMeshProUGUI StepNumber;

    [SerializeField] public StageManager StageManager;
    [SerializeField] public WaveManager WaveManager;
    
    [Header("Step Counter Colors")]
    [Tooltip("Normal color for step counter")]
    [SerializeField] private Color normalStepColor = Color.white;
    [Tooltip("Color when only Infinity cubes remain (safe state - no penalties)")]
    [SerializeField] private Color onlyInfinityColor = new Color(0.4f, 0.8f, 1f); // Light cyan/blue

    void Start()
    {
        StageManager = FindFirstObjectByType<StageManager>();
        WaveManager = FindFirstObjectByType<WaveManager>();

        UpdateDisplay();
    }

    void Update()
    {
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (StageManager != null)
        {
            StageNumber.text = StageManager.CurrentStageIndex.ToString();
        }
        
        if (WaveManager != null)
        {
            StepNumber.text = WaveManager.MoveStep.ToString();
            
            // Change color when only Infinity cubes remain
            bool onlyInfinityRemaining = WaveManager.HasOnlyInfinityCubesRemaining();
            StepNumber.color = onlyInfinityRemaining ? onlyInfinityColor : normalStepColor;
        }
    }
}
