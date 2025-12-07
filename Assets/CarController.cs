using UnityEngine;
// Uncomment the next line if you're using Photon (package: Photon PUN 2)
#if PHOTON_PUN
using Photon.Pun;
#endif

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders (order: FL, FR, RL, RR)")]
    public WheelCollider[] wheelColliders = new WheelCollider[4];
    public Transform[] wheelMeshes = new Transform[4]; // optional visuals; same order as wheelColliders

    [Header("Steering & Drive")]
    [Tooltip("Maximum steering angle (degrees) for front wheels)")]
    public float maxSteerAngle = 30f;
    [Tooltip("Maximum motor torque applied to drive wheels")]
    public float maxMotorTorque = 1500f;
    [Tooltip("Which wheels receive motor torque (rear-wheel by default)")]
    public bool motorRear = true; // if false -> motor front (FWD)
    public bool motorAll = false;  // if true -> AWD

    [Header("Brakes")]
    public float maxBrakeTorque = 3000f;
    public float handbrakeTorque = 5000f;

    [Header("Speed & Handling")]
    public float maxSpeed = 40f; // meters per second
    public float downforce = 100f; // added downward force proportional to speed
    public float steerHelper = 0.5f; // helps keep wheels stable when changing direction (0-1)

    [Header("Other")]
    public float centerOfMassYOffset = -0.5f; // lower the CoM for better stability
    public bool useSpeedCurveLimiter = true;
    public AnimationCurve torqueFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0); // normalized speed -> torque multiplier

    private Rigidbody rb;

    // input
    private float steerInput;
    private float motorInput;
    private float brakeInput;
    private bool handbrakeInput;

    // photon/local control flag
    private bool isLocal = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // adjust center of mass for stability
        rb.centerOfMass += new Vector3(0, centerOfMassYOffset, 0);

        // Photon check (optional)
#if PHOTON_PUN
        PhotonView pv = GetComponent<PhotonView>();
        isLocal = pv == null || pv.IsMine;
#endif

        // Sanity checks
        if (wheelColliders.Length != 4)
            Debug.LogWarning("[CarController] Expecting 4 WheelColliders (FL, FR, RL, RR).");

        if (wheelMeshes.Length != 4)
            Debug.Log("[CarController] If you want rotating wheel visuals, assign 4 Transforms in wheelMeshes.");
    }

    void Update()
    {
        // Only local player should read input
        if (!isLocal) return;

        // standard input axes
        steerInput = Input.GetAxis("Horizontal");      // A/D or left/right
        motorInput = Input.GetAxis("Vertical");        // W/S or up/down
        // brakeInput can be mapped to a separate axis if desired; for now we'll use negative motor as brake
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f; // optional: space as brake (alternate to handbrake)
        handbrakeInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space); // left shift or space for handbrake
    }

    void FixedUpdate()
    {
        // Skip physics control for non-local players
        if (!isLocal) return;

        ApplyPhysics();
        UpdateWheelVisuals();
    }

    void ApplyPhysics()
    {
        // Compute steering
        float steerAngle = maxSteerAngle * steerInput;
        // Apply to front wheels (indices 0 and 1 assumed FL, FR)
        if (wheelColliders.Length >= 2)
        {
            wheelColliders[0].steerAngle = steerAngle;
            wheelColliders[1].steerAngle = steerAngle;
        }

        // Speed and torque limiting
        float currentSpeed = rb.linearVelocity.magnitude; // m/s
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        float torqueMultiplier = 1f;
        if (useSpeedCurveLimiter && torqueFalloff != null)
            torqueMultiplier = torqueFalloff.Evaluate(speedRatio);

        // motor torque to drive wheels (indices 2 and 3 assumed RL, RR)
        float motorTorque = maxMotorTorque * motorInput * torqueMultiplier;

        // Optionally prevent adding positive motor torque if already near maxSpeed (simple limiter)
        if (currentSpeed > maxSpeed && motorInput > 0f)
            motorTorque = 0f;

        // Apply torque depending on FWD/RWD/AWD configuration
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            bool isFront = (i == 0 || i == 1);
            bool isDriveWheel = motorAll || (motorRear ? !isFront : isFront);

            if (isDriveWheel)
                wheelColliders[i].motorTorque = motorTorque;
            else
                wheelColliders[i].motorTorque = 0f;
        }

        // Brakes
        float brakeTorque = brakeInput * maxBrakeTorque;
        if (handbrakeInput) brakeTorque = handbrakeTorque;

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            // For simplicity apply brakes to rear wheels and any wheel with no motor if you want stronger stop
            bool isRear = (i == 2 || i == 3);
            if (isRear || !motorAll)
                wheelColliders[i].brakeTorque = brakeTorque;
            else
                wheelColliders[i].brakeTorque = 0f;
        }

        // Apply simple downforce proportional to forward speed
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);

        // Optional: steer helper to stabilize direction changes (keeps forward velocity aligned more with car forward)
        SteerHelper();
    }

    void SteerHelper()
    {
        // Prevents sudden jitter when the wheels don't align with velocity
        if (Mathf.Abs(steerInput) < 0.01f) return;

        for (int i = 0; i < 2 && i < wheelColliders.Length; i++)
        {
            WheelHit hit;
            if (wheelColliders[i].GetGroundHit(out hit))
            {
                // compute the angle between current velocity and car's forward direction
                Vector3 vel = rb.linearVelocity;
                if (vel.sqrMagnitude < 0.01f) continue;

                float turnAdjustment = steerHelper * steerInput * Time.fixedDeltaTime * 100f;
                // Apply small rotation to velocity vector to help the car follow the desired steering
                Quaternion velRot = Quaternion.AngleAxis(turnAdjustment, transform.up);
                rb.linearVelocity = velRot * rb.linearVelocity;
                break;
            }
        }
    }

    void UpdateWheelVisuals()
    {
        // Sync wheel meshes with colliders
        if (wheelMeshes == null || wheelMeshes.Length != wheelColliders.Length) return;

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelCollider wc = wheelColliders[i];
            Transform mesh = wheelMeshes[i];
            if (mesh == null) continue;

            Vector3 pos;
            Quaternion rot;
            wc.GetWorldPose(out pos, out rot);

            mesh.position = pos;
            mesh.rotation = rot;
        }
    }

    // Optional: visualize wheel rays in editor for debugging
    void OnDrawGizmosSelected()
    {
        if (wheelColliders == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] == null) continue;
            Gizmos.DrawSphere(wheelColliders[i].transform.position, 0.05f);
        }
    }
}
