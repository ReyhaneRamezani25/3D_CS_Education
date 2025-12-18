using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueVisualController : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;

    [Header("Visibility Group")]
    public GameObject ledObject;
    public GameObject otherObject;
    public bool startHidden = true;
    public int visibilityDialogueIndex = 1;
    public float ledShowDelay = 0.5f;
    public float ledHideDelay = 1.0f;
    public float otherShowDelay = 0.5f;
    public float otherHideDelay = 1.0f;

    [Header("Color Group")]
    public int colorDialogueIndex = 2;
    public float colorChangeDelay = 0.5f;
    public int colorRevertDialogueIndex = 3;
    public float colorRevertDelay = 0.5f;

    [System.Serializable]
    public class ColorTarget
    {
        public Renderer renderer;
        public Light pointLight;
        public Color targetColor = Color.red;
    }

    public List<ColorTarget> colorTargets = new List<ColorTarget>();

    Color[] originalMainColors;
    Color[] originalEmissionColors;
    bool[] hasMainBaseProperty;
    bool[] hasMainColorProperty;
    bool[] hasEmissionProperty;
    Color[] originalLightColors;

    bool colorSnapshotsTaken;

    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        if (startHidden)
        {
            if (ledObject) ledObject.SetActive(false);
            if (otherObject) otherObject.SetActive(false);
        }
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
        if (index == visibilityDialogueIndex)
        {
            if (ledObject)
                StartCoroutine(ShowHideObject(ledObject, ledShowDelay, ledHideDelay));
            if (otherObject)
                StartCoroutine(ShowHideObject(otherObject, otherShowDelay, otherHideDelay));
        }

        if (index == colorDialogueIndex)
        {
            if (!colorSnapshotsTaken)
                TakeColorSnapshots();

            StartCoroutine(ChangeColors());
        }

        if (index == colorRevertDialogueIndex)
        {
            if (colorSnapshotsTaken)
                StartCoroutine(RevertColors());
        }
    }

    IEnumerator ShowHideObject(GameObject obj, float showDelay, float hideDelay)
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        obj.SetActive(true);

        if (hideDelay > 0f)
            yield return new WaitForSeconds(hideDelay);

        obj.SetActive(false);
    }

    void TakeColorSnapshots()
    {
        int count = colorTargets.Count;

        originalMainColors = new Color[count];
        originalEmissionColors = new Color[count];
        hasMainBaseProperty = new bool[count];
        hasMainColorProperty = new bool[count];
        hasEmissionProperty = new bool[count];
        originalLightColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            var t = colorTargets[i];

            if (t.renderer && t.renderer.sharedMaterial)
            {
                Material mat = t.renderer.sharedMaterial;

                if (mat.HasProperty(PID_BaseColor))
                {
                    originalMainColors[i] = mat.GetColor(PID_BaseColor);
                    hasMainBaseProperty[i] = true;
                }
                else if (mat.HasProperty(PID_Color))
                {
                    originalMainColors[i] = mat.GetColor(PID_Color);
                    hasMainColorProperty[i] = true;
                }

                if (mat.HasProperty(PID_EmissionColor))
                {
                    originalEmissionColors[i] = mat.GetColor(PID_EmissionColor);
                    hasEmissionProperty[i] = true;
                }
            }

            if (t.pointLight)
            {
                originalLightColors[i] = t.pointLight.color;
            }
        }

        colorSnapshotsTaken = true;
    }

    IEnumerator ChangeColors()
    {
        if (colorChangeDelay > 0f)
            yield return new WaitForSeconds(colorChangeDelay);

        for (int i = 0; i < colorTargets.Count; i++)
        {
            var t = colorTargets[i];

            if (t.renderer && t.renderer.sharedMaterial)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                t.renderer.GetPropertyBlock(block);

                Material mat = t.renderer.sharedMaterial;

                if (mat.HasProperty(PID_BaseColor))
                    block.SetColor(PID_BaseColor, t.targetColor);
                if (mat.HasProperty(PID_Color))
                    block.SetColor(PID_Color, t.targetColor);
                if (mat.HasProperty(PID_EmissionColor))
                    block.SetColor(PID_EmissionColor, t.targetColor);

                t.renderer.SetPropertyBlock(block);
            }

            if (t.pointLight)
            {
                t.pointLight.color = t.targetColor;
            }
        }
    }

    IEnumerator RevertColors()
    {
        if (colorRevertDelay > 0f)
            yield return new WaitForSeconds(colorRevertDelay);

        for (int i = 0; i < colorTargets.Count; i++)
        {
            var t = colorTargets[i];

            if (t.renderer && t.renderer.sharedMaterial)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                t.renderer.GetPropertyBlock(block);

                Material mat = t.renderer.sharedMaterial;

                if (hasMainBaseProperty[i] && mat.HasProperty(PID_BaseColor))
                    block.SetColor(PID_BaseColor, originalMainColors[i]);
                if (hasMainColorProperty[i] && mat.HasProperty(PID_Color))
                    block.SetColor(PID_Color, originalMainColors[i]);
                if (hasEmissionProperty[i] && mat.HasProperty(PID_EmissionColor))
                    block.SetColor(PID_EmissionColor, originalEmissionColors[i]);

                t.renderer.SetPropertyBlock(block);
            }

            if (t.pointLight)
            {
                t.pointLight.color = originalLightColors[i];
            }
        }
    }
}
