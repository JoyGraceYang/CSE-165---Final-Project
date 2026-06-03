// GameDriver.cs — the thin Unity shell. Attach to a GameObject.
// Week-1 milestone: typed question -> LLM -> logic -> Debug.Log the gesture.
// Voice (Person 4) and animation (Person 2) are STUBBED. Swap them in later.

using UnityEngine;
using TMPro;

public class GameDriver : MonoBehaviour
{
    public TMP_InputField inputField;   // drag your input field here in the Inspector
    public TMP_Text avatarSpeechText;   // optional: shows what the avatar "says"

    GameLogic game;

    void Start()
    {
        game = new GameLogic();
        Debug.Log($"[debug] secret object: {game.SecretObject}");  // remove before demo
        if (inputField != null)
            inputField.onSubmit.AddListener(OnPlayerQuestion);
    }

    void OnPlayerQuestion(string question)
    {
        inputField.text = "";
        inputField.ActivateInputField();

        // === SEAM 1: Person 4's voice pipeline. For now, stubbed. ===
        // Real version: send `question` + history to the LLM, await the JSON string.
        string llmJson = SendToLLM_Stub(question);

        TurnResult result = game.ProcessTurn(llmJson);

        // === SEAM 2: Person 2's animation. For now, just log it. ===
        TriggerGesture(result.gesture);

        if (avatarSpeechText != null) avatarSpeechText.text = result.speech;
        Debug.Log($"[avatar] {result.speech}");

        switch (result.outcome)
        {
            case Outcome.PlayerWins: Debug.Log(">>> PLAYER WINS <<<"); break;
            case Outcome.AvatarWins: Debug.Log(">>> AVATAR WINS <<<"); break;
            case Outcome.GaveUp:     Debug.Log(">>> player gave up <<<"); break;
        }
    }

    // Person 2 replaces the body of this with the real Animator trigger.
    void TriggerGesture(string tag)
    {
        Debug.Log($"[gesture] {tag}");
    }

    // Person 4 replaces this with the real LLM HTTP call (UnityWebRequest).
    // Hardcoded for now so the editor loop runs with zero network code.
    string SendToLLM_Stub(string question)
    {
        return "{\"answer\":\"no\",\"gesture\":\"considering\",\"speech\":\"Nope, not that.\"}";
    }
}
