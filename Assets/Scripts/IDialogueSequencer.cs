using UnityEngine;

public interface IDialogueSequencer
{
    void Play();
    void StopSequence();
    bool IsRunning { get; }
}
