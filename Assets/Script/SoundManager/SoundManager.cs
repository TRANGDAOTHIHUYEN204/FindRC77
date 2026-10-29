using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource soundBackGround;
    [SerializeField] private AudioSource sfxSource;   
    [SerializeField] private int sfxPoolSize = 8;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private AudioSource[] _sfxPool;
    private int _poolIndex;
    private bool _musicOn;
    private bool _sfxOn;

    public bool MusicOn => _musicOn;
    public bool SfxOn => _sfxOn;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   

        _musicOn = SaveGame.MusicOn;
        _sfxOn = SaveGame.SfxOn;
        BuildPool();
    }

    private void BuildPool()
    {
        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"Sfx_{i}");
            go.transform.SetParent(transform);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            if (sfxSource != null) s.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            _sfxPool[i] = s;
        }
    }

    void Start()
    {
        if (soundBackGround == null) return;
        soundBackGround.playOnAwake = false;
        soundBackGround.loop = true;
        soundBackGround.mute = !_musicOn;
        soundBackGround.Play();
    }

    public void ToggleMusic()
    {
        _musicOn = !_musicOn;
        SaveGame.MusicOn = _musicOn;
        if (soundBackGround != null) soundBackGround.mute = !_musicOn;
    }

    public void ToggleSfx()
    {
        _sfxOn = !_sfxOn;
        SaveGame.SfxOn = _sfxOn;
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
        => PlayInternal(clip, volume, 1f);

    public void PlaySfxRandomPitch(AudioClip clip, float volume = 1f, float range = 0.1f)
        => PlayInternal(clip, volume, 1f + Random.Range(-range, range));

    public void PlaySfxRandomPitch(AudioClip clip, Vector2 pitchRange, float volume = 1f)
        => PlayInternal(clip, volume, Random.Range(pitchRange.x, pitchRange.y));

    private void PlayInternal(AudioClip clip, float volume, float pitch)
    {
        if (!_sfxOn || clip == null || _sfxPool == null) return;

        var src = GetFreeSource();
        src.clip = clip;
        src.pitch = pitch;
        src.volume = volume * sfxVolume;
        src.Play();
    }
    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            int idx = (_poolIndex + i) % _sfxPool.Length;
            if (!_sfxPool[idx].isPlaying)
            {
                _poolIndex = (idx + 1) % _sfxPool.Length;
                return _sfxPool[idx];
            }
        }

        var s = _sfxPool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _sfxPool.Length;
        return s;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}