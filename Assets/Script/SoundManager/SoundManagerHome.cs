using UnityEngine;

public class SoundManagerHome : MonoBehaviour
{
    public static SoundManagerHome Instance { get; private set; }

    [SerializeField] private AudioSource soundBackGround;
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private bool _musicOn;
    private bool _sfxOn;

    public bool MusicOn => _musicOn;
    public bool SfxOn => _sfxOn;

    void Awake()
    {
        Instance = this;
        _musicOn = SaveGame.MusicOn;
        _sfxOn = SaveGame.SfxOn;
    }

    void Start()
    {
        soundBackGround.playOnAwake = false;
        soundBackGround.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        soundBackGround.mute = !_musicOn;
        soundBackGround.Play();
    }

    public void ToggleMusic()
    {
        _musicOn = !_musicOn;
        SaveGame.MusicOn = _musicOn;
        soundBackGround.mute = !_musicOn;
    }

    public void ToggleSfx()
    {
        _sfxOn = !_sfxOn;
        SaveGame.SfxOn = _sfxOn;
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!_sfxOn || clip == null) return;
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}