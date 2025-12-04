using UnityEngine;

public class OVRHandGrabber : MonoBehaviour
{
    [Header("Settings")]
    // Set this to 'L Touch' in the Inspector for your Left Hand
    public OVRInput.Controller grabbingHand = OVRInput.Controller.LTouch;
    public float grabRadius = 0.1f;
    public LayerMask grabMask; 

    // Internal State
    private GameObject currentObject; 
    private GameObject hoveredObject; 
    private bool isGrabbing = false;

    // Hardcoded: Right Hand 'B' button is Button.Two on RTouch
    private OVRInput.Controller deleteHand = OVRInput.Controller.RTouch;
    private OVRInput.Button deleteButton = OVRInput.Button.Two; 

    void Update()
    {
        // 1. GET GRAB INPUT (From Left Hand)
        float gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, grabbingHand);
        bool gripPressed = gripValue > 0.5f;

        // 2. GET DELETE INPUT (From Right Hand - Button B)
        // We check RTouch specifically, regardless of which hand this script is on
        bool deletePressed = OVRInput.GetDown(deleteButton, deleteHand);

        // 3. CHECK DELETE
        if (deletePressed)
        {
            TryDeleteObject();
        }

        // 4. CHECK GRAB
        if (gripPressed && !isGrabbing)
        {
            if (hoveredObject != null)
            {
                GrabObject(hoveredObject);
            }
        }
        // 5. CHECK RELEASE
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
        hoveredObject = null; 

        Rigidbody rb = currentObject.GetComponent<Rigidbody>();
        if (rb) 
        {
            rb.isKinematic = true; 
        }

        currentObject.transform.SetParent(this.transform, true); 
    }

    void ReleaseObject()
    {
        if (currentObject != null)
        {
            currentObject.transform.SetParent(null, true);

            Rigidbody rb = currentObject.GetComponent<Rigidbody>();
            if (rb)
            {
                // Keeping IsKinematic = true per your previous request (Ghost mode)
                rb.isKinematic = true; 
            }

            currentObject = null;
        }
        isGrabbing = false;
    }

    void TryDeleteObject()
    {
        // ONLY delete if we are currently holding the object
        if (isGrabbing && currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
            isGrabbing = false; // Reset state immediately
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}