using System;

namespace ReactNPC.Prediction
{
    public enum IntentType
    {
        Unknown,
        Greeting,
        Question,
        Request,
        Thanks,
        Accusation,
        Threat
    }

    public enum EmotionType
    {
        Neutral,
        Friendly,
        Happy,
        Suspicious,
        Nervous,
        Angry
    }

    [Serializable]
    public struct PredictionResult
    {
        public IntentType intent;
        public EmotionType emotion;
        public float confidence;

        public PredictionResult(
            IntentType intent,
            EmotionType emotion,
            float confidence)
        {
            this.intent = intent;
            this.emotion = emotion;
            this.confidence = confidence;
        }
    }
}
