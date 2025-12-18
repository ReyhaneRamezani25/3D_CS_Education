using System.Collections.Generic;

public class QuizPayload
{
    public List<MCQ> questions;
    public int questionCount;
    public bool shuffle;
}

public static class QuizTransfer
{
    public static QuizPayload Payload;

    public static void Set(List<MCQ> questions, int questionCount, bool shuffle)
    {
        Payload = new QuizPayload {
            questions = questions,
            questionCount = questionCount,
            shuffle = shuffle
        };
    }

    public static void Clear() => Payload = null;
}
