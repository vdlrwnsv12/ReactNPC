using ReactNPC.Prediction;
using TMPro;
using UnityEngine;

namespace ReactNPC.UI
{
    public class NpcReactionController : MonoBehaviour
    {
        [Header("State UI")]
        [SerializeField] private TMP_Text npcStateText;

        [Header("Optional Animator")]
        [SerializeField] private Animator animator;

        private string currentState = "Idle";

        public void React(PredictionResult prediction)
        {
            string nextState = GetStateFromPrediction(prediction);
            currentState = nextState;

            UpdateStateText(prediction);

            if (animator != null)
            {
                animator.SetTrigger(nextState);
            }
        }

        public void SetThinking()
        {
            currentState = "Thinking";
            UpdateSimpleStateText();
        }

        public void SetSpeaking()
        {
            currentState = "Speaking";
            UpdateSimpleStateText();
        }

        public void SetIdle()
        {
            currentState = "Idle";
            UpdateSimpleStateText();
        }

        private static string GetStateFromPrediction(PredictionResult prediction)
        {
            switch (prediction.emotion)
            {
                case EmotionType.Happy:
                    return "Smile";

                case EmotionType.Angry:
                    return "Defensive";

                case EmotionType.Suspicious:
                case EmotionType.Nervous:
                    return "Nervous";

                case EmotionType.Friendly:
                    return "Listening";

                default:
                    return "Idle";
            }
        }

        private void UpdateStateText(PredictionResult prediction)
        {
            if (npcStateText == null)
            {
                return;
            }

            npcStateText.text =
                $"NPC State: {currentState}\n" +
                $"Intent: {prediction.intent}\n" +
                $"Emotion: {prediction.emotion}\n" +
                $"Confidence: {prediction.confidence:0.00}";
        }

        private void UpdateSimpleStateText()
        {
            if (npcStateText == null)
            {
                return;
            }

            npcStateText.text = $"NPC State: {currentState}";
        }
    }
}
