using UnityEngine;

[System.Serializable]
public struct ChoiceParts
{
    [TextArea] public string part1;
    [TextArea] public string part2;

    public string Combine(string separator = "\n")
    {
        string p1 = string.IsNullOrWhiteSpace(part1) ? "" : part1.Trim();
        string p2 = string.IsNullOrWhiteSpace(part2) ? "" : part2.Trim();

        if (string.IsNullOrEmpty(p1) && string.IsNullOrEmpty(p2)) return "";
        if (string.IsNullOrEmpty(p2)) return p1;
        if (string.IsNullOrEmpty(p1)) return p2;
        return p1 + separator + p2;
    }
}


[System.Serializable]
public class MCQ
{
    [Header("Question Image")]
    public Sprite questionImage; // سوال به صورت تصویر

    [Header("Choices (each has 2 parts)")]
    public ChoiceParts[] choices = new ChoiceParts[4];

    [Header("Choice Font Sizes (per option)")]
    [Tooltip("سایز فونت ۴ گزینه. مقدار 0 یعنی از پیش‌فرض صحنه استفاده کن.")]
    public float[] optionFontSizes = new float[4]; // NEW

    [Range(0, 3)] public int correctIndex = 0;

    [TextArea] public string hint;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ایمن‌سازی طول آرایه‌ی گزینه‌ها
        if (choices == null || choices.Length != 4)
        {
            var tmp = new ChoiceParts[4];
            if (choices != null)
            {
                for (int i = 0; i < Mathf.Min(choices.Length, 4); i++)
                    tmp[i] = choices[i];
            }
            choices = tmp;
        }

        // ایمن‌سازی طول آرایه‌ی سایز فونت
        if (optionFontSizes == null || optionFontSizes.Length != 4)
        {
            var tmpSizes = new float[4];
            if (optionFontSizes != null)
            {
                for (int i = 0; i < Mathf.Min(optionFontSizes.Length, 4); i++)
                    tmpSizes[i] = Mathf.Max(0f, optionFontSizes[i]);
            }
            optionFontSizes = tmpSizes;
        }
        else
        {
            for (int i = 0; i < 4; i++)
                optionFontSizes[i] = Mathf.Max(0f, optionFontSizes[i]); // منفی نباشد
        }

        correctIndex = Mathf.Clamp(correctIndex, 0, 3);
        hint = hint ?? string.Empty;
    }
#endif
}
