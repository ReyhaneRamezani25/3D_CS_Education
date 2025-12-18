using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSyncOnDialogue : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int applyDialogueIndex = 1;
    public float applyDelay = 0f;
    public int revertDialogueIndex = 2;
    public float revertDelay = 0f;
    public bool useUnscaledTime = true;

    public Light pointLight;
    public bool disableLightOnApply = true;

    [System.Serializable]
    public class Pair
    {
        public Renderer source;
        public int sourceMaterialIndex = 0;
        public Renderer target;
        public int targetMaterialIndex = -1;
        public bool copyBaseColor = true;
        public bool copyEmissionColor = true;
        public bool replaceMaterial = false;
    }

    public Pair[] pairs;

    public bool enforceWhileApplied = true;

    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");
    static readonly int PID_EmissiveColor = Shader.PropertyToID("_EmissiveColor");

    readonly Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();
    readonly Dictionary<Renderer, Color[]> originalBase = new Dictionary<Renderer, Color[]>();
    readonly Dictionary<Renderer, Color[]> originalEmiss = new Dictionary<Renderer, Color[]>();
    bool originalLightEnabled;
    bool isApplied;

    void Awake()
    {
        SnapshotTargets();
        if (pointLight) originalLightEnabled = pointLight.enabled;
    }

    void OnEnable()
    {
        if (dialogue != null) dialogue.OnDialogueStart += OnDialogueStart;
    }

    void OnDisable()
    {
        if (dialogue != null) dialogue.OnDialogueStart -= OnDialogueStart;
    }

    void LateUpdate()
    {
        if (!isApplied || !enforceWhileApplied) return;
        ForceApplyOnce();
    }

    void OnDialogueStart(int index)
    {
        if (index == applyDialogueIndex) StartCoroutine(ApplyRoutine(applyDelay));
        if (index == revertDialogueIndex) StartCoroutine(RevertRoutine(revertDelay));
    }

    IEnumerator ApplyRoutine(float delay)
    {
        if (delay > 0f)
        {
            float t = 0f;
            while (t < delay) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        }
        ApplyNow();
    }

    IEnumerator RevertRoutine(float delay)
    {
        if (delay > 0f)
        {
            float t = 0f;
            while (t < delay) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        }
        RevertNow();
    }

    [ContextMenu("Apply Now")]
    public void ApplyNow()
    {
        isApplied = true;
        ForceApplyOnce();
        if (pointLight && disableLightOnApply) pointLight.enabled = false;
    }

    void ForceApplyOnce()
    {
        if (pairs == null) return;

        for (int i = 0; i < pairs.Length; i++)
        {
            var p = pairs[i];
            if (p == null || p.source == null || p.target == null) continue;

            var srcShared = p.source.sharedMaterials;
            if (srcShared == null || srcShared.Length == 0) continue;
            int sIdx = Mathf.Clamp(p.sourceMaterialIndex, 0, srcShared.Length - 1);
            var src = srcShared[sIdx];
            if (src == null) continue;

            var dst = p.target.materials;
            if (dst == null || dst.Length == 0) continue;

            if (p.replaceMaterial)
            {
                if (p.targetMaterialIndex >= 0 && p.targetMaterialIndex < dst.Length)
                {
                    var inst = GetOrClone(dst[p.targetMaterialIndex], src);
                    dst[p.targetMaterialIndex] = inst;
                    p.target.materials = dst;
                }
                else
                {
                    for (int m = 0; m < dst.Length; m++)
                        dst[m] = GetOrClone(dst[m], src);
                    p.target.materials = dst;
                }
            }
            else
            {
                Color srcBase, srcEmiss;
                bool hasSrcBase = TryGetBaseColor(src, out srcBase);
                bool hasSrcEmiss = TryGetEmissionColor(src, out srcEmiss);

                if (p.targetMaterialIndex >= 0 && p.targetMaterialIndex < dst.Length)
                {
                    ApplyToMat(dst[p.targetMaterialIndex], p.copyBaseColor && hasSrcBase, srcBase, p.copyEmissionColor && hasSrcEmiss, srcEmiss);
                }
                else
                {
                    for (int m = 0; m < dst.Length; m++)
                        ApplyToMat(dst[m], p.copyBaseColor && hasSrcBase, srcBase, p.copyEmissionColor && hasSrcEmiss, srcEmiss);
                }
            }
        }
    }

    [ContextMenu("Revert Now")]
    public void RevertNow()
    {
        isApplied = false;

        foreach (var kv in originalMats)
        {
            var r = kv.Key;
            if (r == null) continue;
            if (kv.Value != null && kv.Value.Length > 0) r.materials = CloneArray(kv.Value);
        }

        foreach (var kv in originalBase)
        {
            var r = kv.Key;
            if (r == null) continue;
            var mats = r.materials;
            var arr = kv.Value;
            for (int i = 0; i < mats.Length && i < arr.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                if (m.HasProperty(PID_BaseColor)) m.SetColor(PID_BaseColor, arr[i]);
                else if (m.HasProperty(PID_Color)) m.SetColor(PID_Color, arr[i]);
            }
        }

        foreach (var kv in originalEmiss)
        {
            var r = kv.Key;
            if (r == null) continue;
            var mats = r.materials;
            var arr = kv.Value;
            for (int i = 0; i < mats.Length && i < arr.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                if (m.HasProperty(PID_EmissionColor)) m.SetColor(PID_EmissionColor, arr[i]);
                else if (m.HasProperty(PID_EmissiveColor)) m.SetColor(PID_EmissiveColor, arr[i]);
            }
        }

        if (pointLight) pointLight.enabled = originalLightEnabled;
    }

    [ContextMenu("Snapshot Targets")]
    public void SnapshotTargets()
    {
        originalMats.Clear();
        originalBase.Clear();
        originalEmiss.Clear();

        if (pairs == null) return;
        for (int i = 0; i < pairs.Length; i++)
        {
            var p = pairs[i];
            if (p == null || p.target == null) continue;

            var mats = p.target.materials;
            if (mats == null || mats.Length == 0) continue;

            originalMats[p.target] = CloneArray(mats);

            var bases = new Color[mats.Length];
            var emiss = new Color[mats.Length];

            for (int m = 0; m < mats.Length; m++)
            {
                var mm = mats[m];
                if (mm == null) continue;

                Color bc = Color.white;
                if (mm.HasProperty(PID_BaseColor)) bc = mm.GetColor(PID_BaseColor);
                else if (mm.HasProperty(PID_Color)) bc = mm.GetColor(PID_Color);
                bases[m] = bc;

                Color ec = Color.black;
                if (mm.HasProperty(PID_EmissionColor)) ec = mm.GetColor(PID_EmissionColor);
                else if (mm.HasProperty(PID_EmissiveColor)) ec = mm.GetColor(PID_EmissiveColor);
                emiss[m] = ec;
            }

            originalBase[p.target] = bases;
            originalEmiss[p.target] = emiss;
        }
    }

    Material GetOrClone(Material dst, Material src)
    {
        if (dst != null && dst.shader == src.shader) return dst;
        return new Material(src);
    }

    void ApplyToMat(Material m, bool setBase, Color baseCol, bool setEmiss, Color emissCol)
    {
        if (m == null) return;
        if (setBase)
        {
            if (m.HasProperty(PID_BaseColor)) m.SetColor(PID_BaseColor, baseCol);
            else if (m.HasProperty(PID_Color)) m.SetColor(PID_Color, baseCol);
        }
        if (setEmiss)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty(PID_EmissionColor)) m.SetColor(PID_EmissionColor, emissCol);
            else if (m.HasProperty(PID_EmissiveColor)) m.SetColor(PID_EmissiveColor, emissCol);
        }
    }

    bool TryGetBaseColor(Material m, out Color c)
    {
        if (m == null) { c = default; return false; }
        if (m.HasProperty(PID_BaseColor)) { c = m.GetColor(PID_BaseColor); return true; }
        if (m.HasProperty(PID_Color)) { c = m.GetColor(PID_Color); return true; }
        c = default;
        return false;
    }

    bool TryGetEmissionColor(Material m, out Color c)
    {
        if (m == null) { c = default; return false; }
        if (m.HasProperty(PID_EmissionColor)) { c = m.GetColor(PID_EmissionColor); return true; }
        if (m.HasProperty(PID_EmissiveColor)) { c = m.GetColor(PID_EmissiveColor); return true; }
        c = default;
        return false;
    }

    Material[] CloneArray(Material[] arr)
    {
        if (arr == null) return null;
        var outArr = new Material[arr.Length];
        for (int i = 0; i < arr.Length; i++) outArr[i] = arr[i];
        return outArr;
    }
}
