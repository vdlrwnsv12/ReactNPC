using UnityEngine;

namespace ReactNPC.Prediction
{
    public class IntentPredictor : MonoBehaviour
    {
        public PredictionResult Predict(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new PredictionResult(
                    IntentType.Unknown,
                    EmotionType.Neutral,
                    0f
                );
            }

            string normalizedText = text.ToLower();

            if (ContainsAny(normalizedText, "죽", "패", "꺼져", "조져", "가만 안"))
            {
                return new PredictionResult(
                    IntentType.Threat,
                    EmotionType.Angry,
                    0.95f
                );
            }

            if (ContainsAny(normalizedText, "화나", "짜증", "씨발", "시발", "빡치", "좆"))
            {
                return new PredictionResult(
                    IntentType.Accusation,
                    EmotionType.Angry,
                    0.9f
                );
            }

            if (ContainsAny(normalizedText, "고마워", "감사", "좋아", "멋지다", "최고"))
            {
                return new PredictionResult(
                    IntentType.Thanks,
                    EmotionType.Happy,
                    0.9f
                );
            }

            if (ContainsAny(normalizedText, "도와", "부탁", "해줘", "살려"))
            {
                return new PredictionResult(
                    IntentType.Request,
                    EmotionType.Friendly,
                    0.8f
                );
            }

            if (ContainsAny(normalizedText, "안녕", "반가워", "하이", "ㅎㅇ"))
            {
                return new PredictionResult(
                    IntentType.Greeting,
                    EmotionType.Friendly,
                    0.8f
                );
            }

            if (ContainsAny(normalizedText, "왜", "뭐", "어디", "누구", "언제", "어떻게", "?"))
            {
                return new PredictionResult(
                    IntentType.Question,
                    EmotionType.Suspicious,
                    0.75f
                );
            }

            return new PredictionResult(
                IntentType.Unknown,
                EmotionType.Neutral,
                0.2f
            );
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (text.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
