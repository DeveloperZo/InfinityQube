using UnityEngine;

public class WaveDebugInputHandler : MonoBehaviour
{
    private WaveDebugUIRenderer uiRenderer;
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;

    private void Awake()
    {
        uiRenderer = GetComponent<WaveDebugUIRenderer>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            uiRenderer.SendMessage("ToggleDebuggerVisibility");
    }
}

