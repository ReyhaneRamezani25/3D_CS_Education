using UnityEngine;
using TMPro;

public class RtlTest : MonoBehaviour
{
    public TextMeshProUGUI txt;

    void Start()
    {
        string text = "سلام!\nاین یک تست چندخطی است.\nهر خط باید از راست شروع شود.";

        // Option 1: RLE-based fixing (most accurate)
        txt.text = RtlHelpers.FixMultilineRLE(text);

        // Option 2 (alternative):
        // txt.text = RtlHelpers.FixMultilineRLM(text);

        txt.alignment = TextAlignmentOptions.TopRight;
    }
}
