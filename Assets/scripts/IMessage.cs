using System;
using UnityEngine;

public interface IMessageSystem
{
    void ShowMessage(string text, bool requireConfirmation, float autoHideDelay);
    void HideMessage(int messageId);
    void HideAllMessages();
    int GetActiveMessageCount();
    event Action<int> OnMessageClosed;
}