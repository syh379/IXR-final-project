using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 3.0f;
    public float turnSpeed = 45.0f;
    
    [Header("Dependencies")]
    public Transform cameraTransform; // Drag your "CenterEyeAnchor" here

    void Update()
    {
        // 1. LEFT STICK: Movement (Forward/Back/Strafe)
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        if (input.magnitude > 0.1f)
        {
            // Calculate direction based on where the HEAD is looking
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Flatten y so we don't fly upwards when looking up
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = (forward * input.y + right * input.x).normalized;
            
            // Move the Rig
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

        // 2. RIGHT STICK: Snap Turning (Optional: Smooth turning if you remove the timer)
        Vector2 turnInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        
        // Simple Smooth Turn
        if (Mathf.Abs(turnInput.x) > 0.1f)
        {
            transform.Rotate(0, turnInput.x * turnSpeed * Time.deltaTime, 0);
        }
    }
}