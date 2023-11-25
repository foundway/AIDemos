using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading;
using Utilities.Extensions;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Models;

public class TestRecording : MonoBehaviour
{
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
    AudioSource recording;

    OpenAIClient openAI;
    string audioPath;
    string result;
    bool isResponsePlaying = false;
    bool isResponseProcessing = false;
    bool isRecording = false;
    float loudness = 0f;
    float timeBelowThreshold = 1.0f; // Used to keep track of how long the mic volume has been below the threshold

    // Start recording with built-in Microphone and play the recorded audio right away
    void Start()
    {
        openAI = new OpenAIClient();
        recording = gameObject.AddComponent<AudioSource>();

        MonitorMic();
    }

    private void Update()
    {
        UpdateRecording();
    }

    void MonitorMic()
    {
        recording.volume = 0; // Mute the first audio source to prevent mic monitoring
        recording.clip = Microphone.Start(null, true, recordingLength, recordingSampleRate);
        recording.loop = true;
        while (!(Microphone.GetPosition(null) > 0)) { } // this is important but need a better way
        recording.Play();
    }

    void UpdateRecording()
    {
        if (!isResponsePlaying)
        {
            loudness = GetAveragedVolume() * micSensitivity;
        }

        if (!isRecording && !isResponseProcessing && loudness > recordingVolumeThreshold)
        {
            isRecording = true;
            //StartRecording();
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
        float[] data = new float[1024];
        int micPosition = Microphone.GetPosition(null) - (1024 + 1); // Get the position 1024 samples ago
        if (micPosition < 0)
        {
            return 0; // Return 0 if the position is negative
        }
        recording.clip.GetData(data, micPosition);
        float a = 0;
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / 1024;
    }

    public void StartRecording()
    {
        recording.clip = Microphone.Start(Microphone.devices[0], true, recordingLength, recordingSampleRate);
    }

    public void StopRecording()
    {
        Microphone.End(Microphone.devices[0]);
        SaveWav.Save("recordingCache", recording.clip, true);
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

        result = await openAI.AudioEndpoint.CreateTranscriptionAsync(transcriptionRequest);
        Debug.Log(result);
        closeCaptionUI.text = result;
        isResponseProcessing = false;
    }
}