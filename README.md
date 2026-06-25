# ReactNPC

플레이어가 **타이핑하거나 말하는 도중에도** NPC가 즉시 반응하고, 입력이 끝나면 AI(Gemini 또는 로컬 Ollama)가 NPC 성격에 맞는 대사를 생성해서 실시간 대화처럼 느껴지게 만드는 Unity NPC 대화 시스템입니다.

이 문서는 "왜 이렇게 만들었는지"까지 같이 적어둔, 학습용 README입니다.

---

## 1. 핵심 아이디어

일반적인 AI NPC 챗봇은 이런 흐름입니다.

```
플레이어 입력 완료 → AI에게 전송 → (1~3초 대기) → 답변 표시
```

이 1~3초의 침묵이 대화를 부자연스럽게 만듭니다. ReactNPC는 그 공백을 두 가지로 메웁니다.

1. **타이핑 중 실시간 반응** — 플레이어가 아직 입력 중일 때부터 키워드 기반으로 감정/의도를 예측해서 NPC 표정·상태를 바꿉니다. AI 호출은 안 합니다(비용 없음, 지연 없음).
2. **제출 즉시 선행 반응 + 스트리밍** — 엔터를 치는 순간 "그건...", "음..." 같은 짧은 문구를 로컬 데이터로 즉시 보여주고, 그 뒤에 실제 AI 답변이 토큰 단위로 스트리밍되어 같은 줄에 자연스럽게 이어붙습니다.

```
플레이어 엔터
  → 즉시 표정 변화 + 선행 반응 "그건..." 표시      (AI 호출 전, 0ms)
  → AI 요청 시작
  → 첫 토큰 도착부터 한 글자씩 이어붙임            "그건... 네가"
  → 스트리밍 종료                                  "그건... 네가 알 필요 없어."
```

---

## 2. 폴더/네임스페이스 구조

```
Assets/01.Scripts/
├── Core/        ReactNPC.Core      — AINpcAgent (전체 오케스트레이터)
├── Input/       ReactNPC.Input     — StreamingInputManager (타이핑 감지)
├── Prediction/  ReactNPC.Prediction — IntentPredictor, PredictionTypes
├── UI/          ReactNPC.UI        — NpcDialogueView, NpcReactionController
├── Dialogue/    ReactNPC.Dialogue  — ConversationMemory, DialogueMessage, PrecursorPhraseBank
├── Data/        ReactNPC.Data      — NpcProfile (ScriptableObject)
├── AI/          ReactNPC.AI        — Gemini/Ollama Provider 전체
└── SO/                              — NpcProfile 에셋 인스턴스
```

각 폴더가 네임스페이스와 1:1로 대응합니다. 클래스 하나가 뭘 하는지 헷갈리면 폴더 이름만 보고도 역할을 추측할 수 있게 의도적으로 맞췄습니다.

---

## 3. 데이터 흐름

### 3-1. 타이핑 중 (AI 호출 없음)

```
TMP_InputField.onValueChanged
  → StreamingInputManager.OnPartialTextChanged 이벤트 발생
    → AINpcAgent.HandlePartialTextChanged(text)
      → IntentPredictor.Predict(text)   // 키워드 매칭, 즉시 반환
      → NpcReactionController.React(prediction)  // 표정/상태 텍스트 갱신
```

`IntentPredictor`는 "죽", "패", "고마워" 같은 키워드를 보고 `IntentType`/`EmotionType`을 추측하는 아주 단순한 규칙 기반 분류기입니다. AI가 아니라 진짜 `if (text.Contains(...))`만 합니다 — 그래서 글자 하나 칠 때마다 호출해도 비용이 0이고 지연도 없습니다.

### 3-2. 입력 제출 시 (AI 호출)

```
TMP_InputField.onSubmit
  → StreamingInputManager가 한국어 IME 보정 코루틴을 거쳐
    OnFinalTextSubmitted 이벤트 발생
      → AINpcAgent.HandleFinalTextSubmitted(text)
        1. dialogueView.ShowPlayerMessage(text)        // 플레이어 말풍선
        2. reactionController.SetThinking()             // NPC "생각 중" 상태
        3. PrecursorPhraseBank.GetPhrase(...)           // 선행 반응 문구 선택
        4. dialogueView.BeginNpcReply(precursor)         // 즉시 화면에 표시
        5. aiProvider.GenerateReply(...) 코루틴 시작     // Gemini 또는 Ollama
           → onChunk마다 dialogueView.AppendNpcReplyChunk(chunk)
           → onSuccess에서 dialogueView.EndNpcReply() + ConversationMemory에 저장
           → onError에서 dialogueView.ShowError(...)
```

