using UnityEngine;

namespace ReactNPC.AI
{
    public class GeminiSettings : MonoBehaviour
    {
        [Header("개발 테스트용")]
        [SerializeField]
        [Tooltip("배포 빌드에 실제 API 키를 포함하면 안 됩니다.")]
        private string apiKey;

        public string ApiKey => apiKey;
    }
}
