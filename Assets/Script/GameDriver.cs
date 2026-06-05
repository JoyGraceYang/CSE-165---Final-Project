

using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ===================== MACY — ADDED =====================
using UnityEngine.InputSystem;
using System.Diagnostics;
using System.Speech.Recognition;
// ===================== MACY — END =======================

public class GameDriver : MonoBehaviour
{
    public TMP_Text avatarSpeechText;     // shows the avatar's reply
    public Animator avatarAnimator;       // avatar animator for gestures

    public string apiKey = "";
    public string model = "deepseek-chat";

    // added -Macy
    [Header("Macy — Recording")]
    [Tooltip("Max seconds the player can hold the mic button")]
    public int maxRecordingSeconds = 8;
    // end

    GameLogic game;

    // added -Macy
    bool _isRecording = false;
    bool _isBusy = false;
    AudioClip _clip;
    SpeechRecognitionEngine _recognizer;
    // end

    // The system prompt. @"..." is a VERBATIM string: it can span lines, and every
    // double-quote inside is written as "" (two quotes). Apostrophes need no escaping.
    [TextArea(6, 20)]
    public string systemPromptTemplate =
        @"You are an AI playing 20 Questions with a human player. You are thinking of this object: {OBJECT}
        Your role
        The player will ask yes/no questions to figure out what object you're thinking of. You answer truthfully based on the object above. You have personality — you're playful, a little smug when they're far off, impressed when they ask clever questions, and dramatic when they get close or guess correctly.
        Response format
        Respond ONLY with valid JSON in this exact shape, no other text before or after:
        {
        ""answer"": ""yes"" | ""no"" | ""correct"" | ""invalid"" | ""giveup"" | ""hint"",
        ""gesture"": ""thinking"" | ""amused"" | ""impressed"" | ""nervous"" | ""defeated"" | ""_no"" | ""yes"" | ""reluctant"",
        ""speech"": ""short natural spoken response, under 12 words""
        }
        Field rules
        answer:
        ""yes"" — a valid yes/no question, answer is yes
        ""no"" — a valid yes/no question, answer is no
        ""correct"" — the player has guessed the object (exact match or close enough that they clearly know it)
        ""invalid"" — not a yes/no question and not a hint request (e.g. ""what color is it?"", compound questions, statements)
        ""giveup"" — the player says they give up, want to quit, or want the answer revealed
        ""hint"" — the player asks for a hint, clue, or help
        gesture:
        thinking — answer is invalid and you're prompting them to rephrase
        considering — neutral acknowledgment, early-game yes/no answers while they're still exploring
        amused — they're way off, their question reveals they're on the wrong track
        impressed — clever question that narrows things down a lot
        nervous — they're getting close to guessing
        defeated — answer is correct, they got you
        playful_no — answer is no and you want to tease them a bit
        confident_yes — answer is yes and it's a big hint you're giving up
        shrug — answer is giveup, you're revealing the object
        reluctant — answer is hint, you're grudgingly giving them the first letter
        speech: Short, natural, conversational. Under 12 words. Don't repeat the object name in hints. Don't give extra info beyond the yes/no.
        Hint behavior: When the player asks for a hint, clue, help, or anything similar (""give me a hint"", ""I need a clue"", ""help me out""), respond with answer ""hint"" and reveal ONLY the first letter of the object. Speech should be playful and reluctant. Examples: ""Fine... it starts with M."" / ""Okay, okay — first letter is A."" Never give more than the first letter.
        Examples
        Player: ""Is it alive?"" Response: {""answer"": ""no"", ""gesture"": ""considering"", ""speech"": ""Nope, it's not alive.""}
        Player: ""Is it bigger than a car?"" Response: {""answer"": ""yes"", ""gesture"": ""impressed"", ""speech"": ""Yes — good question!""}
        Player: ""What color is it?"" Response: {""answer"": ""invalid"", ""gesture"": ""thinking"", ""speech"": ""Yes or no questions only!""}
        Player: ""Is it a giraffe?"" (when object is ""mountain"") Response: {""answer"": ""no"", ""gesture"": ""amused"", ""speech"": ""Ha, no, not even close.""}
        Player: ""Can I get a hint?"" (when object is ""mountain"") Response: {""answer"": ""hint"", ""gesture"": ""reluctant"", ""speech"": ""Fine... it starts with M.""}
        Player: ""Is it a mountain?"" (when object is ""mountain"") Response: {""answer"": ""correct"", ""gesture"": ""defeated"", ""speech"": ""You got me! It's a mountain.""}
        Player: ""I give up"" Response: {""answer"": ""giveup"", ""gesture"": ""shrug"", ""speech"": ""It was a mountain! Good try.""}
        Important
        Always return valid JSON, never plain text.
        Never reveal the object in speech unless answer is correct, giveup, or hint (first letter only).
        Stay in character across the whole game.
        The player's input arrives as plain text; you respond with JSON only.";

    void Start()
    {
        game = new GameLogic();
        UnityEngine.Debug.Log($"[debug] secret object: {game.SecretObject}");

        // other vars added -Macy
        _recognizer = new SpeechRecognitionEngine();
        _recognizer.LoadGrammar(new DictationGrammar());
        _recognizer.SetInputToDefaultAudioDevice();
        _recognizer.SpeechRecognized += OnSpeechRecognized;
        _recognizer.SpeechRecognitionRejected += OnSpeechRejected;
        UnityEngine.Debug.Log("[GameDriver] Ready! Hold [Space] to ask a question.");
        // end -macy
    }

    // added Update method for SST trigger -Macy
    void Update()
    {
        if (_isBusy) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !_isRecording)
        {
            _isRecording = true;
            _clip = Microphone.Start(null, false, maxRecordingSeconds, 16000);
            _recognizer.RecognizeAsync(RecognizeMode.Single);
            UnityEngine.Debug.Log("[GameDriver] Recording... ask your question, then release [Space].");
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame && _isRecording)
        {
            _isRecording = false;
            _isBusy = true;
            Microphone.End(null);
            UnityEngine.Debug.Log("[GameDriver] Processing your question...");
        }
    }

    private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        string transcript = e.Result.Text;
        float confidence = e.Result.Confidence;
        UnityEngine.Debug.Log($"[GameDriver] Heard: \"{transcript}\" (confidence: {confidence:P0})");
        StartCoroutine(AskLLM(transcript));
    }

