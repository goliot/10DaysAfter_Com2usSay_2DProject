using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static string NextScene;

    public Slider LoadingBar;

    private void Start()
    {
        StartCoroutine(StartLoadingScene());
    }

    private IEnumerator StartLoadingScene()
    {
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(NextScene);
        op.allowSceneActivation = false;

        float timer = 0f;

        while(!op.isDone)
        {
            yield return null;

            timer += Time.deltaTime;

            if (op.progress < 0.9f)
            {
                LoadingBar.value = Mathf.Lerp(LoadingBar.value, op.progress, timer);

                if(LoadingBar.value >= op.progress)
                {
                    timer = 0f;
                }
            }
            else
            {
                LoadingBar.value = Mathf.Lerp(LoadingBar.value, 1f, timer);

                if(LoadingBar.value == 1f)
                {
                    yield return new WaitForSeconds(2f);
                    op.allowSceneActivation = true;
                    yield break;
                }
            }
        }
    }

    public static void LoadScene(string sceneName)
    {
        NextScene = sceneName;
        SceneManager.LoadScene("Loading");
    }
}
