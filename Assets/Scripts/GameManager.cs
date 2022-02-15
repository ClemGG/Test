using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TeasingGame
{

    /// <summary>
    /// Manages the Tiles and checks the victory conditions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Fields


        #region UIs

        [SerializeField] private TeasingGameHomeSceneController _sceneController;
        [SerializeField] private Transform _tilePrefab;
        [SerializeField] private Transform _gridParent;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _victoryText;
        [SerializeField] private Image _resultImg;

        #endregion

        [SerializeField] private Vector2Int _gridSize = new Vector2Int(3, 3);   //Hides the Tile set at this sibling index.
        [SerializeField] private int _tileIndexToHide = 4;                      //Hides the Tile set at this sibling index.
        [SerializeField] private float _mouseDeltaThreshold = 20f;              //Move the Tile after the drag delta overcomes this value.
        [SerializeField] private float _timeLeft = 180f;                        //Timer set to 3 minutes.
        private float _timer;

        private Tile[][] _tiles;
        private List<Tile> _shuffledTilesList;   //Keeps the shuffled list of Tiles
        private List<Tile> _adjacentTiles;        //Keeps the list of adjacent Tiles to the current inspected one.
        private Tile _emptyTile;                 //We keep track of the empty Tile for the Tile swapping.
        private Tile _currentDraggedTile;
        private Vector3 _previousMousePos;

        private int _score = 0;             //The number of moves needed to complete the puzzle.
        private bool _isGameOver = false;   //Set to true if _timer = 0f or all Tiles match their position.
        private bool _isShuffled = false;

        #endregion

        #region Mono


        void Start()
        {
            _resultImg.enabled = false;
            _victoryText.enabled = false;
            _timer = _timeLeft;

            SetupTilesArray();


            //Then we shuffle the Tiles at random to make a new game
            Shuffle();
        }


        void Update()
        {
            ComputeTimer();

            if(_currentDraggedTile != null)
            {
                OnTileDragUpdated();
            }

#if UNITY_EDITOR
            //Debugging purposes
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Shuffle();
            }
#endif
        }


        void OnDisable()
        {
            //We unsubscribe when we leave the scene to avoid memory leaks
            for (int x = 0; x < _tiles.Length; x++)
            {
                for (int y = 0; y < _tiles[x].Length; y++)
                {
                    _tiles[x][y].OnTileMoved -= CheckVictoryCondition;
                    _tiles[x][y].OnTileDragStarted -= OnTileDragStarted;
                    _tiles[x][y].OnTileDragEnded -= OnTileDragEnded;
                }
            }
        }
        #endregion


        #region Private Methods


        #region Setup

        private void SetupTilesArray()
        {
            _shuffledTilesList = new List<Tile>();
            _adjacentTiles = new List<Tile>();
            _tiles = new Tile[_gridSize.x][];
            for (int i = 0; i < _gridSize.x; i++)
            {
                _tiles[i] = new Tile[_gridSize.y];
            }

            //We automatically retrieve the Tiles from the grid
            for (int x = 0; x < _tiles.Length; x++)
            {
                for (int y = 0; y < _tiles[x].Length; y++)
                {
                    int index = x * _gridSize.x + y;

                    _tiles[x][y] = _gridParent.GetChild(index).GetComponent<Tile>();
                    _tiles[x][y].InitTile(index == _tileIndexToHide);
                    _tiles[x][y].OnTileMoved += CheckVictoryCondition;
                    _tiles[x][y].OnTileDragStarted += OnTileDragStarted;
                    _tiles[x][y].OnTileDragEnded += OnTileDragEnded;

                    _shuffledTilesList.Add(_tiles[x][y]);

                    //We retrieve the empty Tile for later uses
                    if (index == _tileIndexToHide)
                    {
                        _emptyTile = _tiles[x][y];
                    }
                }
            }

        }

        /// <summary>
        /// Moves all Tiles at random at the start of the game
        /// </summary>
        private void Shuffle()
        {
            bool allAreMoved = true;
            do
            {
                allAreMoved = true;
                for (int i = 0; i < 100; i++)
                {
                    _emptyTile.GetAdjacentTiles(_shuffledTilesList, _adjacentTiles, _gridSize);
                    Tile randomTile = _adjacentTiles[Random.Range(0, _adjacentTiles.Count)];
                    _emptyTile.SwapTileWith(randomTile, false);
                }

                for (int j = 0; j < _shuffledTilesList.Count; j++)
                {
                    if (_shuffledTilesList[j].T.GetSiblingIndex() == _shuffledTilesList[j].ID)
                    {
                        allAreMoved = false;
                        break;
                    }
                }
            }
            while (!allAreMoved);  //We keep shuffling if the empty Tile is once again at its start position.
            _isShuffled = true;
        }


        #endregion


        #region Tile Drag

        private void OnTileDragStarted(Tile draggedTile)
        {
            if (_isGameOver)
            {
                return;
            }
            //print("drag started");


            //When we begin the drag, we check if the empty Tile is nearby.
            //If it isn't, we stop the drag since we can't move the Tile anywhere.
            _emptyTile.GetAdjacentTiles(_shuffledTilesList, _adjacentTiles, _gridSize);

            if (_adjacentTiles.Contains(draggedTile))
            {
                _currentDraggedTile = draggedTile;


#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                _previousMousePos = Input.mousePosition;
#elif UNITY_ANDROID || UNITY_IOS
                _previousMousePos = Input.GetTouch(0).position;
#endif
            }
        }

        private void OnTileDragUpdated()
        {
            if (_isGameOver)
            {
                return;
            }
            //print("drag updated");

#if UNITY_EDITOR|| UNITY_STANDALONE_WIN

            //In the Update loop, we get the position of the cursor/the finger
            //to get the direction of the delta.
            Vector3 mousePos = Input.mousePosition;

#elif UNITY_ANDROID || UNITY_IOS
            Vector3 mousePos = Input.GetTouch(0).position;
#endif
            //Check the scroll direction with the highest value (vertical or horizontal)
            Vector3 delta = mousePos - _previousMousePos;
            float highest = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            if(Mathf.Approximately(highest, Mathf.Abs(delta.x)))
            {
                delta = new Vector3(delta.x, 0f, 0f);
            }
            else
            {
                delta = new Vector3(0f, delta.y, 0f);
            }

            //If the delta is strong enough, we move the Tile in the corresponding direction
            if (delta.sqrMagnitude > _mouseDeltaThreshold * _mouseDeltaThreshold)
            {

                Vector2Int draggedIndexInGrid = _currentDraggedTile.GetIndexInGrid(_gridSize);
                Vector2Int emptyIndexInGrid = _emptyTile.GetIndexInGrid(_gridSize);
                bool shouldMove = false;

                //We check if the emptyTile is in the direction of the drag delta
                if (delta.x > 0)
                {
                    //print("right");
                    if(emptyIndexInGrid.y == draggedIndexInGrid.y + 1)
                    {
                        shouldMove = true;
                    }
                }
                else if (delta.x < 0)
                {
                    //print("left"); 
                    if (emptyIndexInGrid.y == draggedIndexInGrid.y - 1)
                    {
                        shouldMove = true;
                    }
                }
                else if (delta.y > 0)
                {
                    //print("top");
                    if (emptyIndexInGrid.x == draggedIndexInGrid.x - 1)
                    {
                        shouldMove = true;
                    }
                }
                else if (delta.y < 0)
                {
                    //print("bottom"); 
                    if (emptyIndexInGrid.x == draggedIndexInGrid.x + 1)
                    {
                        shouldMove = true;
                    }
                }


                if (shouldMove)
                {
                    //print("move");
                    _currentDraggedTile.SwapTileWith(_emptyTile, false);
                    _score++;

                    //When we have finished moving a Tile, we exit the drag update
                    OnTileDragEnded(_currentDraggedTile);
                }
            }


            _previousMousePos = mousePos;
        }

        private void OnTileDragEnded(Tile draggedTile)
        {
            if (_isGameOver)
            {
                return;
            }
            //print("drag ended");
            _currentDraggedTile = null;
        }


        #endregion

        #region Victory Conditions

        /// <summary>
        /// Subscribed to each Tile's OnTileMoved Action to check the victory condition after each move.
        /// </summary>
        private void CheckVictoryCondition()
        {

            if (_timer > 0f && _isShuffled)
            {
                //We check if all Tiles are in their right place
                bool allInPlace = true;
                for (int x = 0; x < _tiles.Length; x++)
                {
                    for (int y = 0; y < _tiles[x].Length; y++)
                    {
                        if (_tiles[x][y].T.GetSiblingIndex() != _tiles[x][y].ID)
                        //if (!_tiles[x][y].IsInRightPlace)
                        {
                            allInPlace = false;
                            break;
                        }
                    }
                }

                //If so, we mark the game as won
                if (allInPlace)
                {
                    _isGameOver = true;
                    _victoryText.text = "Victory!";

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE_WIN
                    _resultImg.sprite = Resources.Load<Sprite>($"Android/Result");
#elif UNITY_IOS
                    _resultImg.sprite = Resources.Load<Sprite>($"Apple/Result");
#endif

                    _victoryText.enabled = true;
                    _resultImg.enabled = true;

                    //We also set the High Score in the PlayerPrefs if it is lower than the previous one
                    //so that the player can see it between each game.
                    int highscore = PlayerPrefs.GetInt("HS", int.MaxValue);
                    if (_score < highscore || highscore == 0)
                    {
                        PlayerPrefs.SetInt("HS", _score);
                    }

                    //After we win, we wait 1 second before loading the Home scene.
                    StartCoroutine(GoHomeCo());
                }
            }
        }


        private void ComputeTimer()
        {
            if (_timer <= 0f && !_isGameOver)
            {
                _timer = 0f;
                _isGameOver = true;
                _victoryText.text = "Time's up! You lose!";
                _victoryText.enabled = true;

                //After we lose, we wait 1 second before loading the Home scene.
                StartCoroutine(GoHomeCo());

            }

            if (_isGameOver) return;

            _timer -= Time.deltaTime;

            //To avoid setting minutes and seconds to -1;
            int minutes = Mathf.FloorToInt(Mathf.Max(0f, _timer / 60f));
            int seconds = Mathf.FloorToInt(Mathf.Max(0f, _timer % 60f));

            string secondsStr = seconds > 10 ? seconds.ToString() : $"0{seconds}";
            _timerText.text = $"{minutes}:{secondsStr}";
        }


        private IEnumerator GoHomeCo()
        {
            WaitForSeconds wait = new WaitForSeconds(2f);
            yield return wait;

            _sceneController.GoToGameScene();
        }

#endregion

#endregion
    }
}