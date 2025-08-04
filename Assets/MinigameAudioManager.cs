using UnityEngine;
using System.Collections;

namespace DigitalForensicsQuiz
{
    public class MinigameAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Music Clips")]
        [SerializeField] private AudioClip gameplayBGM;
        [SerializeField] private AudioClip menuBGM;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip glitchSFX;
        [SerializeField] private AudioClip typewriterSFX;
        [SerializeField] private AudioClip successSFX;
        [SerializeField] private AudioClip failSFX;
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip dragDropSFX;

        [Header("Audio Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private float voiceVolume = 0.9f;

        // Singleton instance
        public static MinigameAudioManager Instance { get; private set; }

        // Events for audio state changes
        public System.Action<bool> OnMusicStateChanged;
        public System.Action<bool> OnSFXStateChanged;

        private bool isMusicEnabled = true;
        private bool isSFXEnabled = true;
        private Coroutine currentTypewriterCoroutine;

        #region Initialization

        private void Awake()
        {
            // Singleton pattern with proper cleanup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            // Only apply DontDestroyOnLoad if this is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("MinigameAudioManager should be a root GameObject for DontDestroyOnLoad to work properly.");
            }

            InitializeAudioSources();
            LoadAudioSettings();
        }

        private void Start()
        {
            // Start with menu music if available
            if (menuBGM != null)
            {
                PlayMusic(menuBGM, true);
            }
        }

        private void InitializeAudioSources()
        {
            // Create audio sources if they don't exist
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.loop = false;
                voiceSource.playOnAwake = false;
            }

            UpdateVolumeSettings();
        }

        #endregion

        #region Music Control

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (!isMusicEnabled || clip == null || musicSource == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null && isMusicEnabled)
            {
                musicSource.UnPause();
            }
        }

        public void FadeOutMusic(float duration = 1f)
        {
            if (musicSource != null)
            {
                StartCoroutine(FadeAudioSource(musicSource, musicSource.volume, 0f, duration, true));
            }
        }

        public void FadeInMusic(float duration = 1f)
        {
            if (musicSource != null && isMusicEnabled)
            {
                float targetVolume = musicVolume * masterVolume;
                StartCoroutine(FadeAudioSource(musicSource, 0f, targetVolume, duration, false));
            }
        }

        #endregion

        #region SFX Control

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (!isSFXEnabled || clip == null || sfxSource == null) return;

            sfxSource.PlayOneShot(clip, volumeScale);
        }

        public void PlayGlitchSFX()
        {
            PlaySFX(glitchSFX);
        }

        public void PlaySuccessSFX()
        {
            PlaySFX(successSFX);
        }

        public void PlayFailSFX()
        {
            PlaySFX(failSFX);
        }

        public void PlayButtonClickSFX()
        {
            PlaySFX(buttonClickSFX, 0.7f);
        }

        public void PlayDragDropSFX()
        {
            PlaySFX(dragDropSFX, 0.8f);
        }

        #endregion

        #region Typewriter Audio

        public void StartTypewriterSFX(float duration, float interval = 0.03f)
        {
            StopTypewriterSFX();
            
            if (isSFXEnabled && typewriterSFX != null)
            {
                currentTypewriterCoroutine = StartCoroutine(PlayTypewriterSFX(duration, interval));
            }
        }

        public void StopTypewriterSFX()
        {
            if (currentTypewriterCoroutine != null)
            {
                StopCoroutine(currentTypewriterCoroutine);
                currentTypewriterCoroutine = null;
            }
        }

        private IEnumerator PlayTypewriterSFX(float duration, float interval)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                if (isSFXEnabled && typewriterSFX != null && sfxSource != null)
                {
                    sfxSource.PlayOneShot(typewriterSFX, 0.3f);
                }
                
                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }
            
            currentTypewriterCoroutine = null;
        }

        #endregion

        #region Game State Audio

        public void OnGameStart()
        {
            if (gameplayBGM != null)
            {
                FadeOutMusic(0.5f);
                StartCoroutine(DelayedMusicStart(gameplayBGM, 0.7f));
            }
        }

        public void OnGameEnd(bool success)
        {
            StopTypewriterSFX();
            
            if (success)
            {
                PlaySuccessSFX();
            }
            else
            {
                PlayFailSFX();
            }
        }

        public void OnQuestionCorrect()
        {
            PlaySuccessSFX();
        }

        public void OnQuestionIncorrect()
        {
            PlayFailSFX();
        }

        public void OnMenuReturn()
        {
            if (menuBGM != null)
            {
                FadeOutMusic(0.5f);
                StartCoroutine(DelayedMusicStart(menuBGM, 0.7f));
            }
        }

        private IEnumerator DelayedMusicStart(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayMusic(clip, true);
            FadeInMusic(0.5f);
        }

        #endregion

        #region Settings Control

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumeSettings();
            SaveAudioSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumeSettings();
            SaveAudioSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumeSettings();
            SaveAudioSettings();
        }

        public void SetMusicEnabled(bool enabled)
        {
            isMusicEnabled = enabled;
            
            if (!enabled)
            {
                PauseMusic();
            }
            else
            {
                ResumeMusic();
            }
            
            OnMusicStateChanged?.Invoke(enabled);
            SaveAudioSettings();
        }

        public void SetSFXEnabled(bool enabled)
        {
            isSFXEnabled = enabled;
            
            if (!enabled)
            {
                StopTypewriterSFX();
            }
            
            OnSFXStateChanged?.Invoke(enabled);
            SaveAudioSettings();
        }

        private void UpdateVolumeSettings()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
            
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume * masterVolume;
            }
            
            if (voiceSource != null)
            {
                voiceSource.volume = voiceVolume * masterVolume;
            }
        }

        #endregion

        #region Utility Methods

        private IEnumerator FadeAudioSource(AudioSource source, float startVolume, float endVolume, float duration, bool stopAfterFade)
        {
            if (source == null) yield break;

            source.volume = startVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                source.volume = Mathf.Lerp(startVolume, endVolume, normalizedTime);
                yield return null;
            }

            source.volume = endVolume;

            if (stopAfterFade && endVolume <= 0f)
            {
                source.Stop();
            }
        }

        private void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("Audio_MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("Audio_SFXVolume", 0.8f);
            isMusicEnabled = PlayerPrefs.GetInt("Audio_MusicEnabled", 1) == 1;
            isSFXEnabled = PlayerPrefs.GetInt("Audio_SFXEnabled", 1) == 1;
            
            UpdateVolumeSettings();
        }

        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("Audio_MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("Audio_MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("Audio_SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("Audio_MusicEnabled", isMusicEnabled ? 1 : 0);
            PlayerPrefs.SetInt("Audio_SFXEnabled", isSFXEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region Properties

        public bool IsMusicEnabled => isMusicEnabled;
        public bool IsSFXEnabled => isSFXEnabled;
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public bool IsPlayingMusic => musicSource != null && musicSource.isPlaying;

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            StopTypewriterSFX();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion
    }
}