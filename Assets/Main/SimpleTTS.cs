using UnityEngine;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Models;
using System.Threading;
using System;

public class SimpleTTS : MonoBehaviour
{
    [SerializeField] TMPro.TMP_InputField inputField;
    OpenAIClient openAI;
    AudioSource audioSource;
    private CancellationTokenSource lifetimeCancellationTokenSource;

    private void Awake()
    {
        lifetimeCancellationTokenSource = new CancellationTokenSource();
    }

    // Start is called before the first frame update
    void Start()
    {
        openAI = new OpenAIClient();
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnDestroy()
    {
        lifetimeCancellationTokenSource.Cancel();
        lifetimeCancellationTokenSource.Dispose();
        lifetimeCancellationTokenSource = null;
    }

    public void SubmitRequest()
    {
        GenerateSpeech(inputField.text);
    }

    private async void GenerateSpeech(string text)
    {
        try
        {
            var request = new SpeechRequest(text, Model.TTS_1, voice: SpeechVoice.Nova);
            Debug.Log("Asking TTS_1...");
            openAI.AudioEndpoint.EnableDebug = true;
            var (clipPath, clip) = await openAI.AudioEndpoint.CreateSpeechAsync(request, lifetimeCancellationTokenSource.Token);
            audioSource.clip = clip;
            audioSource.Play();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
