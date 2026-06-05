using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

public class LLMTEST : MonoBehaviour
{
    [Header("API")]
    public string apiKey = "paste-your-gemini-key-here";

    private const string GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    private const string SYSTEM_PROMPT =
        "You are playing 20 Questions. You have secretly chosen the word 'penguin'. " +
        "The player will ask yes/no questions. " +
        "Always respond with ONLY a JSON object in this exact format, nothing else: " +
        "{\"answer\": \"yes\" or \"no\" or \"correct\" or \"invalid\" or \"hint\" or \"giveup\", " +
        "\"gesture\": one of [thinking, considering, amused, impressed, nervous, defeated, playful_no, confident_yes, shrug, reluctant, victorious], " +
        "\"speech\": \"your short spoken reply\"}";

    void Start()
    {
        Debug.Log("[LLMTest] Ready! Press [Space] to send a test question to Gemini.");
    }

    void Update()
    {

        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(TestLLMCall("Is it an animal?"));
        }
    }

    private IEnumerator TestLLMCall(string playerQuestion)
    {
        Debug.Log($"[LLMTest] Sending question: \"{playerQuestion}\"");


        string bodyJson = BuildRequestJson(playerQuestion);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

        string url = $"{GEMINI_URL}?key={apiKey}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LLMTest] Request failed: {req.error}");
                Debug.LogError($"[LLMTest] Response: {req.downloadHandler.text}");
                yield break;
            }


            string rawJson = ExtractGeminiText(req.downloadHandler.text);
            Debug.Log($"[LLMTest] Raw LLM response: {rawJson}");
            GameLogic gameLogic = new GameLogic();
            TurnResult result = gameLogic.ProcessTurn(rawJson);
            Debug.Log($"[LLMTest] Speech: \"{result.speech}\" | Gesture: \"{result.gesture}\" | Outcome: {result.outcome}");

        }
    }

    private string BuildRequestJson(string playerQuestion)
    {
        string escapedSystem = EscapeJson(SYSTEM_PROMPT);
        string escapedQuestion = EscapeJson(playerQuestion);

        return $@"{{
            ""system_instruction"": {{
                ""parts"": [{{ ""text"": ""{escapedSystem}"" }}]
            }},
            ""contents"": [
                {{
                    ""role"": ""user"",
                    ""parts"": [{{ ""text"": ""{escapedQuestion}"" }}]
                }}
            ],
            ""generationConfig"": {{
                ""temperature"": 0.7,
                ""maxOutputTokens"": 300
            }}
        }}";
    }

    private string ExtractGeminiText(string responseJson)
    {
        Match m = Regex.Match(responseJson, @"""text""\s*:\s*""((?:[^""\\]|\\.)*)""");
        if (!m.Success)
        {
            Debug.LogWarning("[LLMTest] Could not extract text from response.");
            return null;
        }

        string text = m.Groups[1].Value
            .Replace("\\n", "\n")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");

        int first = text.IndexOf('{');
        int last = text.LastIndexOf('}');
        if (first >= 0 && last > first)
            text = text.Substring(first, last - first + 1);

        return text;
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
    }
}