**중요**: 선행 반응 문구(`PrecursorPhraseBank`)는 화면에만 표시되고, `ConversationMemory`에는 저장되지 않습니다. AI가 실제로 생성한 텍스트만 대화 기록에 남습니다 — 그래야 다음 턴에 AI가 자기가 하지도 않은 말("그건...")을 기억하는 척하는 걸 막을 수 있습니다.

---

## 4. AI Provider 추상화 (Gemini ↔ Ollama 전환)

### 왜 `interface`가 아니라 추상 클래스인가

```csharp
public abstract class DialogueProviderBase : MonoBehaviour
{
    public abstract IEnumerator GenerateReply(...);
    public abstract void CancelActiveRequest();
}
```

Unity Inspector는 일반 C# `interface` 타입 필드를 드래그&드롭으로 연결할 수 없습니다(별도 PropertyDrawer 없이는 빈 슬롯으로만 보임). 그래서 `MonoBehaviour`를 상속하는 **추상 클래스**로 만들었습니다 — 이러면 `GeminiDialogueService`, `OllamaDialogueService` 둘 다 이 타입의 컴포넌트로 취급되어 Inspector에서 정상적으로 드래그할 수 있습니다.

### 라우팅

```csharp
AINpcAgent.aiProvider (AiProviderController)
  └─ providerType: Gemini | Ollama   ← Inspector 드롭다운
  └─ geminiService: GeminiDialogueService
  └─ ollamaService: OllamaDialogueService
```

`AiProviderController.GenerateReply(...)`는 `providerType`에 따라 둘 중 하나로 그대로 위임합니다. `AINpcAgent`는 `AiProviderController` 하나만 알고 있어서, Gemini와 Ollama 어느 쪽으로 바꿔도 `AINpcAgent` 코드는 한 줄도 안 바뀝니다.

### Gemini vs Ollama 동작 차이

| | Gemini | Ollama |
|---|---|---|
| 호출 방식 | 단일 HTTP 요청, 끝나면 전체 텍스트 한 번에 옴 | `stream: true`로 보내서 NDJSON(줄마다 JSON 하나)이 끊임없이 옴 |
| `onChunk` 호출 횟수 | 1번(전체 텍스트) | 토큰/단어 단위로 여러 번 |
| API 키 | 필요 (`GeminiSettings.apiKey`) | 필요 없음(로컬 서버) |
| 사전 로딩 | 없음 | `PrepareModel()`이 연결 확인 → 모델 설치 확인 → 워밍업 요청까지 미리 수행 |

Gemini는 실제로 스트리밍을 안 하지만, `onChunk`를 한 번 호출해서 UI 쪽(`NpcDialogueView`)은 두 Provider를 구분할 필요 없이 항상 같은 방식(Begin → Append* → End)으로 동작합니다.

### Ollama 스트리밍 구현 디테일

`UnityWebRequest`는 기본적으로 응답이 다 끝나야 결과를 주기 때문에, 진짜 스트리밍을 하려면 `DownloadHandlerScript`를 직접 상속해서 `ReceiveData(byte[] data, int length)`를 오버라이드해야 합니다. `OllamaDialogueService.OllamaStreamDownloadHandler`가 이걸 하고 있습니다:

1. 바이트가 도착하는 대로 문자열 버퍼에 쌓음
2. `\n` 기준으로 완성된 줄만 떼어내서 JSON 파싱
3. 마지막에 잘린(아직 안 끝난) 줄은 버퍼에 남겨두고 다음 데이터를 기다림
4. 파싱된 한 줄(`{"message":{"content":"..."},"done":false}`)에서 `content`만 콜백으로 흘려보냄

### Qwen 계열 모델의 한자/전각 문장부호 오염 방지

Qwen2.5처럼 중국어 데이터 비중이 높은 모델은 한국어로 답해도 한자(漢字)나 전각 문장부호(？！，。)를 섞어서 내보내는 버릇이 있습니다. 한국어 폰트엔 한자 글리프가 없어서 □로 깨집니다. `OllamaDialogueService.SanitizeText()`가 모든 청크를 거쳐가면서:

- CJK 한자 영역(U+4E00~U+9FFF, U+3400~U+4DBF) 문자를 제거
- 전각 ASCII 영역(U+FF01~U+FF5E)을 `(char)(c - 0xFEE0)` 연산으로 반각으로 환산 (`？`→`?`, `！`→`!` 등)
- `。`→`.`, `、`→`,` 개별 매핑

프롬프트에도 "한자/영어를 섞지 않는다"를 명시해서 이중으로 막습니다.

---

## 5. 즉시 선행 반응 (`PrecursorPhraseBank`)

```csharp
PrecursorPhraseBank.GetPhrase(npcProfile, prediction.emotion, lastPrecursorPhrase)
```

