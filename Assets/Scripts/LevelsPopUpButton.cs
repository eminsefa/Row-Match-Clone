using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

//Would make the script generic if the game had different buttons 
public class LevelsPopUpButton : MonoBehaviour,IPointerClickHandler
{
    [SerializeField] private GameObject levelsPanel;
    
    private void Start()
    {
        SceneHandler.Instance.SetCameraSizeForMenu();
        transform.DOScale(1.5f, 0.5f)
            .From(0)
            .SetEase(Ease.OutBack);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        GetComponent<BoxCollider2D>().enabled = false;
        transform.DOScale(0, 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                levelsPanel.transform.DOScale(1, 0.5f)
                    .From(0)
                    .OnComplete(() => LevelBoxesHandler.Instance.SetLevelMenu());
                gameObject.SetActive(false);
            });
    }

}
