using System;
using System.Collections.Generic;

namespace ReactNPC.AI
{
    // 필드명이 snake_case인 것은 실수가 아니라, Unity JsonUtility가 필드명을
    // JSON 키와 그대로 매칭하기 때문에 Ollama API 스키마(keep_alive 등)와
    // 똑같이 맞춰준 것입니다.

    [Serializable]
    public class OllamaChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class OllamaChatRequest
    {
        public string model;
        public List<OllamaChatMessage> messages;
        public bool stream;
        public int keep_alive;
    }

    [Serializable]
    public class OllamaChatResponseChunk
    {
        public string model;
        public OllamaChatMessage message;
        public bool done;
    }

    [Serializable]
    public class OllamaModelInfo
    {
        public string name;
    }

    [Serializable]
    public class OllamaTagsResponse
    {
        public List<OllamaModelInfo> models;
    }
}
