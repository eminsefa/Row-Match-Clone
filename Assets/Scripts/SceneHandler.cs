using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{
    public static SceneHandler Instance;
    private float _screenAspect;
    private const float CamDefaultSize = 6;
    public LevelData LevelData { get; private set; }
    [SerializeField] private GameObject highScoreParent;
    [SerializeField] private TextMeshPro scoreText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        //Calls this only on application start
        StartCoroutine(LevelDataHandler.DownloadLevels());
        _screenAspect = (float)Screen.width / Screen.height;
    }

    public void SetCameraSizeForGrid(int gridWidth, int gridHeight)
    {
        var cam = Camera.main;
        var gridAspect = (float)gridWidth / gridHeight;

        if (gridAspect > _screenAspect) cam.orthographicSize = (gridWidth / (2 * _screenAspect)) + 1;
        else cam.orthographicSize = (gridHeight + 2) / 2f;
    }

    public void SetCameraSizeForMenu()
    {
        var cam = Camera.main;
        if (_screenAspect > 1) cam.orthographicSize = CamDefaultSize / 2 * _screenAspect;
        else cam.orthographicSize = CamDefaultSize / 2 / _screenAspect;
    }

    public void StartLevel(LevelData levelData)
    {
        LevelData = levelData;
        SceneManager.LoadScene($"LevelScene");
    }

    public void LevelEnded(bool highScored, int score)
    {
        if (highScored)
        {
            scoreText.text = score.ToString();
            var level = LevelData.LevelNumber;
            LevelDataHandler.LevelEndedWithHighScore(level, score);
            AnimateHighScore();
            return;
        }

        SceneManager.LoadScene($"MainScene");
    }

    private void AnimateHighScore()
    {
        //Was not sure if animation must be in main scene
        var scene = SceneManager.LoadSceneAsync("MainScene");
        scene.allowSceneActivation = false;
        highScoreParent.transform.DOScale(1, 0.25f)
            .From(0)
            .SetEase(Ease.OutBack)
            .OnStart(() => highScoreParent.SetActive(true));
        highScoreParent.transform.DOScale(0, 0.5f)
            .SetEase(Ease.InBack)
            .SetDelay(2f)
            .OnComplete(() =>
            {
                highScoreParent.SetActive(false);
                scene.allowSceneActivation = true;
            });
    }

    private void OnApplicationQuit()
    {
        StopAllCoroutines(); //Not sure if download coroutine can create a bug
    }
}