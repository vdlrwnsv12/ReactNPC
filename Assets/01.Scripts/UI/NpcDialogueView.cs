using System.Text;
using TMPro;
using UnityEngine;

namespace ReactNPC.UI
{
    public class NpcDialogueView : MonoBehaviour
    {
        [SerializeField] private TMP_Text dialogueText;

        private string playerLine = string.Empty;
        private readonly StringBuilder npcReplyBuilder = new StringBuilder();

        public void ShowPlayerMessage(string message)
        {
            if (dialogueText == null)
            {
                Debug.LogError("[NpcDialogueView] Dialogue Text가 연결되지 않았습니다.");
                return;
            }

            playerLine = $"Player: {message}";
            npcReplyBuilder.Clear();

            Redraw();
        }

        /// <summary>
        /// NPC 대사 줄을 즉시 선행 반응 문구로 시작한다. 이후 AppendNpcReplyChunk로
        /// 같은 줄에 토큰을 이어 붙인다.
        /// </summary>
        public void BeginNpcReply(string initialText)
        {
            if (dialogueText == null)
            {
                return;
            }

            npcReplyBuilder.Clear();
            npcReplyBuilder.Append(initialText);

            Redraw();
        }

        /// <summary>
        /// 스트리밍으로 도착한 텍스트 조각을 현재 NPC 줄에 이어 붙인다.
        /// 전체 로그를 다시 만들지 않고 짧은 두 줄(Player/NPC)만 갱신한다.
        /// </summary>
        public void AppendNpcReplyChunk(string chunk)
        {
            if (dialogueText == null || string.IsNullOrEmpty(chunk))
            {
                return;
            }

            npcReplyBuilder.Append(chunk);
            Redraw();
        }

        /// <summary>
        /// 스트리밍 종료를 알린다. 현재는 별도 마무리 처리가 없지만,
        /// 추후 커서 깜빡임 제거 등 UI 마감 처리를 붙일 자리로 남겨둔다.
        /// </summary>
        public void EndNpcReply()
        {
        }

        public void ShowNpcReply(string reply)
        {
            if (dialogueText == null)
            {
                return;
            }

            npcReplyBuilder.Clear();
            npcReplyBuilder.Append(reply);

            Redraw();
        }

        public void ShowError(string message)
        {
            if (dialogueText == null)
            {
                return;
            }

            dialogueText.text += $"\n\nError: {message}";
        }

        public void Clear()
        {
            if (dialogueText == null)
            {
                return;
            }

            playerLine = string.Empty;
            npcReplyBuilder.Clear();
            dialogueText.text = string.Empty;
        }

        private void Redraw()
        {
            dialogueText.text = npcReplyBuilder.Length > 0
                ? $"{playerLine}\n\nNPC: {npcReplyBuilder}"
                : playerLine;
        }
    }
}
