// GameLogic.cs — pure C#, NO UnityEngine

using System;
using Newtonsoft.Json;   // Unity: add package "com.unity.nuget.newtonsoft-json" via Package Manager

public enum Outcome { Continue, PlayerWins, AvatarWins, GaveUp }

public struct TurnResult
{
    public string speech;
    public string gesture;
    public Outcome outcome;
}

public class GameLogic
{
    public static readonly string[] ValidGestures = {
        "thinking","considering","amused","impressed","nervous",
        "defeated","playful_no","confident_yes","shrug","reluctant",
        "victorious"   // avatar-wins gesture, fired by code
    };

    const int MaxQuestions = 20;
    int questionsAsked = 0;
    bool hintUsed = false;
    public string SecretObject { get; private set; }

    static readonly string[] ObjectPool = { "mountain", "guitar", "penguin", "umbrella" };

    public GameLogic()
    {
        SecretObject = ObjectPool[new Random().Next(ObjectPool.Length)];
    }

    public int QuestionsLeft => MaxQuestions - questionsAsked;

    // Feed it the raw JSON string the LLM returned. Returns what to show + do.
    public TurnResult ProcessTurn(string llmJson)
    {
        LlmResponse r = Parse(llmJson);
        if (r == null)
            return new TurnResult { speech = "Hmm, ask me that again?", gesture = "thinking", outcome = Outcome.Continue };

        switch (r.answer)
        {
            case "hint":
                if (hintUsed)
                    return new TurnResult { speech = "Nope, one hint per game!", gesture = "playful_no", outcome = Outcome.Continue };
                hintUsed = true;
                return new TurnResult { speech = r.speech, gesture = Validate(r.gesture), outcome = Outcome.Continue };

            case "correct":
                return new TurnResult { speech = r.speech, gesture = Validate(r.gesture), outcome = Outcome.PlayerWins };

            case "giveup":
                return new TurnResult { speech = $"It was {SecretObject}! {r.speech}", gesture = "shrug", outcome = Outcome.GaveUp };

            case "invalid":
                return new TurnResult { speech = r.speech, gesture = Validate(r.gesture), outcome = Outcome.Continue };

            default: // yes / no — a real question
                questionsAsked++;
                if (questionsAsked >= MaxQuestions)
                    return new TurnResult { speech = $"Out of questions! It was {SecretObject}.", gesture = "victorious", outcome = Outcome.AvatarWins };
                return new TurnResult { speech = r.speech, gesture = Validate(r.gesture), outcome = Outcome.Continue };
        }
    }

    LlmResponse Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();
        if (raw.StartsWith("```"))
        {
            int first = raw.IndexOf('{');
            int last = raw.LastIndexOf('}');
            if (first >= 0 && last > first) raw = raw.Substring(first, last - first + 1);
        }
        try
        {
            return JsonConvert.DeserializeObject<LlmResponse>(raw);
        }
        catch { return null; }
    }

    string Validate(string g) => Array.IndexOf(ValidGestures, g) >= 0 ? g : "considering";
}

public class LlmResponse
{
    public string answer { get; set; } = "";
    public string gesture { get; set; } = "";
    public string speech { get; set; } = "";
}
