using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//Each level box sets texts and holds data to play
public class LevelBox : MonoBehaviour, IPointerClickHandler
{
    private LevelData _levelData;
    [SerializeField] private TextMeshPro levelText;
    [SerializeField] private TextMeshPro highScoreText;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject lockedImage;
    [SerializeField] private GameObject unlockedParticle;
    public void SetData(LevelData levelData)
    {
        _levelData = levelData;
        levelText.text = "Level " + levelData.LevelNumber + " - " + levelData.MoveCount + " Moves";
        if (levelData.Unlocked == 0) highScoreText.text = "Locked Level";
        else
        {
            if (LevelDataHandler.HighScored &&
                LevelDataHandler.NextLevelToUnlock - 1 == levelData.LevelNumber) //To animate newly unlock box
            {
                highScoreText.text = "Locked Level";
                return;
            }

            playButton.SetActive(true);
            lockedImage.SetActive(false);
            if (_levelData.HighScore == 0) highScoreText.text = "No Score";
            else highScoreText.text = "High Score : " + _levelData.HighScore;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        LevelBoxesHandler.Instance.PlayButtonTapped(_levelData);
    }

    public void LevelUnlocked(float delayTime)
    {
        lockedImage.transform.DOLocalMoveY(0.1f, 0.25f)
            .SetDelay(delayTime)
            .SetRelative(true)
            .OnComplete(() =>
            {
                playButton.transform.localPosition = lockedImage.transform.localPosition;
                playButton.SetActive(true);
                playButton.transform.DOLocalMoveY(-0.1f, 0.25f)
                    .SetRelative(true)
                    .OnComplete(() =>
                    {
                        highScoreText.text = "No Score";
                        var p = Instantiate(unlockedParticle, lockedImage.transform.position,
                            unlockedParticle.transform.rotation,playButton.transform);
                        Destroy(p,2f);
                    });
                lockedImage.SetActive(false);
            });
    }
}