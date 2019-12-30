using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms;
using System;

namespace UI.ListView
{
    /// <summary>
    /// A reusable table for for (vertical) tables. API inspired by Cocoa's UITableView
    /// Hierarchy structure should be :
    /// GameObject + TableView (this) + Mask + Scroll Rect (point to child)
    /// - Child GameObject + Vertical Layout Group
    /// This class should be after Unity's internal UI components in the Script Execution Order
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class TableView : MonoBehaviour
    {

        #region Public API
        public enum Direction
        {
            Vertical,       //垂直方向
            Horizontal      //水平方向
        }

        public Direction mDirection = Direction.Vertical;

        /// <summary>
        /// The data source that will feed this table view with information. Required.
        /// </summary>
        public ITableViewDataSource dataSource
        {
            get { return m_dataSource; }
            set { m_dataSource = value; m_requiresReload = true; }
        }

        [System.Serializable]
        public class CellVisibilityChangeEvent : UnityEvent<int, bool> { }
        /// <summary>
        /// This event will be called when a cell's visibility changes
        /// First param (int) is the row index, second param (bool) is whether or not it is visible
        /// </summary>
        public CellVisibilityChangeEvent onCellVisibilityChanged;

        /// <summary>
        /// Get a cell that is no longer in use for reusing
        /// </summary>
        /// <param name="reuseIdentifier">The identifier for the cell type</param>
        /// <returns>A prepared cell if available, null if none</returns>
        public TableViewCell GetReusableCell(string reuseIdentifier)
        {
            LinkedList<TableViewCell> cells;
            if (!m_reusableCells.TryGetValue(reuseIdentifier, out cells))
            {
                return null;
            }
            if (cells.Count == 0)
            {
                return null;
            }
            TableViewCell cell = cells.First.Value;
            cells.RemoveFirst();
            return cell;
        }

        public bool isEmpty { get; private set; }

        /// <summary>
        /// Reload the table view. Manually call this if the data source changed in a way that alters the basic layout
        /// (number of rows changed, etc)
        /// </summary>
        public void ReloadData()
        {
            if (m_LayoutGroup == null)
            {
                Debug.LogError("layoutGroup is null");
                return;
            }
            m_sizes = new float[m_dataSource.GetNumberOfRowsForTableView(this)];
            this.isEmpty = m_sizes.Length == 0;
            ClearAllRows();
            if (this.isEmpty)
            {
                return;
            }
            m_cumulativeSizes = new float[m_sizes.Length];
            m_cleanCumulativeIndex = -1;

            for (int i = 0; i < m_sizes.Length; i++)
            {
                m_sizes[i] = m_dataSource.GetSizeForIndexInTableView(this, i);
                if (i > 0)
                {
                    m_sizes[i] += m_LayoutGroup.spacing;
                }
            }

            switch (mDirection)
            {
                case Direction.Horizontal:
                    var size = new Vector2(GetCumulativeCellSize(m_sizes.Length - 1) - m_LayoutGroup.padding.horizontal, m_scrollRect.content.sizeDelta[1]);
                    size.x = Mathf.Max(size.x, rect.width + 1);
                    var v = size.x - rect.width;
                    var w = m_sizes[0];
                    if (v > 10 && v < w) size.x = size.x + (w - v) - m_LayoutGroup.padding.left;
                    m_scrollRect.content.sizeDelta = size;
                    break;
                case Direction.Vertical:
                    var s = new Vector2(m_scrollRect.content.sizeDelta[0], GetCumulativeCellSize(m_sizes.Length - 1) + m_LayoutGroup.padding.vertical);
                    s.y = Mathf.Max(s.y, rect.height + 1);
                    m_scrollRect.content.sizeDelta = s;
                    break;
                default:
                    break;
            }

            RecalculateVisibleCellsFromScratch();
            m_requiresReload = false;
        }

        /// <summary>
        /// Get cell at a specific row (if active). Returns null if not.
        /// </summary>
        public TableViewCell GetCellAtIndex(int index)
        {
            TableViewCell retVal = null;
            m_visibleCells.TryGetValue(index, out retVal);
            return retVal;
        }

        /// <summary>
        /// Get the range of the currently visible rows
        /// </summary>
        public Range visibleCellwRange
        {
            get { return m_visibleCellRange; }
        }

        /// <summary>
        /// Notify the table view that one of its rows changed size
        /// </summary>
        public void NotifyCellDimensionsChanged(int index)
        {
            float oldSize = m_sizes[index];
            m_sizes[index] = m_dataSource.GetSizeForIndexInTableView(this, index);
            if (index > 0)
            {
                m_sizes[index] += m_LayoutGroup.spacing;
            }

            m_cleanCumulativeIndex = Mathf.Min(m_cleanCumulativeIndex, index - 1);
            TableViewCell cell = GetCellAtIndex(index);
            if (cell != null)
            {
                switch (mDirection)
                {
                    case Direction.Horizontal:
                        var le = cell.GetComponent<LayoutElement>();
                        le.preferredWidth = m_sizes[index];
                        if (index > 0)
                        {
                            le.preferredWidth -= m_LayoutGroup.spacing;
                        }
                        le.minWidth = le.preferredWidth;
                        break;
                    case Direction.Vertical:
                        var _le = cell.GetComponent<LayoutElement>();
                        _le.preferredHeight = m_sizes[index];
                        if (index > 0)
                        {
                            _le.preferredHeight -= m_LayoutGroup.spacing;
                        }
                        _le.minHeight = _le.minHeight;
                        break;
                    default:
                        break;
                }
            }
            float sizeDelta = m_sizes[index] - oldSize;
            switch (mDirection)
            {
                case Direction.Horizontal:
                    m_scrollRect.content.sizeDelta = new Vector2(m_scrollRect.content.sizeDelta.x + sizeDelta, m_scrollRect.content.sizeDelta.y);
                    break;
                case Direction.Vertical:
                    m_scrollRect.content.sizeDelta = new Vector2(m_scrollRect.content.sizeDelta.x, m_scrollRect.content.sizeDelta.y + sizeDelta);
                    break;
                default:
                    break;
            }

            m_requiresRefresh = true;
        }

        /// <summary>
        /// Get the maximum scrollable height of the table. scrollY property will never be more than this.
        /// </summary>
        public float scrollableHeight
        {
            get
            {
                return m_scrollRect.content.rect.height - (this.transform as RectTransform).rect.height;
            }
        }

        public float scrollableWidth
        {
            get
            {
                return m_scrollRect.content.rect.width - (this.transform as RectTransform).rect.width;
            }
        }

        /// <summary>
        /// Get or set the current scrolling position of the table
        /// </summary>
        public float Scroll
        {
            get
            {
                return m_scroll;
            }
            set
            {
                if (this.isEmpty)
                {
                    return;
                }
                value = Mathf.Clamp(value, 0, GetScrollForIndex(m_sizes.Length - 1, true));
                if (m_scroll != value)
                {
                    m_scroll = value;
                    m_requiresRefresh = true;
                    float relativeScroll = 0;
                    switch (mDirection)
                    {
                        case Direction.Horizontal:
                            relativeScroll = value / this.scrollableWidth;
                            m_scrollRect.horizontalNormalizedPosition = 1 - relativeScroll;
                            break;
                        case Direction.Vertical:
                            relativeScroll = value / this.scrollableHeight;
                            m_scrollRect.verticalNormalizedPosition = 1 - relativeScroll;
                            break;
                        default:
                            break;
                    }

                }
            }
        }

        /// <summary>
        /// Get the y that the table would need to scroll to to have a certain row at the top
        /// </summary>
        /// <param name="index">The desired row</param>
        /// <param name="above">Should the top of the table be above the row or below the row?</param>
        /// <returns>The y position to scroll to, can be used with scrollY property</returns>
        public float GetScrollForIndex(int index, bool above)
        {
            float retVal = GetCumulativeCellSize(index);
            if (mDirection == Direction.Horizontal)
            {
                retVal += m_LayoutGroup.padding.left;
            }
            else
            {
                retVal += m_LayoutGroup.padding.top;
            }
            if (above)
            {
                retVal -= m_sizes[index];
            }
            return retVal;
        }

        #endregion

        #region Private implementation

        private ITableViewDataSource m_dataSource;
        private bool m_requiresReload;
        private HorizontalOrVerticalLayoutGroup m_LayoutGroup;
        private ScrollRect m_scrollRect;
        private LayoutElement m_leftOrTopPadding;
        private LayoutElement m_rightOrBottomPadding;

        private float[] m_sizes;
        //cumulative[i] = sum(rowHeights[j] for 0 <= j <= i)
        private float[] m_cumulativeSizes;
        private int m_cleanCumulativeIndex;

        private Dictionary<int, TableViewCell> m_visibleCells;
        private Range m_visibleCellRange;

        private RectTransform m_reusableCellContainer;
        private Dictionary<string, LinkedList<TableViewCell>> m_reusableCells;

        private float m_scroll;

        private bool m_requiresRefresh;
        private Rect rect
        {
            get
            {
                return transform.GetComponent<RectTransform>().rect;
            }
        }
        public TableViewCell[] GetCells()
        {
            return m_scrollRect.content.GetComponentsInChildren<TableViewCell>();
        }
        private void ScrollViewValueChanged(Vector2 newScrollValue)
        {
            switch (mDirection)
            {
                case Direction.Horizontal:
                    m_scroll = newScrollValue.x * scrollableWidth;
                    break;
                case Direction.Vertical:
                    m_scroll = (1 - newScrollValue.y) * scrollableHeight;
                    break;
                default:
                    break;
            }
            m_requiresRefresh = true;
        }

        private void RecalculateVisibleCellsFromScratch()
        {
            ClearAllRows();
            SetInitialVisibleCells();
        }

        private void ClearAllRows()
        {
            while (m_visibleCells.Count > 0)
            {
                HideCell(false);
            }
            m_visibleCellRange = new Range(0, 0);
        }

        void Awake()
        {
            isEmpty = true;
            m_scrollRect = GetComponent<ScrollRect>();
            m_scrollRect.vertical = mDirection == Direction.Vertical;
            m_scrollRect.horizontal = mDirection == Direction.Horizontal;

            var content = m_scrollRect.content;
            if (!content)
            {
                Debug.LogError("The ScrollRect Content is null!!");
                return;
            }

            if (m_scrollRect.horizontal)
            {
                m_LayoutGroup = m_scrollRect.content.GetComponent<HorizontalLayoutGroup>();
                //if(m_LayoutGroup == null)
                //{
                //    m_LayoutGroup = m_scrollRect.content.gameObject.AddComponent<HorizontalLayoutGroup>();
                //}
                //m_LayoutGroup.childForceExpandHeight = true;
                //m_LayoutGroup.childForceExpandWidth = false;
                //var rect = (m_LayoutGroup.transform as RectTransform);
                //rect.anchorMin = Vector2.zero;
                //rect.anchorMax = Vector2.up;
                //rect.pivot = Vector2.up/2;
            }
            else
            {
                m_LayoutGroup = m_scrollRect.content.GetComponent<VerticalLayoutGroup>();
                //if (m_LayoutGroup == null)
                //{
                //    m_LayoutGroup = m_scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                //}
                //m_LayoutGroup.childForceExpandHeight = false;
                //m_LayoutGroup.childForceExpandWidth = true;
                //var rect = (m_LayoutGroup.transform as RectTransform);
                //rect.anchorMin = Vector2.up;
                //rect.anchorMax = Vector2.one;
                //rect.pivot = new Vector2(0.5f, 1);
            }
            m_leftOrTopPadding = CreateEmptyPaddingElement("LeftTopPadding");
            m_leftOrTopPadding.transform.SetParent(m_scrollRect.content, false);
            m_rightOrBottomPadding = CreateEmptyPaddingElement("RightBottom");
            m_rightOrBottomPadding.transform.SetParent(m_scrollRect.content, false);
            m_visibleCells = new Dictionary<int, TableViewCell>();

            m_reusableCellContainer = new GameObject("ReusableCells", typeof(RectTransform)).GetComponent<RectTransform>();
            m_reusableCellContainer.SetParent(this.transform, false);
            m_reusableCellContainer.gameObject.SetActive(false);
            m_reusableCells = new Dictionary<string, LinkedList<TableViewCell>>();
        }

        void Update()
        {
            if (m_requiresReload)
            {
                ReloadData();
            }
        }

        void LateUpdate()
        {
            if (m_requiresRefresh)
            {
                RefreshVisibleCells();
            }
        }

        void OnEnable()
        {
            m_scrollRect.onValueChanged.AddListener(ScrollViewValueChanged);
        }

        void OnDisable()
        {
            m_scrollRect.onValueChanged.RemoveListener(ScrollViewValueChanged);
        }

        private Range CalculateCurrentVisibleCellRange()
        {
            float start = m_scroll;
            float end = 0;
            int startIndex = 0, endIndex = 0;
            switch (mDirection)
            {
                case Direction.Horizontal:
                    end = m_scroll + (this.transform as RectTransform).rect.width;
                    startIndex = FindIndexOfCellAtScroll(start);
                    endIndex = FindIndexOfCellAtScroll(end);
                    break;
                case Direction.Vertical:
                    start = Math.Max(m_scroll - m_LayoutGroup.padding.top, 0);
                    var visibleTopPadding = Math.Max(m_LayoutGroup.padding.top - m_scroll, 0);
                    end = start + (this.transform as RectTransform).rect.height - visibleTopPadding;
                    startIndex = FindIndexOfCellAtScroll(start);
                    endIndex = FindIndexOfCellAtScroll(end);
                    break;
                default:
                    break;
            }

            return new Range(startIndex, endIndex - startIndex + 1);
        }

        private void SetInitialVisibleCells()
        {
            Range visibleCells = CalculateCurrentVisibleCellRange();
            for (int i = 0; i < visibleCells.count; i++)
            {
                AddCell(visibleCells.from + i, true);
            }
            m_visibleCellRange = visibleCells;
            UpdatePaddingElements();
        }

        private void AddCell(int row, bool atEnd)
        {
            TableViewCell newCell = m_dataSource.GetCellForRowInTableView(this, row);
            newCell.transform.SetParent(m_scrollRect.content, false);

            LayoutElement layoutElement = newCell.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = newCell.gameObject.AddComponent<LayoutElement>();
            }
            switch (mDirection)
            {
                case Direction.Horizontal:
                    layoutElement.preferredWidth = m_sizes[row];
                    if (row > 0)
                    {
                        layoutElement.preferredWidth -= m_LayoutGroup.spacing;
                    }
                    layoutElement.minWidth = layoutElement.preferredWidth;
                    break;
                case Direction.Vertical:
                    layoutElement.preferredHeight = m_sizes[row];
                    if (row > 0)
                    {
                        layoutElement.preferredHeight -= m_LayoutGroup.spacing;
                    }
                    layoutElement.minHeight = layoutElement.minHeight;
                    break;
                default:
                    break;
            }



            m_visibleCells[row] = newCell;
            if (atEnd)
            {
                newCell.transform.SetSiblingIndex(m_scrollRect.content.childCount - 2); //One before bottom padding
            }
            else
            {
                newCell.transform.SetSiblingIndex(1); //One after the top padding
            }
            this.onCellVisibilityChanged.Invoke(row, true);
        }

