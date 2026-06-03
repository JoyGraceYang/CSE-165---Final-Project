// GameDriver.cs — real LLM call via DeepSeek (OpenAI-compatible Chat Completions).
// Requires the Newtonsoft package (com.unity.nuget.newtonsoft-json).
// Attach to a GameObject, drag in a TMP InputField, paste your DeepSeek API key in the Inspector.

using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GameDriver : MonoBehaviour
{
    public TMP_InputField inputField;     // drag your input field here
    public TMP_Text avatarSpeechText;     // optional: shows the avatar's reply

    public string apiKey = "";
    public string model = "deepseek-v4-flash";

    GameLogic game;

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
        Debug.Log($"[debug] secret object: {game.SecretObject}");
        if (inputField != null)
            inputField.onSubmit.AddListener(OnPlayerQuestion);
    }

    void OnPlayerQuestion(string question)
    {
        inputField.text = "";
        inputField.ActivateInputField();
        if (string.IsNullOrWhiteSpace(question)) return;
        StartCoroutine(AskLLM(question));   // network call runs async; resumes when the reply lands
    }

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
                Debug.LogError("API error: " + req.error + "\n" + req.downloadHandler.text);
                yield break;
            }

            string content = JObject.Parse(req.downloadHandler.text)["choices"][0]["message"]["content"].ToString();
            Debug.Log("[raw JSON] " + content); 
            HandleResult(game.ProcessTurn(content));
        }
    }

    void HandleResult(TurnResult result)
    {
        TriggerGesture(result.gesture);                                   // Person 2's Animator
        if (avatarSpeechText != null) avatarSpeechText.text = result.speech;
        Debug.Log($"[avatar] {result.speech}  [gesture: {result.gesture}]");

        switch (result.outcome)
        {
            case Outcome.PlayerWins: Debug.Log(">>> PLAYER WINS <<<"); break;
            case Outcome.AvatarWins: Debug.Log(">>> AVATAR WINS <<<"); break;
            case Outcome.GaveUp:     Debug.Log(">>> player gave up <<<"); break;
        }
    }

    // Person 2 replaces this with the real Animator trigger.
    void TriggerGesture(string tag) => Debug.Log($"[gesture] {tag}");
}