using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueColorController : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;

    public int changeDialogueIndex = 1;
    public int revertDialogueIndex = 2;

    public float changeDelay = 0.5f;
    public float revertDelay = 0.5f;

    [System.Serializable]
    public class ColorTarget
    {
        public Renderer renderer;
        public Light pointLight;
        public Color targetColor = Color.red;
    }

    public List<ColorTarget> targets = new List<ColorTarget>();

    Color[] originalMainColors;
    Color[] originalEmissionColors;
    Color[] originalLightColors;

    bool[] hasBaseColor;
    bool[] hasColor;
    bool[] hasEmission;

    bool snapshotTaken;

    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");

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
        if (index == changeDialogueIndex)
        {
            if (!snapshotTaken)
                TakeSnapshots();

            StartCoroutine(ChangeColors());
        }

        if (index == revertDialogueIndex && snapshotTaken)
        {
            StartCoroutine(RevertColors());
        }
    }

    void TakeSnapshots()
    {
        int count = targets.Count;

        originalMainColors = new Color[count];
        originalEmissionColors = new Color[count];
        originalLightColors = new Color[count];

        hasBaseColor = new bool[count];
        hasColor = new bool[count];
        hasEmission = new bool[count];

        for (int i = 0; i < count; i++)
        {
            var t = targets[i];

            if (t.renderer && t.renderer.sharedMaterial)
            {
                var mat = t.renderer.sharedMaterial;

                if (mat.HasProperty(PID_BaseColor))
                {
                    originalMainColors[i] = mat.GetColor(PID_BaseColor);
                    hasBaseColor[i] = true;
                }
                else if (mat.HasProperty(PID_Color))
                {
                    originalMainColors[i] = mat.GetColor(PID_Color);
                    hasColor[i] = true;
                }

                if (mat.HasProperty(PID_EmissionColor))
                {
                    originalEmissionColors[i] = mat.GetColor(PID_EmissionColor);
                    hasEmission[i] = true;
                }
            }

            if (t.pointLight)
                originalLightColors[i] = t.pointLight.color;
        }

        snapshotTaken = true;
    }

    IEnumerator ChangeColors()
    {
        if (changeDelay > 0f)
            yield return new WaitForSeconds(changeDelay);

        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];

            if (t.renderer && t.renderer.sharedMaterial)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                t.renderer.GetPropertyBlock(block);
                var mat = t.renderer.sharedMaterial;

                if (mat.HasProperty(PID_BaseColor))
                    block.SetColor(PID_BaseColor, t.targetColor);
                if (mat.HasProperty(PID_Color))
                    block.SetColor(PID_Color, t.targetColor);
                if (mat.HasProperty(PID_EmissionColor))
                    block.SetColor(PID_EmissionColor, t.targetColor);

                t.renderer.SetPropertyBlock(block);
            }

            if (t.pointLight)
                t.pointLight.color = t.targetColor;
        }
    }

    IEnumerator RevertColors()
    {
        if (revertDelay > 0f)
            yield return new WaitForSeconds(revertDelay);

        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];

            if (t.renderer && t.renderer.sharedMaterial)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                t.renderer.GetPropertyBlock(block);
                var mat = t.renderer.sharedMaterial;

                if (hasBaseColor[i] && mat.HasProperty(PID_BaseColor))
                    block.SetColor(PID_BaseColor, originalMainColors[i]);
                if (hasColor[i] && mat.HasProperty(PID_Color))
                    block.SetColor(PID_Color, originalMainColors[i]);
                if (hasEmission[i] && mat.HasProperty(PID_EmissionColor))
                    block.SetColor(PID_EmissionColor, originalEmissionColors[i]);

                t.renderer.SetPropertyBlock(block);
            }

            if (t.pointLight)
                t.pointLight.color = originalLightColors[i];
        }
    }
}
