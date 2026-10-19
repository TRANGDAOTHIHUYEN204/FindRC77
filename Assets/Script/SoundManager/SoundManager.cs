using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    [SerializeField] private AudioSource soundBackGround;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private bool _musicOn = true;
    private bool _sfxOn = true;
    private bool _lastIsPlaying;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        soundBackGround.playOnAwake = false;
        soundBackGround.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        _lastIsPlaying = stateGame.IsPlaying;
        Refresh();
    }

    void Update()
    {
        if (stateGame.IsPlaying != _lastIsPlaying)
        {
            _lastIsPlaying = stateGame.IsPlaying;
            Refresh();
        }
    }

    public void ToggleMusic()
    {
        _musicOn = !_musicOn;
        Refresh();
    }

    private void Refresh()
    {
        bool shouldPlay = _musicOn && stateGame.IsPlaying;

        if (shouldPlay)
        {
            if (!soundBackGround.isPlaying)
                soundBackGround.Play();
        }
        else
        {
            soundBackGround.Stop();
        }
    }
    public void ToggleSfx() => _sfxOn = !_sfxOn;

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!_sfxOn || clip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }

    public void PlaySfxRandomPitch(AudioClip clip, float volume = 1f, float range = 0.1f)
    {
        if (!_sfxOn || clip == null) return;
        sfxSource.pitch = 1f + Random.Range(-range, range);
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}