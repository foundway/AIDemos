using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using OpenAI;
using OpenAI.Audio;
using Microphone = FrostweepGames.MicrophonePro.Microphone; // Required for using Microphone on WebGL. Purchased on https://assetstore.unity.com/packages/tools/input-management/microphone-pro-webgl-mobiles-desktop-79989

public class SimpleRecording : MonoBehaviour
{
    [SerializeField] Text textUI;
    [SerializeField] AudioSource testAudio;
    AudioSource recording;
    AudioSource playback;
    OpenAIClient openAI;

    void Start()
    {
        recording = gameObject.AddComponent<AudioSource>();
        playback = gameObject.AddComponent<AudioSource>();
        openAI = new OpenAIClient();
        var forceInitiateMicrophone = Microphone.GetPosition("");
    }

    public void StartRecording()
    {
        recording.clip = Microphone.Start(Microphone.devices[0], true, 10, 44100);
    }

    public void StopRecording()
    {
        Microphone.End(Microphone.devices[0]);
        playback.PlayOneShot(recording.clip);
        StartCoroutine(SaveWavAsync(recording));
    }

    public void PlayTestAudio() 
    {
        testAudio.PlayOneShot(testAudio.clip);
        StartCoroutine(SaveWavAsync(testAudio));
    }

    IEnumerator SaveWavAsync(AudioSource src) // Required for WebGL because AudioClip.GetData is async
    {
        while (src.clip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null;
        }
        SaveWav.Save("recordingCache", src.clip, true);
        TranscriptAudio();
    }

    async void TranscriptAudio()
    {
        Debug.Log("Asking Wisper-1...");
        AudioTranscriptionRequest transcriptionRequest = new AudioTranscriptionRequest(
            Application.persistentDataPath + "/recordingCache.wav",
            model: "whisper-1",
            responseFormat: AudioResponseFormat.Json,
            temperature: 0.1f,
            language: "en"
        );
        string result = await openAI.AudioEndpoint.CreateTranscriptionAsync(transcriptionRequest);
        Debug.Log(result);
        textUI.text = result;
    }
}