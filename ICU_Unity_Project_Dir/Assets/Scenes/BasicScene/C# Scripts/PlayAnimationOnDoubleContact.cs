using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayOnBothHandlesTouch : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string triggerName = "Play";
    public bool playOnlyOnce = true;

    [Header("Audio")]
    public AudioSource audioSource;   // <-- ADD THIS

    [Header("Handle Tags")]
    public string leftHandleTag = "LeftHandle";
    public string rightHandleTag = "RightHandle";

    private readonly HashSet<Collider> _leftContacts = new HashSet<Collider>();
    private readonly HashSet<Collider> _rightContacts = new HashSet<Collider>();
    private bool _hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(leftHandleTag))
            _leftContacts.Add(other);
        else if (other.CompareTag(rightHandleTag))
            _rightContacts.Add(other);

        TryPlay();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(leftHandleTag))
            _leftContacts.Remove(other);
        else if (other.CompareTag(rightHandleTag))
            _rightContacts.Remove(other);

        if (!playOnlyOnce && (_leftContacts.Count == 0 || _rightContacts.Count == 0))
            _hasPlayed = false;
    }

    private void TryPlay()
    {
        if (_leftContacts.Count > 0 && _rightContacts.Count > 0 && (!_hasPlayed || !playOnlyOnce))
        {
            _hasPlayed = true;
            StartCoroutine(PlayAudioThenAnimation());   // <-- USE COROUTINE
        }
    }

    private IEnumerator PlayAudioThenAnimation()
{
    if (audioSource != null && audioSource.clip != null)
    {
        audioSource.Play();

        float animStartTime = audioSource.clip.length - 0.75f;

        if (animStartTime < 0f)
            animStartTime = 0f; // safety: never wait negative time

        yield return new WaitForSeconds(animStartTime);
    }

    // Trigger animation slightly before the end
    if (animator != null)
        animator.SetTrigger(triggerName);
}
}
