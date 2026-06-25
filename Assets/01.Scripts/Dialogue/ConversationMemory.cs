using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ReactNPC.Dialogue
{
    public class ConversationMemory : MonoBehaviour
    {
        [Header("Memory")]
        [SerializeField] private int maxMessageCount = 10;

        private readonly List<DialogueMessage> messages =
            new List<DialogueMessage>();

        public void AddPlayerMessage(string text)
        {
            AddMessage("Player", text);
        }

        public void AddNpcMessage(string text)
        {
            AddMessage("NPC", text);
        }

        private void AddMessage(string speaker, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            messages.Add(new DialogueMessage(speaker, text));
            TrimOldMessages();
        }

        private void TrimOldMessages()
        {
            while (messages.Count > maxMessageCount)
            {
                messages.RemoveAt(0);
            }
        }

        public string BuildHistoryText()
        {
            if (messages.Count == 0)
            {
                return "이전 대화 없음";
            }

            StringBuilder builder = new StringBuilder();

            foreach (DialogueMessage message in messages)
            {
                builder.AppendLine($"{message.speaker}: {message.text}");
            }

            return builder.ToString();
        }

        public void Clear()
        {
            messages.Clear();
        }
    }
}
