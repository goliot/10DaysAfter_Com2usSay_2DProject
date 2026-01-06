using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#region Addressables AudioClip Reference
// 인스펙터에서 AudioClip만 들어오게 타입 제한된 AssetReference
[Serializable]
public class AssetReferenceAudioClip : AssetReferenceT<AudioClip>
{
    public AssetReferenceAudioClip(string guid) : base(guid) { }
}
#endregion

public class SoundManager : Singleton<SoundManager> // 문의 : 수민
{
    public enum AudioType { BGM, SFX }

    private const string KEY_BGM = "BGM_Volume";
    private const string KEY_SFX = "SFX_Volume";

    [Header("#BGM (Addressables)")]
    [SerializeField] private AssetReferenceAudioClip[] bgmClipRefs; // enum 인덱스 맞춰서 넣기
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;
    [SerializeField] private bool preloadAllOnStart = true;

    [Header("#SFX (Addressables)")]
    [SerializeField] private AssetReferenceAudioClip[] sfxClipRefs; // enum 인덱스 맞춰서 넣기
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.5f;
    [SerializeField, Min(1)] private int channels = 15;

    private AudioSource bgmPlayer;          // BGM은 단일
    private AudioSource[] sfxPlayers;       // SFX는 여러개
    private int channelIndex;

    [Header("# PlayingInfo")]
    [SerializeField] private string _currentBGM;

    // 로드 캐시
    private AudioClip[] _bgmClips;
    private AudioClip[] _sfxClips;

    // 핸들(Release용)
    private AsyncOperationHandle<AudioClip>[] _bgmHandles;
    private AsyncOperationHandle<AudioClip>[] _sfxHandles;

    // 로딩 중 중복 방지
    private Task<AudioClip>[] _bgmLoadTasks;
    private Task<AudioClip>[] _sfxLoadTasks;

    public float BGMVolume
    {
        get => GetVolume(AudioType.BGM);
        set => OnVolumeChanged(AudioType.BGM, value);
    }

    public float SFXVolume
    {
        get => GetVolume(AudioType.SFX);
        set => OnVolumeChanged(AudioType.SFX, value);
    }

    private void Awake()
    {
        Initialize_DontDestroyOnLoad();
        InitPlayers();
        LoadVolumesFromPrefs();
        ApplyVolumes();

        PrepareCaches();
    }

    private async void Start()
    {
        if (preloadAllOnStart)
            await PreloadAllAsync();
    }

    private void PrepareCaches()
    {
        _bgmClips = new AudioClip[bgmClipRefs?.Length ?? 0];
        _sfxClips = new AudioClip[sfxClipRefs?.Length ?? 0];

        _bgmHandles = new AsyncOperationHandle<AudioClip>[bgmClipRefs?.Length ?? 0];
        _sfxHandles = new AsyncOperationHandle<AudioClip>[sfxClipRefs?.Length ?? 0];

        _bgmLoadTasks = new Task<AudioClip>[bgmClipRefs?.Length ?? 0];
        _sfxLoadTasks = new Task<AudioClip>[sfxClipRefs?.Length ?? 0];
    }

