using UnityEngine;

// Root component that auto-adds and wires all sub-systems
[RequireComponent(typeof(WaveDebugDataCollector))]
[RequireComponent(typeof(WaveDebugGridConfigurator))]
[RequireComponent(typeof(WaveDebugWaveController))]
[RequireComponent(typeof(WaveDebugUIRenderer))]
[RequireComponent(typeof(WaveDebugInputHandler))]
public class WaveDebugFacade : MonoBehaviour
{
    // Intentionally empty: RequireComponent attributes ensure sub-components are present
}

