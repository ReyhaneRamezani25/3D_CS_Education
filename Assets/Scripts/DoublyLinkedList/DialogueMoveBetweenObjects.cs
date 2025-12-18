using System.Collections;
using UnityEngine;

public class DialogueMoveBetweenObjects : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;

    public Transform mover;
    public Transform rail;

    public int steps = 5;
    public float stepDistance = 1f;
    public float moveDuration = 0.5f;
    public float delayBetweenSteps = 0.5f;

    public int startDialogueIndex = 1;

    public int hideDialogueIndex = 0;
    public float hideDelay = 0f;

    public bool startHidden = true;
    public float showDelay = 0.5f;

    public bool invertAxis = false;
    public bool lockYToInitial = true;

    Vector3 initialPosition;
    Coroutine sequenceRoutine;
    Coroutine hideRoutine;

    void Awake()
    {
        if (mover)
            initialPosition = mover.position;

        if (startHidden && mover)
            mover.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (dialogue) dialogue.OnDialogueStart += OnDialogueStart;
    }

    void OnDisable()
    {
        if (dialogue) dialogue.OnDialogueStart -= OnDialogueStart;
    }

    void OnDialogueStart(int index)
    {
        if (index == startDialogueIndex)
        {
            if (sequenceRoutine != null)
                StopCoroutine(sequenceRoutine);

            sequenceRoutine = StartCoroutine(Sequence());
        }

        if (index == hideDialogueIndex)
        {
            if (hideRoutine != null)
                StopCoroutine(hideRoutine);

            hideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    IEnumerator Sequence()
    {
        mover.position = initialPosition;

        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        mover.gameObject.SetActive(true);

        Vector3 axis = rail.right.normalized;
        if (invertAxis) axis *= -1f;

        Vector3 basePoint = rail.position;
        Vector3 r0 = mover.position - basePoint;
        float along = Vector3.Dot(r0, axis);
        Vector3 perp = r0 - axis * along;

        float currentAlong = along;

        for (int i = 0; i < steps; i++)
        {
            currentAlong += stepDistance;
            Vector3 targetPos = basePoint + perp + axis * currentAlong;

            if (lockYToInitial)
                targetPos.y = initialPosition.y;

            yield return StartCoroutine(MoveTo(targetPos));

            if (i < steps - 1 && delayBetweenSteps > 0f)
                yield return new WaitForSeconds(delayBetweenSteps);
        }
    }

    IEnumerator HideAfterDelay()
    {
        if (hideDelay > 0f)
            yield return new WaitForSeconds(hideDelay);

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        mover.gameObject.SetActive(false);
    }

    IEnumerator MoveTo(Vector3 targetPosition)
    {
        Vector3 startPos = mover.position;
        float t = 0f;

        if (moveDuration <= 0f)
        {
            mover.position = targetPosition;
            yield break;
        }

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            mover.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        mover.position = targetPosition;
    }
}
