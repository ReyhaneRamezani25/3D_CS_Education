using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TwoCubeItem
{
    public Renderer[] cubeRenderers;
    public Light[] pointLights;
    public GameObject rootObject;
    public TMP_Text label;
}

public class PairColorSequence : MonoBehaviour
{
    [SerializeField] private DialogueVoiceControllerBasic controller;
    [SerializeField] private int triggerDialogueIndex = 6;
    [SerializeField] private bool oneBasedIndex = false;

    [Header("Items")]
    [SerializeField] private TwoCubeItem[] items;

    [Header("Arrows")]
    [SerializeField] private Renderer[] arrows;
    [SerializeField] private Light[] arrowLights;
    [SerializeField] private GameObject[] arrowRoots;

    [Header("Colors")]
    [SerializeField] private Color blueColor = Color.blue;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color arrowActiveColor = Color.white;

    [Header("Lights")]
    [SerializeField] private float cubeLightIntensity = 5f;
    [SerializeField] private float arrowLightIntensity = 5f;

    [Header("Timing")]
    [SerializeField] private float delayFromStart = 0.5f;
    [SerializeField] private float pairHoldDuration = 2f;
    [SerializeField] private float betweenPairsDelay = 0.2f;

    [Header("Show/Hide Objects")]
    [SerializeField] private GameObject[] objectsToShowAndHide;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private float objectShowDelay = 0.5f;
    [SerializeField] private float objectVisibleDuration = 3f;
    [SerializeField] private bool hideAfterSequence = true;
    [SerializeField] private int earlyGroupCount = 3;
    [SerializeField] private float secondGroupExtraDelay = 0.3f;

    [Header("Labels")]
    [SerializeField] private string blueText = "knows this";
    [SerializeField] private string redText = "this";

    [SerializeField] private bool debugApplyNow = false;

    private static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PID_Color = Shader.PropertyToID("_Color");
    private static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");

    private Color[][] itemLightOrigColors;
    private float[][] itemLightOrigIntensities;
    private bool[][] itemLightOrigEnabled;

    private Color[] arrowLightOrigColors;
    private float[] arrowLightOrigIntensities;
    private bool[] arrowLightOrigEnabled;

    private bool[] origActiveStates;
    private Coroutine sequenceRoutine;
    private Coroutine showHideRoutine;
    private MaterialPropertyBlock mpb;

