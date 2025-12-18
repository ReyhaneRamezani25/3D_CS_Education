using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class QuizSceneManager : MonoBehaviour
{
    public Image imgQuestion;

    public TextMeshProUGUI txtProgress;
    public TextMeshProUGUI txtFeedback;

    public ToggleGroup optionsGroup;
    public Toggle[] optionToggles;

    public string choicePartSeparator = "\n";

    public Color optionOnBgColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color optionOffBgColor = Color.white;
    public Color correctBgColor = Color.green;
    public Color wrongBgColor = Color.red;

    public Button btnSubmit;
    public Button btnNext;

    public TextMeshProUGUI txtFinalTitle;
    public TextMeshProUGUI txtScoreValue;
    public Button btnRestart;
    public Image imgEnd;

    private List<MCQ> _questions;
    private int _total;
    private int _idx;
    private int _score;
    private int _correctIndexCurrent;
    private bool _answered;
    private bool _finished;

    void Start()
    {
        if (QuizTransfer.Payload == null ||
            QuizTransfer.Payload.questions == null ||
            QuizTransfer.Payload.questions.Count == 0) { Debug.LogError("No payload"); return; }

        if (optionToggles == null || optionToggles.Length != 4) { Debug.LogError("Need 4 toggles"); return; }
        if (btnSubmit == null) { Debug.LogError("btnSubmit missing"); return; }

        if (imgQuestion) imgQuestion.raycastTarget = false;
        if (imgEnd) imgEnd.raycastTarget = false;
        if (txtFinalTitle) txtFinalTitle.raycastTarget = false;
        if (txtScoreValue) txtScoreValue.raycastTarget = false;

        btnSubmit.onClick.RemoveAllListeners();
        btnSubmit.onClick.AddListener(Submit);

        if (btnNext != null)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(NextQuestion);
        }

        if (btnRestart != null)
        {
            btnRestart.onClick.RemoveAllListeners();
            btnRestart.onClick.AddListener(RestartQuiz);
            btnRestart.gameObject.SetActive(false);
        }

        if (txtFinalTitle) txtFinalTitle.gameObject.SetActive(false);
        if (txtScoreValue) txtScoreValue.gameObject.SetActive(false);
        if (imgEnd) imgEnd.gameObject.SetActive(false);

        if (optionsGroup != null) optionsGroup.allowSwitchOff = true;

        foreach (var tg in optionToggles)
        {
            tg.onValueChanged.RemoveAllListeners();
            if (tg.group != optionsGroup) tg.group = optionsGroup;
            tg.transition = Selectable.Transition.None;
            var nav = tg.navigation; nav.mode = Navigation.Mode.None; tg.navigation = nav;
        }

        for (int i = 0; i < optionToggles.Length; i++)
        {
            int idx = i;
            optionToggles[i].onValueChanged.AddListener(_ =>
            {
                if (_finished) return;
                if (_answered) return;
                RefreshOptionBgColors(false);
            });
        }

        _questions = QuizTransfer.Payload.questions;
        _total = Mathf.Clamp(QuizTransfer.Payload.questionCount, 1, _questions.Count);

        _idx = 0;
        _score = 0;
        _finished = false;

        ShowQuestion();
    }

    void RefreshOptionBgColors(bool forceAllOff)
    {
        for (int i = 0; i < optionToggles.Length; i++)
        {
            var t = optionToggles[i];
            if (t == null) continue;
            var g = t.targetGraphic as Graphic;
            if (g == null) continue;

            if (forceAllOff) g.color = optionOffBgColor;
            else g.color = t.isOn ? optionOnBgColor : optionOffBgColor;
        }
    }

    void ShowQuestion()
    {
        _answered = false;

        if (txtFinalTitle) txtFinalTitle.gameObject.SetActive(false);
        if (txtScoreValue) txtScoreValue.gameObject.SetActive(false);
        if (btnRestart) btnRestart.gameObject.SetActive(false);
        if (imgEnd) imgEnd.gameObject.SetActive(false);

        if (imgQuestion) imgQuestion.gameObject.SetActive(true);
        foreach (var t in optionToggles) t.gameObject.SetActive(true);
        if (btnSubmit) btnSubmit.gameObject.SetActive(true);
        if (btnNext) btnNext.gameObject.SetActive(true);
        if (txtProgress) txtProgress.gameObject.SetActive(true);
        if (txtFeedback) txtFeedback.gameObject.SetActive(true);

        btnSubmit.interactable = true;
        if (btnNext) btnNext.interactable = false;

        if (_idx >= _total)
        {
            _finished = true;
            EndQuiz();
            return;
        }

        var q = _questions[_idx];
        if (q == null || q.choices == null || q.choices.Length != 4)
        {
            _idx++;
            ShowQuestion();
            return;
        }

        if (imgQuestion)
        {
            imgQuestion.sprite = q.questionImage;
            imgQuestion.preserveAspect = true;
            imgQuestion.gameObject.SetActive(q.questionImage != null);
        }

        if (txtProgress) txtProgress.text = $"{_idx + 1}/{_total}";
        if (txtFeedback) txtFeedback.text = "";

        int[] map = { 0, 1, 2, 3 };
        map = map.OrderBy(_ => Random.value).ToArray();
        int safeCorrect = Mathf.Clamp(q.correctIndex, 0, 3);
        _correctIndexCurrent = System.Array.IndexOf(map, safeCorrect);

        for (int i = 0; i < 4; i++)
        {
            var txt = optionToggles[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = q.choices[map[i]].Combine(choicePartSeparator);

            optionToggles[i].SetIsOnWithoutNotify(false);
            optionToggles[i].interactable = true;
        }

        RefreshOptionBgColors(true);
    }

    void ColorResultBackgrounds(int chosen, bool noSelection, bool correct)
    {
        RefreshOptionBgColors(true);

        if (noSelection)
        {
            var g = optionToggles[_correctIndexCurrent].targetGraphic as Graphic;
            if (g != null) g.color = correctBgColor;
            return;
        }

        if (correct)
        {
            var g = optionToggles[chosen].targetGraphic as Graphic;
            if (g != null) g.color = correctBgColor;
        }
        else
        {
            var gc = optionToggles[_correctIndexCurrent].targetGraphic as Graphic;
            if (gc != null) gc.color = correctBgColor;

            var gw = optionToggles[chosen].targetGraphic as Graphic;
            if (gw != null) gw.color = wrongBgColor;
        }
    }

    void EndQuiz()
    {
        if (imgQuestion) imgQuestion.gameObject.SetActive(false);
        foreach (var t in optionToggles) t.gameObject.SetActive(false);
        if (btnSubmit) btnSubmit.gameObject.SetActive(false);
        if (btnNext) btnNext.gameObject.SetActive(false);
        if (txtFeedback) txtFeedback.gameObject.SetActive(false);
        if (txtProgress) txtProgress.gameObject.SetActive(false);

        if (txtFinalTitle) txtFinalTitle.gameObject.SetActive(true);
        if (txtScoreValue)
        {
            int maxScore = _total * 10;
            int finalScore = _score;
            txtScoreValue.text = $"{finalScore} از {maxScore}";
            txtScoreValue.gameObject.SetActive(true);
        }

        if (btnRestart) btnRestart.gameObject.SetActive(true);
        if (imgEnd) imgEnd.gameObject.SetActive(true);
    }

    void Submit()
    {
        if (_finished) return;

        int chosen = GetChosenIndex();
        bool noSelection = (chosen == -1);
        bool correct = (!noSelection && chosen == _correctIndexCurrent);

        for (int i = 0; i < optionToggles.Length; i++)
        {
            optionToggles[i].transition = Selectable.Transition.None;
            var cb = optionToggles[i].colors;
            cb.disabledColor = cb.normalColor;
            optionToggles[i].colors = cb;
        }

        if (noSelection)
        {
            if (txtFeedback) txtFeedback.text = "بدون جواب!";
        }
        else if (correct)
        {
            if (txtFeedback) txtFeedback.text = "درست";
            _score += 10;
        }
        else
        {
            if (txtFeedback) txtFeedback.text = "اشتباه!";
        }

        ColorResultBackgrounds(chosen, noSelection, correct);

        foreach (var t in optionToggles) t.interactable = false;
        btnSubmit.interactable = false;
        if (btnNext) btnNext.interactable = true;

        _answered = true;
    }

    int GetChosenIndex()
    {
        for (int i = 0; i < optionToggles.Length; i++)
            if (optionToggles[i] != null && optionToggles[i].isOn)
                return i;
        return -1;
    }

    public void NextQuestion()
    {
        if (_finished) return;

        if (!_answered)
        {
            if (txtFeedback) txtFeedback.text = "اول پاسخ بده!";
            return;
        }

        _idx++;
        ShowQuestion();
    }

    public void RestartQuiz()
    {
        _idx = 0;
        _score = 0;
        _finished = false;

        if (txtFinalTitle) txtFinalTitle.gameObject.SetActive(false);
        if (txtScoreValue) txtScoreValue.gameObject.SetActive(false);
        if (btnRestart) btnRestart.gameObject.SetActive(false);
        if (imgEnd) imgEnd.gameObject.SetActive(false);

        ShowQuestion();
    }
}
