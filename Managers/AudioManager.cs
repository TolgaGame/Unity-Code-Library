// Standalone: no other scripts needed.
// Music (with crossfade) + sound effects (small AudioSource pool). Volume and mute are saved in PlayerPrefs.
//
// Setup: add this to a GameObject in your first scene, then fill the Music / Sfx lists.
// Usage:
//   AudioManager.Instance.PlayMusic("menu");
//   AudioManager.Instance.PlaySfx("click");
//   AudioManager.Instance.SetMusicVolume(0.5f);
//   AudioManager.Instance.SetMuted(true);
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    public List<NamedClip> musicClips = new List<NamedClip>();
    public List<NamedClip> sfxClips = new List<NamedClip>();

    [Header("Settings")]
    [Min(1)] public int sfxPoolSize = 8;
    [Min(0f)] public float crossfadeDuration = 1f;
    public bool dontDestroyOnLoad = true;

    const string MusicVolumeKey = "Audio_MusicVolume";
    const string SfxVolumeKey = "Audio_SfxVolume";
    const string MutedKey = "Audio_Muted";

    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool IsMuted { get; private set; }

    readonly Dictionary<string, NamedClip> music = new Dictionary<string, NamedClip>();
    readonly Dictionary<string, NamedClip> sfx = new Dictionary<string, NamedClip>();
    readonly List<AudioSource> sfxSources = new List<AudioSource>();

    AudioSource musicSource;
    AudioSource fadingSource;
    NamedClip currentMusic;
    Coroutine crossfadeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        foreach (NamedClip item in musicClips)
            if (item != null && !string.IsNullOrEmpty(item.name)) music[item.name] = item;
        foreach (NamedClip item in sfxClips)
            if (item != null && !string.IsNullOrEmpty(item.name)) sfx[item.name] = item;

        musicSource = CreateSource(true);
        fadingSource = CreateSource(true);
        for (int i = 0; i < sfxPoolSize; i++)
            sfxSources.Add(CreateSource(false));

        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        IsMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        return source;
    }

    float TargetMusicVolume(NamedClip item)
    {
        return IsMuted || item == null ? 0f : MusicVolume * item.volume;
    }

    public void PlayMusic(string musicName)
    {
        if (!music.TryGetValue(musicName, out NamedClip next))
        {
            Debug.LogWarning("[AudioManager] Music not found: " + musicName);
            return;
        }

        if (currentMusic == next && musicSource.isPlaying)
            return;

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(Crossfade(next));
    }

    public void StopMusic()
    {
        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(Crossfade(null));
    }

    IEnumerator Crossfade(NamedClip next)
    {
        // The current music becomes the fading source, the new one starts on the main source.
        AudioSource temp = fadingSource;
        fadingSource = musicSource;
        musicSource = temp;

        float fadeFromVolume = fadingSource.volume;
        currentMusic = next;

        musicSource.Stop();
        if (next != null)
        {
            musicSource.clip = next.clip;
            musicSource.volume = 0f;
            musicSource.Play();
        }

        float targetVolume = TargetMusicVolume(next);
        float time = 0f;
        while (time < crossfadeDuration)
        {
            // Unscaled time, so music still fades while the game is paused (timeScale = 0).
            time += Time.unscaledDeltaTime;
            float t = crossfadeDuration <= 0f ? 1f : Mathf.Clamp01(time / crossfadeDuration);
            fadingSource.volume = Mathf.Lerp(fadeFromVolume, 0f, t);
            if (next != null)
                musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        fadingSource.Stop();
        fadingSource.volume = 0f;
        if (next != null)
            musicSource.volume = TargetMusicVolume(next);
        crossfadeRoutine = null;
    }

    public void PlaySfx(string sfxName)
    {
        if (!sfx.TryGetValue(sfxName, out NamedClip item) || item.clip == null)
        {
            Debug.LogWarning("[AudioManager] SFX not found: " + sfxName);
            return;
        }

        if (IsMuted)
            return;

        AudioSource source = GetFreeSfxSource();
        source.pitch = 1f;
        source.PlayOneShot(item.clip, SfxVolume * item.volume);
    }

    /// <summary>Plays an SFX with a small random pitch change. Good for footsteps, hits and clicks.</summary>
    public void PlaySfx(string sfxName, float minPitch, float maxPitch)
    {
        if (!sfx.TryGetValue(sfxName, out NamedClip item) || item.clip == null || IsMuted)
            return;

        AudioSource source = GetFreeSfxSource();
        source.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        source.PlayOneShot(item.clip, SfxVolume * item.volume);
    }

    AudioSource GetFreeSfxSource()
    {
        foreach (AudioSource source in sfxSources)
            if (!source.isPlaying)
                return source;

        // All sources are busy: reuse the first one.
        return sfxSources[0];
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        ApplyMusicVolume();
    }

    public void ToggleMute()
    {
        SetMuted(!IsMuted);
    }

    void ApplyMusicVolume()
    {
        // While a crossfade runs, it applies the volume at the end.
        if (crossfadeRoutine == null && musicSource != null)
            musicSource.volume = TargetMusicVolume(currentMusic);
    }
}
