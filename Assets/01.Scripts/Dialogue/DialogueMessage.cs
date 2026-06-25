using System;

namespace ReactNPC.Dialogue
{
    [Serializable]
    public class DialogueMessage
    {
        public string speaker;
        public string text;

        public DialogueMessage(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }
}