    private void InitPlayers()
    {
        // BGM 플레이어 초기화
        GameObject bgmObject = new GameObject("BGMPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;

        // 용량/연산 최적화 (기존 코드 유지)
        bgmPlayer.dopplerLevel = 0.0f;
        bgmPlayer.reverbZoneMix = 0.0f;

        // SFX 플레이어 초기화
        GameObject sfxObject = new GameObject("SFXPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int idx = 0; idx < sfxPlayers.Length; idx++)
        {
            sfxPlayers[idx] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[idx].playOnAwake = false;
            sfxPlayers[idx].dopplerLevel = 0.0f;
            sfxPlayers[idx].reverbZoneMix = 0.0f;
        }
    }

    private void LoadVolumesFromPrefs()
    {
        // 기존 로직(1 - value) 유지 + 키 통일
        bgmVolume = 1.0f - PlayerPrefs.GetFloat(KEY_BGM, 1.0f - bgmVolume);
        sfxVolume = 1.0f - PlayerPrefs.GetFloat(KEY_SFX, 1.0f - sfxVolume);
    }

    private void ApplyVolumes()
    {
        if (bgmPlayer != null) bgmPlayer.volume = bgmVolume;
        if (sfxPlayers != null)
        {
            foreach (var p in sfxPlayers)
                p.volume = sfxVolume;
        }
    }

    public async Task PreloadAllAsync()
    {
        // BGM
        for (int i = 0; i < _bgmClips.Length; i++)
            await EnsureBgmLoadedAsync(i);

        // SFX
        for (int i = 0; i < _sfxClips.Length; i++)
            await EnsureSfxLoadedAsync(i);
    }

    #region BGM
    // 기존 시그니처 유지: 내부에서 async로 처리
    public void PlayBgm(EBgmType bgm)
    {
        _ = PlayBgmInternalAsync(bgm);
    }

    private async Task PlayBgmInternalAsync(EBgmType bgm)
    {
        if (bgmPlayer == null) return;

        string name = bgm.ToString();
        if (_currentBGM == name) return;

        int idx = (int)bgm;
        var clip = await EnsureBgmLoadedAsync(idx);
        if (clip == null)
        {
            Debug.LogError($"[SoundManager] BGM clip load failed. idx={idx}, enum={bgm}");
            return;
        }

        bgmPlayer.clip = clip;
        bgmPlayer.Play();
        _currentBGM = name;
    }

    public void StopBgm()
    {
        if (bgmPlayer != null) bgmPlayer.Stop();
    }

    private Task<AudioClip> EnsureBgmLoadedAsync(int idx)
    {
        if (!IsValidIndex(_bgmClips, idx)) return Task.FromResult<AudioClip>(null);
        if (_bgmClips[idx] != null) return Task.FromResult(_bgmClips[idx]);

        if (_bgmLoadTasks[idx] != null) return _bgmLoadTasks[idx];

        var reference = bgmClipRefs[idx];
        _bgmLoadTasks[idx] = LoadClipAsync(reference, isBgm: true, index: idx);
        return _bgmLoadTasks[idx];
    }
    #endregion

    #region SFX
    public void PlaySfx(ESfxType sfx)
    {
        _ = PlaySfxInternalAsync(sfx);
    }

    private async Task PlaySfxInternalAsync(ESfxType sfx)
    {
        if (sfxPlayers == null || sfxPlayers.Length == 0) return;

        int idx = (int)sfx;
        var clip = await EnsureSfxLoadedAsync(idx);
        if (clip == null)
        {
            Debug.LogError($"[SoundManager] SFX clip load failed. idx={idx}, enum={sfx}");
            return;
        }

        // 쉬고 있는 하나의 채널에 재생
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % sfxPlayers.Length;
            if (sfxPlayers[loopIndex].isPlaying) continue;

            channelIndex = loopIndex;
            var player = sfxPlayers[loopIndex];

            player.clip = clip;
            player.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            player.PlayOneShot(clip);
            break;
        }
    }

    private Task<AudioClip> EnsureSfxLoadedAsync(int idx)
    {
        if (!IsValidIndex(_sfxClips, idx)) return Task.FromResult<AudioClip>(null);
        if (_sfxClips[idx] != null) return Task.FromResult(_sfxClips[idx]);

        if (_sfxLoadTasks[idx] != null) return _sfxLoadTasks[idx];

        var reference = sfxClipRefs[idx];
        _sfxLoadTasks[idx] = LoadClipAsync(reference, isBgm: false, index: idx);
        return _sfxLoadTasks[idx];
    }
    #endregion

    private async Task<AudioClip> LoadClipAsync(AssetReferenceAudioClip reference, bool isBgm, int index)
    {
        if (reference == null || !reference.RuntimeKeyIsValid())
        {
            Debug.LogError($"[SoundManager] Invalid AssetReferenceAudioClip. {(isBgm ? "BGM" : "SFX")} index={index}");
            return null;
        }

        // (선택) 원격이면 여기서 의존성 다운로드를 먼저 걸고 진행률 UI 붙일 수 있음
        // var d = reference.DownloadDependenciesAsync();
        // await d.Task;
        // Addressables.Release(d);

        var handle = reference.LoadAssetAsync();
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SoundManager] Load failed. {(isBgm ? "BGM" : "SFX")} index={index}\n{handle.OperationException}");
            return null;
        }

        if (isBgm)
        {
            _bgmHandles[index] = handle;
            _bgmClips[index] = handle.Result;
            return _bgmClips[index];
        }
        else
        {
            _sfxHandles[index] = handle;
            _sfxClips[index] = handle.Result;
            return _sfxClips[index];
        }
    }

    public void OnChangedBGMVolume(float value)
    {
        BGMVolume = value;
        if (bgmPlayer != null) bgmPlayer.volume = BGMVolume;
    }

    public float GetVolume(AudioType type)
    {
        if (type == AudioType.BGM) return bgmPlayer != null ? bgmPlayer.volume : bgmVolume;
        return (sfxPlayers != null && sfxPlayers.Length > 0) ? sfxPlayers[0].volume : sfxVolume;
    }

    public void OnVolumeChanged(AudioType type, float value)
    {
        // 기존 저장 방식 유지(1 - value)
        PlayerPrefs.SetFloat(type == AudioType.BGM ? KEY_BGM : KEY_SFX, 1.0f - value);

        if (type == AudioType.BGM)
        {
            bgmVolume = value;
            if (bgmPlayer != null) bgmPlayer.volume = value;
        }
        else
        {
            sfxVolume = value;
            if (sfxPlayers != null)
            {
                foreach (var player in sfxPlayers)
                    player.volume = value;
            }
        }
    }

    private static bool IsValidIndex<T>(T[] arr, int idx)
    {
        return arr != null && idx >= 0 && idx < arr.Length;
    }

    private void OnDestroy()
    {
        ReleaseAll();
    }

    public void ReleaseAll()
    {
        // BGM handles release
        if (_bgmHandles != null)
        {
            for (int i = 0; i < _bgmHandles.Length; i++)
            {
                if (_bgmHandles[i].IsValid())
                    Addressables.Release(_bgmHandles[i]);
            }
        }

        // SFX handles release
        if (_sfxHandles != null)
        {
            for (int i = 0; i < _sfxHandles.Length; i++)
            {
                if (_sfxHandles[i].IsValid())
                    Addressables.Release(_sfxHandles[i]);
            }
        }

        _currentBGM = null;

        // 캐시/태스크 초기화
        if (_bgmClips != null) Array.Clear(_bgmClips, 0, _bgmClips.Length);
        if (_sfxClips != null) Array.Clear(_sfxClips, 0, _sfxClips.Length);

        if (_bgmLoadTasks != null) Array.Clear(_bgmLoadTasks, 0, _bgmLoadTasks.Length);
        if (_sfxLoadTasks != null) Array.Clear(_sfxLoadTasks, 0, _sfxLoadTasks.Length);
    }
}
