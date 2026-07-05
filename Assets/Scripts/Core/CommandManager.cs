using System.Collections.Generic;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance { get; private set; }

    private const float UndoSoundMaxDuration = 1f;

    private readonly Stack<ICommand> history = new Stack<ICommand>();

    public bool HasHistory => history.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool ExecuteCommand(ICommand command)
    {
        if (command == null)
            return false;

        bool success = command.Execute();

        if (!success)
            return false;

        history.Push(command);
        return true;
    }

    public bool UndoLastCommand()
    {
        if (history.Count <= 0)
        {
            Debug.Log("더 이상 되돌릴 행동이 없습니다.");
            return false;
        }

        ICommand lastCommand = history.Pop();
        lastCommand.Undo();

        if (UndoGlitchEffect.Instance != null)
            UndoGlitchEffect.Instance.Play();

        PlayUndoSound();

        return true;
    }

    private void PlayUndoSound()
    {
        if (AudioManager.I == null || AudioManager.I.sfxReverse == null)
            return;

        AudioManager.PlayDetachedOneShot(AudioManager.I.sfxReverse, 0.2f, UndoSoundMaxDuration);
    }

    public void ClearHistory()
    {
        history.Clear();
    }
}
