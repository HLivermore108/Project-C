using UnityEngine;
using Fusion;

// This script controls switching between local (owned) and remote behavior on the car prefab.
// The prefab must contain:
//  - NetworkObject
//  - Rigidbody
//  - NetworkTransform (to replicate transforms)
//  - CarController (a normal MonoBehaviour that reads Input and moves the Rigidbody)

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkCar : NetworkBehaviour
{
    Rigidbody rb;
    public MonoBehaviour carController; // assign the CarController (or leave null and it will search)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (carController == null)
        {
            // try find a CarController-like component
            carController = GetComponent<MonoBehaviour>();
        }
    }

    public override void Spawned()
    {
        // If this object is owned by the local player, enable physics & controller
        if (Object.HasInputAuthority) // the player who owns it provides input
        {
            rb.isKinematic = false;
            if (carController != null) carController.enabled = true;
        }
        else
        {
            // remote objects: turn physics off and let NetworkTransform drive the transform
            rb.isKinematic = true;
            if (carController != null) carController.enabled = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // cleanup if needed
    }

    // Optional: if you want to run network ticks for non-physics state, override FixedUpdateNetwork()
}
