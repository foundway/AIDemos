using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using Microphone = FrostweepGames.MicrophonePro.Microphone;
using System.Collections;

public class SimpleRecording : MonoBehaviour
{
    //[SerializeField] TextMeshProUGUI textUI;
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
    }

    public void StartRecording()
    {
        recording.clip = Microphone.Start(Microphone.devices[0], true, 10, 44100);
        Debug.Log("freq = " + recording.clip.frequency);
    }

    public void StopRecording()
    {
        Microphone.End(Microphone.devices[0]);
        playback.PlayOneShot(recording.clip);
        //SaveWav.Save(Application.persistentDataPath + "/recordingCache.wav", recording.clip, true);
        //TranscriptAudio();
        //StartCoroutine(AsyncSaveWav(recording));
        SaveWav.Save("recordingCache", recording.clip, true);
        TranscriptAudio();
    }

    public void PlayTestAudio() {
        testAudio.Play();
        //StartCoroutine(AsyncSaveWav(testAudio));
        SaveWav.Save("recordingCache", testAudio.clip, true);
        TranscriptAudio();
    }

    //IEnumerator AsyncSaveWav(AudioSource src)
    //{
    //    while (src.clip.loadState != AudioDataLoadState.Loaded)
    //    {
    //        yield return null;
    //    }
    //}

    public async void TranscriptAudio()
    {
        Debug.Log("Asking Wisper-1...");
        AudioTranscriptionRequest transcriptionRequest = new AudioTranscriptionRequest(
            Application.persistentDataPath + "/recordingCache.wav",
            //recording.clip,
            model: "whisper-1",
            responseFormat: AudioResponseFormat.Json,
            temperature: 0.1f
            //language: "en"
        );
        string result = await openAI.AudioEndpoint.CreateTranscriptionAsync(transcriptionRequest);
        Debug.Log(result);
        textUI.text = result;
    }
}