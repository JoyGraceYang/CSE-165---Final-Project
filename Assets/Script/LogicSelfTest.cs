// LogicSelfTest.cs — attach to any empty GameObject, press Play, read the Console.
// No InputField, no Canvas needed. This is the quickest "does my logic work in Unity" check.
// Delete it (or the GameObject) once you've confirmed everything's green.

using UnityEngine;

public class LogicSelfTest : MonoBehaviour
{
    int pass = 0, fail = 0;

    void Check(string name, bool ok)
    {
        if (ok) { pass++; Debug.Log("PASS  " + name); }
        else    { fail++; Debug.LogError("FAIL  " + name); }
    }

    void Start()
    {
        var g = new GameLogic();
        var r = g.ProcessTurn("this is not json at all");
        Check("garbage -> Continue + thinking", r.outcome == Outcome.Continue && r.gesture == "thinking");

        g = new GameLogic();
        r = g.ProcessTurn("```json\n{\"answer\":\"yes\",\"gesture\":\"impressed\",\"speech\":\"hi\"}\n```");
        Check("fenced JSON parses -> impressed", r.gesture == "impressed" && r.speech == "hi");

        g = new GameLogic();
        r = g.ProcessTurn("{\"answer\":\"no\",\"gesture\":\"bogus_typo\",\"speech\":\"x\"}");
        Check("unknown gesture -> considering", r.gesture == "considering");

        g = new GameLogic();
        var h1 = g.ProcessTurn("{\"answer\":\"hint\",\"gesture\":\"reluctant\",\"speech\":\"starts with M\"}");
        var h2 = g.ProcessTurn("{\"answer\":\"hint\",\"gesture\":\"reluctant\",\"speech\":\"again?\"}");
        Check("first hint allowed", h1.gesture == "reluctant");
        Check("second hint refused", h2.speech == "Nope, one hint per game!" && h2.gesture == "playful_no");

        g = new GameLogic();
        int before = g.QuestionsLeft;
        g.ProcessTurn("{\"answer\":\"invalid\",\"gesture\":\"thinking\",\"speech\":\"yes/no only\"}");
        Check("invalid doesn't burn a question", g.QuestionsLeft == before);

        g = new GameLogic();
        before = g.QuestionsLeft;
        g.ProcessTurn("{\"answer\":\"yes\",\"gesture\":\"considering\",\"speech\":\"yep\"}");
        Check("yes burns one question", g.QuestionsLeft == before - 1);

        g = new GameLogic();
        r = g.ProcessTurn("{\"answer\":\"correct\",\"gesture\":\"defeated\",\"speech\":\"you got me\"}");
        Check("correct -> PlayerWins", r.outcome == Outcome.PlayerWins);

        g = new GameLogic();
        r = g.ProcessTurn("{\"answer\":\"giveup\",\"gesture\":\"shrug\",\"speech\":\"better luck\"}");
        Check("giveup -> GaveUp + reveals object", r.outcome == Outcome.GaveUp && r.speech.Contains(g.SecretObject));

        g = new GameLogic();
        TurnResult last = default;
        for (int i = 0; i < 20; i++)
            last = g.ProcessTurn("{\"answer\":\"no\",\"gesture\":\"considering\",\"speech\":\"nope\"}");
        Check("20 questions -> AvatarWins + victorious", last.outcome == Outcome.AvatarWins && last.gesture == "victorious");

        Debug.Log($"=== {pass} passed, {fail} failed ===");
    }
}