    private string[] itemOrigTexts;
    private bool[] itemLabelOrigActive;
    private Color[] itemLabelOrigColors;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        Prepare();
        if (debugApplyNow)
        {
            sequenceRoutine = StartCoroutine(RunSequence());
            showHideRoutine = StartCoroutine(ShowHideObjectsFlow());
        }
    }

    private void OnEnable()
    {
        if (controller == null) controller = GetComponent<DialogueVoiceControllerBasic>();
        if (controller != null) controller.OnDialogueStart += OnDialogueStart;
    }

    private void OnDisable()
    {
        if (controller != null) controller.OnDialogueStart -= OnDialogueStart;
        if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
        if (showHideRoutine != null) StopCoroutine(showHideRoutine);
        RevertAll();
        RestoreOriginalActiveStates();
    }

    private void OnDialogueStart(int index)
    {
        int normalizedIndex = oneBasedIndex ? (index + 1) : index;
        if (normalizedIndex != triggerDialogueIndex) return;

        if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
        if (showHideRoutine != null) StopCoroutine(showHideRoutine);

        sequenceRoutine = StartCoroutine(RunSequence());
        showHideRoutine = StartCoroutine(ShowHideObjectsFlow());
    }

    private void Prepare()
    {
        if (items == null) items = new TwoCubeItem[0];

        itemLightOrigColors = new Color[items.Length][];
        itemLightOrigIntensities = new float[items.Length][];
        itemLightOrigEnabled = new bool[items.Length][];

        itemOrigTexts = new string[items.Length];
        itemLabelOrigActive = new bool[items.Length];
        itemLabelOrigColors = new Color[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            int lc = it.pointLights != null ? it.pointLights.Length : 0;
            itemLightOrigColors[i] = new Color[lc];
            itemLightOrigIntensities[i] = new float[lc];
            itemLightOrigEnabled[i] = new bool[lc];
            for (int l = 0; l < lc; l++)
            {
                var L = it.pointLights[l];
                if (L == null) continue;
                itemLightOrigColors[i][l] = L.color;
                itemLightOrigIntensities[i][l] = L.intensity;
                itemLightOrigEnabled[i][l] = L.enabled;
            }

            if (it.label != null)
            {
                itemOrigTexts[i] = it.label.text;
                itemLabelOrigActive[i] = it.label.gameObject.activeSelf;
                itemLabelOrigColors[i] = it.label.color;
            }
            else
            {
                itemOrigTexts[i] = null;
                itemLabelOrigActive[i] = false;
                itemLabelOrigColors[i] = Color.white;
            }
        }

        if (arrowLights == null) arrowLights = new Light[0];
        arrowLightOrigColors = new Color[arrowLights.Length];
        arrowLightOrigIntensities = new float[arrowLights.Length];
        arrowLightOrigEnabled = new bool[arrowLights.Length];
        for (int i = 0; i < arrowLights.Length; i++)
        {
            var L = arrowLights[i];
            if (L == null) continue;
            arrowLightOrigColors[i] = L.color;
            arrowLightOrigIntensities[i] = L.intensity;
            arrowLightOrigEnabled[i] = L.enabled;
        }

        if (objectsToShowAndHide == null) objectsToShowAndHide = new GameObject[0];
        origActiveStates = new bool[objectsToShowAndHide.Length];
        for (int i = 0; i < objectsToShowAndHide.Length; i++)
        {
            var go = objectsToShowAndHide[i];
            origActiveStates[i] = go != null && go.activeSelf;
            if (startHidden && go != null) go.SetActive(false);
        }
    }

    private IEnumerator RunSequence()
    {
        if (delayFromStart > 0f) yield return new WaitForSecondsRealtime(delayFromStart);

        if (items.Length < 2) { sequenceRoutine = null; yield break; }
        int lastPairIndex = items.Length - 2;

        for (int i = 0; i <= lastPairIndex; i++)
        {
            if (i > 0)
            {
                RevertPair(i - 1, i - 1 + 1);
                if (betweenPairsDelay > 0f) yield return new WaitForSecondsRealtime(betweenPairsDelay);
            }

            ApplyPair(i, i + 1, i);
            if (pairHoldDuration > 0f) yield return new WaitForSecondsRealtime(pairHoldDuration);
        }

        RevertPair(lastPairIndex, lastPairIndex + 1);
        sequenceRoutine = null;
    }

    private IEnumerator ShowHideObjectsFlow()
    {
        if (objectShowDelay > 0f) yield return new WaitForSecondsRealtime(objectShowDelay);

        float latestShow = 0f;
        for (int i = 0; i < objectsToShowAndHide.Length; i++)
        {
            var go = objectsToShowAndHide[i];
            if (go == null) continue;

            float extra = (i < earlyGroupCount) ? 0f : Mathf.Max(0f, secondGroupExtraDelay);
            float showAt = objectShowDelay + extra;
            latestShow = Mathf.Max(latestShow, showAt);
            StartCoroutine(ShowAfterDelay(go, showAt));
        }

        float globalHideAt = latestShow + Mathf.Max(0f, objectVisibleDuration);
        if (globalHideAt > 0f) yield return new WaitForSecondsRealtime(globalHideAt);

        if (hideAfterSequence)
        {
            for (int i = 0; i < objectsToShowAndHide.Length; i++)
                if (objectsToShowAndHide[i] != null) objectsToShowAndHide[i].SetActive(false);
        }

        showHideRoutine = null;
    }

    private IEnumerator ShowAfterDelay(GameObject go, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        go.SetActive(true);
    }

    private void ApplyPair(int a, int b, int arrowIndex)
    {
        if (a < 0 || b < 0 || a >= items.Length || b >= items.Length) return;

        ApplyItem(a, blueColor, blueText);
        ApplyItem(b, redColor, redText);

        ApplyArrowColor(arrowIndex, arrowActiveColor);
        if (arrowRoots != null && arrowIndex >= 0 && arrowIndex < arrowRoots.Length && arrowRoots[arrowIndex] != null)
            arrowRoots[arrowIndex].SetActive(true);
    }

    private void ApplyItem(int index, Color color, string labelText)
    {
        ApplyColorToItem(index, color);
        ApplyLightToItem(index, color);
        var t = items[index].label;
        if (t != null)
        {
            t.gameObject.SetActive(true);
            t.enabled = true;
            var c = t.color; c.a = 1f; t.color = c;
            t.text = labelText;
            var cg = t.GetComponentInParent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            var cv = t.GetComponentInParent<Canvas>();
            if (cv != null)
            {
                cv.enabled = true;
                if (cv.renderMode == RenderMode.WorldSpace && cv.worldCamera == null) cv.worldCamera = Camera.main;
            }
            t.ForceMeshUpdate(true);
        }
    }

    private void RevertPair(int a, int b)
    {
        RevertItem(a);
        RevertItem(b);
        RevertArrow(a);
        if (arrowRoots != null && a >= 0 && a < arrowRoots.Length && arrowRoots[a] != null)
            arrowRoots[a].SetActive(false);
    }

    private void ApplyColorToItem(int index, Color c)
    {
        if (index < 0 || index >= items.Length) return;
        var it = items[index];
        if (it.cubeRenderers != null)
        {
            foreach (var rend in it.cubeRenderers)
            {
                if (rend == null || rend.sharedMaterial == null) continue;
                rend.GetPropertyBlock(mpb);
                if (rend.sharedMaterial.HasProperty(PID_BaseColor)) mpb.SetColor(PID_BaseColor, c);
                else if (rend.sharedMaterial.HasProperty(PID_Color)) mpb.SetColor(PID_Color, c);
                if (rend.sharedMaterial.HasProperty(PID_EmissionColor)) mpb.SetColor(PID_EmissionColor, c * 0.5f);
                rend.SetPropertyBlock(mpb);
            }
        }
        if (it.rootObject != null) it.rootObject.SetActive(true);
    }

    private void ApplyLightToItem(int index, Color c)
    {
        if (index < 0 || index >= items.Length) return;
        var it = items[index];
        if (it.pointLights == null) return;
        foreach (var L in it.pointLights)
        {
            if (L == null) continue;
            L.color = c;
            L.intensity = cubeLightIntensity;
            L.enabled = true;
        }
    }

    private void ApplyArrowColor(int index, Color c)
    {
        if (arrows != null && index >= 0 && index < arrows.Length)
        {
            var r = arrows[index];
            if (r != null && r.sharedMaterial != null)
            {
                r.GetPropertyBlock(mpb);
                if (r.sharedMaterial.HasProperty(PID_BaseColor)) mpb.SetColor(PID_BaseColor, c);
                else if (r.sharedMaterial.HasProperty(PID_Color)) mpb.SetColor(PID_Color, c);
                if (r.sharedMaterial.HasProperty(PID_EmissionColor)) mpb.SetColor(PID_EmissionColor, c * 0.5f);
                r.SetPropertyBlock(mpb);
            }
        }

        if (arrowLights != null && index >= 0 && index < arrowLights.Length)
        {
            var L = arrowLights[index];
            if (L != null)
            {
                L.color = c;
                L.intensity = arrowLightIntensity;
                L.enabled = true;
            }
        }
    }

    private void RevertItem(int index)
    {
        if (index < 0 || index >= items.Length) return;
        var it = items[index];

        if (it.cubeRenderers != null)
        {
            foreach (var rend in it.cubeRenderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(mpb);
                mpb.Clear();
                rend.SetPropertyBlock(mpb);
            }
        }

        if (it.pointLights != null)
        {
            for (int l = 0; l < it.pointLights.Length; l++)
            {
                var L = it.pointLights[l];
                if (L == null) continue;
                L.color = itemLightOrigColors[index][l];
                L.intensity = itemLightOrigIntensities[index][l];
                L.enabled = itemLightOrigEnabled[index][l];
            }
        }

        if (it.label != null)
        {
            it.label.text = itemOrigTexts[index] ?? "";
            it.label.color = itemLabelOrigColors[index];
            it.label.gameObject.SetActive(itemLabelOrigActive[index]);
            it.label.enabled = itemLabelOrigActive[index];
            it.label.ForceMeshUpdate(true);
        }
    }

    private void RevertArrow(int index)
    {
        if (arrows != null && index >= 0 && index < arrows.Length)
        {
            var r = arrows[index];
            if (r != null)
            {
                r.GetPropertyBlock(mpb);
                mpb.Clear();
                r.SetPropertyBlock(mpb);
            }
        }

        if (arrowLights != null && index >= 0 && index < arrowLights.Length)
        {
            var L = arrowLights[index];
            if (L != null)
            {
                L.color = arrowLightOrigColors[index];
                L.intensity = arrowLightOrigIntensities[index];
                L.enabled = arrowLightOrigEnabled[index];
            }
        }
    }

    private void RevertAll()
    {
        for (int i = 0; i < items.Length; i++) RevertItem(i);
        for (int i = 0; i < arrows.Length; i++) RevertArrow(i);

        if (hideAfterSequence)
        {
            for (int i = 0; i < objectsToShowAndHide.Length; i++)
                if (objectsToShowAndHide[i] != null) objectsToShowAndHide[i].SetActive(false);
            if (arrowRoots != null)
                for (int i = 0; i < arrowRoots.Length; i++)
                    if (arrowRoots[i] != null) arrowRoots[i].SetActive(false);
        }
    }

    private void RestoreOriginalActiveStates()
    {
        for (int i = 0; i < objectsToShowAndHide.Length; i++)
            if (objectsToShowAndHide[i] != null) objectsToShowAndHide[i].SetActive(origActiveStates[i]);
    }
}
