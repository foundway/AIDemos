using UnityEngine;
using TMPro;
using System.Collections;
using OpenAI;
using OpenAI.Audio;
using Microphone = FrostweepGames.MicrophonePro.Microphone; // Required for using Microphone on WebGL. Purchased on https://assetstore.unity.com/packages/tools/input-management/microphone-pro-webgl-mobiles-desktop-7998

public class TestRecording : MonoBehaviour
{
    [SerializeField] int recordingLength = 10;
    [SerializeField] float micSensitivity = 100.0f; // Used to adjust the sensitivity of the mic volume
    [SerializeField] float recordingVolumeThreshold = 0.8f; // Mic volume threshold for triggering recording
    [SerializeField] float recordingTimeThreshold = 1.5f; // Time threshold for stopping recording after mic volume drops below threshold
    [SerializeField] int recordingSampleRate = 44100;
    [SerializeField] TextMeshProUGUI closeCaptionUI;
    [SerializeField] int monitorSampleSize = 1024;

    AudioSource recording;
    OpenAIClient openAI;
    string result;
    bool isResponsePlaying = false;
    bool isResponseProcessing = false;
    bool isRecording = false;
    float loudness = 0f;
    float timeBelowThreshold = 1.0f; // Used to keep track of how long the mic volume has been below the threshold
    bool isAudioDataLoaded;

    void Start()
    {
        Microphone.PermissionChangedEvent += PermissionChangedEvent;
        openAI = new OpenAIClient();
        recording = gameObject.AddComponent<AudioSource>();
    }

    public void StartMonitoring() 
    {
        Invoke("MonitorMic", 0.5f); // need some delay to use WebGL microphone correctly
    }

    void Update()
    {
        UpdateRecording();
    }

    void OnDestroy()
    {
        Microphone.PermissionChangedEvent -= PermissionChangedEvent;
    }

    void MonitorMic()
    {
        recording.clip = Microphone.Start(Microphone.devices[0], true, recordingLength, recordingSampleRate);
        Debug.Log("Start Monitoring "+Microphone.devices[0]);
        StartCoroutine(CheckAudioDataLoadState());
    }

    void UpdateRecording()
    {
        if (!isAudioDataLoaded)
        {
            return;
        }

        if (!isResponsePlaying)
        {
            loudness = GetAveragedVolume() * micSensitivity;
        }

        if (!isRecording && !isResponseProcessing && loudness > recordingVolumeThreshold)
        {
            isRecording = true;
            Debug.Log("Start Recording");
        }
        else if (isRecording && loudness < recordingVolumeThreshold)
        {
            timeBelowThreshold += Time.deltaTime;
            if (timeBelowThreshold >= recordingTimeThreshold)
            {
                Debug.Log("Stop Recording");
                StopRecording();
                isResponseProcessing = true;
                isRecording = false;
                timeBelowThreshold = 0.0f;
                loudness = 0.0f;
            }
        }
        else if (isRecording && loudness > recordingVolumeThreshold)
        {
            timeBelowThreshold = 0.0f;
        }
    }

    float GetAveragedVolume()
    {
        float[] data = new float[monitorSampleSize];
        int micPosition = Microphone.GetPosition(Microphone.devices[0]) - (monitorSampleSize + 1); // Get the position X samples ago
        if (micPosition < 0)
        {
            return 0; // Return 0 if the position is negative
        }
        Microphone.GetData(data, micPosition); // use this instead of audioClip.GetData, which doesn't work in WebGL
        float a = 0;
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        Debug.Log("Mic data of " + Microphone.devices[0] + " is " + a/monitorSampleSize);
        return a / monitorSampleSize;
    }

    public void StartRecording()
    {
        recording.clip = Microphone.Start(Microphone.devices[0], true, recordingLength, recordingSampleRate);
    }

    public void StopRecording()
    {
        Microphone.End(Microphone.devices[0]);
        StartCoroutine(SaveWavAsync(recording));
    }

    public async void TranscriptAudio()
    {
        Debug.Log("Asking Wisper-1...");
        AudioTranscriptionRequest transcriptionRequest = new AudioTranscriptionRequest(
            Application.persistentDataPath + "/recordingCache.wav",
            model: "whisper-1",
            responseFormat: AudioResponseFormat.Json,
            temperature: 0.1f,
            language: "en"
        );

        result = await openAI.AudioEndpoint.CreateTranscriptionAsync(transcriptionRequest);
        Debug.Log(result);
        closeCaptionUI.text = result;
        isResponseProcessing = false;
        MonitorMic(); // Resume monitor mic after getting the transcription
    }

    void PermissionChangedEvent(bool granted)
    {
        Debug.Log($"Permission state changed on: {granted}"+"device: "+Microphone.devices[0]);
    }

    IEnumerator CheckAudioDataLoadState()
    {
        Debug.Log("Checking audio data load state at " + System.DateTime.Now);
        while (recording.clip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null;
        }
        isAudioDataLoaded = true;
        Debug.Log("Audio data loaded at " + System.DateTime.Now);
    }

    IEnumerator SaveWavAsync(AudioSource src) // Required for WebGL because AudioClip.GetData is async
    {
        while (recording.clip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null;
        }
        SaveWav.Save("recordingCache", src.clip, true);
        TranscriptAudio();
    }
}