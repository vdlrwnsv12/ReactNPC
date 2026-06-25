using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ReactNPC.Data;
using ReactNPC.Prediction;
using UnityEngine;
using UnityEngine.Networking;

namespace ReactNPC.AI
{
    public class GeminiDialogueService : DialogueProviderBase
    {
        private const string ApiBaseUrl =
            "https://generativelanguage.googleapis.com/v1beta/models";

        [Header("Settings")]
        [SerializeField] private GeminiSettings settings;

        [Header("Model")]
        [SerializeField] private string model = "gemini-2.5-flash";

        private UnityWebRequest activeRequest;
        private bool cancelRequested;

        public override IEnumerator GenerateReply(
            string playerText,
            PredictionResult prediction,
            NpcProfile npcProfile,
            string conversationHistory,
            Action<string> onChunk,
            Action<string> onSuccess,
            Action<string> onError)
        {
            cancelRequested = false;

            if (settings == null)
            {
                onError?.Invoke("GeminiSettings가 연결되지 않았습니다.");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                onError?.Invoke("Gemini API Key가 비어 있습니다.");
                yield break;
            }

            if (npcProfile == null)
            {
                onError?.Invoke("NpcProfile이 연결되지 않았습니다.");
                yield break;
            }

            string prompt = BuildPrompt(
                playerText,
                prediction,
                npcProfile,
                conversationHistory
            );

            GeminiRequest requestData = new GeminiRequest
            {
                contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        role = "user",
                        parts = new List<GeminiPart>
                        {
                            new GeminiPart
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };

            string json = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            string apiUrl =
                $"{ApiBaseUrl}/{model}:generateContent";

            using UnityWebRequest request = new UnityWebRequest(
                apiUrl,
                UnityWebRequest.kHttpVerbPOST
            );

            activeRequest = request;

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.SetRequestHeader(
                "x-goog-api-key",
                settings.ApiKey
            );

            yield return request.SendWebRequest();

            activeRequest = null;

            if (cancelRequested)
            {
                yield break;
            }

            string responseText = request.downloadHandler.text;

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage =
                    $"Gemini API 요청 실패\n" +
                    $"Status: {request.responseCode}\n" +
                    $"Error: {request.error}\n" +
                    $"Response: {responseText}";

                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
                yield break;
            }

            GeminiResponse response =
                JsonUtility.FromJson<GeminiResponse>(responseText);

            string reply = ExtractReply(response);

            if (string.IsNullOrWhiteSpace(reply))
            {
                onError?.Invoke(
                    "Gemini 응답에서 NPC 대사를 찾지 못했습니다."
                );

                yield break;
            }

            string finalReply = reply.Trim();

            onChunk?.Invoke(finalReply);
            onSuccess?.Invoke(finalReply);
        }

        public override void CancelActiveRequest()
        {
            cancelRequested = true;
            activeRequest?.Abort();
        }

        private static string BuildPrompt(
            string playerText,
            PredictionResult prediction,
            NpcProfile npc,
            string conversationHistory)
        {
            return
                "너는 게임 속 NPC다.\n\n" +

                $"NPC 이름: {npc.npcName}\n" +
                $"역할: {npc.role}\n" +
                $"성격: {npc.personality}\n" +
                $"말투: {npc.speakingStyle}\n" +
                $"배경: {npc.backstory}\n\n" +

                $"최근 대화:\n{conversationHistory}\n\n" +

                $"플레이어의 새 입력:\n{playerText}\n\n" +

                $"예측된 의도: {prediction.intent}\n" +
                $"예측된 감정: {prediction.emotion}\n\n" +

                "규칙:\n" +
                "- NPC 역할을 유지한다.\n" +
                "- 자신이 AI라고 말하지 않는다.\n" +
                "- 이전 대화를 자연스럽게 기억한다.\n" +
                $"- 최대 {npc.maxSentences}문장으로 답한다.\n" +
                "- NPC가 실제로 말할 대사만 출력한다.\n" +
                "- 행동 설명, 괄호, 따옴표, 해설을 출력하지 않는다.\n" +
                "- 한국어로 답한다.";
        }

        private static string ExtractReply(GeminiResponse response)
        {
            if (response?.candidates == null ||
                response.candidates.Count == 0)
            {
                return null;
            }

            GeminiCandidate candidate = response.candidates[0];

            if (candidate?.content?.parts == null ||
                candidate.content.parts.Count == 0)
            {
                return null;
            }

            return candidate.content.parts[0].text;
        }
    }
}
