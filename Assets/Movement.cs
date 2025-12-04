using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float maxVelocityChange = 10f;
    [Tooltip("Small deadzone to ignore tiny input noise")]
    public float inputDeadzone = 0.01f;

    private Vector2 input;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Prevent the physics system from tipping the character over
        rb.freezeRotation = true;
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();
    }

    void FixedUpdate()
    {
        Vector3 velocityChange = CalculateMovement(walkSpeed);
        if (velocityChange != Vector3.zero)
        {
            // Apply instantaneous velocity change in horizontal plane while preserving y velocity behavior
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        else
        {
            // Optionally apply a small drag when no input to slow to stop (not required)
            // rb.velocity = new Vector3(rb.velocity.x * 0.95f, rb.velocity.y, rb.velocity.z * 0.95f);
        }
    }

    Vector3 CalculateMovement(float _speed)
    {
        // desired velocity in local space (x,z)
        Vector3 targetVelocity = new Vector3(input.x, 0f, input.y);
        targetVelocity = transform.TransformDirection(targetVelocity); // convert from local to world
        targetVelocity *= _speed;

        // current world velocity
        Vector3 velocity = rb.linearVelocity;

        // if input is basically zero, return no change
        if (input.sqrMagnitude <= inputDeadzone * inputDeadzone)
            return Vector3.zero;

        Vector3 velocityChange = targetVelocity - new Vector3(velocity.x, 0f, velocity.z);

        // clamp per-axis change so it's not too large
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);

        // we don't want to directly change vertical velocity here
        velocityChange.y = 0f;

        return velocityChange;
    }
}
