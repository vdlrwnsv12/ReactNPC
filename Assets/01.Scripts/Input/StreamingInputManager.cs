using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactNPC.Input
{
    public class StreamingInputManager : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField inputField;

        [Header("Optional Button")]
        [SerializeField] private Button sendButton;

        public event Action<string> OnPartialTextChanged;
        public event Action<string> OnFinalTextSubmitted;

        private bool isSubmitting;

        private void Awake()
        {
            if (inputField == null)
            {
                Debug.LogError("[StreamingInputManager] TMP_InputField가 연결되지 않았습니다.");
                return;
            }

            inputField.onValueChanged.AddListener(HandleValueChanged);
            inputField.onSubmit.AddListener(HandleSubmitRequested);

            if (sendButton != null)
            {
                sendButton.onClick.AddListener(SubmitByButton);
            }
        }

        private void OnDestroy()
        {
            if (inputField != null)
            {
                inputField.onValueChanged.RemoveListener(HandleValueChanged);
                inputField.onSubmit.RemoveListener(HandleSubmitRequested);
            }

            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(SubmitByButton);
            }
        }

        private void HandleValueChanged(string text)
        {
            OnPartialTextChanged?.Invoke(text);
        }

        private void HandleSubmitRequested(string submittedText)
        {
            if (isSubmitting)
            {
                return;
            }

            StartCoroutine(SubmitAfterKoreanIme());
        }

        private void SubmitByButton()
        {
            if (isSubmitting)
            {
                return;
            }

            StartCoroutine(SubmitAfterKoreanIme());
        }

        private IEnumerator SubmitAfterKoreanIme()
        {
            isSubmitting = true;

            yield return null;
            yield return new WaitForEndOfFrame();

            string finalText = inputField.text;

            if (!string.IsNullOrWhiteSpace(finalText))
            {
                OnFinalTextSubmitted?.Invoke(finalText);
            }

            inputField.SetTextWithoutNotify(string.Empty);

            yield return null;

            inputField.ActivateInputField();
            inputField.Select();

            isSubmitting = false;
        }
    }
}
