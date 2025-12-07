using UnityEngine;

// Very simple Rigidbody-based movement for testing.
// This is NOT a full wheel-collider vehicle. It just lets you drive the prefab.

[RequireComponent(typeof(Rigidbody))]
public class SimpleCarController : MonoBehaviour
{
    public float forwardForce = 1500f;
    public float steerTorque = 300f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float v = Input.GetAxis("Vertical");   // W/S or up/down
        float h = Input.GetAxis("Horizontal"); // A/D or left/right

        // apply forward/backward force relative to car forward
        Vector3 force = transform.forward * v * forwardForce * Time.fixedDeltaTime;
        rb.AddForce(force);

        // simple yaw torque to steer (for demo only)
        rb.AddTorque(transform.up * h * steerTorque * Time.fixedDeltaTime);
    }
}
