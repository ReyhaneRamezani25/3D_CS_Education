using System.Collections.Generic;
using UnityEngine;

public class SceneSnapshotRestorer : MonoBehaviour
{
    [System.Serializable]
    public class DialogueResetTarget
    {
        [Header("Reset after dialogue ends")]
        public int dialogueIndexToResetAfterEnd = -1;

        [Header("Restore color/material at dialogue start")]
        public int dialogueIndexToRestoreMaterialAtStart = -1;

        [Header("Lights on full reset")]
        public bool restoreLightsOnReset = false;
        public bool forceLightsEnabledOnReset = false;

        [Header("Active state management")]
        public bool manageActiveState = false;

        public List<GameObject> targetObjects = new List<GameObject>();
        public List<GameObject> forceHideObjects = new List<GameObject>();
    }

    public DialogueVoiceControllerBasic dialogue;
    public List<DialogueResetTarget> resetGroups = new List<DialogueResetTarget>();

    struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool activeSelf;
        public bool wasInactiveInHierarchy;
    }

    struct LightState
    {
        public Color color;
        public float intensity;
        public float range;
        public bool enabled;
    }

    struct RendererState
    {
        public Material[] materials;
        public Color baseColor;
        public Color emissionColor;
    }

    Dictionary<GameObject, TransformState> transformSnapshots = new Dictionary<GameObject, TransformState>();
    Dictionary<Light, LightState> lightSnapshots = new Dictionary<Light, LightState>();
    Dictionary<Renderer, RendererState> rendererSnapshots = new Dictionary<Renderer, RendererState>();

    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock mpb;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        TakeSceneSnapshot();
    }

    void OnEnable()
    {
        if (dialogue)
        {
            dialogue.OnDialogueStart += OnDialogueStart;
            dialogue.OnDialogueEnd += OnDialogueEnd;
        }
    }

    void OnDisable()
    {
        if (dialogue)
        {
            dialogue.OnDialogueStart -= OnDialogueStart;
            dialogue.OnDialogueEnd -= OnDialogueEnd;
        }
    }

    void TakeSceneSnapshot()
    {
        transformSnapshots.Clear();
        lightSnapshots.Clear();
        rendererSnapshots.Clear();

        foreach (var group in resetGroups)
        {
            foreach (var root in group.targetObjects)
            {
                if (!root) continue;

                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    if (!go || transformSnapshots.ContainsKey(go)) continue;

                    transformSnapshots[go] = new TransformState
                    {
                        position = t.position,
                        rotation = t.rotation,
                        scale = t.localScale,
                        activeSelf = go.activeSelf,
                        wasInactiveInHierarchy = !go.activeInHierarchy
                    };

                    var l = go.GetComponent<Light>();
                    if (l && !lightSnapshots.ContainsKey(l))
                    {
                        lightSnapshots[l] = new LightState
                        {
                            color = l.color,
                            intensity = l.intensity,
                            range = l.range,
                            enabled = l.enabled
                        };
                    }

                    var r = go.GetComponent<Renderer>();
                    if (r && !rendererSnapshots.ContainsKey(r))
                    {
                        var mat = r.sharedMaterial;
                        var state = new RendererState
                        {
                            materials = r.sharedMaterials,
                            baseColor = mat && mat.HasProperty(PID_BaseColor)
                                ? mat.GetColor(PID_BaseColor)
                                : (mat && mat.HasProperty(PID_Color)
                                    ? mat.GetColor(PID_Color)
                                    : Color.white),
                            emissionColor = mat && mat.HasProperty(PID_EmissionColor)
                                ? mat.GetColor(PID_EmissionColor)
                                : Color.black
                        };
                        rendererSnapshots[r] = state;
                    }
                }
            }
        }
    }

    void OnDialogueStart(int index)
    {
        foreach (var group in resetGroups)
        {
            if (group.dialogueIndexToRestoreMaterialAtStart == index)
            {
                RestoreMaterialsOnly(group);
            }
        }
    }

    void OnDialogueEnd(int index)
    {
        foreach (var group in resetGroups)
        {
            if (group.dialogueIndexToResetAfterEnd == index)
            {
                RestoreSnapshot(group);
            }
        }
    }

    void RestoreSnapshot(DialogueResetTarget group)
    {
        var toActivate = new List<GameObject>();
        var toDeactivate = new List<GameObject>();

        foreach (var root in group.targetObjects)
        {
            if (!root) continue;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                if (!go) continue;

                if (transformSnapshots.TryGetValue(go, out var tState))
                {
                    t.position = tState.position;
                    t.rotation = tState.rotation;
                    t.localScale = tState.scale;

                    if (group.manageActiveState)
                    {
                        bool shouldBeActive = tState.activeSelf && !tState.wasInactiveInHierarchy;
                        if (shouldBeActive) toActivate.Add(go); else toDeactivate.Add(go);
                    }
                }

                var l = go.GetComponent<Light>();
                if (l && group.restoreLightsOnReset && lightSnapshots.TryGetValue(l, out var lState))
                {
                    l.color = lState.color;
                    l.intensity = lState.intensity;
                    l.range = lState.range;
                    if (group.forceLightsEnabledOnReset) l.enabled = true;
                    // مهم: دیگر l.enabled را از اسنپ‌شات برنمی‌گردانیم تا خاموش نشود
                }

                var r = go.GetComponent<Renderer>();
                if (r && rendererSnapshots.TryGetValue(r, out var rState))
                {
                    r.sharedMaterials = rState.materials;
                    r.SetPropertyBlock(null);
                }
            }
        }

        if (group.manageActiveState)
        {
            foreach (var go in toActivate) if (go) go.SetActive(true);
            foreach (var go in toDeactivate) if (go) go.SetActive(false);
        }

        foreach (var go in group.forceHideObjects)
        {
            if (go) go.SetActive(false);
        }
    }

    void RestoreMaterialsOnly(DialogueResetTarget group)
    {
        foreach (var root in group.targetObjects)
        {
            if (!root) continue;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r || !rendererSnapshots.TryGetValue(r, out var state)) continue;
                r.sharedMaterials = state.materials;
                r.SetPropertyBlock(null);
            }
        }
    }
}
