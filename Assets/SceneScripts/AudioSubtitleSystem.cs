using System.Collections;
using UnityEngine;
using TMPro; // For subtitle text

public class AudioSubtitleSystem : MonoBehaviour
{
    public AudioSource firstAudio; // Audio source for the first audio
    public AudioSource secondAudio; // Audio source for the second audio
    public TextMeshProUGUI subtitleText; // UI text for subtitles
    public string firstSubtitle; // Subtitle for the first audio
    public string secondSubtitle; // Subtitle for the second audio
    public GameObject interactableObject; // Object to interact with

    private int interactionCount = 0;
    private const int maxInteractions = 5;
    private bool isSecondAudioPlaying = false;

    void Start()
    {
        // Start the first audio and subtitle after 20 seconds
        StartCoroutine(PlayFirstAudioAfterDelay(5f));
    }

    private IEnumerator PlayFirstAudioAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayAudioWithSubtitle(firstAudio, firstSubtitle);
    }

    private void PlayAudioWithSubtitle(AudioSource audioSource, string subtitle)
    {
        audioSource.Play();
        subtitleText.text = subtitle;
        StartCoroutine(ClearSubtitleAfterDelay(audioSource.clip.length));
    }

    private IEnumerator ClearSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        subtitleText.text = "";
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == interactableObject && interactionCount < maxInteractions && !isSecondAudioPlaying)
        {
            StartCoroutine(PlaySecondAudioAfterDelay(15f));
        }
    }

    private IEnumerator PlaySecondAudioAfterDelay(float delay)
    {
        isSecondAudioPlaying = true;
        yield return new WaitForSeconds(delay);
        PlayAudioWithSubtitle(secondAudio, secondSubtitle);
        interactionCount++;

        if (interactionCount >= maxInteractions)
        {
            // Disable further interactions
            interactableObject.SetActive(false);
        }

        isSecondAudioPlaying = false;
    }
}
