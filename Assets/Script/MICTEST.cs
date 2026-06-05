using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(AudioSource))]

public class MICTEST: MonoBehaviour

{



    public int maxRecordingSeconds = 8;



    private AudioSource _audioSource;

    private AudioClip _clip;

    private bool _isRecording = false;



    void Start()

    {

        _audioSource = GetComponent<AudioSource>();



       

        if (Microphone.devices.Length == 0)

        {

            Debug.LogError("[MicTest] No microphone found! Check your system settings.");

            return;

        }



        Debug.Log($"[MicTest] Found {Microphone.devices.Length} microphone(s):");

        foreach (string device in Microphone.devices)

            Debug.Log($"  - {device}");



        Debug.Log("[MicTest] Ready! Hold [Space] to record, release to play back.");

    }



    void Update()

    {

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !_isRecording)

        {

            _isRecording = true;

            
            _clip = Microphone.Start(null, false, maxRecordingSeconds, 16000);

            Debug.Log("[MicTest] Recording... (release [Space] to stop)");

        }



        if (Keyboard.current.spaceKey.wasReleasedThisFrame && _isRecording)

        {

            _isRecording = false;



            int samplesRecorded = Microphone.GetPosition(null);

            Microphone.End(null);



            if (samplesRecorded < 1600) 

            {

                Debug.Log("[MicTest] Too short — try holding [Space] a bit longer.");

                return;

            }




            float[] samples = new float[samplesRecorded];

            _clip.GetData(samples, 0);

            AudioClip trimmed = AudioClip.Create("recording", samplesRecorded, 1, 16000, false);

            trimmed.SetData(samples, 0);



            float duration = samplesRecorded / 16000f;

            Debug.Log($"[MicTest] Recorded {duration:F1}s — playing back now...");



            _audioSource.clip = trimmed;

            _audioSource.Play();

        }

    }

}

