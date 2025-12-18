using System.Collections;
using UnityEngine;

public class LedMoverSimple : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueVoiceControllerBasic controller;
    [Tooltip("4 = fifth dialogue (index starts from 0).")]
    public int triggerDialogueIndex = 4;

    [Header("LED Object")]
    public GameObject led;
    [Tooltip("If true, LED will use UI (RectTransform) movement. If false, world position is used.")]
    public bool isUI = false;

    [Header("Positions")]
    [Tooltip("Start position (in pixels for UI, in world units otherwise).")]
    public Vector3 startPosition = new Vector3(0, 0, 0);
    [Tooltip("End position (in pixels for UI, in world units otherwise).")]
    public Vector3 endPosition = new Vector3(300, 0, 0);

    [Header("Timing")]
    [Tooltip("Delay (seconds) after dialogue starts before LED appears.")]
    public float startDelay = 0.3f;
    [Tooltip("Time (seconds) for LED to reach the end position.")]
    public float moveDuration = 1.0f;
    [Tooltip("Ease curve for movement (x=time, y=progress).")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visibility")]
    public bool hideBeforeStart = true;
    public bool hideOnDialogueEnd = true;

    Coroutine _runner;

    void Awake()
    {
        if (led != null && hideBeforeStart)
            led.SetActive(false);
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart += HandleStart;
            controller.OnDialogueEnd += HandleEnd;
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart -= HandleStart;
            controller.OnDialogueEnd -= HandleEnd;
        }
    }

    void HandleStart(int index)
    {
        if (_runner != null) StopCoroutine(_runner);

        if (index == triggerDialogueIndex)
            _runner = StartCoroutine(CoRun());
        else if (hideBeforeStart && led != null)
            led.SetActive(false);
    }

    void HandleEnd(int index)
    {
        if (index != triggerDialogueIndex) return;

        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }

        if (hideOnDialogueEnd && led != null)
            led.SetActive(false);
    }

    IEnumerator CoRun()
    {
        if (led == null) yield break;
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        led.SetActive(true);
        float dur = Mathf.Max(0.0001f, moveDuration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float eased = ease.Evaluate(k);
            Vector3 pos = Vector3.LerpUnclamped(startPosition, endPosition, eased);

            if (isUI)
            {
                var rt = led.GetComponent<RectTransform>();
                if (rt) rt.anchoredPosition3D = pos;
            }
            else
            {
                led.transform.position = pos;
            }

            yield return null;
        }

        if (isUI)
        {
            var rt = led.GetComponent<RectTransform>();
            if (rt) rt.anchoredPosition3D = endPosition;
        }
        else
        {
            led.transform.position = endPosition;
        }

        _runner = null;
    }
}