        private void RefreshVisibleCells()
        {
            m_requiresRefresh = false;

            if (this.isEmpty)
            {
                return;
            }

            Range newVisibleRows = CalculateCurrentVisibleCellRange();
            int oldTo = m_visibleCellRange.Last();
            int newTo = newVisibleRows.Last();

            if (newVisibleRows.from > oldTo || newTo < m_visibleCellRange.from)
            {
                //We jumped to a completely different segment this frame, destroy all and recreate
                RecalculateVisibleCellsFromScratch();
                return;
            }

            //Remove rows that disappeared to the top
            for (int i = m_visibleCellRange.from; i < newVisibleRows.from; i++)
            {
                HideCell(false);
            }
            //Remove rows that disappeared to the bottom
            for (int i = newTo; i < oldTo; i++)
            {
                HideCell(true);
            }
            //Add rows that appeared on top
            for (int i = m_visibleCellRange.from - 1; i >= newVisibleRows.from; i--)
            {
                AddCell(i, false);
            }
            //Add rows that appeared on bottom
            for (int i = oldTo + 1; i <= newTo; i++)
            {
                AddCell(i, true);
            }
            m_visibleCellRange = newVisibleRows;
            UpdatePaddingElements();
        }

        private void UpdatePaddingElements()
        {
            float hiddenElementsSum = 0;
            for (int i = 0; i < m_visibleCellRange.from; i++)
            {
                hiddenElementsSum += m_sizes[i];
            }
            switch (mDirection)
            {
                case Direction.Horizontal:
                    m_leftOrTopPadding.preferredWidth = hiddenElementsSum;
                    m_leftOrTopPadding.gameObject.SetActive(m_leftOrTopPadding.preferredWidth > 0);
                    for (int i = m_visibleCellRange.from; i <= m_visibleCellRange.Last(); i++)
                    {
                        hiddenElementsSum += m_sizes[i];
                    }
                    float paddingWidth = m_scrollRect.content.rect.width - hiddenElementsSum;
                    m_rightOrBottomPadding.preferredWidth = paddingWidth - m_LayoutGroup.spacing;
                    m_rightOrBottomPadding.gameObject.SetActive(m_rightOrBottomPadding.preferredWidth > 0 && hiddenElementsSum > rect.width);
                    break;
                case Direction.Vertical:
                    m_leftOrTopPadding.preferredHeight = hiddenElementsSum;
                    m_leftOrTopPadding.gameObject.SetActive(m_leftOrTopPadding.preferredHeight > 0);
                    for (int i = m_visibleCellRange.from; i <= m_visibleCellRange.Last(); i++)
                    {
                        hiddenElementsSum += m_sizes[i];
                    }
                    float paddingHeight = m_scrollRect.content.rect.height - hiddenElementsSum;
                    m_rightOrBottomPadding.preferredHeight = paddingHeight - m_LayoutGroup.spacing;
                    m_rightOrBottomPadding.gameObject.SetActive(m_rightOrBottomPadding.preferredHeight > 0 && hiddenElementsSum > rect.height);
                    break;
                default:
                    break;
            }

        }