1. `NpcProfile.precursorPhrases`에 그 감정에 대한 커스텀 문구가 있으면 그걸 사용
2. 없으면 코드에 내장된 기본 문구(`DefaultPhrases`) 사용
3. 직전에 썼던 문구(`excludePhrase`)는 가능하면 피해서 매번 똑같은 말이 나오는 걸 방지

`NpcProfile`은 `ScriptableObject`라서 이 필드를 추가해도 기존에 저장된 `.asset` 파일이 깨지지 않습니다(Unity가 없는 필드는 그냥 기본값으로 채움).

---

## 6. 요청 취소 / 중복 방지

- `AINpcAgent.isWaitingForAi` — 답변 기다리는 중엔 새 제출을 막음
- `AINpcAgent.currentRequestId` — 매 요청마다 증가시키는 정수. 콜백이 올 때 이 값이 다르면(이미 새 요청이 시작됐으면) 무시 — 응답이 뒤섞이는 것을 방지
- `DialogueProviderBase.CancelActiveRequest()` — 진행 중인 `UnityWebRequest`를 `Abort()`. `OnDestroy()`에서 호출되어 씬 전환/오브젝트 파괴 시 요청이 허공에 남지 않게 함
- `UnityWebRequest.Result`에는 `Aborted`라는 값이 없어서(`InProgress/Success/ConnectionError/ProtocolError/DataProcessingError`뿐), 취소 여부는 별도 `cancelRequested` 불 플래그로 직접 추적합니다

---

## 7. 설정 방법

### Gemini 쓰기
1. `GeminiManager` GameObject의 `GeminiSettings.apiKey`에 API 키 입력
2. `AiProviderController.providerType`을 `Gemini`로

### Ollama(로컬) 쓰기
1. [ollama.com](https://ollama.com)에서 설치 (또는 `brew install ollama`)
2. `ollama serve`로 서버 실행 (켜진 채로 유지)
3. `ollama pull <모델명>` (예: `qwen2.5:7b`, `llama3`)
4. `OllamaSettings.model`에 같은 모델명 입력
5. `AiProviderController.providerType`을 `Ollama`로
6. Play하면 `AiProviderController.Start()`가 자동으로 `OllamaDialogueService.PrepareModel()`을 호출해서 연결 확인 → 모델 설치 확인 → 메모리 워밍업까지 미리 끝내둠

### Inspector 연결표

`GeminiManager` GameObject에 다음 9개 컴포넌트가 모두 붙어있어야 합니다: `StreamingInputManager`, `IntentPredictor`, `ConversationMemory`, `GeminiSettings`, `GeminiDialogueService`, `OllamaSettings`, `OllamaDialogueService`, `AiProviderController`, `AINpcAgent`.

`AINpcAgent`의 7개 필드(`inputManager`, `intentPredictor`, `reactionController`, `dialogueView`, `conversationMemory`, `aiProvider`, `npcProfile`) 중 **하나라도 비어있으면 `Start()`에서 `enabled = false`로 전체가 조용히 꺼집니다.** 가장 흔히 막히는 지점입니다.

`npcProfile`은 Hierarchy 오브젝트가 아니라 **Project 창의 `.asset` 파일**(`Assets/01.Scripts/SO/NewNpcProfile.asset`)을 연결해야 합니다.

---

## 8. 알려진 한계 / 다음 단계

- **타이핑 중간 답변 예측(8번 설계)**: 입력이 잠시 멈췄을 때 짧은 Ollama 예측 요청을 보내 답변 방향을 미리 잡아두는 기능은 구조만 염두에 두고 아직 구현하지 않았습니다. 지금은 타이핑 중엔 감정 반응만 하고, 실제 AI 호출은 최종 제출 후에만 일어납니다.
- **새 입력이 이전 스트리밍을 끊고 들어오는 동작**: 현재는 응답을 기다리는 동안 새 제출 자체를 막는 방식(`isWaitingForAi`)입니다. "이전 응답을 버리고 새 요청으로 바로 갈아타기"는 추가 구현이 필요합니다.
- **`AI_Status_Text` UI**: Ollama 준비 상태("연결 중...", "모델 로딩 중...", "준비 완료")를 화면에 표시할 TMP 텍스트를 아직 씬에 만들지 않았습니다. `OllamaDialogueService.statusText`에 연결하면 바로 동작합니다.
- 로컬 7B급 모델(llama3, qwen2.5:7b 등)은 Gemini 같은 대형 클라우드 모델보다 한국어 자연스러움이 떨어집니다. 모델 교체/프롬프트 튜닝으로 어느 정도 완화는 가능하지만 근본적인 모델 성능 차이는 남습니다.
