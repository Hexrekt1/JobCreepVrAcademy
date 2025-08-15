using System.Collections;
using UnityEngine;
using TMPro;

public class WhiteboardButtonManager : MonoBehaviour
{
    [SerializeField] private Whiteboard[] whiteboards; // Array of whiteboards
    [SerializeField] private Transform[] whiteboardPositions; // Positions for whiteboards
    [SerializeField] private Transform xrRig; // XR Rig for teleportation
    [SerializeField] private Transform xrRigFinalPosition; // Final position for XR Rig
    [SerializeField] private GameObject[] drawingObjects; // Array of drawing objects
    [SerializeField] private Vector3 drawingActivePosition = new Vector3(0, 0, 0); // Active position
    [SerializeField] private Vector3 drawingInactivePosition = new Vector3(0, -5, 0); // Inactive position
    [SerializeField] private float movementSpeed = 2f; // Speed of the drawing movement
    [SerializeField] private float cooldown = 60f; // Button cooldown time
    [SerializeField] private float initialDelay = 1f; // Delay before the first object moves into place
    [SerializeField] private TextMeshPro timerText; // 3D Timer TextMeshPro object
    [SerializeField] private float maxDrawingTime = 480f; // Timer duration in seconds (8 minutes)
    [SerializeField] private AudioClip[] voiceOvers; // Array of random voice-over audio clips
    [SerializeField] private AudioClip[] sequentialButtonAudioClips; // Sequential button press audio

    private int currentStep = 0;
    private bool isCooldown = false;
    private bool isInitialCooldown = true; // Tracks the initial 1-minute cooldown
    private bool isTimerActive = false; // Tracks whether the timer is running
    private bool isDrawingEnabled = true; // Controls drawing ability
    private float drawingTimer = 0f; // Current countdown time
    private bool timerDisabled = false; // Disables timer after the 8th press
    private int sequentialAudioIndex = 0; // Tracks the current audio in the sequence

    private AudioSource audioSource; // Dynamically created AudioSource

    private void Start()
    {
        if (whiteboards.Length != whiteboardPositions.Length)
        {
            Debug.LogError("Whiteboards and positions arrays must have the same length!");
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();

        foreach (var whiteboard in whiteboards)
        {
            whiteboard.gameObject.SetActive(false); // Set all whiteboards inactive
        }

        StartCoroutine(ShowFirstWhiteboard());

        foreach (var drawingObject in drawingObjects)
        {
            drawingObject.transform.position = drawingInactivePosition;
            drawingObject.GetComponent<MeshRenderer>().enabled = false;
        }

        if (drawingObjects.Length > 0)
        {
            StartCoroutine(MoveFirstDrawingObject());
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false); // Set the timer to be invisible initially
        }

        // Start the initial cooldown
        StartCoroutine(InitialCooldown());
    }

