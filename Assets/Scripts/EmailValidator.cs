using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginValidatorLegacy : MonoBehaviour
{
    [SerializeField] private InputField emailInput;
    [SerializeField] private InputField passwordInput;

    [SerializeField] private TMP_Text emailErrorText;
    [SerializeField] private TMP_Text passwordErrorText;
    [SerializeField] private TMP_Text bothErrorText;

    [SerializeField] private string successSceneName;

    private static readonly Regex EmailRegex = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled
    );

    void Start()
    {
        emailErrorText.gameObject.SetActive(false);
        passwordErrorText.gameObject.SetActive(false);
        bothErrorText.gameObject.SetActive(false);
    }

    public void OnButtonClick()
    {
        var email = emailInput.text.Trim();
        var password = passwordInput.text;

        bool emailValid = EmailRegex.IsMatch(email);
        bool passwordValid = !string.IsNullOrEmpty(password);

        emailErrorText.gameObject.SetActive(false);
        passwordErrorText.gameObject.SetActive(false);
        bothErrorText.gameObject.SetActive(false);

        if (!emailValid && !passwordValid)
        {
            bothErrorText.gameObject.SetActive(true);
            return;
        }

        if (!emailValid)
        {
            emailErrorText.gameObject.SetActive(true);
            return;
        }

        if (!passwordValid)
        {
            passwordErrorText.gameObject.SetActive(true);
            return;
        }

        SceneManager.LoadScene(successSceneName);
    }
}
