using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class SceneHistory
{
    private static readonly Stack<string> stack = new Stack<string>();

    public static void PushCurrent()
    {
        var s = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(s)) stack.Push(s);
    }

    public static bool CanGoBack => stack.Count > 0;

    public static void GoBack(string fallback = null)
    {
        if (stack.Count > 0)
        {
            var prev = stack.Pop();
            SceneManager.LoadScene(prev);
        }
        else if (!string.IsNullOrEmpty(fallback))
        {
            SceneManager.LoadScene(fallback);
        }
    }

    public static void Clear()
    {
        stack.Clear();
    }
}
