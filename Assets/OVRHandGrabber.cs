using UnityEngine;

public class OVRHandGrabber : MonoBehaviour
{
    [Header("Settings")]
    // Select LTouch or RTouch in the Inspector
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    
    // The button to delete objects (Button.Two is 'B' on Right, 'Y' on Left)
    public OVRInput.Button deleteButton = OVRInput.Button.Two; 
    
    public float grabRadius = 0.1f;
    public LayerMask grabMask; // Set this to "Everything" or your specific "Grabbable" layer

    // Internal State
    private GameObject currentObject; // The object we are currently holding
    private GameObject hoveredObject; // The object we are touching but not holding
    private bool isGrabbing = false;

    void Update()
    {
        // 1. GET INPUTS
        // Grip Trigger for Grabbing (Returns float 0 to 1)
        float gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
        bool gripPressed = gripValue > 0.5f;

        // Secondary Button for Deleting (Returns true/false)
        bool deletePressed = OVRInput.GetDown(deleteButton, controller);

        // 2. CHECK DELETE
        if (deletePressed)
        {
            TryDeleteObject();
        }

        // 3. CHECK GRAB
        if (gripPressed && !isGrabbing)
        {
            // If we are hovering over something, pick it up
            if (hoveredObject != null)
            {
                GrabObject(hoveredObject);
            }
        }
        // 4. CHECK RELEASE
        else if (!gripPressed && isGrabbing)
        {
            ReleaseObject();
        }
    }

    void FixedUpdate()
    {
        // Physics check: Find nearby objects if we aren't holding anything
        if (!isGrabbing)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, grabRadius, grabMask);
            if (hits.Length > 0)
            {
                // Find the closest object (optional optimization, otherwise just takes first)
                hoveredObject = hits[0].gameObject;
            }
            else
            {
                hoveredObject = null;
            }
        }
    }

    void GrabObject(GameObject obj)
    {
        isGrabbing = true;
        currentObject = obj;
        hoveredObject = null; // Clear hover so we don't try to grab it again

        // 1. Disable Physics while holding (so it doesn't fight your hand)
        Rigidbody rb = currentObject.GetComponent<Rigidbody>();
        if (rb) 
        {
            rb.isKinematic = true; 
        }

        // 2. Parent it to the hand (Moves 1:1 with you)
        currentObject.transform.SetParent(this.transform, true); 
    }

    void ReleaseObject()
    {
        if (currentObject != null)
        {
            // 1. Un-parent (Drop it back into the world)
            currentObject.transform.SetParent(null, true);

            // 2. PHYSICS DECISION:
            // If you want it to float (Ghost Mode) for slicing: Keep isKinematic = true
            // If you want it to fall to the floor: Set isKinematic = false
            Rigidbody rb = currentObject.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true; // Kept TRUE so planes don't fly away inside cones
            }

            currentObject = null;
        }
        isGrabbing = false;
    }

    void TryDeleteObject()
    {
        // Case A: Delete the object currently in your hand
        if (isGrabbing && currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
            isGrabbing = false;
        }
        // Case B: Delete the object you are touching (hovering)
        else if (!isGrabbing && hoveredObject != null)
        {
            Destroy(hoveredObject);
            hoveredObject = null;
        }
    }

    // Visualize the grab range in the Scene View
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}