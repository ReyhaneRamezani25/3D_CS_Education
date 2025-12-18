using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMultipleLightsOnDialogue : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int triggerDialogueIndex = 3;
    public int revertDialogueIndex = 4;
    public bool useUnscaledTime = true;

    [System.Serializable]
    public class TargetObject
    {
        public Renderer renderer;
        public Light light;
        public Color targetColor = Color.red;
        public float intensity = 2f;
        public float range = 50f;
        public float delay = 0.5f;
    }

    public List<TargetObject> objects = new List<TargetObject>();

    struct LightSnapshot { public Color c; public float i; public float r; public bool e; public bool a; }
    struct RendererSnapshot { public Material[] mats; public bool active; }

    LightSnapshot[] lightSnapshots;
    RendererSnapshot[] rendererSnapshots;

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
            TakeSnapshots();
            for (int i = 0; i < objects.Count; i++)
                StartCoroutine(ChangeOne(i));
        }

        if (index == revertDialogueIndex + 1)
        {
            RevertAll();
        }
    }

    void TakeSnapshots()
    {
        lightSnapshots = new LightSnapshot[objects.Count];
        rendererSnapshots = new RendererSnapshot[objects.Count];

        for (int i = 0; i < objects.Count; i++)
        {
            var t = objects[i];

            if (t.renderer)
            {
                rendererSnapshots[i] = new RendererSnapshot
                {
                    mats = t.renderer.sharedMaterials,
                    active = t.renderer.gameObject.activeSelf
                };
            }

            if (t.light)
            {
                lightSnapshots[i] = new LightSnapshot
                {
                    c = t.light.color,
                    i = t.light.intensity,
                    r = t.light.range,
                    e = t.light.enabled,
                    a = t.light.gameObject.activeSelf
                };
            }
        }
    }

    IEnumerator ChangeOne(int index)
    {
        var t = objects[index];
        float time = 0f;

        while (time < t.delay)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (t.renderer)
        {
            var mpb = new MaterialPropertyBlock();
            t.renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", t.targetColor);
            mpb.SetColor("_Color", t.targetColor);
            mpb.SetColor("_EmissionColor", t.targetColor);
            t.renderer.SetPropertyBlock(mpb);
        }

        if (t.light)
        {
            t.light.color = t.targetColor;
            t.light.intensity = t.intensity;
            t.light.range = t.range;
            t.light.enabled = true;
        }
    }

    void RevertAll()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            var t = objects[i];

            if (t.renderer)
            {
                t.renderer.sharedMaterials = rendererSnapshots[i].mats;
                t.renderer.gameObject.SetActive(rendererSnapshots[i].active);
                t.renderer.SetPropertyBlock(null);
            }

            if (t.light)
            {
                t.light.color = lightSnapshots[i].c;
                t.light.intensity = lightSnapshots[i].i;
                t.light.range = lightSnapshots[i].r;
                t.light.enabled = lightSnapshots[i].e;
                t.light.gameObject.SetActive(lightSnapshots[i].a);
            }
        }
    }
}
