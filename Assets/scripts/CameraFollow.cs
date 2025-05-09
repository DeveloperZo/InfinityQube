using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // The selector to follow
    [SerializeField] private float height = 7f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float positionSmoothTime = 0.25f; // Smaller value for smoother transitions
    [SerializeField] private float rotationSmoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(-5f, -2.5f, 2.5f); // Fine-tune position if needed
    [SerializeField] private Vector3 rotationOffset = new Vector3(0, 15f, -15f);

    // Using separate velocities for more stable movement
    private Vector3 positionVelocity = Vector3.zero;
    private Vector3 lookAtVelocity = Vector3.zero;
    private Vector3 currentLookAt;
    private Quaternion targetRotation;

    private void Start()
    {
        if (target == null)
        {
            // Find the player selector if not assigned
            PlayerManager player = FindObjectOfType<PlayerManager>();
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
        
        // Initialize look target
        currentLookAt = target.position;
        targetRotation = Quaternion.identity;
        transform.rotation = transform.rotation * Quaternion.Euler(rotationOffset.x, rotationOffset.y, rotationOffset.z);

    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate the desired camera position
        Vector3 targetPosition = target.position + offset;
        Vector3 desiredPosition = targetPosition - Vector3.forward * distance + Vector3.up * height;
        
        // Smoothly move the camera to that position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
        
        // Smoothly update the look target (prevents jitter when the selector jumps positions)
        currentLookAt = Vector3.SmoothDamp(currentLookAt, targetPosition, ref lookAtVelocity, rotationSmoothTime);

        // Look at the smoothed target position
        transform.LookAt(currentLookAt);
            
        transform.rotation = transform.rotation * Quaternion.Euler(rotationOffset.x, rotationOffset.y, rotationOffset.z);
    }
    
    // Public method to instantly update camera on scene changes or warps
    public void ForceUpdatePosition()
    {
        if (target == null) return;
        
        Vector3 targetPosition = target.position + offset;
        Vector3 desiredPosition = targetPosition - Vector3.forward * distance + Vector3.up * height;
        
        transform.position = desiredPosition;
        currentLookAt = targetPosition;
        transform.LookAt(currentLookAt);

        transform.rotation = transform.rotation * Quaternion.Euler(rotationOffset.x, rotationOffset.y, rotationOffset.z);

        // Reset velocities
        positionVelocity = Vector3.zero;
        lookAtVelocity = Vector3.zero;
    }
}