using System.Collections;
using UnityEngine;
using TMPro;

public class NOECueController : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("During which dialogue number (1-based) should this LED/text activate? e.g., 6 or 7")]
    public int triggerDialogueIndex = 6;

    [Header("LED Target")]
    [Tooltip("Transform of the LED sphere (Renderer/Light is found from it).")]
    public Transform ledTransform;

    [Header("Cue Colors (LED)")]
    [Tooltip("Cue color (for material and LED light).")]
    public Color cueColor = Color.red;

    [Tooltip("Emission intensity during cue (HDR).")]
    [Range(0f, 10f)] public float cueEmissionIntensity = 4f;

    [Header("Light (optional)")]
    [Tooltip("If null, it will be found on ledTransform or its children.")]
    public Light ledLight;
    [Tooltip("How many times to multiply light intensity during cue.")]
    public float lightIntensityMultiplier = 2f;

    [Header("Advanced (LED)")]
    [Tooltip("If emission is off, enable it during the cue.")]
    public bool forceEnableEmission = true;

    [Header("NOE Text (optional)")]
    [Tooltip("NOE text that should glow during dialogue 6 (TMP UGUI or 3D).")]
    public TMP_Text noeText;

    [Header("Text Highlight Settings")]
    [Tooltip("Effect color (red is recommended).")]
    public Color textHighlightColor = Color.red;

    [Tooltip("HDR multiplier for Face/Material. Works with Bloom (2 to 6 is reasonable).")]
    [Range(1f, 10f)] public float textHDRMultiplier = 3f;

    [Tooltip("Enable Outline (if available) to strengthen edges.")]
    public bool useOutline = true;
    [Range(0f,1f)] public float outlineWidthOnCue = 0.2f;

    [Tooltip("Enable Underlay (if available) as a soft halo.")]
    public bool useUnderlay = true;
    [Range(0f,1f)] public float underlaySoftnessOnCue = 0.6f;

    Renderer _renderer;
    Material _mat;

    string _mainColorProp = null;
    bool   _hasMainColor  = false;
    Color  _origMainColor = Color.white;

    string _emissionProp = null;
    bool   _hasEmission  = false;
    Color  _origEmissionColor = Color.black;
    bool   _hadEmissionKeyword = false;

    Color _origLightColor = Color.white;
    float _origLightIntensity = 0f;

    Material _textMat;
    Color _origFaceVertexColor;
    bool  _hasFaceColorProp = false;
    Color _origFaceMatColor;

    bool  _hasOutline = false;
    float _origOutlineWidth = 0f;
    Color _origOutlineColor = Color.black;

    bool  _hasUnderlay = false;
    Color _origUnderlayColor = Color.black;
    float _origUnderlaySoftness = 0f;
    float _origUnderlayOffsetX = 0f, _origUnderlayOffsetY = 0f;
    bool  _underlayKeywordOnBefore = false;

    Coroutine _runner;

    void Awake()
    {
        if (ledTransform != null)
        {
            _renderer = ledTransform.GetComponentInChildren<Renderer>();
            if (_renderer != null) _mat = _renderer.material;

            if (ledLight == null)
                ledLight = ledTransform.GetComponentInChildren<Light>();
        }

        if (_mat != null)
        {
            if (_mat.HasProperty("_Color")) { _mainColorProp = "_Color"; _hasMainColor = true; }
            else if (_mat.HasProperty("_BaseColor")) { _mainColorProp = "_BaseColor"; _hasMainColor = true; }

            if (_mat.HasProperty("_EmissionColor")) { _emissionProp = "_EmissionColor"; _hasEmission = true; }
            else if (_mat.HasProperty("_EmissiveColor")) { _emissionProp = "_EmissiveColor"; _hasEmission = true; }
        }

        if (noeText != null)
        {
            _textMat = noeText.fontMaterial;
            _origFaceVertexColor = noeText.color;

            if (_textMat != null)
            {
                if (_textMat.HasProperty("_FaceColor"))
                {
                    _hasFaceColorProp = true;
                    _origFaceMatColor = _textMat.GetColor("_FaceColor");
                }

                _hasOutline  = _textMat.HasProperty("_OutlineColor") && _textMat.HasProperty("_OutlineWidth");
                if (_hasOutline)
                {
                    _origOutlineColor = _textMat.GetColor("_OutlineColor");
                    _origOutlineWidth = _textMat.GetFloat("_OutlineWidth");
                }

                _hasUnderlay = _textMat.HasProperty("_UnderlayColor")
                               && _textMat.HasProperty("_UnderlaySoftness")
                               && _textMat.HasProperty("_UnderlayOffsetX")
                               && _textMat.HasProperty("_UnderlayOffsetY");
                if (_hasUnderlay)
                {
                    _origUnderlayColor   = _textMat.GetColor("_UnderlayColor");
                    _origUnderlaySoftness= _textMat.GetFloat("_UnderlaySoftness");
                    _origUnderlayOffsetX = _textMat.GetFloat("_UnderlayOffsetX");
                    _origUnderlayOffsetY = _textMat.GetFloat("_UnderlayOffsetY");
                    _underlayKeywordOnBefore = _textMat.IsKeywordEnabled("UNDERLAY_ON");
                }
            }
        }
    }

    void Start()
    {
        if (_mat != null)
        {
            if (_hasMainColor) _origMainColor = _mat.GetColor(_mainColorProp);
            if (_hasEmission)
            {
                _origEmissionColor  = _mat.GetColor(_emissionProp);
                _hadEmissionKeyword = _mat.IsKeywordEnabled("_EMISSION");
            }
        }

        if (ledLight != null)
        {
            _origLightColor     = ledLight.color;
            _origLightIntensity = ledLight.intensity;
        }
    }

    public void PlayCue(float durationSec) => PlayCueForDialogue(triggerDialogueIndex, durationSec);

    public void PlayCueForDialogue(int dialogueIndex, float durationSec)
    {
        if (dialogueIndex != triggerDialogueIndex) return;

        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(CueRoutine(durationSec));
    }

    IEnumerator CueRoutine(float duration)
    {
        if (_mat != null)
        {
            if (_hasMainColor)
                SafeSet(_mat, _mainColorProp, cueColor);

            if (_hasEmission)
            {
                if (forceEnableEmission) _mat.EnableKeyword("_EMISSION");
                Color em = cueColor * Mathf.LinearToGammaSpace(cueEmissionIntensity);
                SafeSet(_mat, _emissionProp, em);
            }
        }

        if (ledLight != null)
        {
            ledLight.color     = cueColor;
            ledLight.intensity = _origLightIntensity * Mathf.Max(0.01f, lightIntensityMultiplier);
        }

        if (_textMat != null && noeText != null)
        {
            Color hdr = textHighlightColor * Mathf.Max(1f, textHDRMultiplier);
            noeText.color = hdr;
            if (_hasFaceColorProp)
            {
                Color faceMatHDR = hdr; faceMatHDR.a = _origFaceMatColor.a;
                _textMat.SetColor("_FaceColor", faceMatHDR);
            }

            if (useOutline && _hasOutline)
            {
                _textMat.SetColor("_OutlineColor", textHighlightColor);
                _textMat.SetFloat("_OutlineWidth", outlineWidthOnCue);
            }

            if (useUnderlay && _hasUnderlay)
            {
                _textMat.EnableKeyword("UNDERLAY_ON");
                _textMat.SetColor("_UnderlayColor", textHighlightColor);
                _textMat.SetFloat("_UnderlaySoftness", underlaySoftnessOnCue);
                _textMat.SetFloat("_UnderlayOffsetX", 0f);
                _textMat.SetFloat("_UnderlayOffsetY", 0f);
            }

            RefreshTMP(noeText);
        }

        if (duration > 0f) yield return new WaitForSeconds(duration);

        if (_textMat != null && noeText != null)
        {
            noeText.color = _origFaceVertexColor;
            if (_hasFaceColorProp)
            {
                _textMat.SetColor("_FaceColor", _origFaceMatColor);
            }

            if (_hasOutline)
            {
                _textMat.SetColor("_OutlineColor", _origOutlineColor);
                _textMat.SetFloat("_OutlineWidth", _origOutlineWidth);
            }

            if (_hasUnderlay)
            {
                _textMat.SetColor("_UnderlayColor", _origUnderlayColor);
                _textMat.SetFloat("_UnderlaySoftness", _origUnderlaySoftness);
                _textMat.SetFloat("_UnderlayOffsetX", _origUnderlayOffsetX);
                _textMat.SetFloat("_UnderlayOffsetY", _origUnderlayOffsetY);

                if (!_underlayKeywordOnBefore && _origUnderlaySoftness <= 0.0001f)
                    _textMat.DisableKeyword("UNDERLAY_ON");
            }

            RefreshTMP(noeText);
        }

        if (_mat != null)
        {
            if (_hasMainColor)
                SafeSet(_mat, _mainColorProp, _origMainColor);

            if (_hasEmission)
            {
                SafeSet(_mat, _emissionProp, _origEmissionColor);
                if ((!_hadEmissionKeyword) || _origEmissionColor.maxColorComponent <= 0.0001f)
                    _mat.DisableKeyword("_EMISSION");
            }
        }

        if (ledLight != null)
        {
            ledLight.color     = _origLightColor;
            ledLight.intensity = _origLightIntensity;
        }

        _runner = null;
    }

    void RefreshTMP(TMP_Text t)
    {
        t.UpdateMeshPadding();
        t.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }

    void SafeSet(Material m, string prop, Color c)
    {
        try
        {
            if (m != null && m.HasProperty(prop))
                m.SetColor(prop, c);
        }
        catch { }
    }
}
