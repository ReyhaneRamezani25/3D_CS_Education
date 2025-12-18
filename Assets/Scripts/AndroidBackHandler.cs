using UnityEngine;

public class BackTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("✅ Back/Escape detected!");
        }
    }
}