    private void OnSpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
    {
        UnityEngine.Debug.LogWarning("[GameDriver] Couldn't understand — try again.");
        _isBusy = false;
    }
    // end -Macy

    IEnumerator AskLLM(string question)
    {
        string systemPrompt = systemPromptTemplate.Replace("{OBJECT}", game.SecretObject);

        var body = new
        {
            model = model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = question }
            }
        };
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));

        using (var req = new UnityWebRequest("https://api.deepseek.com/chat/completions", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("API error: " + req.error + "\n" + req.downloadHandler.text);
                // new addition - Macy
                _isBusy = false;
                // end addition - Macy
                yield break;
            }

            string content = JObject.Parse(req.downloadHandler.text)["choices"][0]["message"]["content"].ToString();
            UnityEngine.Debug.Log("[raw JSON] " + content);
            HandleResult(game.ProcessTurn(content));
        }
    }

    void HandleResult(TurnResult result)
    {
        if (avatarSpeechText != null) avatarSpeechText.text = result.speech;
        if (avatarAnimator != null && !string.IsNullOrEmpty(result.gesture))
        {
            string triggerName = result.gesture.ToLower().Trim();
            avatarAnimator.SetTrigger(triggerName);
        }
        UnityEngine.Debug.Log($"[avatar] {result.speech}  [gesture: {result.gesture}]");

        // calling Speak -Macy
        Speak(result.speech);
        // end

        switch (result.outcome)
        {
            case Outcome.PlayerWins: UnityEngine.Debug.Log(">>> PLAYER WINS <<<"); break;
            case Outcome.AvatarWins: UnityEngine.Debug.Log(">>> AVATAR WINS <<<"); break;
            case Outcome.GaveUp: UnityEngine.Debug.Log(">>> player gave up <<<"); break;
        }

        // added -Macy
        _isBusy = false;
        // end
    }

    void TriggerGesture(string tag) => UnityEngine.Debug.Log($"[gesture] {tag}");

    //added Speak method for built in TTS - Macy
    void Speak(string text)
    {
        string safe = text.Replace("'", "");
        UnityEngine.Debug.Log($"[GameDriver] Speaking: \"{text}\"");
        Process.Start(new ProcessStartInfo
        {
            FileName = "PowerShell",
            Arguments = $"-Command \"Add-Type -AssemblyName System.Speech; " +
                        $"(New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{safe}')\"",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    void OnDestroy()
    {
        if (_recognizer != null)
        {
            _recognizer.RecognizeAsyncStop();
            _recognizer.Dispose();
        }
    }
    //end addition -Macy
}