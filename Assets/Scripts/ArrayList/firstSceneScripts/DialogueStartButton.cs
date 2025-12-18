using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueStartButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the component that implements IDialogueSequencer here.")]
    public MonoBehaviour sequencerBehaviour;
    public Button startButton;

    private IDialogueSequencer sequencer;

    void Awake()
    {
        if (sequencerBehaviour != null)
            sequencer = sequencerBehaviour as IDialogueSequencer;

        if (sequencer == null && sequencerBehaviour != null)
            Debug.LogError("[DialogueStartButton] The assigned MonoBehaviour does not implement IDialogueSequencer.");

        if (startButton == null)
            startButton = GetComponent<Button>();

        if (startButton == null)
            Debug.LogWarning("[DialogueStartButton] No Button assigned and none found on this GameObject.");
    }

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartDialogue);
            startButton.gameObject.SetActive(true);
        }
    }

    void StartDialogue()
    {
        if (startButton != null)
            startButton.gameObject.SetActive(false);

        if (sequencer != null)
        {
            sequencer.StopSequence();
            sequencer.Play();
            StartCoroutine(WaitForSequenceEnd());
        }
        else
        {
            Debug.LogError("[DialogueStartButton] No sequencer set or it doesn't implement IDialogueSequencer.");
            if (startButton != null) startButton.gameObject.SetActive(true);
        }
    }

    IEnumerator WaitForSequenceEnd()
    {
        while (sequencer != null && sequencer.IsRunning)
            yield return null;

        if (startButton != null)
            startButton.gameObject.SetActive(true);
    }
}
