using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using Utilities.Extensions;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Models;
using Microphone = FrostweepGames.MicrophonePro.Microphone;

public class TestRecording : MonoBehaviour
{
    [SerializeField] AudioSource recording;
    //[SerializeField] AudioSource monitor;
    [SerializeField] int recordingLength = 10;
    [SerializeField] float micSensitivity = 100.0f; // Used to adjust the sensitivity of the mic volume
    [SerializeField] float recordingVolumeThreshold = 0.8f; // Mic volume threshold for triggering recording
    [SerializeField] float recordingTimeThreshold = 1.5f; // Time threshold for stopping recording after mic volume drops below threshold
    [SerializeField] int recordingSampleRate = 16000;
    [SerializeField] TextMeshProUGUI closeCaptionUI;

    [SerializeField] private bool enableDebug;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private AudioSource audioSource;

    //private OpenAIClient openAI;
    private readonly List<Message> chatMessages = new List<Message>();
    private CancellationTokenSource lifetimeCancellationTokenSource;

    OpenAIClient openAI;
    string result;
    bool isResponsePlaying = false;
    bool isResponseProcessing = false;
    bool isRecording = false;
    float loudness = 0f;
    float timeBelowThreshold = 1.0f; // Used to keep track of how long the mic volume has been below the threshold
    bool isAudioDataLoaded = false;
    AudioSource playback;
    string micDevice = "";
    //AudioSource monitor;

    // Start recording with built-in Microphone and play the recorded audio right away
    void Start()
    {
        playback = gameObject.AddComponent<AudioSource>();
        //monitor = gameObject.AddComponent<AudioSource>();
        //Microphone.PermissionChangedEvent += PermissionChangedEvent;

        openAI = new OpenAIClient();
    }

    public void StartMonitoring() { 
        MonitorMic();
        StartCoroutine(CheckAudioDataLoadState());
    }

    void Update()
    {
        UpdateRecording();
    }

    //private void OnDestroy()
    //{
    //    //Microphone.RecordStreamDataEvent -= RecordStreamDataEventHandler;
    //    //Microphone.PermissionChangedEvent -= PermissionChangedEvent;
    //}

    void MonitorMic()
    {
        recording.volume = 0; // Mute the first audio source to prevent mic monitoring
        recording.clip = Microphone.Start(micDevice, true, recordingLength, recordingSampleRate);
        //while (!(Microphone.GetPosition(null) > 0)) { }
        recording.Play();
        Debug.Log("Monitoring Microphone");
    }

    void UpdateRecording()
    {
        if (!isAudioDataLoaded) {
            return;
	    }

        if (!isResponsePlaying)
        {
            //loudness = GetAveragedVolume() * micSensitivity;
            //StartCoroutine(GetAveragedVolume());
            GetAveragedVolume();
        }

        if (!isRecording && !isResponseProcessing && loudness > recordingVolumeThreshold)
        {
            isRecording = true;
            //recording.Stop(); // Maybe I need to reset the audioClip.
            recording.clip = Microphone.Start(micDevice, true, recordingLength, recordingSampleRate);
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

    //float GetAveragedVolume()
    void GetAveragedVolume()
    {
        //Debug.Log("Getting Mic position");
        float[] data = new float[512];
        int micPosition = Microphone.GetPosition(micDevice) - (512+1); // Get the position 1024 samples ago
        if (micPosition < 0) {
            loudness = 0f;
            return; // Return 0 if the position is negative
	    }
        //Debug.Log("Getting audio data");
        recording.clip.GetData(data, micPosition);
        float a = 0;
        foreach (float s in data) {
            a += Mathf.Abs(s);
        }
        //Debug.Log("Calculating loudness");
        //return a / 1024;
        loudness = a / 512 * micSensitivity;
    }

    IEnumerator CheckAudioDataLoadState() {
        while (recording.clip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null;
        }
        isAudioDataLoaded = true;
        Debug.Log("Audio data loaded");
    }

    //IEnumerator GetAveragedVolume()
    //{
    //    while (recording.clip.loadState != AudioDataLoadState.Loaded) {
    //        yield return null;
    //    }

    //    float[] data = new float[1024];
    //    int micPosition = Microphone.GetPosition(null) - (1024+1); // Get the position 1024 samples ago
    //    if (micPosition < 0) {
    //        //return 0; // Return 0 if the position is negative
    //        loudness = 0f;
	   // } else { 
    //        recording.clip.GetData(data, micPosition);
    //        float a = 0;
    //        foreach (float s in data) {
    //            a += Mathf.Abs(s);
    //        }
    //        //return a / 1024;
    //        loudness = a / 1024 * micSensitivity;
	   // }
    //}

    public void StartRecording()
    {
        recording.clip = Microphone.Start(micDevice, true, recordingLength, recordingSampleRate);
    }

    //public void StopRecording()
    //{
    //    Microphone.End(Microphone.devices[0]);
    //    recording.Stop();
    //    StartCoroutine(AsyncSaveWav());
    //}

    public void StopRecording()
    {
        Microphone.End(micDevice);
        playback.PlayOneShot(recording.clip);
        SaveWav.Save(Application.persistentDataPath + "/recordingCache.wav", recording.clip, true);
        Debug.Log(Application.persistentDataPath + "/RecordingCache.wav");
        TranscriptAudio();
        MonitorMic();
    }

    IEnumerator AsyncSaveWav() {
        while (recording.clip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null;
        }
        SaveWav.Save(Application.persistentDataPath + "/recordingCache.wav", recording.clip, true);
        TranscriptAudio();
        MonitorMic();
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
        //AudioTranscriptionRequest transcriptionRequest = new AudioTranscriptionRequest(
        //    recording.clip,
        //    model: "whisper-1",
        //    responseFormat: AudioResponseFormat.Json,
        //    temperature: 0.1f,
        //    language: "en"
        //);
        result = await openAI.AudioEndpoint.CreateTranscriptionAsync(transcriptionRequest);
        Debug.Log(result);
        closeCaptionUI.text = result;
        isResponseProcessing = false;
    }

    private void PermissionChangedEvent(bool granted)
    {
        // handle current permission status

        //if (permissionGranted != granted)
        //    RefreshMicrophoneDevicesButtonOnclickHandler();

        //permissionGranted = granted;

        Debug.Log($"Permission state changed on: {granted}");
        MonitorMic();
    }
}