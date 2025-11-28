using UnityEngine;
using static Enumerations;

/// <summary>
/// Data structure containing all information needed for audio event processing
/// </summary>
[System.Serializable]
public struct AudioEventData
{
    [Header("Event Information")]
    public GameAudioEvent eventType;
    
    [Header("Spatial Information")]
    public Vector3 worldPosition;
    
    [Header("Audio Settings")]
    [Range(0f, 2f)]
    public float intensity;
    
    [Header("Context Information")]
    public CubeType cubeType;
    public object additionalData;

    /// <summary>
    /// Constructor for basic audio events with position and intensity
    /// </summary>
    /// <param name="eventType">Type of audio event</param>
    /// <param name="worldPosition">World position for spatial audio</param>
    /// <param name="intensity">Audio intensity/volume multiplier</param>
    public AudioEventData(GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
    {
        this.eventType = eventType;
        this.worldPosition = worldPosition;
        this.intensity = intensity;
        this.cubeType = CubeType.Unit; // Default value
        this.additionalData = null;
    }

    /// <summary>
    /// Constructor for cube-related audio events with cube type information
    /// </summary>
    /// <param name="eventType">Type of audio event</param>
    /// <param name="cubeType">Type of cube involved in the event</param>
    /// <param name="worldPosition">World position for spatial audio</param>
    /// <param name="intensity">Audio intensity/volume multiplier</param>
    public AudioEventData(GameAudioEvent eventType, CubeType cubeType, Vector3 worldPosition, float intensity = 1f)
    {
        this.eventType = eventType;
        this.worldPosition = worldPosition;
        this.intensity = intensity;
        this.cubeType = cubeType;
        this.additionalData = null;
    }

    /// <summary>
    /// Constructor for audio events with additional contextual data
    /// </summary>
    /// <param name="eventType">Type of audio event</param>
    /// <param name="worldPosition">World position for spatial audio</param>
    /// <param name="intensity">Audio intensity/volume multiplier</param>
    /// <param name="cubeType">Type of cube involved (if applicable)</param>
    /// <param name="additionalData">Additional contextual data</param>
    public AudioEventData(GameAudioEvent eventType, Vector3 worldPosition, float intensity, CubeType cubeType, object additionalData)
    {
        this.eventType = eventType;
        this.worldPosition = worldPosition;
        this.intensity = intensity;
        this.cubeType = cubeType;
        this.additionalData = additionalData;
    }

    /// <summary>
    /// Validates that the audio event data is properly configured
    /// </summary>
    /// <returns>True if the event data is valid</returns>
    public bool IsValid()
    {
        // Intensity should be within reasonable range
        if (intensity < 0f || intensity > 2f)
            return false;

        // Event type should be defined
        if (!System.Enum.IsDefined(typeof(GameAudioEvent), eventType))
            return false;

        return true;
    }

    /// <summary>
    /// Gets a string representation of the audio event data for debugging
    /// </summary>
    /// <returns>Formatted debug string</returns>
    public override string ToString()
    {
        return $"AudioEvent[{eventType}] at {worldPosition} | Intensity: {intensity:F2} | CubeType: {cubeType}" +
               (additionalData != null ? $" | Data: {additionalData}" : "");
    }
}
