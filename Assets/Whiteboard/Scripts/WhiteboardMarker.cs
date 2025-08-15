using System.Collections;
using System.Linq;
using UnityEngine;

public class WhiteboardMarker : MonoBehaviour
{
    [SerializeField] private Transform _tip;
    [SerializeField] private int _penSize = 5;
    [SerializeField] private WhiteboardButtonManager buttonManager; // Reference to Button Manager

    private Renderer _renderer;
    private Color[] _colors;
    private float _tipHeight;

    private RaycastHit _touch;
    private Whiteboard _whiteboard;
    private Vector2 _touchPos, _lastTouchPos;
    private bool _touchedLastFrame;
    private Quaternion _lastTouchRot;

    private AudioSource _brushSound;

    private float _applyTimer = 0f;
    private const float ApplyInterval = 0.1f;

    private Vector3 _lastTipPosition;
    private const float MinMoveThreshold = 0.001f;

    private bool _isPlayingSound = false;

    private bool _drawingEnabled = false; // Local drawing state

    void Start()
    {
        _renderer = _tip.GetComponent<Renderer>();
        _tipHeight = _tip.localScale.y;
        _brushSound = _tip.GetComponent<AudioSource>();
        UpdatePenColor(_renderer.material.color);

        if (_brushSound != null)
        {
            _brushSound.loop = false;
            _brushSound.Stop();
        }

        _lastTipPosition = _tip.position;

        // Ensure buttonManager is assigned
        if (buttonManager == null)
        {
            buttonManager = FindObjectOfType<WhiteboardButtonManager>();
            if (buttonManager == null)
            {
                Debug.LogError("WhiteboardButtonManager reference is missing, and no instance was found in the scene!");
            }
        }

        // Set up initial drawing state
        if (buttonManager != null)
        {
            _drawingEnabled = buttonManager.IsDrawingEnabled();
        }
    }

    void Update()
    {
        // Check if buttonManager state has changed
        if (buttonManager != null)
        {
            _drawingEnabled = buttonManager.IsDrawingEnabled();
        }

        // If drawing is disabled, prevent any drawing action
        if (!_drawingEnabled)
        {
            _touchedLastFrame = false; // Reset drawing state
            return;
        }

        // Perform drawing if enabled
        Draw();

        _applyTimer += Time.deltaTime;
        if (_applyTimer >= ApplyInterval && _whiteboard != null)
        {
            _applyTimer = 0f;
            _whiteboard.texture.Apply();
        }
    }

    private void Draw()
    {
        if (Physics.Raycast(_tip.position, transform.up, out _touch, _tipHeight))
        {
            if (_touch.transform.CompareTag("Whiteboard"))
            {
                if (_whiteboard == null)
                {
                    _whiteboard = _touch.transform.GetComponent<Whiteboard>();
                }

                _touchPos = new Vector2(_touch.textureCoord.x, _touch.textureCoord.y);

                int x = (int)(_touchPos.x * _whiteboard.textureSize.x);
                int y = (int)(_touchPos.y * _whiteboard.textureSize.y);

                x = Mathf.Clamp(x, 0, (int)_whiteboard.textureSize.x - 1);
                y = Mathf.Clamp(y, 0, (int)_whiteboard.textureSize.y - 1);

                if (_touchedLastFrame)
                {
                    DrawLine(_lastTouchPos, new Vector2(x, y));

                    if (Vector3.Distance(_lastTipPosition, _tip.position) > MinMoveThreshold)
                    {
                        PlaySoundOnce();
                    }
                }
                else
                {
                    PlaySoundOnce();
                }

                _lastTouchPos = new Vector2(x, y);
                _lastTouchRot = transform.rotation;
                _lastTipPosition = _tip.position;
                _touchedLastFrame = true;
                return;
            }
        }

        _whiteboard = null;
        _touchedLastFrame = false;
    }

    private void PlaySoundOnce()
    {
        if (_brushSound != null && !_brushSound.isPlaying && !_isPlayingSound)
        {
            _brushSound.Play();
            _isPlayingSound = true;
            StartCoroutine(ResetSoundStateAfterPlay());
        }
    }

    private IEnumerator ResetSoundStateAfterPlay()
    {
        yield return new WaitForSeconds(_brushSound.clip.length);
        _isPlayingSound = false;
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        int steps = Mathf.CeilToInt(distance);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = (int)Mathf.Lerp(start.x, end.x, t);
            int y = (int)Mathf.Lerp(start.y, end.y, t);

            x = Mathf.Clamp(x, 0, (int)_whiteboard.textureSize.x - _penSize);
            y = Mathf.Clamp(y, 0, (int)_whiteboard.textureSize.y - _penSize);

            if (x >= 0 && y >= 0 && x + _penSize <= _whiteboard.textureSize.x && y + _penSize <= _whiteboard.textureSize.y)
            {
                _whiteboard.texture.SetPixels(x, y, _penSize, _penSize, _colors);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ColorChanger"))
        {
            Renderer collidedRenderer = collision.gameObject.GetComponent<Renderer>();
            if (collidedRenderer != null && collidedRenderer.material.HasProperty("_Color"))
            {
                Color newColor = collidedRenderer.material.color;
                UpdatePenColor(newColor);
            }
        }
    }

    private void UpdatePenColor(Color newColor)
    {
        _renderer.material.color = newColor;
        _colors = Enumerable.Repeat(newColor, _penSize * _penSize).ToArray();
    }
}