        private void HideCell(bool last)
        {
            //Debug.Log("Hiding row at scroll y " + m_scrollY.ToString("0.00"));

            int row = last ? m_visibleCellRange.Last() : m_visibleCellRange.from;
            TableViewCell removedCell = m_visibleCells[row];
            StoreCellForReuse(removedCell);
            m_visibleCells.Remove(row);
            m_visibleCellRange.count -= 1;
            if (!last)
            {
                m_visibleCellRange.from += 1;
            }
            this.onCellVisibilityChanged.Invoke(row, false);
        }

        private LayoutElement CreateEmptyPaddingElement(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            LayoutElement le = go.GetComponent<LayoutElement>();
            return le;
        }

        private int FindIndexOfCellAtScroll(float scroll)
        {
            //TODO : Binary search if inside clean cumulative row height area, else walk until found.
            return FindIndexOfCellAtScroll(scroll, 0, m_cumulativeSizes.Length - 1);
        }

        private int FindIndexOfCellAtScroll(float y, int startIndex, int endIndex)
        {
            if (startIndex >= endIndex)
            {
                return startIndex;
            }
            int midIndex = (startIndex + endIndex) / 2;
            if (GetCumulativeCellSize(midIndex) >= y)
            {
                return FindIndexOfCellAtScroll(y, startIndex, midIndex);
            }
            else
            {
                return FindIndexOfCellAtScroll(y, midIndex + 1, endIndex);
            }
        }

