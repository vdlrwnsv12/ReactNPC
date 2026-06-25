using UnityEngine;

namespace ReactNPC.AI
{
    public class OllamaSettings : MonoBehaviour
    {
        [Header("Ollama 서버")]
        [Tooltip("Ollama는 항상 localhost(127.0.0.1)로만 호출합니다. 외부에 노출하지 마세요.")]
        [SerializeField] private string baseUrl = "http://127.0.0.1:11434";

        [Header("모델")]
        [SerializeField] private string model = "llama3";

        [Header("메모리 유지")]
        [Tooltip("-1이면 모델을 메모리에서 내리지 않습니다. 0이면 즉시 내립니다.")]
        [SerializeField] private int keepAliveSeconds = -1;

        [Header("요청 제한 시간")]
        [Tooltip("0 이하이면 제한 시간 없음")]
        [SerializeField] private float timeoutSeconds = 30f;

        public string BaseUrl => baseUrl;
        public string Model => model;
        public int KeepAliveSeconds => keepAliveSeconds;
        public float TimeoutSeconds => timeoutSeconds;
    }
}
