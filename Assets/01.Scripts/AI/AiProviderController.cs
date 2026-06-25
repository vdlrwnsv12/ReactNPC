using System;
using System.Collections;
using ReactNPC.Data;
using ReactNPC.Prediction;
using UnityEngine;

namespace ReactNPC.AI
{
    /// <summary>
    /// Inspector에서 AiProviderType을 선택하면 Gemini/Ollama 중 하나로
    /// 호출을 위임한다. AINpcAgent는 이 클래스 하나만 참조한다.
    /// </summary>
    public class AiProviderController : MonoBehaviour
    {
        [Header("사용할 AI")]
        [SerializeField] private AiProviderType providerType = AiProviderType.Gemini;

        [Header("Provider 구현체")]
        [SerializeField] private GeminiDialogueService geminiService;
        [SerializeField] private OllamaDialogueService ollamaService;

        public AiProviderType ProviderType => providerType;

        private DialogueProviderBase ActiveProvider
        {
            get
            {
                switch (providerType)
                {
                    case AiProviderType.Ollama:
                        return ollamaService;

                    case AiProviderType.Gemini:
                    default:
                        return geminiService;
                }
            }
        }

        private void Start()
        {
            if (providerType == AiProviderType.Ollama && ollamaService != null)
            {
                StartCoroutine(ollamaService.PrepareModel());
            }
        }

        public IEnumerator GenerateReply(
            string playerText,
            PredictionResult prediction,
            NpcProfile npcProfile,
            string conversationHistory,
            Action<string> onChunk,
            Action<string> onSuccess,
            Action<string> onError)
        {
            DialogueProviderBase provider = ActiveProvider;

            if (provider == null)
            {
                onError?.Invoke(
                    $"[AiProviderController] {providerType} Provider가 연결되지 않았습니다."
                );

                yield break;
            }

            yield return provider.GenerateReply(
                playerText,
                prediction,
                npcProfile,
                conversationHistory,
                onChunk,
                onSuccess,
                onError
            );
        }

        public void CancelActiveRequest()
        {
            ActiveProvider?.CancelActiveRequest();
        }
    }
}
