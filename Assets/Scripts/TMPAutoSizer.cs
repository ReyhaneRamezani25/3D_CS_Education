using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPAutoWidth : MonoBehaviour
{
    [Header("Width Settings")]
    public float padding = 20f;
    public float minWidth = 50f;
    public float maxWidth = 1000f;

    private TextMeshProUGUI _tmp;
    private RectTransform _rt;
    private string _lastText;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        _rt = GetComponent<RectTransform>();
    }

    void Start()
    {
        RefreshWidth();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
            if (_rt == null) _rt = GetComponent<RectTransform>();
            RefreshWidth();
        }
    }
#endif

    void Update()
    {
        if (_tmp != null && _tmp.text != _lastText)
        {
            RefreshWidth();
            _lastText = _tmp.text;
        }
    }

    [ContextMenu("Refresh Width")]
    public void RefreshWidth()
    {
        if (_tmp == null || _rt == null) return;

        _tmp.enableAutoSizing = false;
        _tmp.ForceMeshUpdate();

        float newWidth = 0f;

        if (_tmp.textInfo != null && _tmp.textInfo.characterCount > 0)
        {
            var firstChar = _tmp.textInfo.characterInfo[0];
            var lastChar = _tmp.textInfo.characterInfo[_tmp.textInfo.characterCount - 1];
            newWidth = (lastChar.topRight.x - firstChar.bottomLeft.x) + padding;
        }

        if (newWidth <= 0f)
        {
            Vector2 prefSize = _tmp.GetPreferredValues(_tmp.text, Mathf.Infinity, Mathf.Infinity);
            newWidth = prefSize.x + padding;
        }

        newWidth = Mathf.Clamp(newWidth, minWidth, maxWidth);
        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }

    public void SetText(string raw)
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        _tmp.text = raw;
        RefreshWidth();
    }
}
