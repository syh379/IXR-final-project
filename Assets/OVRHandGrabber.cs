using UnityEngine;

public class OVRHandGrabber : MonoBehaviour
{
    [Header("Settings")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public float grabRadius = 0.1f;
    public LayerMask grabMask; // What layer are the shapes on?

    // Internal State
    private GameObject currentObject; // The object we are holding
    private GameObject hoveredObject; // The object we are touching but not holding yet
    private bool isGrabbing = false;

    void Update()
    {
        // 1. CHECK INPUT
        // Use the Hand Trigger (Grip) usually for grabbing, saving Index Trigger for drawing
        float gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
        bool gripPressed = gripValue > 0.5f;

        // 2. LOGIC: GRAB
        if (gripPressed && !isGrabbing)
        {
            if (hoveredObject != null)
            {
                GrabObject(hoveredObject);
            }
        }
        // 3. LOGIC: RELEASE
        else if (!gripPressed && isGrabbing)
        {
            ReleaseObject();
        }
    }

    void FixedUpdate()
    {
        // Physics check to find nearby objects if we aren't holding anything
        if (!isGrabbing)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, grabRadius, grabMask);
            if (hits.Length > 0)
            {
                // Just grab the first thing we find
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

        // Disable physics while holding so it doesn't flop around
        Rigidbody rb = currentObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // PARENTING: This makes the object move/rotate 1:1 with your hand
        currentObject.transform.SetParent(this.transform, true); 
    }

    void ReleaseObject()
    {
        if (currentObject != null)
        {
            // UN-PARENT: Restore it to the world
            currentObject.transform.SetParent(null, true);

            // Re-enable physics so it stays there (or falls if you want gravity)
            Rigidbody rb = currentObject.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false; 
                // Optional: Add "Throw" velocity here if you want to toss it
            }

            currentObject = null;
        }
        isGrabbing = false;
    }

    // Visualize the grab radius in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}