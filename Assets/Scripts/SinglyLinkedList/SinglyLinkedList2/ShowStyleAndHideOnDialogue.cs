using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ShowStyleThenRevertOnHide : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;

    [Header("Timing")]
    public int dialogueIndex = 3;
    public float applyDelayFromDialogueStart = 0f;
    public float revertDelayFromDialogueStart = 2f;
    public bool useUnscaledTime = true;

    [Header("Target")]
    public GameObject target;
    public bool hideOnAwake = true;
    public bool deactivateTargetAfterRevert = false;
    public bool autoReactivateTargetOnApply = true;
    public bool respectExternalHide = true;

    [Header("Renderer Settings")]
    public Renderer targetRenderer;
    public bool overrideMaterialOnApply = false;
    public Material materialOnStyle;
    public bool applyColorWithPropertyBlock = true;
    public Color baseColorOnStyle = new Color(0.984f, 0.051f, 0.051f);
    public Color emissionColorOnStyle = new Color(245f / 255f, 3f / 255f, 3f / 255f);

    [Header("Light Settings")]
    public Light targetLight;
    public bool applyLightChanges = true;
    public Color lightColorOnStyle = new Color(0.968f, 0.086f, 0.141f);
    public float lightIntensityOnStyle = 2f;
    public float lightRangeOnStyle = 100f;
    public int framesToForceLightOnAfterRevert = 3;

    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock mpb;

    Material[] snapshotMaterials;
    Color snapshotBaseColor;
    Color snapshotEmissionColor;
    bool hasRendererSnapshot;

    Color lightColorBackup;
    float lightIntensityBackup;
    float lightRangeBackup;
    bool lightEnabledBackup;
    bool hasLightSnapshot;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        if (hideOnAwake && target) target.SetActive(false);
        if (targetLight && hideOnAwake) targetLight.enabled = false;
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
        if (index == dialogueIndex) StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        float d1 = Mathf.Max(0f, applyDelayFromDialogueStart);
        float d2 = Mathf.Max(0f, revertDelayFromDialogueStart - d1);

        if (d1 > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(d1);
            else yield return new WaitForSeconds(d1);
        }

        TakeSnapshot();

        if (autoReactivateTargetOnApply)
        {
            if (target && !target.activeInHierarchy) target.SetActive(true);
            if (targetLight && target) targetLight.enabled = true;
        }

        ApplyStyle();

        if (d2 > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(d2);
            else yield return new WaitForSeconds(d2);
        }

        yield return StartCoroutine(RevertSnapshotAndKeepLightOn());

        if (deactivateTargetAfterRevert && target)
        {
            target.SetActive(false);
            if (targetLight) targetLight.enabled = false;
        }
    }

    void TakeSnapshot()
    {
        hasRendererSnapshot = false;
        if (targetRenderer)
        {
            snapshotMaterials = targetRenderer.sharedMaterials;
            var mat = targetRenderer.sharedMaterial;
            if (mat)
            {
                if (mat.HasProperty(PID_BaseColor)) snapshotBaseColor = mat.GetColor(PID_BaseColor);
                else if (mat.HasProperty(PID_Color)) snapshotBaseColor = mat.GetColor(PID_Color);
                if (mat.HasProperty(PID_EmissionColor)) snapshotEmissionColor = mat.GetColor(PID_EmissionColor);
            }
            hasRendererSnapshot = true;
        }

        hasLightSnapshot = false;
        if (targetLight)
        {
            lightColorBackup = targetLight.color;
            lightIntensityBackup = targetLight.intensity;
            lightRangeBackup = targetLight.range;
            lightEnabledBackup = targetLight.enabled;
            hasLightSnapshot = true;
        }
    }

    void ApplyStyle()
    {
        if (targetRenderer)
        {
            if (overrideMaterialOnApply && materialOnStyle)
            {
                var count = Mathf.Max(1, targetRenderer.sharedMaterials.Length);
                var mats = new Material[count];
                for (int i = 0; i < count; i++) mats[i] = materialOnStyle;
                targetRenderer.sharedMaterials = mats;
                targetRenderer.SetPropertyBlock(null);
            }
            else if (applyColorWithPropertyBlock)
            {
                var r = targetRenderer;
                r.GetPropertyBlock(mpb);
                var mat = r.sharedMaterial;
                if (mat)
                {
                    if (mat.HasProperty(PID_BaseColor)) mpb.SetColor(PID_BaseColor, baseColorOnStyle);
                    else if (mat.HasProperty(PID_Color)) mpb.SetColor(PID_Color, baseColorOnStyle);
                    if (mat.HasProperty(PID_EmissionColor)) mpb.SetColor(PID_EmissionColor, emissionColorOnStyle);
                }
                r.SetPropertyBlock(mpb);
            }
        }

        if (applyLightChanges && targetLight)
        {
            targetLight.color = lightColorOnStyle;
            targetLight.intensity = lightIntensityOnStyle;
            targetLight.range = lightRangeOnStyle;
            targetLight.enabled = true;
        }
    }

    IEnumerator RevertSnapshotAndKeepLightOn()
    {
        if (hasRendererSnapshot && targetRenderer)
        {
            if (snapshotMaterials != null && snapshotMaterials.Length > 0)
                targetRenderer.sharedMaterials = snapshotMaterials;

            targetRenderer.SetPropertyBlock(null);

            var mat = targetRenderer.sharedMaterial;
            if (mat)
            {
                if (mat.HasProperty(PID_BaseColor)) mat.SetColor(PID_BaseColor, snapshotBaseColor);
                else if (mat.HasProperty(PID_Color)) mat.SetColor(PID_Color, snapshotBaseColor);
                if (mat.HasProperty(PID_EmissionColor)) mat.SetColor(PID_EmissionColor, snapshotEmissionColor);
            }
        }

        if (hasLightSnapshot && targetLight)
        {
            targetLight.color = lightColorBackup;
            targetLight.intensity = lightIntensityBackup;
            targetLight.range = lightRangeBackup;
        }

        if (deactivateTargetAfterRevert) yield break;

        int frames = Mathf.Max(1, framesToForceLightOnAfterRevert);
        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForEndOfFrame();
            if (respectExternalHide)
            {
                if (target && !target.activeInHierarchy) yield break;
            }
            if (targetLight && target && target.activeInHierarchy && !targetLight.enabled)
                targetLight.enabled = true;
        }
    }
}
