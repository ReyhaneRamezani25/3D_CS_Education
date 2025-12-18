using UnityEngine;
using UnityEngine.UI;

public class ToggleGroupBackgroundStyler : MonoBehaviour
{
    public ToggleGroup group;
    public Toggle[] toggles;
    public Color onColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color offColor = Color.white;
    public bool forceTransitionNone = true;
    public bool allowSwitchOff = false;
    public bool resetAllOffOnEnable = true;

    void Awake()
    {
        if (group == null) group = GetComponentInChildren<ToggleGroup>(true);
        if (toggles == null || toggles.Length == 0) toggles = GetComponentsInChildren<Toggle>(true);

        if (group != null) group.allowSwitchOff = allowSwitchOff;

        for (int i = 0; i < toggles.Length; i++)
        {
            var t = toggles[i];
            if (t == null) continue;

            if (t.group != group) t.group = group;
            t.interactable = true;

            if (forceTransitionNone) t.transition = Selectable.Transition.None;
            else
            {
                var cb = t.colors;
                cb.disabledColor = cb.normalColor;
                t.colors = cb;
            }

            t.onValueChanged.AddListener(_ => RefreshAll());
        }
    }

    void OnEnable()
    {
        // مهم: از اول همه خاموش باشن تا «اولی آبی» نشه
        if (resetAllOffOnEnable && toggles != null)
        {
            foreach (var t in toggles) if (t != null) t.SetIsOnWithoutNotify(false);
            if (!allowSwitchOff && toggles.Length > 0 && toggles[0] != null)
                toggles[0].isOn = true; // اگر همیشه باید یکی روشن باشد
        }
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (toggles == null) return;

        for (int i = 0; i < toggles.Length; i++)
        {
            var t = toggles[i];
            if (t == null) continue;

            var g = t.targetGraphic; // فقط پس‌زمینه‌ی خودِ تاگل
            if (g != null) g.color = t.isOn ? onColor : offColor;

            // اطمینان از قابل‌انتخاب بودن
            t.interactable = true;
            t.enabled = true;
        }
    }
}
