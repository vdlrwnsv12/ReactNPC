using ReactNPC.AI;
using ReactNPC.Data;
using ReactNPC.Dialogue;
using ReactNPC.Input;
using ReactNPC.Prediction;
using ReactNPC.UI;
using TMPro;
using UnityEngine;

namespace ReactNPC.Core
{
    public class AINpcAgent : MonoBehaviour
    {
        [Header("Modules")]
        [SerializeField] private StreamingInputManager inputManager;
        [SerializeField] private IntentPredictor intentPredictor;
        [SerializeField] private NpcReactionController reactionController;

        [Header("Dialogue")]
        [SerializeField] private NpcDialogueView dialogueView;
        [SerializeField] private ConversationMemory conversationMemory;

        [Header("AI Provider")]
        [SerializeField] private AiProviderController aiProvider;
        [SerializeField] private NpcProfile npcProfile;

        [Header("Debug")]
        [SerializeField] private TMP_Text debugText;

        private bool isWaitingForAi;
        private string pendingPlayerText;
        private string lastPrecursorPhrase;
        private int currentRequestId;

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            inputManager.OnPartialTextChanged += HandlePartialTextChanged;
            inputManager.OnFinalTextSubmitted += HandleFinalTextSubmitted;
        }

        private void OnDestroy()
        {
            if (inputManager != null)
            {
                inputManager.OnPartialTextChanged -= HandlePartialTextChanged;
                inputManager.OnFinalTextSubmitted -= HandleFinalTextSubmitted;
            }

            aiProvider?.CancelActiveRequest();
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (inputManager == null)
            {
                Debug.LogError("[AINpcAgent] InputManager가 없습니다.");
                valid = false;
            }

            if (intentPredictor == null)
            {
                Debug.LogError("[AINpcAgent] IntentPredictor가 없습니다.");
                valid = false;
            }

            if (reactionController == null)
            {
                Debug.LogError("[AINpcAgent] ReactionController가 없습니다.");
                valid = false;
            }

            if (dialogueView == null)
            {
                Debug.LogError("[AINpcAgent] DialogueView가 없습니다.");
                valid = false;
            }

            if (conversationMemory == null)
            {
                Debug.LogError("[AINpcAgent] ConversationMemory가 없습니다.");
                valid = false;
            }

            if (aiProvider == null)
            {
                Debug.LogError("[AINpcAgent] AiProviderController가 없습니다.");
                valid = false;
            }

            if (npcProfile == null)
            {
                Debug.LogError("[AINpcAgent] NpcProfile이 없습니다.");
                valid = false;
            }

            return valid;
        }

        private void HandlePartialTextChanged(string text)
        {
            PredictionResult prediction =
                intentPredictor.Predict(text);

            reactionController.React(prediction);

            if (debugText != null)
            {
                debugText.text =
                    $"Partial: {text}\n" +
                    $"Intent: {prediction.intent}\n" +
                    $"Emotion: {prediction.emotion}\n" +
                    $"Confidence: {prediction.confidence:0.00}";
            }
        }

        private void HandleFinalTextSubmitted(string text)
        {
            if (isWaitingForAi)
            {
                Debug.LogWarning(
                    "[AINpcAgent] AI 응답을 기다리는 중입니다."
                );

                return;
            }

            PredictionResult prediction =
                intentPredictor.Predict(text);

            pendingPlayerText = text;
            isWaitingForAi = true;

            int requestId = ++currentRequestId;

            dialogueView.ShowPlayerMessage(text);
            reactionController.SetThinking();

            // 엔터를 누른 순간 AI 응답을 기다리지 않고 즉시 선행 반응을 보여준다.
            string precursor = PrecursorPhraseBank.GetPhrase(
                npcProfile,
                prediction.emotion,
                lastPrecursorPhrase
            );

            lastPrecursorPhrase = precursor;

            dialogueView.BeginNpcReply($"{precursor} ");

            string history =
                conversationMemory.BuildHistoryText();

            StartCoroutine(
                aiProvider.GenerateReply(
                    text,
                    prediction,
                    npcProfile,
                    history,
                    chunk => HandleAiChunk(requestId, chunk),
                    reply => HandleAiSuccess(requestId, reply),
                    error => HandleAiError(requestId, error)
                )
            );
        }

        private void HandleAiChunk(int requestId, string chunk)
        {
            if (requestId != currentRequestId)
            {
                return;
            }

            dialogueView.AppendNpcReplyChunk(chunk);
        }

        private void HandleAiSuccess(int requestId, string reply)
        {
            if (requestId != currentRequestId)
            {
                return;
            }

            dialogueView.EndNpcReply();

            // 대화 기록에는 선행 반응 문구를 빼고 실제 AI 대사만 저장한다.
            conversationMemory.AddPlayerMessage(pendingPlayerText);
            conversationMemory.AddNpcMessage(reply);

            reactionController.SetSpeaking();

            isWaitingForAi = false;
            pendingPlayerText = string.Empty;
        }

        private void HandleAiError(int requestId, string error)
        {
            if (requestId != currentRequestId)
            {
                return;
            }

            Debug.LogError(
                $"[AINpcAgent] AI 응답 오류:\n{error}"
            );

            dialogueView.EndNpcReply();
            dialogueView.ShowError("NPC가 응답하지 못했습니다.");

            reactionController.SetIdle();

            isWaitingForAi = false;
            pendingPlayerText = string.Empty;
        }
    }
}
