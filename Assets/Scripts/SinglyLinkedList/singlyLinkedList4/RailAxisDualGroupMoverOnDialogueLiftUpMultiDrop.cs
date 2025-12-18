using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RailAxisDualGroupMoverOnDialogueLiftUpMultiDrop : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int triggerDialogueIndex = 1;
    public int revertDialogueIndex = 4;
    public bool autoPlayOnEnable = false;

    public float startDelay = 0.25f;
    public bool useUnscaledTime = true;

    public Transform rail;
    public List<Transform> leftItems = new List<Transform>();
    public List<Transform> rightItems = new List<Transform>();
    public float leftDistance = 50f;
    public float rightDistance = 50f;
    public float duration = 1f;
    public bool invertAxis = false;

    public List<Transform> dropItems = new List<Transform>();
    public bool hideDropItemsBeforeTrigger = true;
    public float dropDelay = 0f;
    public float dropDistance = 1f;
    public float dropDuration = 0.4f;
    public bool overrideFinalY = false;
    public float finalY = 0f;

    public GameObject hideOnMoveStart;

    public bool enableColorChange = false;
    public float colorDelayFromMoveStart = 0.5f;

    public Renderer[] redRenderers;
    public Renderer[] redHighlightRenderers;
    public float redNormalEmissionMultiplier = 1f;
    public float redHighlightEmissionMultiplier = 2f;
    public bool overrideRedSurface = false;
    public float redMetallic = 0f;
    public float redSmoothness = 0.1f;
    public bool disableRedSpecularHighlights = false;
    public bool disableRedGlossyReflections = false;
    public Light[] redLights;
    public Color redBaseColor = new Color(0.984f, 0.051f, 0.051f);
    public Color redEmissionColor = new Color(245f / 255f, 3f / 255f, 3f / 255f);
    public Color redLightColor = new Color(0.968f, 0.086f, 0.141f);

    public Renderer[] blueRenderers;
    public Renderer[] blueHighlightRenderers;
    public float blueNormalEmissionMultiplier = 1f;
    public float blueHighlightEmissionMultiplier = 2f;
    public bool overrideBlueSurface = false;
    public float blueMetallic = 0f;
    public float blueSmoothness = 0.1f;
    public bool disableBlueSpecularHighlights = false;
    public bool disableBlueGlossyReflections = false;
    public Light[] blueLights;
    public Color blueBaseColor = new Color(0.082f, 0.376f, 0.984f);
    public Color blueEmissionColor = new Color(0.039f, 0.243f, 0.937f);
    public Color blueLightColor = new Color(0.082f, 0.376f, 0.984f);

    public float lightIntensity = 2f;
    public float lightRange = 100f;

    public GameObject textA;
    public GameObject textB;
    public float textADelayFromMoveStart = 0.5f;
    public float textBDelayFromMoveStart = 1.0f;

    public bool enableBlueRevertToOriginal = false;
    public int blueRevertDialogueIndex = 0;
    public float blueRevertDelayFromDialogueStart = 0f;

    public bool revertBlueToReferenceAtDialogueEnd = false;
    public int blueRevertReferenceDialogueIndex = 5;
    public float blueRevertDelayAfterDialogueEnd = 0f;
    public Renderer blueReferenceRenderer;
    public Light blueReferenceLight;

    Coroutine _runner;

    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");
    static readonly int PID_EmissiveColor = Shader.PropertyToID("_EmissiveColor");
    static readonly int PID_EmissionEnabled = Shader.PropertyToID("_EmissionEnabled");
    static readonly int PID_EnableEmissive = Shader.PropertyToID("_EnableEmissive");
    static readonly int PID_Metallic = Shader.PropertyToID("_Metallic");
    static readonly int PID_Smoothness = Shader.PropertyToID("_Smoothness");
    static readonly int PID_Glossiness = Shader.PropertyToID("_Glossiness");

    MaterialPropertyBlock _mpb;

    Dictionary<Transform, Vector3> _initialPos;
    Dictionary<GameObject, bool> _initialActive;
    bool _cached;

    Dictionary<Transform, Vector3> _snapshotPos;
    Dictionary<GameObject, bool> _snapshotActive;
    struct LightState { public Color c; public float i; public float r; public bool e; }
    Dictionary<Light, LightState> _snapshotLights;

    Dictionary<Renderer, Color[]> _blueOrigBase;
    Dictionary<Renderer, Color[]> _blueOrigEmiss;
    Dictionary<Light, LightState> _blueOrigLight;

    void CacheInitial()
    {
        if (_cached) return;
        _initialPos = new Dictionary<Transform, Vector3>();
        _initialActive = new Dictionary<GameObject, bool>();
        if (leftItems != null) for (int i = 0; i < leftItems.Count; i++) { var t = leftItems[i]; if (t) _initialPos[t] = t.position; }
        if (rightItems != null) for (int i = 0; i < rightItems.Count; i++) { var t = rightItems[i]; if (t) _initialPos[t] = t.position; }
        if (dropItems != null) for (int i = 0; i < dropItems.Count; i++) { var t = dropItems[i]; if (t) _initialPos[t] = t.position; }
        if (hideOnMoveStart) _initialActive[hideOnMoveStart] = hideOnMoveStart.activeSelf;
        if (dropItems != null) for (int i = 0; i < dropItems.Count; i++) { var t = dropItems[i]; if (t) _initialActive[t.gameObject] = t.gameObject.activeSelf; }
        if (textA) _initialActive[textA] = textA.activeSelf;
        if (textB) _initialActive[textB] = textB.activeSelf;
        _cached = true;
    }

    void RestoreInitial()
    {
        if (_initialPos != null) foreach (var kv in _initialPos) if (kv.Key) kv.Key.position = kv.Value;
        if (_initialActive != null) foreach (var kv in _initialActive) if (kv.Key) kv.Key.SetActive(kv.Value);
    }

    void TakeSnapshot()
    {
        _snapshotPos = new Dictionary<Transform, Vector3>();
        _snapshotActive = new Dictionary<GameObject, bool>();
        _snapshotLights = new Dictionary<Light, LightState>();
        if (leftItems != null) for (int i = 0; i < leftItems.Count; i++) { var t = leftItems[i]; if (t) _snapshotPos[t] = t.position; }
        if (rightItems != null) for (int i = 0; i < rightItems.Count; i++) { var t = rightItems[i]; if (t) _snapshotPos[t] = t.position; }
        if (dropItems != null) for (int i = 0; i < dropItems.Count; i++) { var t = dropItems[i]; if (t) _snapshotPos[t] = t.position; }
        if (hideOnMoveStart) _snapshotActive[hideOnMoveStart] = hideOnMoveStart.activeSelf;
        if (dropItems != null) for (int i = 0; i < dropItems.Count; i++) { var t = dropItems[i]; if (t) _snapshotActive[t.gameObject] = t.gameObject.activeSelf; }
        if (textA) _snapshotActive[textA] = textA.activeSelf;
        if (textB) _snapshotActive[textB] = textB.activeSelf;
        if (redLights != null) for (int i = 0; i < redLights.Length; i++) { var L = redLights[i]; if (L) _snapshotLights[L] = new LightState { c = L.color, i = L.intensity, r = L.range, e = L.enabled }; }
        if (blueLights != null) for (int i = 0; i < blueLights.Length; i++) { var L = blueLights[i]; if (L) _snapshotLights[L] = new LightState { c = L.color, i = L.intensity, r = L.range, e = L.enabled }; }
    }

    void RestoreSnapshot()
    {
        if (_snapshotPos != null) foreach (var kv in _snapshotPos) if (kv.Key) kv.Key.position = kv.Value;
        if (_snapshotActive != null) foreach (var kv in _snapshotActive) if (kv.Key) kv.Key.SetActive(kv.Value);
        if (_snapshotLights != null) foreach (var kv in _snapshotLights) if (kv.Key) { kv.Key.color = kv.Value.c; kv.Key.intensity = kv.Value.i; kv.Key.range = kv.Value.r; kv.Key.enabled = kv.Value.e; }
        ClearRendererPropertyBlocks(redRenderers);
        ClearRendererPropertyBlocks(blueRenderers);
    }

    void ClearRendererPropertyBlocks(Renderer[] rends)
    {
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++) { var r = rends[i]; if (r) r.SetPropertyBlock(null); }
    }

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _blueOrigBase = new Dictionary<Renderer, Color[]>();
        _blueOrigEmiss = new Dictionary<Renderer, Color[]>();
        _blueOrigLight = new Dictionary<Light, LightState>();
        CacheInitial();
        CaptureBlueOriginals();
        if (hideDropItemsBeforeTrigger && dropItems != null) for (int i = 0; i < dropItems.Count; i++) { var t = dropItems[i]; if (t) t.gameObject.SetActive(false); }
        if (textA) textA.SetActive(false);
        if (textB) textB.SetActive(false);
    }

    void OnEnable()
    {
        if (dialogue != null) dialogue.OnDialogueStart += OnDialogueStart;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        if (autoPlayOnEnable) MoveNow();
    }

    void OnDisable()
    {
        if (dialogue != null) dialogue.OnDialogueStart -= OnDialogueStart;
        if (_runner != null) { StopCoroutine(_runner); _runner = null; }
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode) RestoreInitial();
        else if (state == PlayModeStateChange.EnteredPlayMode) { _cached = false; CacheInitial(); CaptureBlueOriginals(); }
    }
