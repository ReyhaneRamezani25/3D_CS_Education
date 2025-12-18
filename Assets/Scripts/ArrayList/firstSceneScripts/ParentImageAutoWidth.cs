using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class ParentImageAutoWidth : MonoBehaviour
{
    [Header("Source TMP (optional)")]
    [Tooltip("If left empty, the first active child TMP will be used.")]
    public TextMeshProUGUI sourceText;

    [Header("Resize Target (defaults to this RectTransform)")]
    public RectTransform targetRect;

    [Header("Outer Width Settings (Image parent)")]
    public float padding = 24f;
    public float minWidth = 60f;
    public float maxWidth = 1600f;

    [Header("Lock Left Edge")]
    public bool lockLeftEdge = true;
    public float leftMargin = 40f;

    [Header("Inner TMP Padding (inside Image)")]
    public float innerLeftPadding = 24f;
    public float innerRightPadding = 24f;

    [Header("Visibility")]
    public bool hideWhenNoDialogue = true;

    [Header("Update Mode")]
    public bool continuousUpdate = true;

    [Header("Measuring")]
    [Tooltip("Safe measurement: temporarily disables Wrap and sets Overflow to Overflow to measure true single-line width.")]
    public bool measureIgnoringWrap = true;
    [Tooltip("For multiline text, also checks the width of the longest line (line extents).")]
    public bool useMaxLineWidth = true;

    [Header("Layout Override")]
    [Tooltip("If parent/ancestors have LayoutGroup or ContentSizeFitter, enable this to set LayoutElement.ignoreLayout = true on the Image.")]
    public bool detachFromLayout = true;

    [Header("Debug")]
    public bool debugLogs = false;

    private UnityEngine.UI.Image _image;
    private CanvasGroup _cg;
    private LayoutElement _layoutElement;

    void Reset()
    {
        targetRect = GetComponent<RectTransform>();
        _image = GetComponent<UnityEngine.UI.Image>();
        _cg = GetComponent<CanvasGroup>();
        _layoutElement = GetComponent<LayoutElement>();

        if (sourceText == null)
        {
            var tmp = GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp) sourceText = tmp;
        }
        EnsureLeftAnchors();
    }

    void Awake()
    {
        if (targetRect == null) targetRect = GetComponent<RectTransform>();
        if (_image == null) _image = GetComponent<UnityEngine.UI.Image>();
        if (_cg == null) _cg = GetComponent<CanvasGroup>();
        if (_layoutElement == null) _layoutElement = GetComponent<LayoutElement>();
        if (lockLeftEdge) EnsureLeftAnchors();
    }

    void OnEnable() => Refresh();

    void LateUpdate()
    {
        if (continuousUpdate) Refresh();
    }

    [ContextMenu("Refresh Now")]
    public void Refresh()
    {
        var tmp = ResolveActiveTMP();
        bool hasActiveDialogue = tmp != null;

        ToggleVisibility(hasActiveDialogue);
        if (!hasActiveDialogue) return;

        if (detachFromLayout && HasLayoutControllersAbove(targetRect))
        {
            if (_layoutElement == null) _layoutElement = gameObject.AddComponent<LayoutElement>();
            _layoutElement.ignoreLayout = true;
        }

        Canvas.ForceUpdateCanvases();
        tmp.enableAutoSizing = false;

        bool oldWrap = tmp.enableWordWrapping;
        var oldOverflow = tmp.overflowMode;

        if (measureIgnoringWrap)
        {
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        tmp.ForceMeshUpdate();

        float width = 0f;

        if (useMaxLineWidth && tmp.textInfo != null && tmp.textInfo.lineCount > 0)
        {
            for (int i = 0; i < tmp.textInfo.lineCount; i++)
            {
                var ext = tmp.textInfo.lineInfo[i].lineExtents;
                float lineW = ext.max.x - ext.min.x;
                if (lineW > width) width = lineW;
            }
        }

        if (width <= 0f)
        {
            width = tmp.GetPreferredValues(tmp.text, Mathf.Infinity, Mathf.Infinity).x;
        }

        if (width <= 0f && tmp.textInfo != null && tmp.textInfo.characterCount > 0)
        {
            var first = tmp.textInfo.characterInfo[0];
            var last  = tmp.textInfo.characterInfo[tmp.textInfo.characterCount - 1];
            width = (last.topRight.x - first.bottomLeft.x);
        }

        if (measureIgnoringWrap)
        {
            tmp.enableWordWrapping = oldWrap;
            tmp.overflowMode = oldOverflow;
        }

        width = Mathf.Max(0f, width);
        float newWidth = Mathf.Clamp(width + padding, minWidth, maxWidth);

        if (lockLeftEdge)
        {
            EnsureLeftAnchors();
            var p = targetRect.anchoredPosition;
            targetRect.anchoredPosition = new Vector2(leftMargin, p.y);
        }

        targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

        ApplyInnerTMPPadding(tmp);

        if (debugLogs)
        {
            Debug.Log($"[ParentImageAutoWidth] TMP='{tmp.name}', lines={tmp.textInfo?.lineCount}, measured={width:F1}, final={newWidth:F1}, wrap={tmp.enableWordWrapping}, overflow={tmp.overflowMode}");
        }
    }

    public void SetTextOnActive(string raw)
    {
        var tmp = ResolveActiveTMP();
        if (tmp == null) return;
        tmp.text = raw ?? string.Empty;
        Refresh();
    }

    private TextMeshProUGUI ResolveActiveTMP()
    {
        if (sourceText != null && sourceText.gameObject.activeInHierarchy)
            return sourceText;

        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
            if (tmps[i].gameObject.activeInHierarchy)
                return tmps[i];

        return null;
    }

    private void EnsureLeftAnchors()
    {
        if (targetRect == null) return;
        var am = targetRect.anchorMin; var ax = targetRect.anchorMax; var pv = targetRect.pivot;
        am.x = 0f; ax.x = 0f; pv.x = 0f;
        targetRect.anchorMin = am;
        targetRect.anchorMax = ax;
        targetRect.pivot     = pv;
    }

    private void ApplyInnerTMPPadding(TextMeshProUGUI tmp)
    {
        var tr = tmp.rectTransform;

        var am = tr.anchorMin; var ax = tr.anchorMax; var pv = tr.pivot;
        am.x = 0f; ax.x = 1f; pv.x = 0f;
        tr.anchorMin = am;  tr.anchorMax = ax;  tr.pivot = pv;

        var offMin = tr.offsetMin;
        var offMax = tr.offsetMax;
        offMin.x = innerLeftPadding;
        offMax.x = -innerRightPadding;
        tr.offsetMin = offMin;  tr.offsetMax = offMax;
    }

    private void ToggleVisibility(bool visible)
    {
        if (_image != null) _image.enabled = visible;

        if (_cg != null)
        {
            _cg.alpha = visible ? 1f : 0f;
            _cg.interactable = visible;
            _cg.blocksRaycasts = visible;
        }
    }

    private bool HasLayoutControllersAbove(RectTransform rt)
    {
        if (rt == null) return false;
        Transform p = rt.parent;
        while (p != null)
        {
            if (p.GetComponent<LayoutGroup>() != null) return true;
            if (p.GetComponent<ContentSizeFitter>() != null) return true;
            p = p.parent;
        }
        return false;
    }
}
