using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AudioMgr : MonoSingle<AudioMgr>
{
    private const string MusicVolumeKey = "audio_music_volume";
    private const string SoundVolumeKey = "audio_sound_volume";
    private const string MusicMutedKey = "audio_music_muted";
    private const string SoundMutedKey = "audio_sound_muted";

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool musicMuted;
    [SerializeField] private bool sfxMuted;

    private Loader loader;
    private readonly Dictionary<string, AudioClip> audioClipCache = new();
    private readonly Dictionary<string, List<Action<AudioClip>>> pendingClipCallbacks = new();
    private readonly List<ActiveSfxPlayback> activeSfxPlaybacks = new();
    private Transform sfxRoot;

    protected override void Init()
    {
        base.Init();
        LoadSettings();
        EnsureLoader();
        EnsureAudioSources();
        ApplyVolume();
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Logger.Warn("PlayMusic failed: clip is null.", this);
            return;
        }

        EnsureAudioRuntime();

        if (musicSource.clip == clip && musicSource.isPlaying && musicSource.loop == loop)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayMusic(string key, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Logger.Warn("PlayMusic failed: key is null or empty.", this);
            return;
        }

        EnsureLoader();
        LoadClip(key, clip =>
        {
            if (clip == null)
            {
                Logger.Error($"Music load failed: {key}", this);
                return;
            }

            PlayMusic(clip, loop);
        });
    }

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Logger.Warn("PlaySound failed: clip is null.", this);
            return;
        }

        EnsureAudioRuntime();

        var source = PoolMgr.I.SpawnComponent(sfxSourcePrefab, sfxRoot);
        if (source == null)
        {
            Logger.Warn("PlaySound failed: no pooled audio source available.", this);
            return;
        }

        var playback = new ActiveSfxPlayback(source, Mathf.Clamp01(volumeScale));
        activeSfxPlaybacks.Add(playback);
        ApplySfxVolume(playback);

        source.clip = clip;
        source.loop = false;
        source.Play();
        TimeMgr.I.RunCoroutine(ReleaseSoundWhenComplete(playback));
    }

    public void PlaySound(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Logger.Warn("PlaySound failed: key is null or empty.", this);
            return;
        }

        EnsureLoader();
        LoadClip(key, clip =>
        {
            if (clip == null)
            {
                Logger.Error($"Sound load failed: {key}", this);
                return;
            }

            PlaySound(clip, volumeScale);
        });
    }

    public void PrewarmSoundPool(int count = 3)
    {
        EnsureAudioRuntime();
        if (sfxSourcePrefab == null || count <= 0)
        {
            return;
        }

        PoolMgr.I.Prewarm(sfxSourcePrefab.gameObject, count);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        SaveSettings();
        EventMgr.I.Dispatch(GameEventType.AudioMusicVolumeChanged, musicVolume);
    }

    public void SetSoundVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        SaveSettings();
        EventMgr.I.Dispatch(GameEventType.AudioSoundVolumeChanged, sfxVolume);
    }

    public void SetMusicMute(bool mute)
    {
        musicMuted = mute;
        ApplyVolume();
        SaveSettings();
        EventMgr.I.Dispatch(GameEventType.AudioMusicMuteChanged, musicMuted);
    }

    public void SetSoundMute(bool mute)
    {
        sfxMuted = mute;
        ApplyVolume();
        SaveSettings();
        EventMgr.I.Dispatch(GameEventType.AudioSoundMuteChanged, sfxMuted);
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSoundVolume()
    {
        return sfxVolume;
    }

    public bool IsMusicMuted()
    {
        return musicMuted;
    }

    public bool IsSoundMuted()
    {
        return sfxMuted;
    }

    private void EnsureLoader()
    {
        if (loader == null)
        {
            loader = GetComponent<Loader>();
            if (loader == null)
            {
                loader = gameObject.AddComponent<Loader>();
            }
        }
    }

    private void EnsureAudioRuntime()
    {
        EnsureLoader();
        EnsureAudioSources();
        EnsureSfxRoot();
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = GetOrCreateAudioSource("MusicSource", true);
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        if (sfxSourcePrefab == null)
        {
            sfxSourcePrefab = GetOrCreateAudioSource("SfxSourcePrefab", false);
            sfxSourcePrefab.playOnAwake = false;
            sfxSourcePrefab.loop = false;
            sfxSourcePrefab.gameObject.SetActive(false);
        }
    }

    private AudioSource GetOrCreateAudioSource(string nodeName, bool active)
    {
        var child = transform.Find(nodeName);
        AudioSource audioSource;
        if (child == null)
        {
            var node = new GameObject(nodeName, typeof(AudioSource));
            node.transform.SetParent(transform, false);
            audioSource = node.GetComponent<AudioSource>();
        }
        else
        {
            audioSource = child.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = child.gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.gameObject.SetActive(active);
        return audioSource;
    }

    private void EnsureSfxRoot()
    {
        if (sfxRoot != null)
        {
            return;
        }

        var child = transform.Find("SfxRoot");
        if (child == null)
        {
            var root = new GameObject("SfxRoot");
            root.transform.SetParent(transform, false);
            sfxRoot = root.transform;
        }
        else
        {
            sfxRoot = child;
        }
    }

    private void LoadClip(string key, Action<AudioClip> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            onCompleted?.Invoke(null);
            return;
        }

        if (audioClipCache.TryGetValue(key, out var cachedClip))
        {
            onCompleted?.Invoke(cachedClip);
            return;
        }

        if (pendingClipCallbacks.TryGetValue(key, out var callbacks))
        {
            callbacks.Add(onCompleted);
            return;
        }

        pendingClipCallbacks[key] = new List<Action<AudioClip>> { onCompleted };
        loader.Load<AudioClip>(key, clip =>
        {
            if (clip != null)
            {
                audioClipCache[key] = clip;
            }

            if (!pendingClipCallbacks.TryGetValue(key, out var pendingCallbacks))
            {
                return;
            }

            pendingClipCallbacks.Remove(key);
            for (var i = 0; i < pendingCallbacks.Count; i++)
            {
                pendingCallbacks[i]?.Invoke(clip);
            }
        });
    }

    private void LoadSettings()
    {
        musicVolume = PlayerPrefsMgr.I.GetFloat(MusicVolumeKey, musicVolume);
        sfxVolume = PlayerPrefsMgr.I.GetFloat(SoundVolumeKey, sfxVolume);
        musicMuted = PlayerPrefsMgr.I.GetBool(MusicMutedKey, musicMuted);
        sfxMuted = PlayerPrefsMgr.I.GetBool(SoundMutedKey, sfxMuted);
    }

    private void SaveSettings()
    {
        PlayerPrefsMgr.I.SetFloat(MusicVolumeKey, musicVolume, false);
        PlayerPrefsMgr.I.SetFloat(SoundVolumeKey, sfxVolume, false);
        PlayerPrefsMgr.I.SetBool(MusicMutedKey, musicMuted, false);
        PlayerPrefsMgr.I.SetBool(SoundMutedKey, sfxMuted, false);
        PlayerPrefsMgr.I.Save();
    }

    private void ApplyVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicMuted ? 0f : musicVolume;
            musicSource.mute = musicMuted;
        }

        CleanupInactivePlaybacks();
        for (var i = 0; i < activeSfxPlaybacks.Count; i++)
        {
            ApplySfxVolume(activeSfxPlaybacks[i]);
        }
    }

    private void ApplySfxVolume(ActiveSfxPlayback playback)
    {
        if (playback?.Source == null)
        {
            return;
        }

        playback.Source.volume = (sfxMuted ? 0f : sfxVolume) * playback.VolumeScale;
        playback.Source.mute = sfxMuted;
    }

    private IEnumerator ReleaseSoundWhenComplete(ActiveSfxPlayback playback)
    {
        if (playback == null || playback.Source == null)
        {
            yield break;
        }

        yield return null;
        while (playback.Source != null && playback.Source.isPlaying)
        {
            yield return null;
        }

        if (playback.Source != null)
        {
            playback.Source.Stop();
            playback.Source.clip = null;
            PoolMgr.I.Despawn(playback.Source);
        }

        activeSfxPlaybacks.Remove(playback);
    }

    private void CleanupInactivePlaybacks()
    {
        for (var i = activeSfxPlaybacks.Count - 1; i >= 0; i--)
        {
            if (activeSfxPlaybacks[i]?.Source != null)
            {
                continue;
            }

            activeSfxPlaybacks.RemoveAt(i);
        }
    }

    private sealed class ActiveSfxPlayback
    {
        public ActiveSfxPlayback(AudioSource source, float volumeScale)
        {
            Source = source;
            VolumeScale = volumeScale;
        }

        public AudioSource Source { get; }
        public float VolumeScale { get; }
    }
}
