using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRBaseInteractable))]
public class PokeButtonDepthLimited : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Child transform that visually moves (your mesh).")]
    [SerializeField] private Transform visualTarget;

    [Header("Axis & Depth")]
    [Tooltip("Push axis defined in THIS reference's space (usually the visual).")]
    [SerializeField] private Transform axisSpace; // if null, uses visualTarget
    [Tooltip("Axis in 'axisSpace' along which the button moves (e.g., (0,1,0) = Y).")]
    [SerializeField] private Vector3 axisInSpace = Vector3.up;

    // You control this value yourself (kept private on purpose).
    private float depthFraction = 0.0000000001f;

    [Tooltip("Only allow pushing forward from home (no pulling back past home).")]
    [SerializeField] private bool oneDirectionOnly = true;

    [Header("Behavior")]
    [Tooltip("Speed at which the button eases back when not pushed.")]
    [SerializeField] private float returnSpeed = 12f;
    [Tooltip("Snap back instantly when limit reached.")]
    [SerializeField] private bool snapBackOnLimit = true;
    [Tooltip("If true, after snapping we wait for hover exit before the button can be pressed again.")]
    [SerializeField] private bool requireReleaseToReset = true;

    [Header("Events (hook your actions here)")]
    public UnityEvent onPressed;   // fires once when the press LIMIT is reached
    public UnityEvent onReleased;  // fires after the button returns OR when hover exits

    private XRBaseInteractable interactable;
    private XRPokeInteractor activePoke;

    private Vector3 homeLocal;          // starting local position of visual
    private Vector3 axisParentLocal;    // axis in visualTarget's PARENT local space
    private float  maxTravel;           // max distance along axis (world-size aware)

    private bool following;
    private bool frozenUntilRelease;

    // Edge flags per interaction cycle
    private bool pressedThisCycle = false;       // ensure onPressed fires once
    private bool releaseFiredThisCycle = false;  // ensure onReleased fires once

    // For following
    private Vector3 worldOffset;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (!visualTarget)
        {
            Debug.LogError("[PokeButtonDepthLimited] Assign Visual Target.");
            enabled = false; return;
        }

        if (!axisSpace) axisSpace = visualTarget;

        // Cache home pose
        homeLocal = visualTarget.localPosition;

        // Compute axis in the visual's parent local space
        Transform parent = visualTarget.parent;
        if (!parent)
        {
            Debug.LogError("[PokeButtonDepthLimited] Visual target needs a parent.");
            enabled = false; return;
        }
        Vector3 axisWorld = axisSpace.TransformDirection(axisInSpace.normalized);
        axisParentLocal = parent.InverseTransformDirection(axisWorld).normalized;

        // Compute visual length along the axis (scale-safe)
        float length = EstimateVisualLengthAlongAxis(visualTarget, axisWorld);
        maxTravel = Mathf.Max(0.0001f, depthFraction * length);

        // Hook XR events
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDestroy()
    {
        if (interactable)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (frozenUntilRelease && requireReleaseToReset) return;

        // Only react to poke interactors
        activePoke = args.interactorObject as XRPokeInteractor;
        if (activePoke == null) return;

        following = true;
        pressedThisCycle = false;        // reset per-cycle flags
        releaseFiredThisCycle = false;

        // Offset so the visual doesn't jump on first frame
        worldOffset = visualTarget.position - activePoke.attachTransform.position;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (args.interactorObject == activePoke)
        {
            activePoke = null;
            following = false;

            // Fire release exactly once whether we pressed or not
            if (!releaseFiredThisCycle)
            {
                onReleased?.Invoke();
                releaseFiredThisCycle = true;
            }

            frozenUntilRelease = false; // ready for next press
        }
    }

    private void Update()
    {
        if (!visualTarget) return;

        // If not following (no active poke), ease back to home
        if (!following || activePoke == null)
        {
            visualTarget.localPosition = Vector3.Lerp(
                visualTarget.localPosition, homeLocal, Time.deltaTime * returnSpeed);
            return;
        }

        if (frozenUntilRelease && requireReleaseToReset) return;

        // Desired parent-local position from the poke point
        Transform parent = visualTarget.parent;
        Vector3 desiredLocal = parent.InverseTransformPoint(activePoke.attachTransform.position + worldOffset);

        // Vector from home to desired (in parent local)
        Vector3 delta = desiredLocal - homeLocal;

        // Signed distance along axis
        float along = Vector3.Dot(delta, axisParentLocal);
        if (oneDirectionOnly) along = Mathf.Max(0f, along);

        // Clamp to limit
        float clamped = Mathf.Clamp(along, 0f, maxTravel);
        Vector3 targetLocal = homeLocal + axisParentLocal * clamped;
        visualTarget.localPosition = targetLocal;

        // Hit the limit? -> fire onPressed once, then handle reset logic
        if (clamped >= maxTravel - 1e-5f)
        {
            if (!pressedThisCycle)
            {
                pressedThisCycle = true;
                onPressed?.Invoke();
            }

            if (snapBackOnLimit)
                StartCoroutine(SnapBackNow());

            if (requireReleaseToReset)
                frozenUntilRelease = true;

            following = !requireReleaseToReset; // stop following if we require release
        }
    }

    private IEnumerator SnapBackNow()
    {
        // Small yield so the frame shows the pressed state if desired; set to 0 for instant
        yield return null;
        visualTarget.localPosition = homeLocal;

        // Consider snap-back as a "release" and ensure it only fires once
        if (!releaseFiredThisCycle)
        {
            onReleased?.Invoke();
            releaseFiredThisCycle = true;
        }
    }

    private static float EstimateVisualLengthAlongAxis(Transform visual, Vector3 axisWorld)
    {
        axisWorld = axisWorld.normalized;

        // Prefer MeshFilter bounds
        var mf = visual.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
        {
            // Mesh bounds are in the visual's local space
            var sizeLocal = mf.sharedMesh.bounds.size; // local AABB size
            // Map to world using lossyScale
            Vector3 sizeWorld = Vector3.Scale(sizeLocal, visual.lossyScale);

            // Project AABB size onto axis by taking absolute axis components
            Vector3 axisAbs = new Vector3(Mathf.Abs(axisWorld.x), Mathf.Abs(axisWorld.y), Mathf.Abs(axisWorld.z));
            return Vector3.Dot(sizeWorld, axisAbs);
        }

        // Fallback: renderer bounds (already world-space AABB)
        var r = visual.GetComponent<Renderer>();
        if (r) return Vector3.Dot(r.bounds.size, new Vector3(Mathf.Abs(axisWorld.x), Mathf.Abs(axisWorld.y), Mathf.Abs(axisWorld.z)));

        // Last resort constant
        return 0.1f;
    }
}