    private void Update()
    {
        if (isTimerActive)
        {
            drawingTimer -= Time.deltaTime;

            if (drawingTimer <= 0)
            {
                drawingTimer = 0;
                isTimerActive = false;

                if (isDrawingEnabled)
                {
                    isDrawingEnabled = false; // Restrict drawing when timer ends
                    Debug.Log("Timer ended! Drawing is now restricted.");
                }
            }

            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(drawingTimer / 60f);
                int seconds = Mathf.FloorToInt(drawingTimer % 60f);
                timerText.text = $"{minutes:D2}:{seconds:D2}"; // Format as MM:SS
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tip") || other.CompareTag("Hand"))
        {
            Debug.Log("Button pressed!");
            OnButtonPressed();
        }
    }

    private void OnButtonPressed()
    {
        Debug.Log("OnButtonPressed was called!");

        if (isInitialCooldown)
        {
            Debug.Log("Button is in initial cooldown! Please wait.");
            return;
        }

        if (isCooldown)
        {
            Debug.Log("Button is on cooldown!");
            return;
        }

        StartCoroutine(ButtonCooldown());

        if (voiceOvers.Length > 0)
        {
            int randomIndex = Random.Range(0, voiceOvers.Length);
            AudioClip randomClip = voiceOvers[randomIndex];
            audioSource.clip = randomClip;
            audioSource.Play();
            Debug.Log($"Playing random voice-over: {randomClip.name}");
        }

        if (sequentialAudioIndex < sequentialButtonAudioClips.Length)
        {
            StartCoroutine(PlaySequentialAudioAfterDelay());
        }

        if (currentStep < whiteboards.Length - 1)
        {
            whiteboards[currentStep].transform.position = whiteboardPositions[currentStep].position;

            if (currentStep < drawingObjects.Length)
            {
                var currentDrawingObject = drawingObjects[currentStep];
                currentDrawingObject.GetComponent<MeshRenderer>().enabled = false;
                StartCoroutine(MoveObject(currentDrawingObject, drawingInactivePosition, -90));
            }

            currentStep++;

            whiteboards[currentStep].gameObject.SetActive(true);
            var nextWhiteboardRenderer = whiteboards[currentStep].GetComponent<MeshRenderer>();
            if (nextWhiteboardRenderer != null)
            {
                nextWhiteboardRenderer.enabled = true;
            }

            if (currentStep < drawingObjects.Length)
            {
                var nextDrawingObject = drawingObjects[currentStep];
                nextDrawingObject.GetComponent<MeshRenderer>().enabled = true;
                StartCoroutine(MoveObject(nextDrawingObject, drawingActivePosition, -90));
            }

            if (currentStep == 1 || currentStep == 3 || currentStep == 5)
            {
                if (!timerDisabled)
                {
                    drawingTimer = maxDrawingTime;
                    isTimerActive = true;
                    isDrawingEnabled = true;

                    if (timerText != null)
                    {
                        timerText.gameObject.SetActive(true);
                        timerText.text = $"{(int)maxDrawingTime / 60:D2}:{(int)maxDrawingTime % 60:D2}";
                    }

                    Debug.Log("Timer started! Drawing is now enabled.");
                }
            }
            else
            {
                isTimerActive = false;
                isDrawingEnabled = true; // Explicitly re-enable drawing
                if (timerText != null)
                {
                    timerText.gameObject.SetActive(false);
                }
            }
        }
        else if (currentStep == whiteboards.Length - 1)
        {
            xrRig.position = xrRigFinalPosition.position;
            xrRig.rotation = xrRigFinalPosition.rotation;
            Debug.Log("Teleported XR Rig to final position!");

            timerDisabled = true;
            isTimerActive = false;
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }

            isDrawingEnabled = true; // Re-enable drawing at the final step
            Debug.Log("Drawing re-enabled at final step.");
        }
    }

    private IEnumerator PlaySequentialAudioAfterDelay()
    {
        yield return new WaitForSeconds(6f);

        if (sequentialAudioIndex < sequentialButtonAudioClips.Length)
        {
            AudioClip sequentialClip = sequentialButtonAudioClips[sequentialAudioIndex];
            audioSource.clip = sequentialClip;
            audioSource.Play();
            Debug.Log($"Playing sequential button audio: {sequentialClip.name}");
            sequentialAudioIndex++;
        }
    }

    private IEnumerator MoveObject(GameObject obj, Vector3 targetPosition, float rotationOffset)
    {
        Vector3 initialRotation = obj.transform.rotation.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(initialRotation.x, initialRotation.y + rotationOffset, initialRotation.z);

        while (Vector3.Distance(obj.transform.position, targetPosition) > 0.01f || Quaternion.Angle(obj.transform.rotation, targetRotation) > 0.1f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, targetPosition, movementSpeed * Time.deltaTime);
            obj.transform.rotation = Quaternion.RotateTowards(obj.transform.rotation, targetRotation, movementSpeed * 10 * Time.deltaTime);
            yield return null;
        }
        obj.transform.position = targetPosition;
        obj.transform.rotation = targetRotation;
    }

    private IEnumerator ButtonCooldown()
    {
        isCooldown = true;
        yield return new WaitForSeconds(cooldown);
        isCooldown = false;
    }

    private IEnumerator InitialCooldown()
    {
        Debug.Log("Initial cooldown started...");
        yield return new WaitForSeconds(60f); // 1-minute initial cooldown
        isInitialCooldown = false;
        Debug.Log("Initial cooldown ended! Button is now active.");
    }

    private IEnumerator ShowFirstWhiteboard()
    {
        yield return new WaitForSeconds(initialDelay);
        whiteboards[0].gameObject.SetActive(true);
        var firstWhiteboardRenderer = whiteboards[0].GetComponent<MeshRenderer>();
        if (firstWhiteboardRenderer != null)
        {
            firstWhiteboardRenderer.enabled = true;
        }
    }

    private IEnumerator MoveFirstDrawingObject()
    {
        yield return new WaitForSeconds(initialDelay);
        var firstDrawingObject = drawingObjects[0];
        firstDrawingObject.GetComponent<MeshRenderer>().enabled = true;
        yield return MoveObject(firstDrawingObject, drawingActivePosition, -90);
    }

    public bool IsDrawingEnabled()
    {
        Debug.Log($"IsDrawingEnabled called. Current state: {isDrawingEnabled}");
        return isDrawingEnabled;
    }
}