#endif

    void OnDialogueStart(int index)
    {
        if (index == triggerDialogueIndex)
        {
            TakeSnapshot();
            if (_runner != null) { StopCoroutine(_runner); _runner = null; }
            _runner = StartCoroutine(Sequence());
            return;
        }

        if (enableBlueRevertToOriginal && index == blueRevertDialogueIndex)
        {
            StartCoroutine(RevertBlueToOriginalAfter(blueRevertDelayFromDialogueStart));
            return;
        }

        if (revertBlueToReferenceAtDialogueEnd && index == blueRevertReferenceDialogueIndex)
        {
            StartCoroutine(RevertBlueToReferenceAfter(blueRevertDelayAfterDialogueEnd));
            return;
        }

        if (index == revertDialogueIndex + 1) RestoreSnapshot();
    }

    [ContextMenu("Move Now")]
    public void MoveNow()
    {
        if (_runner != null) { StopCoroutine(_runner); _runner = null; }
        _runner = StartCoroutine(SequenceImmediate());
    }

    IEnumerator Sequence()
    {
        float d = Mathf.Max(0f, startDelay);
        float t = 0f;
        if (hideDropItemsBeforeTrigger && dropItems != null) for (int i = 0; i < dropItems.Count; i++) { var tr = dropItems[i]; if (tr) tr.gameObject.SetActive(true); }
        float colorDelayFromDialogueStart = Mathf.Max(0f, colorDelayFromMoveStart - d);
        float textADelayFromDialogueStart = Mathf.Max(0f, textADelayFromMoveStart - d);
        float textBDelayFromDialogueStart = Mathf.Max(0f, textBDelayFromMoveStart - d);
        while (t < d) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        if (hideOnMoveStart) hideOnMoveStart.SetActive(false);
        yield return Drop();
        if (enableColorChange) StartCoroutine(ApplyColorsAfterDelay(colorDelayFromDialogueStart));
        if (textA) StartCoroutine(ShowAfterDelay(textA, textADelayFromDialogueStart));
        if (textB) StartCoroutine(ShowAfterDelay(textB, textBDelayFromDialogueStart));
        yield return Move();
        _runner = null;
    }

    IEnumerator SequenceImmediate()
    {
        if (hideDropItemsBeforeTrigger && dropItems != null) for (int i = 0; i < dropItems.Count; i++) { var tr = dropItems[i]; if (tr) tr.gameObject.SetActive(true); }
        if (hideOnMoveStart) hideOnMoveStart.SetActive(false);
        yield return Drop();
        if (enableColorChange) StartCoroutine(ApplyColorsAfterDelay(colorDelayFromMoveStart));
        if (textA) StartCoroutine(ShowAfterDelay(textA, textADelayFromMoveStart));
        if (textB) StartCoroutine(ShowAfterDelay(textB, textBDelayFromMoveStart));
        yield return Move();
        _runner = null;
    }

    IEnumerator ApplyColorsAfterDelay(float delay)
    {
        float t = 0f;
        float d = Mathf.Max(0f, delay);
        while (t < d) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        ApplyRedRenderers();
        ApplyBlueRenderers();
        ApplyLights(redLights, redLightColor);
        ApplyLights(blueLights, blueLightColor);
    }

    IEnumerator ShowAfterDelay(GameObject go, float delay)
    {
        float t = 0f;
        float d = Mathf.Max(0f, delay);
        while (t < d) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        go.SetActive(true);
    }

    void EnableEmissiveOnRenderer(Renderer r, Color emission)
    {
        var mats = r.materials;
        if (mats == null) return;
        for (int m = 0; m < mats.Length; m++)
        {
            var mat = mats[m];
            if (!mat) continue;
            if (mat.HasProperty(PID_EmissionColor) || mat.HasProperty(PID_EmissiveColor)) mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty(PID_EmissionEnabled)) mat.SetFloat(PID_EmissionEnabled, 1f);
            if (mat.HasProperty(PID_EnableEmissive)) mat.SetFloat(PID_EnableEmissive, 1f);
            if (mat.HasProperty(PID_EmissionColor)) mat.SetColor(PID_EmissionColor, emission);
            else if (mat.HasProperty(PID_EmissiveColor)) mat.SetColor(PID_EmissiveColor, emission);
        }
    }

    void ApplySurfaceOverrides(Renderer r, bool enable, float metallic, float smoothness, bool noSpecular, bool noGlossy)
    {
        if (!enable) return;
        var mats = r.materials;
        if (mats == null) return;
        for (int m = 0; m < mats.Length; m++)
        {
            var mat = mats[m];
            if (!mat) continue;
            if (mat.HasProperty(PID_Metallic)) mat.SetFloat(PID_Metallic, Mathf.Clamp01(metallic));
            if (mat.HasProperty(PID_Smoothness)) mat.SetFloat(PID_Smoothness, Mathf.Clamp01(smoothness));
            if (mat.HasProperty(PID_Glossiness)) mat.SetFloat(PID_Glossiness, Mathf.Clamp01(smoothness));
            if (noSpecular) mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            if (noGlossy) mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        }
    }

    void ApplyBaseAndEmissionMPB(Renderer r, Color baseColor, Color emission)
    {
        _mpb.Clear();
        r.GetPropertyBlock(_mpb);
        var sm = r.sharedMaterial;
        if (sm != null)
        {
            if (sm.HasProperty(PID_BaseColor)) _mpb.SetColor(PID_BaseColor, baseColor);
            else if (sm.HasProperty(PID_Color)) _mpb.SetColor(PID_Color, baseColor);
            if (sm.HasProperty(PID_EmissionColor)) _mpb.SetColor(PID_EmissionColor, emission);
            else if (sm.HasProperty(PID_EmissiveColor)) _mpb.SetColor(PID_EmissiveColor, emission);
        }
        r.SetPropertyBlock(_mpb);
    }

    void ApplyRedRenderers()
    {
        if (redRenderers == null) return;
        HashSet<Renderer> highlights = null;
        if (redHighlightRenderers != null && redHighlightRenderers.Length > 0) highlights = new HashSet<Renderer>(redHighlightRenderers);
        for (int i = 0; i < redRenderers.Length; i++)
        {
            var r = redRenderers[i];
            if (!r) continue;
            float mul = (highlights != null && highlights.Contains(r)) ? Mathf.Max(0f, redHighlightEmissionMultiplier) : Mathf.Max(0f, redNormalEmissionMultiplier);
            Color emiss = redEmissionColor * mul;
            EnableEmissiveOnRenderer(r, emiss);
            ApplySurfaceOverrides(r, overrideRedSurface, redMetallic, redSmoothness, disableRedSpecularHighlights, disableRedGlossyReflections);
            ApplyBaseAndEmissionMPB(r, redBaseColor, emiss);
        }
    }

    void ApplyBlueRenderers()
    {
        if (blueRenderers == null) return;
        HashSet<Renderer> highlights = null;
        if (blueHighlightRenderers != null && blueHighlightRenderers.Length > 0) highlights = new HashSet<Renderer>(blueHighlightRenderers);
        for (int i = 0; i < blueRenderers.Length; i++)
        {
            var r = blueRenderers[i];
            if (!r) continue;
            float mul = (highlights != null && highlights.Contains(r)) ? Mathf.Max(0f, blueHighlightEmissionMultiplier) : Mathf.Max(0f, blueNormalEmissionMultiplier);
            Color emiss = blueEmissionColor * mul;
            EnableEmissiveOnRenderer(r, emiss);
            ApplySurfaceOverrides(r, overrideBlueSurface, blueMetallic, blueSmoothness, disableBlueSpecularHighlights, disableBlueGlossyReflections);
            ApplyBaseAndEmissionMPB(r, blueBaseColor, emiss);
        }
    }

    void ApplyLights(Light[] lights, Color c)
    {
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++)
        {
            var L = lights[i];
            if (!L) continue;
            L.color = c;
            L.intensity = lightIntensity;
            L.range = lightRange;
            L.enabled = true;
        }
    }

    public IEnumerator Move()
    {
        if (!rail || duration <= 0f) yield break;
        Vector3 axis = rail.right.normalized;
        if (invertAxis) axis *= -1f;
        Vector3 basePoint = rail.position;
        var startPos = new Dictionary<Transform, Vector3>();
        var endPos = new Dictionary<Transform, Vector3>();
        for (int i = 0; i < leftItems.Count; i++)
        {
            var tr = leftItems[i];
            if (!tr) continue;
            Vector3 r0 = tr.position - basePoint;
            float along = Vector3.Dot(r0, axis);
            Vector3 perp = r0 - axis * along;
            float targetAlong = along - Mathf.Max(0f, leftDistance);
            if (!startPos.ContainsKey(tr)) startPos[tr] = tr.position;
            endPos[tr] = basePoint + perp + axis * targetAlong;
        }
        for (int i = 0; i < rightItems.Count; i++)
        {
            var tr = rightItems[i];
            if (!tr) continue;
            Vector3 r0 = tr.position - basePoint;
            float along = Vector3.Dot(r0, axis);
            Vector3 perp = r0 - axis * along;
            float targetAlong = along + Mathf.Max(0f, rightDistance);
            if (!startPos.ContainsKey(tr)) startPos[tr] = tr.position;
            endPos[tr] = basePoint + perp + axis * targetAlong;
        }
        if (startPos.Count == 0) yield break;
        float e = 0f;
        float dur = Mathf.Max(0.0001f, duration);
        while (e < dur)
        {
            e += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float tt = Mathf.Clamp01(e / dur);
            foreach (var kv in startPos) { if (!kv.Key) continue; kv.Key.position = Vector3.Lerp(kv.Value, endPos[kv.Key], tt); }
            yield return null;
        }
        foreach (var kv in startPos) { if (!kv.Key) continue; kv.Key.position = endPos[kv.Key]; }
    }

    public IEnumerator Drop()
    {
        if (dropItems == null || dropItems.Count == 0 || dropDuration <= 0f || dropDistance <= 0f) yield break;
        float d = Mathf.Max(0f, dropDelay);
        float t = 0f;
        while (t < d) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        var starts = new Dictionary<Transform, Vector3>();
        var ends = new Dictionary<Transform, Vector3>();
        for (int i = 0; i < dropItems.Count; i++)
        {
            var tr = dropItems[i];
            if (!tr) continue;
            Vector3 start = tr.position;
            Vector3 end = start + Vector3.up * dropDistance;
            if (overrideFinalY) end.y = finalY;
            starts[tr] = start;
            ends[tr] = end;
        }
        if (starts.Count == 0) yield break;
        float e = 0f;
        float dur = Mathf.Max(0.0001f, dropDuration);
        while (e < dur)
        {
            e += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float tt = Mathf.Clamp01(e / dur);
            foreach (var kv in starts)
            {
                if (!kv.Key) continue;
                kv.Key.position = Vector3.Lerp(kv.Value, ends[kv.Key], tt);
            }
            yield return null;
        }
        foreach (var kv in starts)
        {
            if (!kv.Key) continue;
            kv.Key.position = ends[kv.Key];
        }
    }

    IEnumerator RevertBlueToOriginalAfter(float delay)
    {
        float t = 0f;
        float d = Mathf.Max(0f, delay);
        while (t < d) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        RevertBlueToOriginal();
        yield break;
    }

    void CaptureBlueOriginals()
    {
        _blueOrigBase.Clear();
        _blueOrigEmiss.Clear();
        _blueOrigLight.Clear();
        if (blueRenderers != null)
        {
            for (int i = 0; i < blueRenderers.Length; i++)
            {
                var r = blueRenderers[i];
                if (!r) continue;
                var mats = r.materials;
                if (mats == null || mats.Length == 0) continue;
                var bases = new Color[mats.Length];
                var emiss = new Color[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    var mm = mats[m];
                    if (!mm) continue;
                    Color bc = Color.white;
                    if (mm.HasProperty(PID_BaseColor)) bc = mm.GetColor(PID_BaseColor);
                    else if (mm.HasProperty(PID_Color)) bc = mm.GetColor(PID_Color);
                    bases[m] = bc;
                    Color ec = Color.black;
                    if (mm.HasProperty(PID_EmissionColor)) ec = mm.GetColor(PID_EmissionColor);
                    else if (mm.HasProperty(PID_EmissiveColor)) ec = mm.GetColor(PID_EmissiveColor);
                    emiss[m] = ec;
                }
                _blueOrigBase[r] = bases;
                _blueOrigEmiss[r] = emiss;
            }
        }
        if (blueLights != null)
        {
            for (int i = 0; i < blueLights.Length; i++)
            {
                var L = blueLights[i];
                if (!L) continue;
                _blueOrigLight[L] = new LightState { c = L.color, i = L.intensity, r = L.range, e = L.enabled };
            }
        }
    }

    void RevertBlueToOriginal()
    {
        if (blueRenderers != null)
        {
            for (int i = 0; i < blueRenderers.Length; i++)
            {
                var r = blueRenderers[i];
                if (!r) continue;
                r.SetPropertyBlock(null);
                if (!_blueOrigBase.TryGetValue(r, out var bases)) continue;
                _blueOrigEmiss.TryGetValue(r, out var emiss);
                var mats = r.materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (!mat) continue;
                    if (m < bases.Length)
                    {
                        if (mat.HasProperty(PID_BaseColor)) mat.SetColor(PID_BaseColor, bases[m]);
                        else if (mat.HasProperty(PID_Color)) mat.SetColor(PID_Color, bases[m]);
                    }
                    if (emiss != null && m < emiss.Length)
                    {
                        if (mat.HasProperty(PID_EmissionColor)) { mat.EnableKeyword("_EMISSION"); mat.SetColor(PID_EmissionColor, emiss[m]); }
                        else if (mat.HasProperty(PID_EmissiveColor)) { mat.EnableKeyword("_EMISSION"); mat.SetColor(PID_EmissiveColor, emiss[m]); }
                    }
                    if (mat.HasProperty(PID_EmissionEnabled)) mat.SetFloat(PID_EmissionEnabled, 1f);
                    if (mat.HasProperty(PID_EnableEmissive)) mat.SetFloat(PID_EnableEmissive, 1f);
                }
            }
        }
        if (blueLights != null)
        {
            for (int i = 0; i < blueLights.Length; i++)
            {
                var L = blueLights[i];
                if (!L) continue;
                if (_blueOrigLight.TryGetValue(L, out var st))
                {
                    L.color = st.c;
                    L.intensity = st.i;
                    L.range = st.r;
                    L.enabled = st.e;
                }
            }
        }
    }

    IEnumerator RevertBlueToReferenceAfter(float delay)
    {
        float t = 0f;
        float d = Mathf.Max(0f, delay);
        while (t < d) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        RevertBlueToReference();
        yield break;
    }

    void RevertBlueToReference()
    {
        if (blueRenderers == null || blueRenderers.Length == 0 || blueReferenceRenderer == null) return;
        Material refMat = blueReferenceRenderer.sharedMaterial;
        Color refBase = Color.white;
        Color refEmiss = Color.black;
        if (refMat != null)
        {
            if (refMat.HasProperty(PID_BaseColor)) refBase = refMat.GetColor(PID_BaseColor);
            else if (refMat.HasProperty(PID_Color)) refBase = refMat.GetColor(PID_Color);
            if (refMat.HasProperty(PID_EmissionColor)) refEmiss = refMat.GetColor(PID_EmissionColor);
            else if (refMat.HasProperty(PID_EmissiveColor)) refEmiss = refMat.GetColor(PID_EmissiveColor);
        }
        for (int i = 0; i < blueRenderers.Length; i++)
        {
            var r = blueRenderers[i];
            if (!r) continue;
            _mpb.Clear();
            _mpb.SetColor(PID_BaseColor, refBase);
            _mpb.SetColor(PID_EmissionColor, refEmiss);
            r.SetPropertyBlock(_mpb);
            var mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (!mat) continue;
                if (mat.HasProperty(PID_BaseColor)) mat.SetColor(PID_BaseColor, refBase);
                if (mat.HasProperty(PID_Color)) mat.SetColor(PID_Color, refBase);
                if (mat.HasProperty(PID_EmissionColor)) mat.SetColor(PID_EmissionColor, refEmiss);
                if (mat.HasProperty(PID_EmissiveColor)) mat.SetColor(PID_EmissiveColor, refEmiss);
            }
        }
        if (blueLights != null && blueReferenceLight != null)
        {
            for (int i = 0; i < blueLights.Length; i++)
            {
                var L = blueLights[i];
                if (!L) continue;
                L.color = blueReferenceLight.color;
                L.intensity = blueReferenceLight.intensity;
                L.range = blueReferenceLight.range;
                L.enabled = blueReferenceLight.enabled;
            }
        }
    }
}
