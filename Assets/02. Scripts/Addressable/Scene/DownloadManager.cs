using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class DownloadManager : MonoBehaviour
{
    [Header("# UI")]
    public GameObject WaitMessage;
    public GameObject DownMessage;
    public Slider DownSlider;
    public TextMeshProUGUI SizeInfoText;
    public TextMeshProUGUI DownValueText;

    [Header("# Label")]
    public AssetLabelReference DefaultLabel;
    public AssetLabelReference TowerLabel;

    private long _patchSize;
    private Dictionary<string, long> _patchMap = new Dictionary<string, long>();

    private void Start()
    {
        WaitMessage.SetActive(true);
        DownMessage.SetActive(false);

        StartCoroutine(InitAddressable());
        StartCoroutine(CheckUpdateFiles());
    }

    private IEnumerator InitAddressable()
    {
        var init = Addressables.InitializeAsync().Task;
        yield return init;
    }

    private IEnumerator CheckUpdateFiles()
    {
        var labels = new List<string>() { DefaultLabel.labelString, TowerLabel.labelString };

        _patchSize = default;

        foreach(var label in labels)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;

            _patchSize += handle.Result;
        }

        if(_patchSize > decimal.Zero)
        {
            WaitMessage.SetActive(false);
            DownMessage.SetActive(true);

            SizeInfoText.text = GetFileSize(_patchSize);
        }
        else
        {
            DownValueText.text = " 100 % ";
            DownSlider.value = 1f;
            yield return new WaitForSeconds(2f);
            LoadingManager.LoadScene("FinalTitle");
        }
    }

    private string GetFileSize(long byteCount)
    {
        string size = "0 Bytes";

        if (byteCount >= 1073741824.0)
        {
            size = $"{string.Format("{0:##.##}", byteCount / 1073741824.0)} KB";
        }
        else if(byteCount >= 1048576.0)
        {
            size = $"{string.Format("{0:##.##}", byteCount / 1048576.0)} KB";
        }
        else if(byteCount >= 1024.0)
        {
            size = $"{string.Format("{0:##.##}", byteCount / 1024.0)} KB";
        }
        else if(byteCount > 0 && byteCount < 1024.0)
        {
            size = $"{byteCount.ToString()} B";
        }

        return size;
    }

    public void OnClickDownloadButton()
    {
        StartCoroutine(PatchFiles());
    }

    private IEnumerator PatchFiles()
    {
        var labels = new List<string>() { DefaultLabel.labelString, TowerLabel.labelString };

        foreach (var label in labels)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;

            if(handle.Result != decimal.Zero)
            {
                StartCoroutine(DownloadLabel(label));
            }
        }

        yield return CheckDownload();
    }

    private IEnumerator DownloadLabel(string label)
    {
        _patchMap.Add(label, 0);

        var handle = Addressables.DownloadDependenciesAsync(label, false);

        while(!handle.IsDone)
        {
            _patchMap[label] = handle.GetDownloadStatus().DownloadedBytes;
            yield return new WaitForEndOfFrame();
        }

        _patchMap[label] = handle.GetDownloadStatus().TotalBytes;
        Addressables.Release(handle);
    }

    private IEnumerator CheckDownload()
    {
        var total = 0f;
        DownValueText.text = "0 %";

        while(true)
        {
            total += _patchMap.Sum(tmp => tmp.Value);

            DownSlider.value = total / _patchSize;
            DownValueText.text = (int)(DownSlider.value * 100) + " %";

            if(total == _patchSize)
            {
                LoadingManager.LoadScene("FinalTitle");
                break;
            }

            total = 0f;
            yield return new WaitForEndOfFrame();
        }
    }
}