using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactNPC.Data;
using ReactNPC.Prediction;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace ReactNPC.AI
{
    public class OllamaDialogueService : DialogueProviderBase
    {
        [Header("Settings")]
        [SerializeField] private OllamaSettings settings;

        [Header("상태 표시 (옵션)")]
        [SerializeField] private TMP_Text statusText;

        public OllamaStatus Status { get; private set; } = OllamaStatus.NotChecked;
        public string StatusMessage { get; private set; } = string.Empty;

        public event Action<OllamaStatus, string> OnStatusChanged;

        private UnityWebRequest activeRequest;
        private bool cancelRequested;

        /// <summary>
        /// 게임 시작 시 호출. Ollama 연결 확인 → 모델 설치 확인 → 모델을 메모리에 미리 로드한다.
        /// </summary>
        public IEnumerator PrepareModel()
        {
            if (settings == null)
            {
                SetStatus(OllamaStatus.Failed, "OllamaSettings가 연결되지 않았습니다.");
                yield break;
            }

            SetStatus(OllamaStatus.Connecting, "Ollama 서버에 연결 중...");

            string tagsUrl = $"{settings.BaseUrl}/api/tags";

            using UnityWebRequest tagsRequest = UnityWebRequest.Get(tagsUrl);
            tagsRequest.timeout = 5;

            yield return tagsRequest.SendWebRequest();

            if (tagsRequest.result != UnityWebRequest.Result.Success)
            {
                SetStatus(
                    OllamaStatus.Failed,
                    "Ollama 서버에 연결할 수 없습니다. Ollama가 설치되어 실행 중인지 확인하세요."
                );

                yield break;
            }

            OllamaTagsResponse tags;

            try
            {
                tags = JsonUtility.FromJson<OllamaTagsResponse>(
                    tagsRequest.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                SetStatus(
                    OllamaStatus.Failed,
                    $"Ollama 응답을 해석하지 못했습니다(JSON 파싱 실패): {e.Message}"
                );

                yield break;
            }

            bool modelFound =
                tags?.models != null &&
                tags.models.Any(m =>
                    m.name == settings.Model ||
                    m.name.StartsWith(settings.Model + ":")
                );

            if (!modelFound)
            {
                SetStatus(
                    OllamaStatus.Failed,
                    $"모델 '{settings.Model}'을 찾을 수 없습니다. " +
                    $"터미널에서 'ollama pull {settings.Model}'을 실행한 뒤 다시 시도하세요."
                );

                yield break;
            }

            SetStatus(OllamaStatus.LoadingModel, "로컬 AI 준비 중...");

            OllamaChatRequest warmupRequest = new OllamaChatRequest
            {
                model = settings.Model,
                stream = false,
                keep_alive = settings.KeepAliveSeconds,
                messages = new List<OllamaChatMessage>
                {
                    new OllamaChatMessage
                    {
                        role = "user",
                        content = "안녕"
                    }
                }
            };

            string warmupJson = JsonUtility.ToJson(warmupRequest);
            byte[] warmupBody = Encoding.UTF8.GetBytes(warmupJson);

            using UnityWebRequest warmup = new UnityWebRequest(
                $"{settings.BaseUrl}/api/chat",
                UnityWebRequest.kHttpVerbPOST
            );

            warmup.uploadHandler = new UploadHandlerRaw(warmupBody);
            warmup.downloadHandler = new DownloadHandlerBuffer();
            warmup.SetRequestHeader("Content-Type", "application/json");
            warmup.timeout = ToUnityTimeout(settings.TimeoutSeconds);

            yield return warmup.SendWebRequest();

            if (warmup.result != UnityWebRequest.Result.Success)
            {
                SetStatus(
                    OllamaStatus.Failed,
                    $"모델 로딩에 실패했습니다: {warmup.error}"
                );

                yield break;
            }

            SetStatus(OllamaStatus.Ready, "로컬 AI 준비 완료");
        }

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
                onError?.Invoke("OllamaSettings가 연결되지 않았습니다.");
                yield break;
            }

            if (Status != OllamaStatus.Ready)
            {
                onError?.Invoke(
                    $"로컬 AI가 아직 준비되지 않았습니다. ({StatusMessage})"
                );

                yield break;
            }

            if (npcProfile == null)
            {
                onError?.Invoke("NpcProfile이 연결되지 않았습니다.");
                yield break;
            }

            string systemPrompt = BuildSystemPrompt(npcProfile);
            string userPrompt = BuildUserPrompt(playerText, prediction, conversationHistory);

            OllamaChatRequest requestData = new OllamaChatRequest
            {
                model = settings.Model,
                stream = true,
                keep_alive = settings.KeepAliveSeconds,
                messages = new List<OllamaChatMessage>
                {
                    new OllamaChatMessage { role = "system", content = systemPrompt },
                    new OllamaChatMessage { role = "user", content = userPrompt }
                }
            };

            string json = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            StringBuilder fullReply = new StringBuilder();

            OllamaStreamDownloadHandler streamHandler = new OllamaStreamDownloadHandler(
                chunkText =>
                {
                    fullReply.Append(chunkText);
                    onChunk?.Invoke(chunkText);
                }
            );

            using UnityWebRequest request = new UnityWebRequest(
                $"{settings.BaseUrl}/api/chat",
                UnityWebRequest.kHttpVerbPOST
            );

            activeRequest = request;

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = streamHandler;
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = ToUnityTimeout(settings.TimeoutSeconds);

            yield return request.SendWebRequest();

            activeRequest = null;

            if (cancelRequested)
            {
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    $"Ollama 요청 실패\n" +
                    $"Error: {request.error}\n" +
                    "(스트리밍 중 연결이 끊겼을 수 있습니다)"
                );

                yield break;
            }

            if (streamHandler.ParseErrorOccurred)
            {
                onError?.Invoke("Ollama 응답을 해석하지 못했습니다(JSON 파싱 실패).");
                yield break;
            }

            string finalText = fullReply.ToString().Trim();

            if (string.IsNullOrWhiteSpace(finalText))
            {
                onError?.Invoke("Ollama 응답에서 NPC 대사를 찾지 못했습니다.");
                yield break;
            }

            onSuccess?.Invoke(finalText);
        }

        public override void CancelActiveRequest()
        {
            cancelRequested = true;
            activeRequest?.Abort();
        }

        private static int ToUnityTimeout(float timeoutSeconds)
        {
            return timeoutSeconds > 0f ? (int)timeoutSeconds : 0;
        }

        private void SetStatus(OllamaStatus status, string message)
        {
            Status = status;
            StatusMessage = message;

            if (statusText != null)
            {
                statusText.text = message;
            }

            OnStatusChanged?.Invoke(status, message);
        }

        private static string BuildSystemPrompt(NpcProfile npc)
        {
            return
                "너는 게임 속 NPC다.\n" +
                $"NPC 이름: {npc.npcName}\n" +
                $"역할: {npc.role}\n" +
                $"성격: {npc.personality}\n" +
                $"말투: {npc.speakingStyle}\n" +
                $"배경: {npc.backstory}\n\n" +
                "규칙:\n" +
                "- NPC 역할을 유지한다.\n" +
                "- 한국어로 답한다.\n" +
                "- 한자(漢字)나 중국어 문자를 절대 섞지 않는다. 순수 한글과 기본 문장부호만 사용한다.\n" +
                "- 영어 단어도 섞지 않는다.\n" +
                "- 실제 NPC 대사만 출력한다.\n" +
                "- 행동 설명, 괄호, 따옴표, 해설을 출력하지 않는다.\n" +
                "- 답변은 한마디로 짧게 한다. 절대 길게 설명하지 않는다.\n" +
                $"- 최대 {npc.maxSentences}문장을 넘기지 않는다.\n" +
                "- 이전 대화와 현재 게임 상태를 고려한다.\n" +
                "- 자신이 AI라고 말하지 않는다.";
        }

        private static string BuildUserPrompt(
            string playerText,
            PredictionResult prediction,
            string conversationHistory)
        {
            return
                $"최근 대화:\n{conversationHistory}\n\n" +
                $"플레이어의 새 입력:\n{playerText}\n\n" +
                $"예측된 의도: {prediction.intent}\n" +
                $"예측된 감정: {prediction.emotion}";
        }

        /// <summary>
        /// Ollama가 한 줄에 하나씩 보내는 JSON(NDJSON) 스트림을
        /// 도착하는 대로 즉시 파싱해서 콜백으로 흘려보낸다.
        /// </summary>
        private sealed class OllamaStreamDownloadHandler : DownloadHandlerScript
        {
            private readonly Action<string> onTextChunk;
            private readonly StringBuilder lineBuffer = new StringBuilder();

            public bool ParseErrorOccurred { get; private set; }

            public OllamaStreamDownloadHandler(Action<string> onTextChunk)
                : base(new byte[4096])
            {
                this.onTextChunk = onTextChunk;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength <= 0)
                {
                    return false;
                }

                string text = Encoding.UTF8.GetString(data, 0, dataLength);
                lineBuffer.Append(text);

                ProcessCompleteLines();

                return true;
            }

            private void ProcessCompleteLines()
            {
                string buffered = lineBuffer.ToString();
                string[] lines = buffered.Split('\n');

                for (int i = 0; i < lines.Length - 1; i++)
                {
                    string line = lines[i].Trim();

                    if (line.Length == 0)
                    {
                        continue;
                    }

                    TryParseLine(line);
                }

                lineBuffer.Clear();
                lineBuffer.Append(lines[lines.Length - 1]);
            }

            private void TryParseLine(string line)
            {
                try
                {
                    OllamaChatResponseChunk chunk =
                        JsonUtility.FromJson<OllamaChatResponseChunk>(line);

                    if (!string.IsNullOrEmpty(chunk?.message?.content))
                    {
                        string cleaned = SanitizeText(chunk.message.content);

                        if (cleaned.Length > 0)
                        {
                            onTextChunk?.Invoke(cleaned);
                        }
                    }
                }
                catch (Exception)
                {
                    ParseErrorOccurred = true;
                }
            }

            /// <summary>
            /// 일부 모델(특히 Qwen 계열)이 한국어 답변에 한자나 전각(全角) 문장부호
            /// (？！，。 등)를 섞어서 내보낼 때가 있다. 한자는 폰트에 글리프가 없어
            /// □로 깨지므로 제거하고, 전각 문장부호는 일반 반각 문장부호로 바꾼다.
            /// </summary>
            private static string SanitizeText(string text)
            {
                StringBuilder result = new StringBuilder(text.Length);

                foreach (char c in text)
                {
                    bool isHanCharacter =
                        (c >= '一' && c <= '鿿') ||
                        (c >= '㐀' && c <= '䶿');

                    if (isHanCharacter)
                    {
                        continue;
                    }

                    // 전각 ASCII 영역(！~～, U+FF01~U+FF5E)은 반각으로 그대로 환산 가능
                    if (c >= '！' && c <= '～')
                    {
                        result.Append((char)(c - 0xFEE0));
                        continue;
                    }

                    switch (c)
                    {
                        case '。': // 。
                            result.Append('.');
                            break;

                        case '、': // 、
                            result.Append(',');
                            break;

                        default:
                            result.Append(c);
                            break;
                    }
                }

                return result.ToString();
            }
        }
    }
}
