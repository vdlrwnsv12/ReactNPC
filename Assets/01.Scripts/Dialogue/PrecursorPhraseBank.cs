using System.Collections.Generic;
using ReactNPC.Data;
using ReactNPC.Prediction;
using UnityEngine;

namespace ReactNPC.Dialogue
{
    /// <summary>
    /// 감정별 선행 반응 문구를 고른다. NpcProfile에 커스텀 문구가 있으면 그걸 쓰고,
    /// 없으면 여기 정의된 기본 문구를 쓴다.
    /// </summary>
    public static class PrecursorPhraseBank
    {
        private static readonly Dictionary<EmotionType, string[]> DefaultPhrases =
            new Dictionary<EmotionType, string[]>
            {
                { EmotionType.Friendly, new[] { "아, 그래.", "그렇구나." } },
                { EmotionType.Happy, new[] { "오, 그렇군.", "하하, 좋네." } },
                { EmotionType.Nervous, new[] { "그건...", "잠깐..." } },
                { EmotionType.Angry, new[] { "뭐라고?", "지금 그게 무슨 말이지?" } },
                { EmotionType.Suspicious, new[] { "왜 그걸 묻지?", "무슨 뜻이지?" } },
                { EmotionType.Neutral, new[] { "음...", "잠시만." } },
            };

        public static string GetPhrase(
            NpcProfile profile,
            EmotionType emotion,
            string excludePhrase)
        {
            string[] candidates = FindCandidates(profile, emotion);

            if (candidates == null || candidates.Length == 0)
            {
                candidates = DefaultPhrases[EmotionType.Neutral];
            }

            if (candidates.Length == 1)
            {
                return candidates[0];
            }

            string picked;
            int guard = 0;

            do
            {
                picked = candidates[Random.Range(0, candidates.Length)];
                guard++;
            }
            while (picked == excludePhrase && guard < 5);

            return picked;
        }

        private static string[] FindCandidates(NpcProfile profile, EmotionType emotion)
        {
            if (profile != null && profile.precursorPhrases != null)
            {
                foreach (EmotionPhraseSet set in profile.precursorPhrases)
                {
                    if (set.emotion == emotion &&
                        set.phrases != null &&
                        set.phrases.Length > 0)
                    {
                        return set.phrases;
                    }
                }
            }

            return DefaultPhrases.TryGetValue(emotion, out string[] defaults)
                ? defaults
                : null;
        }
    }
}
