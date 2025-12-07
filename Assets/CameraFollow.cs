using UnityEngine;
using Fusion;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5f, -8f);
    public float followSpeed = 8f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}
