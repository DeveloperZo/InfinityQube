using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // This will be your selector
    [SerializeField] private float height = 7f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float smoothTime = 0.3f;
    
    private Vector3 velocity = Vector3.zero;
    
    private void Start()
    {
        if (target == null)
        {
            // Find the player selector if not assigned
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraFollow: No target assigned and no PlayerController found!");
                enabled = false;
            }
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate the desired position
        Vector3 targetPosition = target.position;
        Vector3 desiredPosition = targetPosition - Vector3.forward * distance + Vector3.up * height;
        
        // Smoothly move the camera to that position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        
        // Look at the target
        transform.LookAt(targetPosition);
    }
}