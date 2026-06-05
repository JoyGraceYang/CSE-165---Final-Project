using UnityEngine;
using UnityEngine.InputSystem;
using System.Diagnostics;

public class TTS : MonoBehaviour
{
    void Start()
    {
        UnityEngine.Debug.Log("[TTSTest] Ready! Press [Space] for Yes, [A] for No.");
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            Speak("Yes, it is.");

        if (Keyboard.current.aKey.wasPressedThisFrame)
            Speak("No.");
    }

    public void Speak(string text)
    {
        UnityEngine.Debug.Log($"[TTSTest] Speaking: \"{text}\"");
        Process.Start(new ProcessStartInfo
        {
            FileName = "PowerShell",
            Arguments = $"-Command \"Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{text}')\"",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }
}