using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelBoxesHandler : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public static LevelBoxesHandler Instance;
    private float _slideLimitYPos;
    private bool _lockInput;
    private int _activeLevelCount;
    private Tweener _twEndDragSlide;
    private List<LevelBox> _levelBoxes = new List<LevelBox>();
    [SerializeField] private Transform levelSlideParent;
    [SerializeField] private float sensitivity;
    [SerializeField] private float threshold;
    [SerializeField] private GameObject levelBoxPrefab;

    private void Awake()
    {
        Instance = this;
        var boxes = levelSlideParent.GetComponentsInChildren<LevelBox>();
        _levelBoxes = boxes.OrderBy(x => -x.transform.position.y).ToList();
        SetLevelsFromData();
    }

    public void SetLevelMenu()
    {
        _slideLimitYPos = 0.23f + (_activeLevelCount - 5) * 0.3f;
        var unlockedLevelListNumber = LevelDataHandler.NextLevelToUnlock - 1;

        var yPos = 0.23f + (unlockedLevelListNumber - 4) * 0.3f;
        yPos = Mathf.Clamp(yPos, 0, _slideLimitYPos);
        var slideTime = Mathf.Lerp(0.25f, 1, yPos / 3);

        if (LevelDataHandler.HighScored) //Unlocked new
        {
            _lockInput = true;
            if (unlockedLevelListNumber > 0 && unlockedLevelListNumber < 4)
            {
                _lockInput = false;
                _levelBoxes[unlockedLevelListNumber - 1].LevelUnlocked(slideTime);
                return;
            }
        }

        //Move menu down to first unlocked level
        _twEndDragSlide = levelSlideParent.DOLocalMoveY(yPos, slideTime)
            .SetDelay(0.5f)
            .OnComplete(() =>
            {
                if (LevelDataHandler.HighScored)
                {
                    LevelDataHandler.HighScored = false;
                    _levelBoxes[unlockedLevelListNumber - 1].LevelUnlocked(slideTime);
                }

                _lockInput = false;
            });
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_lockInput) return;
        var dif = eventData.delta;
        var p = levelSlideParent.localPosition;
        var newY = p.y + dif.y * Time.fixedDeltaTime * sensitivity;
        newY = Mathf.Clamp(newY, 0, _slideLimitYPos + 0.2f);
        levelSlideParent.localPosition = new Vector3(p.x, newY, p.z);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_lockInput) return;
        var localYPos = levelSlideParent.localPosition.y;
        if (Mathf.Abs(eventData.delta.y) < threshold)
        {
            if (localYPos > _slideLimitYPos) SlideToEndPos(localYPos);
            return;
        }

        var endY = localYPos + eventData.delta.y * sensitivity / 5;
        SlideToEndPos(endY);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_lockInput) return;
        if (_twEndDragSlide.IsActive()) _twEndDragSlide.Kill();
    }

    public void PlayButtonTapped(LevelData levelData)
    {
        if (_lockInput) return;
        if (_twEndDragSlide.IsActive()) _twEndDragSlide.Kill();
        SceneHandler.Instance.StartLevel(levelData);
    }

    private void SlideToEndPos(float endY)
    {
        endY = Mathf.Clamp(endY, 0, _slideLimitYPos);
        var moveY = Mathf.Abs(levelSlideParent.localPosition.y - endY);
        var time = Mathf.Lerp(0.25f, 1, moveY / 3);
        _twEndDragSlide = levelSlideParent.DOLocalMoveY(endY, time)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Fixed);
    }

    private void SetLevelsFromData()
    {
        var orderedList = LevelDataHandler.ReadLevelData();
        _activeLevelCount = orderedList.Count;

        //---Notes---
        //I wanted to save level boxes to a file, then read it and pass this process every time
        //But it is not possible to save a game object to json and creating a prefab is only possible in editor (as far as I searched)
        //If I serialized a class it was going to read the data and instantiate game object every time too. So I assumed this is the right way
        //---Notes---

        //Instantiate data number of level box and set data
        var existingLastYPos = _levelBoxes[9].transform.localPosition.y;
        for (int i = 0; i < _activeLevelCount; i++)
        {
            if (i > 9)
            {
                var o = Instantiate(levelBoxPrefab, Vector3.zero, Quaternion.identity, levelSlideParent.transform);
                o.name = "Level " + (i + 1);
                o.transform.localPosition = new Vector3(0, existingLastYPos - (i - 9) * 0.025f, -0.2f);
                var lb = o.GetComponent<LevelBox>();
                lb.SetData(orderedList[i]);
                _levelBoxes.Add(lb);
            }

            _levelBoxes[i].SetData(orderedList[i]);
        }
    }
}