using TMPro;
using UnityEngine;

public class StageInfoUI : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI StageNumber;
    [SerializeField] public TextMeshProUGUI StepNumber;

    [SerializeField] public StageManager StageManager;
    [SerializeField] public WaveManager WaveManager;

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
        StageNumber.text = StageManager.CurrentStageIndex.ToString();
        StepNumber.text = WaveManager.MoveStep.ToString();
    }
}
