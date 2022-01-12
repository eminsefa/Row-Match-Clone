using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameEngine : MonoBehaviour, IPointerDownHandler
{
    private SceneHandler _sceneHandler;
    private Transform _clickedPiece;
    private Vector2 _lastInputPos;
    private List<Tweener> _playablePieceTweeners = new List<Tweener>();
    private Tweener _twScore;
    private int _moveGridCount;
    private int _gridWidth;
    private int _gridHeight;
    private int _moveCount;
    private int _score;
    private int _highScore;
    private int _newScore;
    private float _inactiveTime;
    private bool _highScoreExceeded;
    private bool _levelEnded;
    private Dictionary<int, Piece> _gridPieceDictionary = new Dictionary<int, Piece>();

    [SerializeField] private float inputThreshold;
    [SerializeField] private SpriteRenderer grid;
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform background;

    //Texts
    [SerializeField] private TextMeshPro moveLeftText;
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private TextMeshPro highScoreText;

    //Prefabs
    [SerializeField] private GameObject completedPiecePrefab;
    [SerializeField] private List<GameObject> piecePrefabs;

    private class Piece
    {
        public Transform PieceTr;
        public int PieceType;
    }

    private void Awake()
    {
        _sceneHandler = SceneHandler.Instance; //It is okay to do here since Scene Handler is never destroyed
        var data = _sceneHandler.LevelData;
        SetGrid(data);
    }

    private void Update()
    {
        if (_levelEnded) return;
        var inputPos = Vector2.zero;
        //Input For Both Mouse And Touch Input
        if (Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0)) OnInputStart();
            if (Input.GetMouseButtonUp(0)) OnInputEnd();
            if (Input.GetMouseButton(0)) inputPos = Input.mousePosition;
            else _inactiveTime += Time.deltaTime;
        }
        else
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began) OnInputStart();
            if (Input.GetTouch(0).phase == TouchPhase.Ended) OnInputEnd();
            if (Input.touchCount > 0) inputPos = Input.GetTouch(0).position;
            else _inactiveTime += Time.deltaTime;
        }

        if (_inactiveTime > 4f)
        {
            _inactiveTime = 0;
            StartCoroutine(CheckMove(true));
        }

        var dir = _lastInputPos - inputPos;
        if (dir.magnitude < inputThreshold) return;

        var dot = Vector2.Dot(dir.normalized, Vector2.right);
        if (Mathf.Abs(dot) > 0.75f) //Horizontal
        {
            if (dir.x < 0) _moveGridCount = 1; //Right
            else _moveGridCount = -1; //Left
        }
        else if (Mathf.Abs(dot) < 0.25f) //Vertical
        {
            if (dir.y < 0) _moveGridCount = _gridWidth; //Up
            else _moveGridCount = -_gridWidth; //Down
        }
    }

    private void OnInputStart()
    {
        _inactiveTime = 0;
        if (_playablePieceTweeners.Count > 0)
            foreach (var t in _playablePieceTweeners)
                t.Kill();
    }

    private void OnInputEnd()
    {
        if (_clickedPiece == null) return;
        if (_moveGridCount != 0) MovePiece();
        _clickedPiece = null;
        _lastInputPos = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_levelEnded) return;
        if (eventData.pointerEnter == null) return;
        _clickedPiece = eventData.pointerEnter.transform;
        _lastInputPos = mainCam.WorldToScreenPoint(_clickedPiece.position); //Center Point For Smooth Gameplay
    }

    private void MovePiece()
    {
        var gridNumber = _gridPieceDictionary.FirstOrDefault(x => x.Value.PieceTr == _clickedPiece).Key;
        var endGridNumber = gridNumber + _moveGridCount;

        if (Mathf.Abs(_moveGridCount) == 1 && gridNumber / _gridWidth != endGridNumber / _gridWidth)
            return; //Check If Same Width For Horizontal Move
        if (endGridNumber < 0 || endGridNumber >= _gridWidth * _gridHeight) return; //Check If Inside Grid

        var clickedPiece = _gridPieceDictionary[gridNumber];
        var switchPiece = _gridPieceDictionary[endGridNumber];
        var switchPieceTr = switchPiece.PieceTr;
        if (switchPieceTr == null) return;

        //Move
        _clickedPiece.DOMove(switchPieceTr.position, 0.2f)
            .SetEase(Ease.OutQuad);
        switchPieceTr.DOMove(_clickedPiece.position, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (_moveGridCount == 0) _clickedPiece = null; //Prevents null exception if a new move started before tween ends
                CheckRow(gridNumber);
                if (endGridNumber / _gridWidth != gridNumber / _gridWidth) CheckRow(endGridNumber); //Ignore If Same Row
                StartCoroutine(CheckMove(false));
            });

        //Set Dictionary
        _gridPieceDictionary[gridNumber] = switchPiece;
        _gridPieceDictionary[endGridNumber] = clickedPiece;

        _moveGridCount = 0;
        //Set Move Count
        _moveCount--;
        moveLeftText.text = "Move Left : " + _moveCount;
        if (_moveCount == 0) StartCoroutine(LevelEnded());
    }

    private void CheckRow(int gridNumber)
    {
        //Get the first number and check the rest of row
        var rowNumber = gridNumber / _gridWidth;
        var rowFirstNumber = rowNumber * _gridWidth;
        var rowPieceType = _gridPieceDictionary[rowFirstNumber].PieceType;

        var pieces = new List<Transform>();
        for (int i = rowFirstNumber; i < rowFirstNumber + _gridWidth; i++)
        {
            var p = _gridPieceDictionary[i];
            if (p.PieceType == rowPieceType) pieces.Add(p.PieceTr);
            else return;
        }

        if (_twScore.IsActive())
        {
            _twScore.Kill();
            _score = _newScore;
        }

        _newScore = _score +
                    (100 + rowPieceType * 50) *
                    _gridWidth; //This is a variable because on kill gets late to update score to new tween
        _twScore = DOTween.To(() => _score, x => _score = x, _newScore, 0.5f)
            .OnUpdate(() =>
            {
                scoreText.text = "Score : " + _score;
                if (_score > _highScore)
                {
                    _highScoreExceeded = true;
                    _highScore = _score;
                    highScoreText.text = "High Score : " + _highScore;
                }
            });
        foreach (var p in pieces)
        {
            Destroy(p.gameObject);
            Instantiate(completedPiecePrefab, p.position, Quaternion.identity, transform);
        }
    }

    private IEnumerator CheckMove(bool inactive)
    {
        //Gets non completed grid lists, counts piece types foreach list
        //Could implement more futures here, but not sure how to do them optimized
        //Here, when player is inactive shows a random completable pieces in grid number order, so it is not always closest ones
        yield return new WaitForEndOfFrame(); //To prevent bypass CheckRow method

        _playablePieceTweeners = new List<Tweener>();
        var connectedNumbersList = FindConnectedGroups(new List<List<int>>(), new List<int>(), 0);
        foreach (var connectedList in connectedNumbersList)
        {
            var pieceTypeLists = new[]
            {
                new List<Transform>(), new List<Transform>(), new List<Transform>(), new List<Transform>(),
            };
            foreach (var n in connectedList)
            {
                var piece = _gridPieceDictionary[n];
                var pieceType = piece.PieceType;
                pieceTypeLists[pieceType].Add(piece.PieceTr);
            }

            var completablePieces = pieceTypeLists.Where(x => x.Count >= _gridWidth).ToList();
            if (completablePieces.Count > 0)
            {
                if (!inactive) yield break;
                //If inactive animate random completable pieces
                var randomType = completablePieces[Random.Range(0, completablePieces.Count)];
                for (int i = 0; i < _gridWidth; i++)
                {
                    var p = randomType[i];
                    var t = p.DOScale(0.05f, 0.25f)
                        .SetRelative(true)
                        .SetLoops(4, LoopType.Yoyo)
                        .OnKill(() => p.localScale = Vector3.one * 0.2f);
                    _playablePieceTweeners.Add(t);
                }

                yield break;
            }
        }

        StartCoroutine(LevelEnded());
    }

    private List<List<int>> FindConnectedGroups(List<List<int>> allList, List<int> previousList, int start)
    {
        //Starts grid from 0 to a completed row. Adds non completed grids to list
        //Passes completed row and iterates until all grids are checked. Returns all lists in a list
        if (previousList.Count != 0) allList.Add(previousList);
        if (start != _gridPieceDictionary.Count - 1) //If iteration is not ended
        {
            var gridNumbersConnected = new List<int>();
            var iterationEnded = true;
            for (int i = start; i < _gridPieceDictionary.Count; i++)
            {
                if (_gridPieceDictionary[i].PieceTr != null) gridNumbersConnected.Add(i);
                else
                {
                    iterationEnded = false;
                    FindConnectedGroups(allList, gridNumbersConnected, i + _gridWidth);
                    break;
                }
            }

            if (iterationEnded) allList.Add(gridNumbersConnected);
        }

        return allList;
    }

    private IEnumerator LevelEnded()
    {
        if (_levelEnded) yield break; //If move count and possible moves ended at the same time
        _levelEnded = true;
        yield return new WaitForSeconds(1f);
        transform.DOScale(0, 0.5f)
            .OnComplete(() =>
            {
                _sceneHandler.SetCameraSizeForMenu();
                _sceneHandler.LevelEnded(_highScoreExceeded, _highScore);
            });
    }

    private void SetGrid(LevelData data)
    {
        _gridWidth = data.GridWidth;
        _gridHeight = data.GridHeight;
        _highScore = data.HighScore;
        _moveCount = data.MoveCount;

        grid.size = new Vector2(_gridWidth, _gridHeight);
        background.localScale = new Vector3(_gridWidth + 0.1f, _gridHeight + 0.1f, 1);

        _sceneHandler.SetCameraSizeForGrid(_gridWidth, _gridHeight);

        var iCount = 0;
        for (int i = -_gridHeight + 1; i < _gridHeight; i += 2)
        {
            var jCount = 0;
            for (int j = -_gridWidth + 1; j < _gridWidth; j += 2)
            {
                var spawnPos = new Vector3(j / 2f, i / 2f, 0);
                var gridNumber = iCount * _gridWidth + jCount;
                var pieceString = data.Grid[gridNumber];
                var pieceInt = GetPieceType(pieceString);
                var p = Instantiate(piecePrefabs[pieceInt], spawnPos, Quaternion.identity, transform);
                var piece = new Piece { PieceTr = p.transform, PieceType = pieceInt };
                _gridPieceDictionary.Add(gridNumber, piece);
                jCount++;
            }

            iCount++;
        }

        //Set Text Y Positions
        var textGridXPos = (_gridWidth / 2f) - 1.5f;
        var textGridYPos = (_gridHeight + 1) / 2f;
        highScoreText.rectTransform.localPosition = new Vector3(textGridXPos, textGridYPos, 0);
        scoreText.rectTransform.localPosition = new Vector3(-textGridXPos, textGridYPos, 0);
        moveLeftText.rectTransform.localPosition = new Vector3(0, -textGridYPos, 0);
        //Set Texts
        highScoreText.text = "High Score : " + _highScore;
        moveLeftText.text = "Move Left : " + _moveCount;
    }

    private int GetPieceType(string s)
    {
        var number = 0; //red is 0
        if (s == "g") number = 1;
        if (s == "b") number = 2;
        if (s == "y") number = 3;
        return number;
    }
}