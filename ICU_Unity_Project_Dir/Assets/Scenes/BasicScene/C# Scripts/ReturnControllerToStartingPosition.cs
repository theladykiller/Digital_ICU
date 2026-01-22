using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ReturnToDesignTimePose : MonoBehaviour
{
    [Header("Optional: override home pose with a Transform")]
    [SerializeField] private Transform home;                   // Leave null to use scene/editor pose

    [Header("Return behavior")]
    [SerializeField] private bool smoothReturn = false;
    [SerializeField, Min(0.01f)] private float returnDuration = 0.35f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private Rigidbody rb;

    // The design-time pose captured when the game starts
    private Vector3 designPos;
    private Quaternion designRot;

    private bool initialKinematic;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb   = GetComponent<Rigidbody>();
        initialKinematic = rb.isKinematic;

        // Capture the “before loading the program” pose:
        // If a home Transform is set, use that; otherwise use the pose the object has at startup,
        // which is the same as its editor/scene pose.
        if (home != null)
        {
            designPos = home.position;
            designRot = home.rotation;
        }
        else
        {
            designPos = transform.position;
            designRot = transform.rotation;
        }

        grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (grab != null)
            grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnReleased(SelectExitEventArgs _)
    {
        // Stop physics drift the moment it’s released
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Temporarily kinematic while we move it back
        rb.isKinematic = true;

        StopAllCoroutines();
        if (smoothReturn)
            StartCoroutine(ReturnSmoothly());
        else
            SnapBack();
    }

    private void SnapBack()
    {
        transform.SetPositionAndRotation(designPos, designRot);
        rb.isKinematic = initialKinematic;   // restore original setting
    }

    private IEnumerator ReturnSmoothly()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / returnDuration;
            transform.position = Vector3.Lerp(startPos, designPos, t);
            transform.rotation = Quaternion.Slerp(startRot, designRot, t);
            yield return null;
        }

        transform.SetPositionAndRotation(designPos, designRot);
        rb.isKinematic = initialKinematic;   // restore original setting
    }

    // Optional editor helper: capture current scene pose into the 'home' Transform
#if UNITY_EDITOR
    [ContextMenu("Capture Home From Current Pose")]
    private void CaptureHomeFromCurrentPose()
    {
        if (home == null)
        {
            var go = new GameObject($"{name}_Home");
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            home = go.transform;
        }
        else
        {
            home.SetPositionAndRotation(transform.position, transform.rotation);
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
