using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // The selector to follow
    
    [Header("Default Camera Settings (Segment 0)")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(-7.5f, 22.5f, -12.5f);
    [SerializeField] private Vector3 defaultRotation = new Vector3(50f, -15f, 0f);
    
    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.25f;
    [SerializeField] private float rotationSmoothTime = 0.3f;
    
    [Header("Segment Transition")]
    [SerializeField] private float segmentTransitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Current state
    private Vector3 positionVelocity = Vector3.zero;
    private Vector3 currentOffset;
    private Vector3 currentRotation;
    private Vector3 targetOffset;
    private Vector3 targetRotation;
    
    // Segment tracking
    private GridSegmentController currentSegment;
    private bool isTransitioning = false;
    private Coroutine transitionCoroutine;

    private void Start()
    {
        if (target == null)
        {
            PlayerManager player = FindFirstObjectByType<PlayerManager>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraFollow: No target assigned and no PlayerController found!");
                enabled = false;
                return;
            }
        }
        
        // Initialize with default settings
        currentOffset = defaultOffset;
        currentRotation = defaultRotation;
        targetOffset = defaultOffset;
        targetRotation = defaultRotation;
        
        // Set initial position and rotation
        ApplyCameraTransform();
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // SEGMENT CONTROLLERS: Auto-detect player's current segment and use its camera settings
        if (!isTransitioning)
        {
            UpdateCameraForPlayerSegment();
            
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime / rotationSmoothTime);
            currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime / rotationSmoothTime);
        }
        
        ApplyCameraTransform();
    }
    
    /// <summary>
    /// Automatically updates camera settings based on which segment the player is on.
    /// </summary>
    private void UpdateCameraForPlayerSegment()
    {
        var gridManager = GridManager.Instance;
        if (gridManager == null || !gridManager.HasSegmentControllers) return;
        
        // Find which segment the player is on
        var playerSegment = gridManager.GetSegmentControllerAtWorldPosition(target.position);
        
        // If player moved to a different segment, update camera settings
        if (playerSegment != null && playerSegment != currentSegment)
        {
            currentSegment = playerSegment;
            targetOffset = playerSegment.cameraOffset;
            targetRotation = playerSegment.cameraRotation;
            Debug.Log($"[CameraFollow] Player moved to segment {playerSegment.segmentIndex}, updating camera settings");
        }
    }
    
    private void ApplyCameraTransform()
    {
        // Calculate desired position: player position + offset
        Vector3 desiredPosition = target.position + currentOffset;
        
        // Smoothly move camera position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
        
        // Apply rotation directly as Euler angles (no quaternion multiplication)
        transform.rotation = Quaternion.Euler(currentRotation);
    }
    
    /// <summary>
    /// Transitions camera to view a new segment.
    /// Reads camera settings from the GridSegmentController.
    /// </summary>
    public void TransitionToSegment(GridSegmentController segment)
    {
        if (segment == null)
        {
            Debug.LogWarning("[CameraFollow] TransitionToSegment called with null segment");
            return;
        }
        
        currentSegment = segment;
        targetOffset = segment.cameraOffset;
        targetRotation = segment.cameraRotation;
        
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        transitionCoroutine = StartCoroutine(AnimateTransition());
        Debug.Log($"[CameraFollow] Transitioning to segment {segment.segmentIndex}: offset={targetOffset}, rotation={targetRotation}");
    }
    
    /// <summary>
    /// Animates the camera transition between segments.
    /// </summary>
    private IEnumerator AnimateTransition()
    {
        isTransitioning = true;
        
        Vector3 startOffset = currentOffset;
        Vector3 startRotation = currentRotation;
        float elapsed = 0f;
        
        while (elapsed < segmentTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / segmentTransitionDuration);
            
            currentOffset = Vector3.Lerp(startOffset, targetOffset, t);
            currentRotation = Vector3.Lerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        currentOffset = targetOffset;
        currentRotation = targetRotation;
        isTransitioning = false;
        
        Debug.Log($"[CameraFollow] Segment transition complete");
    }
    
    /// <summary>
    /// Resets camera to default (segment 0) settings.
    /// </summary>
    public void ResetToDefault()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        currentSegment = null;
        targetOffset = defaultOffset;
        targetRotation = defaultRotation;
        currentOffset = defaultOffset;
        currentRotation = defaultRotation;
        isTransitioning = false;
        
        Debug.Log("[CameraFollow] Reset to default settings");
    }
    
    /// <summary>
    /// Instantly sets camera to segment settings without animation.
    /// </summary>
    public void SetSegmentInstant(GridSegmentController segment)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        if (segment != null)
        {
            currentSegment = segment;
            currentOffset = segment.cameraOffset;
            currentRotation = segment.cameraRotation;
            targetOffset = segment.cameraOffset;
            targetRotation = segment.cameraRotation;
        }
        else
        {
            currentOffset = defaultOffset;
            currentRotation = defaultRotation;
            targetOffset = defaultOffset;
            targetRotation = defaultRotation;
        }
        
        isTransitioning = false;
        ApplyCameraTransform();
    }
    
    /// <summary>
    /// Forces an immediate position update (useful for teleports).
    /// </summary>
    public void ForceUpdatePosition()
    {
        if (target == null) return;
        
        transform.position = target.position + currentOffset;
        transform.rotation = Quaternion.Euler(currentRotation);
        positionVelocity = Vector3.zero;
    }
    
    #region Legacy API (for compatibility)
    
    /// <summary>
    /// Legacy: Rotates camera for segment by index.
    /// Now queries GridManager for the segment controller.
    /// </summary>
    public void RotateForSegment(int segmentIndex)
    {
        var gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null && gridManager.HasSegmentControllers)
        {
            var segment = gridManager.GetSegmentController(segmentIndex);
            if (segment != null)
            {
                TransitionToSegment(segment);
                return;
            }
        }
        
        // Fallback: no segment found, reset to default
        Debug.LogWarning($"[CameraFollow] RotateForSegment({segmentIndex}): No segment controller found");
    }
    
    /// <summary>
    /// Legacy: Instantly sets segment rotation by index.
    /// </summary>
    public void SetSegmentRotationInstant(int segmentIndex)
    {
        var gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null && gridManager.HasSegmentControllers)
        {
            var segment = gridManager.GetSegmentController(segmentIndex);
            SetSegmentInstant(segment);
            return;
        }
        
        // Fallback: reset to default
        ResetToDefault();
    }
    
    /// <summary>
    /// Legacy: Resets segment rotation.
    /// </summary>
    public void ResetSegmentRotation()
    {
        ResetToDefault();
    }
    
    /// <summary>
    /// Legacy: Current segment rotation (now returns 0, rotation is handled differently).
    /// </summary>
    public float CurrentSegmentRotation => 0f;
    
    /// <summary>
    /// Legacy: Is rotating for segment.
    /// </summary>
    public bool IsRotatingForSegment => isTransitioning;
    
    #endregion
}
