using UnityEngine;

public class AnimatorToggleOnPress : MonoBehaviour
{
    [Header("Target")]
    public Animator animator;

    [Header("Mode A — Triggers (like your old script)")]
    public bool useTriggers = true;
    public string startTrigger = "Animation Start";
    public string stopTrigger  = "Animation Stop";

    [Header("Mode B — Bool parameter (optional)")]
    public bool useBoolParam = false;          // enable if you prefer a bool
    public string boolParam  = "IsPlaying";

    [Header("Looping Audio while animation is ON")]
    public AudioSource loopAudioSource;        // <- assign your looping audio here

    private bool isPlaying = false;

    // Call this from PokeButtonDepthLimited.onPressed
    public void Toggle()
    {
        if (!animator) return;

        isPlaying = !isPlaying;

        // --- Animation handling ---
        if (useBoolParam)
        {
            animator.SetBool(boolParam, isPlaying);
        }
        else if (useTriggers)
        {
            if (isPlaying) animator.SetTrigger(startTrigger);
            else           animator.SetTrigger(stopTrigger);
        }

        // --- Audio handling ---
        HandleLoopingAudio(isPlaying);
    }

    private void HandleLoopingAudio(bool playing)
    {
        if (!loopAudioSource) return;

        // ensure looping
        loopAudioSource.loop = true;

        if (playing)
        {
            if (!loopAudioSource.isPlaying)
                loopAudioSource.Play();
        }
        else
        {
            loopAudioSource.Stop();
        }
    }

    // Optional helpers if you ever want to wire them:
    public void StartAnim()
    {
        if (!animator) return;
        isPlaying = true;

        if (useBoolParam) animator.SetBool(boolParam, true);
        else if (useTriggers) animator.SetTrigger(startTrigger);

        HandleLoopingAudio(true);
    }

    public void StopAnim()
    {
        if (!animator) return;
        isPlaying = false;

        if (useBoolParam) animator.SetBool(boolParam, false);
        else if (useTriggers) animator.SetTrigger(stopTrigger);

        HandleLoopingAudio(false);
    }
}
