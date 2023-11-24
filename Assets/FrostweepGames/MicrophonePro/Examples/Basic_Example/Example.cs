using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

using Microphone = FrostweepGames.MicrophonePro.Microphone;

namespace FrostweepGames.MicrophonePro.Examples
{
    [RequireComponent(typeof(AudioSource))]
    public class Example : MonoBehaviour
    {
        private AudioClip _workingClip;

        public Text permissionStatusText;

        public Text recordingStatusText;

        public Dropdown devicesDropdown;

        public AudioSource audioSource;

        public Button startRecordButton,
                      stopRecordButton,
                      playRecordedAudioButton,
                      requestPermissionButton,
                      refreshDevicesButton;

        public List<AudioClip> recordedClips;

        public int frequency = 44100;

        public int recordingTime = 120;

        [FrostweepGames.Plugins.ReadOnly]
        public string selectedDevice;

        [FrostweepGames.Plugins.ReadOnly]
        public bool permissionGranted;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();

            startRecordButton.onClick.AddListener(StartRecord);
            stopRecordButton.onClick.AddListener(StopRecord);
            playRecordedAudioButton.onClick.AddListener(PlayRecordedAudio);
            requestPermissionButton.onClick.AddListener(RequestPermission);
            refreshDevicesButton.onClick.AddListener(RefreshMicrophoneDevicesButtonOnclickHandler);

            devicesDropdown.onValueChanged.AddListener(DevicesDropdownValueChangedHandler);

            selectedDevice = string.Empty;

            Microphone.RecordStreamDataEvent += RecordStreamDataEventHandler;
            Microphone.PermissionChangedEvent += PermissionChangedEvent;

            // no need to request permission in webgl. it does automatically
            requestPermissionButton.interactable = Application.platform != RuntimePlatform.WebGLPlayer;
        }

		private void OnDestroy()
		{
            Microphone.RecordStreamDataEvent -= RecordStreamDataEventHandler;
            Microphone.PermissionChangedEvent -= PermissionChangedEvent;
        }

        private void Update()
		{
            permissionStatusText.text = $"Microphone permission for device: '{selectedDevice}' is '{(permissionGranted ? "<color=green>granted</color>" : "<color=red>denined</color>")}'";
            recordingStatusText.text = $"Recording status is '{(Microphone.IsRecording(selectedDevice) ? "<color=green>recording</color>" : "<color=yellow>idle</color>")}'";
        }

        /// <summary>
        /// Works only in WebGL
        /// </summary>
        /// <param name="samples"></param>
        private void RecordStreamDataEventHandler(Microphone.StreamData streamData)
        {
            // handle streaming recording data
        }

        private void PermissionChangedEvent(bool granted)
        {
            // handle current permission status

            if(permissionGranted != granted)
                RefreshMicrophoneDevicesButtonOnclickHandler();

            permissionGranted = granted;

            Debug.Log($"Permission state changed on: {granted}");
        }

        private void RefreshMicrophoneDevicesButtonOnclickHandler()
		{
            devicesDropdown.ClearOptions();
            devicesDropdown.AddOptions(Microphone.devices.ToList());
            DevicesDropdownValueChangedHandler(0);
        }

        private void RequestPermission()
        {
            Microphone.RequestPermission();
        }

        private void StartRecord()
        {
            _workingClip = Microphone.Start(selectedDevice, false, recordingTime, frequency);
        }

        private void StopRecord()
        {
            Microphone.End(selectedDevice);

            PlayRecordedAudio();
        }

        private void PlayRecordedAudio()
        {
            if (_workingClip == null)
                return;

            audioSource.clip = _workingClip;
            audioSource.Play();

            Debug.Log("start playing");
        }

        private void DevicesDropdownValueChangedHandler(int index)
		{
            if (index < Microphone.devices.Length)
            {
                selectedDevice = Microphone.devices[index];
            }
        }
    }
}