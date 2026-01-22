using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class HideControllerWhenHandInteracts : MonoBehaviour
{
    [Header("Parent that contains ONLY the controller visuals")]
    public Transform visualRoot; // e.g., "Right Controller Visual" or "Left Controller Visual"

    [Tooltip("Also hide while hovering over XRGrabInteractables")]
    public bool hideOnHover = false;

    private readonly List<Renderer> _renderers = new();
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor[] _allInteractorsInHand; // Direct, Ray, Poke, Near-Far, etc.
    private bool _visible = true;

    void Awake()
    {
        if (!visualRoot)
            Debug.LogWarning($"{nameof(HideControllerWhenHandInteracts)}: Assign the visualRoot (… Controller Visual).");

        if (visualRoot)
            _renderers.AddRange(visualRoot.GetComponentsInChildren<Renderer>(true));

        // Collect ALL interactors that live under this hand/controller object
        _allInteractorsInHand = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true);

#if UNITY_EDITOR
        Debug.Log($"[{name}] Collected {_allInteractorsInHand.Length} interactors and {_renderers.Count} renderers.");
#endif
    }

    void Update()
    {
        if (_renderers.Count == 0 || _allInteractorsInHand == null || _allInteractorsInHand.Length == 0)
            return;

        // Hide if any interactor in this hand is selecting (or hovering, if enabled) a GRAB interactable
        bool anyGrabSelecting = _allInteractorsInHand.Any(IsSelectingGrabInteractable);
        bool anyGrabHovering = hideOnHover && _allInteractorsInHand.Any(IsHoveringGrabInteractable);

        bool shouldBeVisible = !(anyGrabSelecting || anyGrabHovering);

        if (shouldBeVisible != _visible)
        {
            for (int i = 0; i < _renderers.Count; i++)
                if (_renderers[i]) _renderers[i].enabled = shouldBeVisible;

            _visible = shouldBeVisible;

#if UNITY_EDITOR
            Debug.Log($"[{name}] Controller visuals -> {(_visible ? "VISIBLE" : "HIDDEN")}");
#endif
        }
    }

    // --- Helper methods ------------------------------------------------------

    private static bool IsSelectingGrabInteractable(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        if (interactor == null) return false;

        var selector = interactor as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor;
        var list = selector?.interactablesSelected;
        if (list == null || list.Count == 0) return false;

        foreach (var interactable in list)
        {
            var comp = interactable as Object as Component;
            if (!comp) continue;

            // Only hide for XRGrabInteractable objects
            if (comp.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() != null)
                return true;
        }

        return false;
    }

    private static bool IsHoveringGrabInteractable(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        if (interactor == null) return false;

        var hoverer = interactor as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRHoverInteractor;
        var list = hoverer?.interactablesHovered;
        if (list == null || list.Count == 0) return false;

        foreach (var interactable in list)
        {
            var comp = interactable as Object as Component;
            if (!comp) continue;

            // Only hide for XRGrabInteractable objects
            if (comp.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() != null)
                return true;
        }

        return false;
    }
}
