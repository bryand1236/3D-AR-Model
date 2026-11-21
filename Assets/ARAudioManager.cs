using UnityEngine;

/// <summary>
/// Central audio manager for the AR Wayang experience:
/// - Plays an authentic Javanese gamelan bronze chime when an AR card is recognized.
/// - Plays a tactile wooden percussion click when 3D buttons / language switches are tapped.
/// </summary>
[DisallowMultipleComponent]
public class ARAudioManager : MonoBehaviour
{
    private static ARAudioManager instance;
    public static ARAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindAnyObjectByType<ARAudioManager>();
                if (instance == null)
                {
                    var go = new GameObject("ARAudioManager");
                    instance = go.AddComponent<ARAudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null && Object.FindAnyObjectByType<ARAudioManager>() == null)
        {
            var go = new GameObject("ARAudioManager");
            go.AddComponent<ARAudioManager>();
            DontDestroyOnLoad(go);
        }
    }

    [Header("Audio Clips")]
    [Tooltip("Gamelan chime played when a Wayang card is recognized.")]
    public AudioClip cardDiscoveryChime;

    [Tooltip("Wooden percussion click played on button tap / language switch.")]
    public AudioClip buttonClickSound;

    [Header("Volume Controls")]
    [Range(0f, 1f)] public float chimeVolume = 0.85f;
    [Range(0f, 1f)] public float clickVolume = 0.75f;

    private AudioSource audioSource;
    private float lastChimeTime = -10f;
    private const float CHIME_COOLDOWN = 1.2f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D clean audio

        LoadClipsIfNeeded();
    }

    private void LoadClipsIfNeeded()
    {
        if (cardDiscoveryChime == null)
        {
#if UNITY_EDITOR
            cardDiscoveryChime = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/javanese_saron_chime.mp3");
            if (cardDiscoveryChime == null)
            {
                cardDiscoveryChime = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Gamelan_Chime.wav");
            }
#endif
        }

        if (buttonClickSound == null)
        {
#if UNITY_EDITOR
            buttonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/javanese_woodblock_click.mp3");
            if (buttonClickSound == null)
            {
                buttonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Wood_Click.wav");
            }
#endif
        }
    }

    /// <summary>
    /// Plays the authentic Javanese gamelan chime on card discovery.
    /// Includes cooldown to prevent audio spam when tracking rapidly updates.
    /// </summary>
    public void PlayCardDiscoveryChime()
    {
        if (Time.time - lastChimeTime < CHIME_COOLDOWN) return;
        lastChimeTime = Time.time;

        if (cardDiscoveryChime != null && audioSource != null)
        {
            audioSource.PlayOneShot(cardDiscoveryChime, chimeVolume);
        }
    }

    /// <summary>
    /// Plays the tactile wooden percussion click when a 3D button is pressed.
    /// </summary>
    public void PlayButtonClick()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound, clickVolume);
        }
    }
}
