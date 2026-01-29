using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEntranceSequence : MonoBehaviour
{
    public RectTransform header;
    public RectTransform buttonsParent;
    public float startDelay = 0.05f;
    public float headerDelay = 0.12f;
    public float buttonDelay = 0.12f;
    public float animDuration = 0.45f;
    public float moveDistance = 70f;

    readonly List<RectTransform> buttons = new List<RectTransform>();

    void OnEnable()
    {
        StopAllCoroutines();
        CacheButtons();
        PrepareAll();
        StartCoroutine(PlaySequence());
    }

    void CacheButtons()
    {
        buttons.Clear();
        if (!buttonsParent) return;

        for (int i = 0; i < buttonsParent.childCount; i++)
        {
            var t = buttonsParent.GetChild(i) as RectTransform;
            if (t != null && t.gameObject.activeSelf)
                buttons.Add(t);
        }
    }

    void PrepareAll()
    {
        Prepare(header);
        for (int i = 0; i < buttons.Count; i++)
            Prepare(buttons[i]);
    }

    void Prepare(RectTransform rt)
    {
        if (!rt) return;
        var cg = rt.GetComponent<CanvasGroup>();
        if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        rt.localScale = Vector3.one * 0.85f;
        rt.anchoredPosition -= new Vector2(0, moveDistance);
    }

    IEnumerator PlaySequence()
    {
        yield return null;
        yield return new WaitForSecondsRealtime(startDelay);

        yield return Animate(header);
        yield return new WaitForSecondsRealtime(headerDelay);

        for (int i = 0; i < buttons.Count; i++)
        {
            StartCoroutine(Animate(buttons[i]));
            yield return new WaitForSecondsRealtime(buttonDelay);
        }
    }

    IEnumerator Animate(RectTransform rt)
    {
        if (!rt) yield break;

        var cg = rt.GetComponent<CanvasGroup>();
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, moveDistance);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animDuration;
            float f = Mathf.Clamp01(t);

            cg.alpha = Mathf.Lerp(0f, 1f, EaseOutCubic(f));
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, EaseOutCubic(f));
            rt.localScale = Vector3.LerpUnclamped(Vector3.one * 0.85f, Vector3.one, EaseOutBack(f));

            yield return null;
        }

        cg.alpha = 1f;
        rt.anchoredPosition = endPos;
        rt.localScale = Vector3.one;
    }

    float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
