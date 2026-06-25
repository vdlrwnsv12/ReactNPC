using System;
using System.Collections.Generic;
using ReactNPC.Prediction;
using UnityEngine;

namespace ReactNPC.Data
{
    [CreateAssetMenu(
        fileName = "NewNpcProfile",
        menuName = "ReactNPC/NPC Profile"
    )]
    public class NpcProfile : ScriptableObject
    {
        [Header("기본 정보")]
        public string npcName = "볼프";
        public string role = "지하실 경비병";

        [Header("성격")]
        [TextArea(3, 6)]
        public string personality =
            "겁이 많지만 책임감이 있다. 거짓말을 잘하지 못한다.";

        [Header("말투")]
        [TextArea(2, 5)]
        public string speakingStyle =
            "짧게 말한다. 긴장하면 말을 더듬는다.";

        [Header("배경")]
        [TextArea(3, 8)]
        public string backstory =
            "지하실 안의 괴물을 숨기기 위해 문을 잠갔다.";

        [Header("답변 제한")]
        [Range(1, 5)]
        public int maxSentences = 2;

        [Header("선행 반응 문구 (선택, 비워두면 기본 문구 사용)")]
        public List<EmotionPhraseSet> precursorPhrases =
            new List<EmotionPhraseSet>();
    }

    [Serializable]
    public class EmotionPhraseSet
    {
        public EmotionType emotion;
        public string[] phrases;
    }
}
