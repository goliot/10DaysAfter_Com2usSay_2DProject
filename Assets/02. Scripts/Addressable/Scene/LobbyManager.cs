using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public void OnClickStartButton()
    {
        SceneManager.LoadScene("Download");
    }
}
