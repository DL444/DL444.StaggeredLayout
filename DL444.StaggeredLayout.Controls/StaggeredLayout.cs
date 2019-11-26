using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace DL444.StaggeredLayout.Controls
{
    /// <summary>
    /// The StaggeredLayout allows for layout of items in a column approach 
    /// where an item will be added to whichever column has used the least amount of space.
    /// </summary>
    public sealed class StaggeredLayout : VirtualizingLayout
    {
        /// <summary>
        /// The horizontal alignment characteristics of its children.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignmentProperty); }
            set { SetValue(HorizontalAlignmentProperty, value); }
        }

        /// <summary>
        /// The space between one item and the next item in its column.
        /// </summary>
        public double RowSpacing
        {
            get { return _rowSpacing; }
            set { SetValue(RowSpacingProperty, value); }
        }

        /// <summary>
        /// The space between each columns.
        /// </summary>
        public double ColumnSpacing
        {
            get { return _columnSpacing; }
            set { SetValue(ColumnSpacingProperty, value); }
        }

        /// <summary>
        /// The desired width of each column. 
        /// The width of columns can exceed the DesiredColumnWidth if the HorizontalAlignment is set to Stretch.
        /// </summary>
        public double DesiredColumnWidth
        {
            get { return _desiredColumnWidth; }
            set { SetValue(DesiredColumnWidthProperty, value); }
        }

        /// <summary>
        /// The dimensions of the space between the edge and its child as a Thickness value. 
        /// Thickness is a structure that stores dimension values using pixel measures.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        #region Dependency Properties
        /// <summary>
        /// Represents the HorizontalAlignment property.
        /// </summary>
        public static DependencyProperty HorizontalAlignmentProperty { get; } 
            = DependencyProperty.Register("HorizontalAlignment", typeof(HorizontalAlignment), typeof(StaggeredLayout), new PropertyMetadata(default(HorizontalAlignment), OnLayoutPropertyChanged));

        /// <summary>
        /// Represents the RowSpacing property.
        /// </summary>
        public static DependencyProperty RowSpacingProperty { get; } 
            = DependencyProperty.Register("RowSpacing", typeof(double), typeof(StaggeredLayout), new PropertyMetadata(0.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Represents the ColumnSpacing property.
        /// </summary>
        public static DependencyProperty ColumnSpacingProperty { get; } 
            = DependencyProperty.Register("ColumnSpacing", typeof(double), typeof(StaggeredLayout), new PropertyMetadata(0.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Represents the DesireColumnWidth property.
        /// </summary>
        public static DependencyProperty DesiredColumnWidthProperty { get; } 
            = DependencyProperty.Register("DesiredColumnWidth", typeof(double), typeof(StaggeredLayout), new PropertyMetadata(250.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Represents the Padding property.
        /// </summary>
        public static DependencyProperty PaddingProperty { get; } 
            = DependencyProperty.Register("Padding", typeof(Thickness), typeof(StaggeredLayout), new PropertyMetadata(default(Thickness), OnLayoutPropertyChanged));
        #endregion

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            if (context.ItemCount == 0)
            {
                return new Size(availableSize.Width, 0.0);
            }

            GetMetrics(availableSize, out int columns, out double colWidth);
            Columns = columns;
            ColumnWidth = colWidth;

            var state = (StaggeredLayoutState)context.LayoutState;
            var reason = state.UpdateViewport(context.RealizationRect);

            if (reason == ViewportChangeReason.HorizontalResize || InvalidateMeasureCache)
            {
                state.ElementCache.InvalidateAllMeasure();
                state.PositionCache.RemoveFrom(0);
            }
            else if (InvalidatePositionCache)
            {
                state.PositionCache.RemoveFrom(0);
            }

            InvalidateMeasureCache = false;
            InvalidatePositionCache = false;

            for (int i = 0; i < context.ItemCount; i++)
            {
                double x = 0.0;
                if (i < state.PositionCache.Count)
                {
                    // Position cached.
                    x = state.PositionCache[i].Top;
                }
                else
                {
                    // Compute + Cache.
                    var element = state.ElementCache.GetOrCreateElementAt(i);
                    if (!element.Measured)
                    {
                        element.Element.Measure(new Size(ColumnWidth, availableSize.Height));
                        element.Measured = true;
                    }
                    int col;
                    double top = 0.0;
                    if (state.PositionCache.Count < Columns)
                    {
                        col = state.PositionCache.Count;
                        top = Padding.Top;
                    }
                    else
                    {
                        col = state.PositionCache.GetNextTargetColumn(out top);
                        top += RowSpacing;
                    }
                    state.PositionCache.Add(new StaggeredLayoutPosition(col, top, element.Element.DesiredSize.Height));
                    x = top;
                }

                if (x < context.RealizationRect.Top)
                {
                    state.ElementCache.RecycleElementAt(i);
                }
                else if (x > context.RealizationRect.Bottom)
                {
                    state.ElementCache.RemoveElementsFrom(i);
                    break;
                }
            }

            var estimatedHeight = state.PositionCache.AverageHeight * context.ItemCount;
            return new Size(availableSize.Width, estimatedHeight);
        }
        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            double horizontalOffset = Padding.Left;
            double totalWidth = ColumnWidth + ((Columns - 1) * (ColumnWidth + _columnSpacing));
            if (HorizontalAlignment == HorizontalAlignment.Right)
            {
                horizontalOffset += finalSize.Width - Padding.Left - Padding.Right - totalWidth;
            }
            else if (HorizontalAlignment == HorizontalAlignment.Center)
            {
                horizontalOffset += (finalSize.Width - Padding.Left - Padding.Right - totalWidth) / 2;
            }

            var state = (StaggeredLayoutState)context.LayoutState;
            state.PositionCache.GetRealizationBound(context.RealizationRect, out int start, out int end);

            for (int i = start; i < end; i++)
            {
                var element = state.ElementCache.GetOrCreateElementAt(i);
                var position = state.PositionCache[i];
                var x = horizontalOffset + (ColumnWidth + ColumnSpacing) * position.Column;
                var bound = new Rect(x, position.Top, ColumnWidth, position.Height);
                element.Element.Arrange(bound);
            }

            return base.ArrangeOverride(context, finalSize);
        }
        protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
        {
            var state = (StaggeredLayoutState)context.LayoutState;
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    state.PositionCache.RemoveFrom(0);
                    state.ElementCache.RemoveElementsFrom(0);
                    break;
                case NotifyCollectionChangedAction.Add:
                    state.PositionCache.RemoveFrom(args.NewStartingIndex);
                    state.ElementCache.ReserveSpaceAt(args.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    state.PositionCache.RemoveFrom(args.OldStartingIndex);
                    state.ElementCache.RemoveElementAt(args.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    state.PositionCache.RemoveFrom(args.OldStartingIndex);
                    state.ElementCache.InvalidateMeasureAt(args.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move:
                    int start = Math.Min(args.OldStartingIndex, args.NewStartingIndex);
                    state.PositionCache.RemoveFrom(start);
                    state.ElementCache.MoveElement(args.OldStartingIndex, args.NewStartingIndex);
                    break;
            }
            InvalidateMeasure();
        }

        protected override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            context.LayoutState = new StaggeredLayoutState(context);
        }
        protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            context.LayoutState = null;
        }

        private void GetMetrics(Size availableSize, out int columnsCount, out double columnWidth)
        {
            // Remove padding.
            double availableWidth = availableSize.Width - Padding.Left - Padding.Right;

            // Column width cannot be wider than the available width.
            columnWidth = Math.Min(DesiredColumnWidth, availableWidth);
            // There should be at least 1 column.
            columnsCount = Math.Max(1, (int)Math.Floor(availableWidth / columnWidth));

            // If horizontal alignment is stretch,
            // Then the entire width is divided.
            if (HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                var contentWidth = availableWidth - ((columnsCount - 1) * ColumnSpacing);
                columnWidth = contentWidth / columnsCount;
                return;
            }

            // If not, we first need to see if space is enough
            // for columns and spacing.
            double totalWidth = columnWidth + ((columnsCount - 1) * (columnWidth + ColumnSpacing));
            // If not, remove one column.
            // Note that if there is only one column, this will never be executed.
            if (totalWidth > availableWidth)
            {
                columnsCount--;
            }
            // Of course, the layout cannot fill "infinitively".
            // In this case, we take what we need.
            else if (double.IsInfinity(availableWidth))
            {
                availableWidth = totalWidth;
            }
        }
        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var layout = (StaggeredLayout)d;

            // For col spacing and padding and desired width change, we need to invalidate measure cache.
            // for row others, we need to invalidate position cache.

            if (e.Property == RowSpacingProperty)
            {
                layout._rowSpacing = (double)e.NewValue;
                layout.InvalidatePositionCache = true;
            }
            else if (e.Property == ColumnSpacingProperty)
            {
                layout._columnSpacing = (double)e.NewValue;
                layout.InvalidateMeasureCache = true;
                layout.InvalidatePositionCache = true;
            }
            else if (e.Property == DesiredColumnWidthProperty)
            {
                layout._desiredColumnWidth = (double)e.NewValue;
                layout.InvalidateMeasureCache = true;
                layout.InvalidatePositionCache = true;
            }
            else if (e.Property == PaddingProperty)
            {
                layout.InvalidateMeasureCache = true;
                layout.InvalidatePositionCache = true;
            }
            else if (e.Property == HorizontalAlignmentProperty)
            {
                layout.InvalidatePositionCache = true;
            }

            layout.InvalidateMeasure();
        }

        private int Columns { get; set; }
        private double ColumnWidth { get; set; }
        private bool InvalidateMeasureCache { get; set; }
        private bool InvalidatePositionCache { get; set; }

        private double _rowSpacing;
        private double _columnSpacing;
        private double _desiredColumnWidth = 250.0;
    }

    internal class StaggeredLayoutState
    {
        public StaggeredLayoutState(VirtualizingLayoutContext context) => ElementCache = new ElementCache(context);

        public Rect LastViewport { get; private set; }
        public ElementCache ElementCache { get; }
        public PositionCache PositionCache { get; } = new PositionCache();

        public ViewportChangeReason UpdateViewport(Rect viewport)
        {
            ViewportChangeReason reason;
            if (LastViewport.Width != viewport.Width)
            {
                reason = ViewportChangeReason.HorizontalResize;
            }
            else if (LastViewport.Height != viewport.Height)
            {
                reason = ViewportChangeReason.VerticalResize;
            }
            else
            {
                reason = ViewportChangeReason.Scroll;
            }
            LastViewport = viewport;
            return reason;
        }
    }

    internal class ElementCache
    {
        public ElementCache(VirtualizingLayoutContext context) => this.context = context;

        public bool ContainsCacheFor(int index)
        {
            return index < elementCache.Count && elementCache[index] != null;
        }
        public void ReserveSpaceAt(int index)
        {
            if (index < elementCache.Count)
            {
                elementCache.Insert(index, null);
            }
        }
        public ElementCacheItem GetOrCreateElementAt(int index)
        {
            if (ContainsCacheFor(index))
            {
                return elementCache[index];
            }
            else
            {
                EnsureCount(index + 1);
                var element = context.GetOrCreateElementAt(index, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
                elementCache[index] = new ElementCacheItem() { Element = element };
                return elementCache[index];
            }
        }
        public void InvalidateAllMeasure()
        {
            for (int i = 0; i < elementCache.Count; i++)
            {
                InvalidateMeasureAt(i);
            }
        }
        public void InvalidateMeasureAt(int index)
        {
            if (ContainsCacheFor(index))
            {
                elementCache[index].Measured = false;
            }
        }
        public void RemoveElementAt(int index)
        {
            RecycleElementAt(index);
            if (index < elementCache.Count)
            {
                elementCache.RemoveAt(index);
            }
        }
        public void RecycleElementAt(int index)
        {
            if (ContainsCacheFor(index))
            {
                context.RecycleElement(elementCache[index].Element);
                elementCache[index] = null;
            }
            TrimEnd();
        }
        public void RemoveElementsFrom(int start)
        {
            while (start < elementCache.Count)
            {
                RemoveElementAt(start);
            }
        }
        public void MoveElement(int oldIndex, int newIndex)
        {
            if (oldIndex < elementCache.Count)
            {
                var element = elementCache[oldIndex];
                elementCache.RemoveAt(oldIndex);
                EnsureCount(newIndex + 1);
                ReserveSpaceAt(newIndex);
                elementCache[newIndex] = element;
            }
        }

        private void EnsureCount(int count)
        {
            while (elementCache.Count < count)
            {
                elementCache.Add(null);
            }
        }
        private void TrimEnd()
        {
            while (elementCache.Count > 0 && elementCache[elementCache.Count - 1] == null)
            {
                elementCache.RemoveAt(elementCache.Count - 1);
            }
        }

        private List<ElementCacheItem> elementCache = new List<ElementCacheItem>();
        private VirtualizingLayoutContext context;
    }

    internal class PositionCache
    {
        public int Count => positionCache.Count;
        public double CachedHeight { get; private set; }
        public double AverageHeight => CachedHeight / Count;
        public StaggeredLayoutPosition this[int index] => positionCache[index];

        public void RemoveFrom(int start)
        {
            if (start < positionCache.Count)
            {
                positionCache.RemoveRange(start, positionCache.Count - start);
            }

            if (positionCache.Count == 0)
            {
                CachedHeight = 0.0;
            }
            else
            {
                CachedHeight = positionCache.Max(x => x.Bottom);
            }
        }
        public void Add(StaggeredLayoutPosition position)
        {
            positionCache.Add(position);
            if (position.Bottom > CachedHeight)
            {
                CachedHeight = position.Bottom;
            }
        }
        public void GetRealizationBound(Rect viewport, out int start, out int end)
        {
            if (positionCache.Count == 0)
            {
                start = 0;
                end = 0;
                return;
            }

            var first = positionCache.First(x => x.Top > viewport.Top);
            start = positionCache.IndexOf(first);
            if (positionCache.Exists(x => x.Top > viewport.Bottom))
            {
                var last = positionCache.First(x => x.Top > viewport.Bottom);
                end = positionCache.IndexOf(last);
            }
            else
            {
                end = positionCache.Count;
            }
        }
        public int GetNextTargetColumn(out double bottom)
        {
            var groups = positionCache.GroupBy(x => x.Column).ToArray();

            double minY = double.MaxValue;
            int col = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                double bot = groups.First(x => x.Key == i).Last().Bottom;
                if (bot < minY)
                {
                    minY = bot;
                    col = i;
                }
            }
            bottom = minY;
            return col;
        }

        private List<StaggeredLayoutPosition> positionCache = new List<StaggeredLayoutPosition>();
    }

    internal struct StaggeredLayoutPosition
    {
        public StaggeredLayoutPosition(int column, double top, double height)
        {
            Column = column;
            Top = top;
            Height = height;
        }

        public int Column { get; set; }
        public double Top { get; set; }
        public double Bottom => Top + Height;
        public double Height { get; set; }
    }

    internal class ElementCacheItem
    {
        public UIElement Element { get; set; }
        public bool Measured { get; set; }
    }

    internal enum ViewportChangeReason
    {
        Scroll, VerticalResize, HorizontalResize
    }
}
