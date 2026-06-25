using System;
using System.Collections.Generic;

namespace ReactNPC.AI
{
    [Serializable]
    public class GeminiRequest
    {
        public List<GeminiContent> contents;
    }

    [Serializable]
    public class GeminiContent
    {
        public string role;
        public List<GeminiPart> parts;
    }

    [Serializable]
    public class GeminiPart
    {
        public string text;
    }

    [Serializable]
    public class GeminiResponse
    {
        public List<GeminiCandidate> candidates;
    }

    [Serializable]
    public class GeminiCandidate
    {
        public GeminiContent content;
    }

    [Serializable]
    public class GeminiErrorResponse
    {
        public GeminiError error;
    }

    [Serializable]
    public class GeminiError
    {
        public int code;
        public string message;
        public string status;
    }
}