        private float GetCumulativeCellSize(int row)
        {
            while (m_cleanCumulativeIndex < row)
            {
                m_cleanCumulativeIndex++;
                //Debug.Log("Cumulative index : " + m_cleanCumulativeIndex.ToString());
                m_cumulativeSizes[m_cleanCumulativeIndex] = m_sizes[m_cleanCumulativeIndex];
                if (m_cleanCumulativeIndex > 0)
                {
                    m_cumulativeSizes[m_cleanCumulativeIndex] += m_cumulativeSizes[m_cleanCumulativeIndex - 1];
                }
            }
            return m_cumulativeSizes[row];
        }

        private void StoreCellForReuse(TableViewCell cell)
        {
            string reuseIdentifier = cell.reuseIdentifier;

            if (string.IsNullOrEmpty(reuseIdentifier))
            {
                Destroy(cell.gameObject);
                return;
            }

            if (!m_reusableCells.ContainsKey(reuseIdentifier))
            {
                m_reusableCells.Add(reuseIdentifier, new LinkedList<TableViewCell>());
            }
            m_reusableCells[reuseIdentifier].AddLast(cell);
            cell.transform.SetParent(m_reusableCellContainer, false);
        }

        #endregion



    }

    internal static class RangeExtensions
    {
        public static int Last(this Range range)
        {
            if (range.count == 0)
            {
                throw new System.InvalidOperationException("Empty range has no to()");
            }
            return (range.from + range.count - 1);
        }

        public static bool Contains(this Range range, int num)
        {
            return num >= range.from && num < (range.from + range.count);
        }
    }
}