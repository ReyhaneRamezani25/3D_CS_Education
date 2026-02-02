using UnityEngine;

public class ExpandableButton : MonoBehaviour
{
    public GameObject subItemsPanel; // پنل حاوی دکمه‌های زیرمجموعه

    // این تابع با هر کلیک روی دکمه اصلی فراخوانی می‌شود
    public void ToggleSubItems()
    {
        // وضعیت فعال بودن پنل زیرمجموعه را تغییر می‌دهد
        subItemsPanel.SetActive(!subItemsPanel.activeSelf);
    }
}