using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RailCueHighlighter : MonoBehaviour
{
    [Header("Rail / Items")]
    public DistributeAlongRail railDistributor;
    public bool useRailUp = true;

    [Header("Lift")]
    public bool enableLift = true;
    public float liftAmount = 0.5f;
    public float liftTweenSeconds = 0.25f;

    [Header("Glow")]
    public bool enableGlow = true;
    public Color glowColor = Color.yellow;
    public float glowIntensity = 2f;

    [Header("Text Highlight")]
    [Tooltip("If enabled, changes the color of a child text under the same cube during the cue.")]
    public bool enableTextColorChange = true;

    [Tooltip("The temporary color applied to the text during the cue.")]
    public Color textCueColor = Color.black;

    [Tooltip("If enabled, inactive texts are also considered when searching.")]
    public bool includeInactiveTexts = true;

    [Tooltip("If enabled, only one text will be colored (the first by default). If disabled, all child texts will be colored.")]
    public bool onlyFirstText = true;

    [Tooltip("Index of the text to color (0 = first found). If larger than the count, it will be clamped to a valid value.")]
    public int textIndexToColor = 0;

    private readonly Dictionary<int, Coroutine> _running = new Dictionary<int, Coroutine>();
    private readonly Dictionary<Renderer, Color> _origEmission = new Dictionary<Renderer, Color>();
    private readonly Dictionary<TMP_Text, Color> _origTextColor = new Dictionary<TMP_Text, Color>();

    public void PlayCue(int index, float durationSec)
    {
        if (railDistributor == null || railDistributor.transform.childCount == 0) return;
        if (index < 0 || index >= railDistributor.transform.childCount) return;

        if (_running.TryGetValue(index, out var co) && co != null)
        {
            StopCoroutine(co);
            _running.Remove(index);
        }

        var t = railDistributor.transform.GetChild(index);
        _running[index] = StartCoroutine(CueRoutine(index, t, durationSec));
    }

    private IEnumerator CueRoutine(int index, Transform target, float duration)
    {
        if (target == null) yield break;

        Vector3 upRef = (useRailUp && railDistributor != null && railDistributor.rail != null)
                        ? railDistributor.rail.up : Vector3.up;

        Vector3 origLocalPos = target.localPosition;
        if (enableLift && Mathf.Abs(liftAmount) > 0.0001f)
        {
            Vector3 to = origLocalPos + upRef * liftAmount;
            yield return StartCoroutine(LerpLocalPosition(target, target.localPosition, to, liftTweenSeconds));
        }

        Renderer rend = null;
        if (enableGlow)
        {
            rend = target.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                if (!_origEmission.ContainsKey(rend))
                {
                    Color baseEm = Color.black;
                    try { if (rend.material.HasProperty("_EmissionColor")) baseEm = rend.material.GetColor("_EmissionColor"); } catch {}
                    _origEmission[rend] = baseEm;
                }
                try
                {
                    rend.material.EnableKeyword("_EMISSION");
                    Color em = glowColor * Mathf.LinearToGammaSpace(glowIntensity);
                    rend.material.SetColor("_EmissionColor", em);
                }
                catch {}
            }
        }

        List<TMP_Text> changedTexts = null;
        if (enableTextColorChange)
        {
            var texts = target.GetComponentsInChildren<TMP_Text>(includeInactiveTexts);
            if (texts != null && texts.Length > 0)
            {
                changedTexts = new List<TMP_Text>();

                if (onlyFirstText)
                {
                    int idx = Mathf.Clamp(textIndexToColor, 0, texts.Length - 1);
                    TMP_Text t = texts[idx];
                    if (t != null)
                    {
                        if (!_origTextColor.ContainsKey(t)) _origTextColor[t] = t.color;
                        t.color = textCueColor;
                        changedTexts.Add(t);
                    }
                }
                else
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        TMP_Text t = texts[i];
                        if (t == null) continue;
                        if (!_origTextColor.ContainsKey(t)) _origTextColor[t] = t.color;
                        t.color = textCueColor;
                        changedTexts.Add(t);
                    }
                }
            }
        }

        if (duration > 0f) yield return new WaitForSeconds(duration);

        if (enableTextColorChange && changedTexts != null)
        {
            for (int i = 0; i < changedTexts.Count; i++)
            {
                TMP_Text t = changedTexts[i];
                if (t == null) continue;
                if (_origTextColor.TryGetValue(t, out var baseCol))
                    t.color = baseCol;
            }
        }

        if (enableGlow && rend != null)
        {
            try
            {
                if (_origEmission.TryGetValue(rend, out var baseEm))
                {
                    rend.material.SetColor("_EmissionColor", baseEm);
                    if (baseEm.maxColorComponent <= 0.0001f)
                        rend.material.DisableKeyword("_EMISSION");
                }
                else rend.material.DisableKeyword("_EMISSION");
            }
            catch {}
        }

        if (enableLift && Mathf.Abs(liftAmount) > 0.0001f)
        {
            yield return StartCoroutine(LerpLocalPosition(target, target.localPosition, origLocalPos, liftTweenSeconds));
        }

        _running.Remove(index);
    }

    private IEnumerator LerpLocalPosition(Transform t, Vector3 from, Vector3 to, float time)
    {
        float d = Mathf.Max(0.0001f, time);
        float t0 = 0f;
        while (t0 < d)
        {
            t0 += Time.deltaTime;
            float a = Mathf.Clamp01(t0 / d);
            float e = a * a * (3f - 2f * a);
            t.localPosition = Vector3.Lerp(from, to, e);
            yield return null;
        }
        t.localPosition = to;
    }
}
