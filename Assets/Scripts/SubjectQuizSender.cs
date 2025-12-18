using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public class SubjectQuizSender : MonoBehaviour
{
    public MCQ[] subjectQuestions;
    public int questionCount = 8;
    public bool shuffle = true;
    public string quizSceneName = "QuizScene";

    public void SendToQuiz()
    {
        if (subjectQuestions == null || subjectQuestions.Length == 0)
        {
            Debug.LogError("[SubjectQuizSender] No questions assigned.");
            return;
        }

        var safeList = new List<MCQ>(subjectQuestions.Length);

        foreach (var q in subjectQuestions)
        {
            if (q == null) continue;

            if (q.choices == null || q.choices.Length != 4)
            {
                var fixedChoices = new ChoiceParts[4];
                if (q.choices != null)
                {
                    for (int i = 0; i < Mathf.Min(q.choices.Length, 4); i++)
                        fixedChoices[i] = q.choices[i];
                }
                q.choices = fixedChoices;
            }

            if (q.optionFontSizes == null || q.optionFontSizes.Length != 4)
            {
                var fixedSizes = new float[4];
                if (q.optionFontSizes != null)
                {
                    for (int i = 0; i < Mathf.Min(q.optionFontSizes.Length, 4); i++)
                        fixedSizes[i] = Mathf.Max(0f, q.optionFontSizes[i]);
                }
                q.optionFontSizes = fixedSizes;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    q.optionFontSizes[i] = Mathf.Max(0f, q.optionFontSizes[i]);
            }

            q.correctIndex = Mathf.Clamp(q.correctIndex, 0, 3);

            if (q.questionImage == null)
            {
                Debug.LogWarning("[SubjectQuizSender] A question has no questionImage assigned. It will render as empty image.");
            }

            q.hint = q.hint ?? string.Empty;

            safeList.Add(q);
        }

        if (safeList.Count == 0)
        {
            Debug.LogError("[SubjectQuizSender] No valid questions after sanitization.");
            return;
        }

        if (shuffle) safeList = safeList.OrderBy(_ => Random.value).ToList();

        int count = Mathf.Clamp(questionCount, 1, safeList.Count);
        var finalList = safeList.Take(count).ToList();

        QuizTransfer.Set(finalList, count, shuffle);

        SceneHistory.PushCurrent();
        SceneManager.LoadScene(quizSceneName);
    }
}
