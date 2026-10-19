using UnityEngine;

public class SoundManagerHome : MonoBehaviour
{
    public static SoundManagerHome Instance { get; private set; }

    [SerializeField] private AudioSource soundBackGround;
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private bool _musicOn = true;
    private bool _sfxOn = true;

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

        Refresh();
    }

    public void ToggleMusic()
    {
        _musicOn = !_musicOn;
        Refresh();
    }

    private void Refresh()
    {
        if (_musicOn)
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
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}