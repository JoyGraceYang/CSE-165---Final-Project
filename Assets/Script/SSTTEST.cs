using UnityEngine;

using UnityEngine.InputSystem;

using System.Speech.Recognition;

[RequireComponent(typeof(AudioSource))]

public class STTTest : MonoBehaviour

{

    public int maxRecordingSeconds = 8;



    private AudioSource _audioSource;

    private AudioClip _clip;

    private bool _isRecording = false;

    private SpeechRecognitionEngine _recognizer;



    void Start()

    {

        _audioSource = GetComponent<AudioSource>();




        _recognizer = new SpeechRecognitionEngine();





        _recognizer.LoadGrammar(new DictationGrammar());

        _recognizer.SetInputToDefaultAudioDevice();




        _recognizer.SpeechRecognized += OnSpeechRecognized;

        _recognizer.SpeechRecognitionRejected += OnSpeechRejected;



        Debug.Log("[STTTest] Ready! Hold [Space] to record your question, release to transcribe.");

    }



    void Update()

    {

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !_isRecording)

        {

            _isRecording = true;

            _clip = Microphone.Start(null, false, maxRecordingSeconds, 16000);

            _recognizer.RecognizeAsync(RecognizeMode.Single);

            Debug.Log("[STTTest] Recording... ask your yes/no question, then release [Space].");

        }



        if (Keyboard.current.spaceKey.wasReleasedThisFrame && _isRecording)

        {

            _isRecording = false;

            Microphone.End(null);

            Debug.Log("[STTTest] Processing speech...");

        }

    }



    private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)

    {

        string transcript = e.Result.Text;

        float confidence = e.Result.Confidence;



        Debug.Log($"[STTTest] Transcript: \"{transcript}\" (confidence: {confidence:P0})");





    }



    private void OnSpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)

    {

        Debug.LogWarning("[STTTest] Couldn't understand — try speaking more clearly or closer to the mic.");

    }



    void OnDestroy()

    {



        if (_recognizer != null)

        {

            _recognizer.RecognizeAsyncStop();

            _recognizer.Dispose();

        }

    }

}

