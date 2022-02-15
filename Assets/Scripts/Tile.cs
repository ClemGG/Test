using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TeasingGame
{

    public class Tile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Fields

        public int ID { get; private set; }
        public bool IsInRightPlace { get; set; }                    //Checks if the Tile's ID matches its index under the Grid parent.
        public bool IsEmptyTile { get; private set; } = false;      //Set to true if the Tile is missing and cannot be moved by the player.
        public Action OnTileMoved { get; set; }                     //Subscribed by the GameManager to auto check the victory condition.
        public Action<Tile> OnTileDragStarted { get; set; }         //Subscribed by the GameManager to move a Tile when it's dragged in a specific direction.
        public Action<Tile> OnTileDragEnded { get; set; }           //Subscribed by the GameManager to move a Tile when it's dragged in a specific direction.

        public int _currentIndex { get; set; }                    //Used for the shuffle procedure in the GameManager

        public Transform T { get; private set; }
        private Image _tileImage;

        #endregion


        #region Public Methods

        /// <summary>
        /// Sets the Tile's ID to compare with its index inside the Grid parent.
        /// If both numbers match, the Tile is in the right place.
        /// </summary>
        public void InitTile(bool isEmptyTile)
        {
            T = transform;
            _tileImage = GetComponent<Image>();

            ID = _currentIndex = T.GetSiblingIndex();
            SetSprite();

            //Hide the Image if this is the missing Tile
            //(We set the alpha to 0 instead of disabling the Image, so that we can still drag the gameObject in the scene)
            IsEmptyTile = isEmptyTile;
            Color tileColor = Color.white;
            tileColor.a = isEmptyTile ? 0f : 1f;
            _tileImage.color = tileColor;
        }


        public void SwapTileWith(Tile otherTile, bool animation = true)
        {
            //If we want to lerp the transition, we use a Coroutine to delay the tile placement
            if (animation)
            {
                StartCoroutine(MoveTileAnimCo(otherTile));
                otherTile.StartCoroutine(MoveTileAnimCo(this));
            }
            //Otherwise, we swap the Tiles' positions and places under the Grid parent
            else
            {
                Vector3 tempPos = T.position;
                int tempIndex = T.GetSiblingIndex();

                T.position = otherTile.T.position;
                otherTile.T.position = tempPos;

                //Set the current indices for the suffle mechanic
                _currentIndex = otherTile.T.GetSiblingIndex();
                otherTile._currentIndex = tempIndex;

                T.SetSiblingIndex(_currentIndex);
                otherTile.T.SetSiblingIndex(otherTile._currentIndex);


                //We then check if the Tile's ID and index match to valid the game
                IsInRightPlace = ID == _currentIndex;

                //Calls the GameManager's CheckVictory() method
                OnTileMoved?.Invoke();
            }
        }

        public void GetAdjacentTiles(List<Tile> tiles, List<Tile> adjacentTiles, Vector2Int gridSize)
        {
            //We sort it by _currentIndex so that we always get the last shuffled array
            tiles = tiles.OrderBy(tile => tile._currentIndex).ToList();

            adjacentTiles.Clear();
            Vector2Int indexInGrid = GetIndexInGrid(gridSize);

            //Add Tile to the right of the emptyTile
            if (indexInGrid.y + 1 <= gridSize.y - 1)
            {
                adjacentTiles.Add(tiles[_currentIndex + 1]);
            }
            //Add Tile to the left of the emptyTile
            if (indexInGrid.y - 1 >= 0)
            {
                adjacentTiles.Add(tiles[_currentIndex - 1]);
            }
            //Add Tile to the bottom of the emptyTile
            if (indexInGrid.x + 1 <= gridSize.x - 1)
            {
                adjacentTiles.Add(tiles[_currentIndex + gridSize.y]);
            }
            //Add Tile to the top of the emptyTile
            if (indexInGrid.x - 1 >= 0)
            {
                adjacentTiles.Add(tiles[_currentIndex - gridSize.y]);
            }
        }

        public Vector2Int GetIndexInGrid(Vector2Int gridSize)
        {
            return new Vector2Int(_currentIndex / gridSize.x, _currentIndex % gridSize.y);
        }

        #endregion



        #region Private Methods

        /// <summary>
        /// We retrieve the right sprite depending on the platform we compile for (Android or iOS).
        /// </summary>
        private void SetSprite()
        {
            Sprite spriteToLoad = null;
#if UNITY_IOS
        spriteToLoad = Resources.Load<Sprite>($"Apple/{ID+1}");
#elif UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE
            spriteToLoad = Resources.Load<Sprite>($"Android/{ID + 1}");
#endif

            _tileImage.sprite = spriteToLoad;
        }


        IEnumerator MoveTileAnimCo(Tile otherTile)
        {
            Vector3 startPos = T.position;
            Vector3 endPos = otherTile.T.position;

            //Moves the Tile towards its destination over time
            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                T.position = Vector3.MoveTowards(T.position, endPos, timer * 50f);
                yield return null;
            }

            T.position = endPos;

            //Once the movement is done, we swap their places under the Grid parent
            int tempIndex = T.GetSiblingIndex();

            //Set the current indices for the suffle mechanic
            _currentIndex = otherTile.T.GetSiblingIndex();
            otherTile._currentIndex = tempIndex;

            T.SetSiblingIndex(_currentIndex);
            otherTile.T.SetSiblingIndex(otherTile._currentIndex);



            //We then check if the Tile's ID and index match to valid the game
            IsInRightPlace = ID == _currentIndex;

            //Calls the GameManager's CheckVictory() method
            OnTileMoved?.Invoke();
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsEmptyTile)
            {
                OnTileDragStarted?.Invoke(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsEmptyTile)
            {
                OnTileDragEnded?.Invoke(this);
            }
        }

        #endregion
    }


}