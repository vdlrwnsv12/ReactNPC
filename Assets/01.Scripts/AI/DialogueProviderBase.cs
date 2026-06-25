using System;
using System.Collections;
using ReactNPC.Data;
using ReactNPC.Prediction;
using UnityEngine;

namespace ReactNPC.AI
{
    public abstract class DialogueProviderBase : MonoBehaviour
    {
        public abstract IEnumerator GenerateReply(
            string playerText,
            PredictionResult prediction,
            NpcProfile npcProfile,
            string conversationHistory,
            Action<string> onChunk,
            Action<string> onSuccess,
            Action<string> onError);

        public abstract void CancelActiveRequest();
    }
}
