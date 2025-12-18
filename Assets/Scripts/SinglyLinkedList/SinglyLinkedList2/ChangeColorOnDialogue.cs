using System.Collections;
using UnityEngine;

public class ChangeColorOnDialogue : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int triggerDialogueIndex = 3;
    public int revertDialogueIndex = 4;
    public float changeDelay = 0.5f;
    public Renderer targetRenderer;
    public Light targetLight;
    public Material customMaterial;
    public bool useUnscaledTime = true;
    public bool onlyOnce = true;
    public GameObject[] objectsToHideOnRevert;

    bool triggered;
    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock mpb;

    Material snapshotSingleMaterial;
    Material[] snapshotMaterialsArray;
    bool snapshotHadRenderer;
    bool snapshotUsedPropertyBlock;

    struct LightState { public Color c; public float i; public float r; public bool e; public bool a; }
    LightState snapshotLight;
    bool snapshotHadLight;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
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
        if (index == triggerDialogueIndex)
        {
            if (onlyOnce && triggered) return;
            TakeSnapshot();
            StartCoroutine(ChangeToRed());
            return;
        }

        if (index == revertDialogueIndex + 1)
        {
            RevertAndHide();
        }
    }

    void TakeSnapshot()
    {
        snapshotHadRenderer = targetRenderer;
        if (snapshotHadRenderer)
        {
            snapshotSingleMaterial = targetRenderer.sharedMaterial;
            snapshotMaterialsArray = targetRenderer.sharedMaterials;
            snapshotUsedPropertyBlock = false;
        }

        snapshotHadLight = targetLight;
        if (snapshotHadLight)
        {
            snapshotLight = new LightState
            {
                c = targetLight.color,
                i = targetLight.intensity,
                r = targetLight.range,
                e = targetLight.enabled,
                a = targetLight.gameObject.activeSelf
            };
        }
    }

    void RevertAndHide()
    {
        if (snapshotHadRenderer && targetRenderer)
        {
            if (snapshotMaterialsArray != null && snapshotMaterialsArray.Length > 1)
                targetRenderer.sharedMaterials = snapshotMaterialsArray;
            else
                targetRenderer.sharedMaterial = snapshotSingleMaterial;

            targetRenderer.SetPropertyBlock(null);
            targetRenderer.gameObject.SetActive(true);
        }

        if (snapshotHadLight && targetLight)
        {
            targetLight.color = snapshotLight.c;
            targetLight.intensity = snapshotLight.i;
            targetLight.range = snapshotLight.r;
            targetLight.enabled = snapshotLight.e;
            targetLight.gameObject.SetActive(snapshotLight.a);
        }

        if (objectsToHideOnRevert != null)
        {
            for (int i = 0; i < objectsToHideOnRevert.Length; i++)
            {
                var go = objectsToHideOnRevert[i];
                if (go) go.SetActive(false);
            }
        }
    }

    IEnumerator ChangeToRed()
    {
        float t = 0f;
        float d = Mathf.Max(0f, changeDelay);
        while (t < d)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (targetRenderer)
        {
            if (customMaterial)
            {
                if (targetRenderer.sharedMaterials != null && targetRenderer.sharedMaterials.Length > 1)
                {
                    var mats = targetRenderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = customMaterial;
                    targetRenderer.sharedMaterials = mats;
                }
                else
                {
                    targetRenderer.sharedMaterial = customMaterial;
                }
            }
            else
            {
                Color baseColor = new Color(0.984f, 0.051f, 0.051f);
                Color emissionColor = new Color(245f / 255f, 3f / 255f, 3f / 255f);
                var r = targetRenderer;
                r.GetPropertyBlock(mpb);
                bool changed = false;
                if (r.sharedMaterial && r.sharedMaterial.HasProperty(PID_BaseColor)) { mpb.SetColor(PID_BaseColor, baseColor); changed = true; }
                else if (r.sharedMaterial && r.sharedMaterial.HasProperty(PID_Color)) { mpb.SetColor(PID_Color, baseColor); changed = true; }
                if (r.sharedMaterial && r.sharedMaterial.HasProperty(PID_EmissionColor)) { mpb.SetColor(PID_EmissionColor, emissionColor); changed = true; }
                r.SetPropertyBlock(mpb);
                snapshotUsedPropertyBlock = changed;
            }
        }

        if (targetLight)
        {
            targetLight.color = new Color(0.968f, 0.086f, 0.141f);
            targetLight.intensity = 2f;
            targetLight.range = 100f;
            targetLight.enabled = true;
        }

        triggered = true;
    }
}
