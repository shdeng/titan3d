﻿using EngineNS.UI.Canvas;
using NPOI.SS.Formula.PTG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EngineNS.UI.Controls.Containers
{
    public class TtUIElementCollection : IList<TtUIElement>
    {
        internal List<TtUIElement> mChildren = new List<TtUIElement>();

        TtContainer mVisualParent;
        public TtContainer VisualParent => mVisualParent;
        TtContainer mLogicalParent;
        public TtContainer LogicalParent => mLogicalParent;
        public TtUIElementCollection(TtContainer visualParent, TtContainer logicalParent)
        {
            if (visualParent == null)
                throw new ArgumentNullException("visualParent can not be null");
            mVisualParent = visualParent;
            mLogicalParent = logicalParent;
        }

        public int Count
        {
            get
            {
                if (mVisualParent.TemplateInternal != null)
                {
                    if (mLogicalParent.HasTemplateGeneratedSubTree)
                    {
                        if (mLogicalParent.mLogicContentsPresenter != null)
                            return mLogicalParent.mLogicContentsPresenter.Children.Count;
                        else
                            return 0;
                    }
                    else
                        return mChildrenHolder.Count;
                }
                else
                    return mChildren.Count;
            }
        }

        public bool IsReadOnly => false;

        // template parent default get logical tree item, other get it's child
        public TtUIElement this[int index] 
        {
            get
            {
                //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
                //{
                //    throw new InvalidOperationException("need be called in logic thread");
                //}

                if (mVisualParent.TemplateInternal != null)
                {
                    if (mLogicalParent.HasTemplateGeneratedSubTree)
                    {
                        if (mLogicalParent.mLogicContentsPresenter != null)
                            return mLogicalParent.mLogicContentsPresenter.Children[index];
                        else
                            return null;
                    }
                    else
                        return mChildrenHolder[index];
                }
                else
                    return mChildren[index];
            }
            set
            {
                //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
                //{
                //    throw new InvalidOperationException("need be called in logic thread");
                //}

                if(mVisualParent.TemplateInternal != null)
                {
                    if(mLogicalParent.HasTemplateGeneratedSubTree)
                    {
                        if (mLogicalParent.mLogicContentsPresenter != null)
                        {
                            var children = mLogicalParent.mLogicContentsPresenter.Children;
                            children[index] = value;
                        }
                    }
                    else
                        mChildrenHolder[index] = value;
                }
                else
                {
                    if(mChildren[index] != value)
                    {
                        var c = mChildren[index];
                        ClearParent(c);
                    }
                    mChildren[index] = value;
                    SetParent(value);
                    mVisualParent.InvalidateMeasure();
                    if (value != null && value is TtContentsPresenter)
                    {
                        mVisualParent.ChildIsContentsPresenter = true;
                    }
                }
            }
        }
        //public TtUIElement GetVisualChild(int index)
        //{
        //    if (mVisualParent.ChildIsContentsPresenter)
        //        return mLogicalParent.mLogicContentsPresenter.Children[index];
        //    return mChildren[index];
        //}
        protected void ClearParent(TtUIElement element)
        {
            if (mLogicalParent != null)
            {
                if (mLogicalParent.IsLogicalChildrenIterationInProgress)
                    throw new InvalidOperationException("Can not modify logical children during three walk");
                mLogicalParent.HasLogicalChildren = (mLogicalParent.mLogicContentsPresenter == null) ? false : mLogicalParent.mLogicContentsPresenter.Children.Count > 0;
            }
            if(element != null)
            {
                element.mParent = null;
                element.RootUIHost = null; 
                element.mVisualParent = null;
            }
        }
        protected void SetParent(TtUIElement element)
        {
            if (mLogicalParent != null)
            {
                if (mLogicalParent.IsLogicalChildrenIterationInProgress)
                    throw new InvalidOperationException("Can not modify logical children during three walk");
                mLogicalParent.HasLogicalChildren = true;
                // todo: fire trigger
            }
            if(element != null)
            {
                element.mParent = mLogicalParent;
                element.RootUIHost = mVisualParent.RootUIHost;
                element.mVisualParent = mVisualParent;
            }
        }

        bool HasContentsPresenter()
        {
            for(int i=0; i<mChildren.Count; i++)
            {
                if (mChildren[i] is TtContentsPresenter)
                    return true;
            }
            return false;
        }

        public int IndexOf(TtUIElement item)
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}

            if (mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        return children.IndexOf(item);
                    }
                    else
                        return -1;
                }
                else
                    return mChildrenHolder.IndexOf(item);
            }
            else
                return mChildren.IndexOf(item);
        }

        public void Insert(int index, TtUIElement item)
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            if (mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        children.Insert(index, item);
                    }
                }
                else
                    mChildrenHolder.Insert(index, item);
            }
            else
            {
                if (item is TtContentsPresenter)
                {
                    mVisualParent.ChildIsContentsPresenter = true;
                }
                SetParent(item);
                mChildren.Insert(index, item);
                mVisualParent.InvalidateMeasure();
            }
        }

        public void RemoveAt(int index)
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            if(mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        children.RemoveAt(index);
                    }
                }
                else
                    mChildrenHolder.RemoveAt(index);
            }
            else
            {
                var e = mChildren[index];
                mChildren.RemoveAt(index);
                ClearParent(e);
                mVisualParent.InvalidateMeasure();
                mVisualParent.ChildIsContentsPresenter = HasContentsPresenter();
            }
        }
        public bool Remove(TtUIElement item)
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            bool returnValue = false;
            if (mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        returnValue = children.Remove(item);
                    }
                    else
                        returnValue = false;
                }
                else
                    return mChildrenHolder.Remove(item);
            }
            else
            {
                if (mChildren.Remove(item))
                {
                    ClearParent(item);
                    mVisualParent.InvalidateMeasure();
                    mVisualParent.ChildIsContentsPresenter = HasContentsPresenter();
                    returnValue = true;
                }
            }
            return returnValue;
        }

        public void Add(TtUIElement item)
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            if (item == null)
                throw new ArgumentNullException("item is null");
            if(mVisualParent.TemplateInternal != null)
            {
                if(mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        children.Add(item);
                    }
                }
                else
                {
                    mChildrenHolder.Add(item);
                }
            }
            else
            {
                if(item is TtContentsPresenter)
                {
                    mVisualParent.ChildIsContentsPresenter = true;
                    mVisualParent.mVisualParent.InvalidateMeasure();
                }
                else
                    mVisualParent.InvalidateMeasure();

                SetParent(item);
                mChildren.Add(item);
            }
        }

        public void Clear()
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            // todo: mChildrenHolder process
            if (mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        children.Clear();
                    }
                }
                else
                    mChildrenHolder.Clear();
            }
            else
            {
                var count = mChildren.Count;
                if(count > 0)
                {
                    for(int i = mChildren.Count - 1; i>=0; i--)
                    {
                        if (mChildren[i] != null)
                        {
                            ClearParent(mChildren[i]);
                            mChildren[i].Cleanup();
                        }
                    }
                    mChildren.Clear();
                    mVisualParent.InvalidateMeasure();
                }
            }

            mVisualParent.ChildIsContentsPresenter = false;
        }

        public bool Contains(TtUIElement item)
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            if(mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        return children.Contains(item);
                    }
                    else
                        return false;
                }
                else
                    return mChildrenHolder.Contains(item);
            }
            else
                return mChildren.Contains(item);
        }

        public void CopyTo(TtUIElement[] array, int arrayIndex)
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            if(mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        children.CopyTo(array, arrayIndex);
                    }
                }
                else
                    mChildrenHolder.CopyTo(array, arrayIndex);
            }
            else
                mChildren.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TtUIElement> GetEnumerator()
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            if(mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        return children.GetEnumerator();
                    }
                    else
                        return null;
                }
                else
                    return mChildrenHolder.GetEnumerator();
            }
            else
                return mChildren.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            //if (!UEngine.Instance.EventPoster.IsThread(Thread.Async.EAsyncTarget.Logic))
            //{
            //    throw new InvalidOperationException("need be called in logic thread");
            //}
            if(mVisualParent.TemplateInternal != null)
            {
                if (mLogicalParent.HasTemplateGeneratedSubTree)
                {
                    if (mLogicalParent.mLogicContentsPresenter != null)
                    {
                        var children = mLogicalParent.mLogicContentsPresenter.Children;
                        return children.GetEnumerator();
                    }
                    else
                        return null;
                }
                else
                    return mChildrenHolder.GetEnumerator();
            }
            else
                return mChildren.GetEnumerator();
        }

        List<TtUIElement> mChildrenHolder = new List<TtUIElement>();
        public void OnApplyTemplate()
        {
            if (!mLogicalParent.HasTemplateGeneratedSubTree)
                throw new InvalidOperationException("OnApplyTemplate need generated subtree");
            if (mLogicalParent.mLogicContentsPresenter != null)
            {
                var children = mLogicalParent.mLogicContentsPresenter.Children;
                for (int i = 0; i < mChildrenHolder.Count; i++)
                {
                    children.Add(mChildrenHolder[i]);
                }
            }
            mChildrenHolder.Clear();
        }
    }

    public abstract partial class TtContainer : TtUIElement
    {
        TtUIElementCollection mChildren;
        [Browsable(false)]
        public TtUIElementCollection Children => mChildren;
        [Browsable(false)]
        public bool ChildIsContentsPresenter
        {
            get => ReadInternalFlag(eInternalFlags.ChildIsContentsPresenter);
            set => WriteInternalFlag(eInternalFlags.ChildIsContentsPresenter, value);
        }

        public override void Cleanup()
        {
            mChildren.Clear();
        }

        protected TtBrush mBrush;
        [Rtti.Meta, Bind.BindProperty]
        public TtBrush Brush
        {
            get => mBrush;
            set
            {
                OnValueChange(value, mBrush);
                mBrush = value;
            }
        }
        protected Thickness mBorderThickness = Thickness.Empty;
        [Rtti.Meta, Bind.BindProperty]
        public Thickness BorderThickness
        {
            get => mBorderThickness;
            set
            {
                OnValueChange(value, mBorderThickness);
                mBorderThickness = value;
            }
        }
        protected Thickness mPadding = Thickness.Empty;
        [Rtti.Meta, Bind.BindProperty]
        public Thickness Padding
        {
            get => mPadding;
            set
            {
                OnValueChange(value, mPadding);
                mPadding = value;
            }
        }
        protected TtBrush mBorderBrush;
        [Rtti.Meta, Bind.BindProperty]
        public TtBrush BorderBrush
        {
            get => mBorderBrush;
            set
            {
                OnValueChange(value, mBorderBrush);
                mBorderBrush = value;
            }
        }
        protected TtBrush mBackground;
        [Rtti.Meta, Bind.BindProperty]
        public TtBrush Background
        {
            get => mBackground;
            set
            {
                OnValueChange(value, mBackground);
                mBackground = value;
            }
        }
        public TtContainer()
        {
            mChildren = new TtUIElementCollection(this, this);
            mBorderBrush = new TtBrush();
            mBorderBrush.HostElement = this;
            mBorderBrush.BrushType = TtBrush.EBrushType.Border;
            mBackground = new TtBrush();
            mBackground.HostElement = this;
        }
        public TtContainer(TtContainer parent)
            : base(parent)
        {
            mChildren = new TtUIElementCollection(this, parent);
            mBorderBrush = new TtBrush();
            mBorderBrush.HostElement = this;
            mBorderBrush.BrushType = TtBrush.EBrushType.Border;
            mBackground = new TtBrush();
            mBackground.HostElement = this;
        }
        // pt位置相对于linecheck到的element
        public override TtUIElement GetPointAtElement(in Vector2 pt, bool onlyClipped = true)
        {
            // todo: inv transform
            if (onlyClipped)
            {
                if (!DesignRect.Contains(in pt))
                    return null;
                for (int i = mChildren.Count - 1; i >= 0; i--)
                {
                    var child = mChildren[i];
                    if (child.Is3D)
                        continue;
                    if (!child.DesignRect.Contains(in pt))
                        continue;

                    var container = child as TtContainer;
                    if (container != null)
                    {
                        var retVal = container.GetPointAtElement(in pt, onlyClipped);
                        if (retVal != null)
                            return retVal;
                    }
                    else
                        return child;
                }
                return this;
            }
            else
            {
                for (int i = mChildren.Count - 1; i >= 0; i--)
                {
                    var child = mChildren[i];
                    if (child.Is3D)
                        continue;
                    var container = child as TtContainer;
                    if (container != null)
                    {
                        var retVal = container.GetPointAtElement(in pt, onlyClipped);
                        if (retVal != null)
                            return retVal;
                    }
                    else if(child.DesignRect.Contains(in pt))
                        return child;
                }
                if (DesignRect.Contains(in pt))
                    return this;
                return null;
            }
        }
        public virtual bool NeedUpdateLayoutWhenChildDesiredSizeChanged(TtUIElement child)
        {
            return false;
        }
        public virtual void OnChildDesiredSizeChanged(TtUIElement child)
        {
            if (IsMeasureValid)
            {
                if (NeedUpdateLayoutWhenChildDesiredSizeChanged(child))
                    InvalidateMeasure();
            }
        }

        public override void Draw(TtCanvas canvas, TtCanvasDrawBatch batch)
        {
            var count = VisualTreeHelper.GetChildrenCount(this);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(this, i);
                child.Draw(canvas, batch);
            }
        }

        protected override void OnApplyTemplate()
        {
            if (!HasTemplateGeneratedSubTree)
                throw new InvalidOperationException("Template need generated sub tree");
            mChildren.OnApplyTemplate();
        }

        internal bool IsLogicalChildrenIterationInProgress
        {
            get => ReadInternalFlag(eInternalFlags.IsLogicalChildrenIterationInProgress);
            set => WriteInternalFlag(eInternalFlags.IsLogicalChildrenIterationInProgress, value);
        }
        protected internal TtContentsPresenter mLogicContentsPresenter;

        protected override SizeF MeasureOverride(in SizeF availableSize)
        {
            var count = VisualTreeHelper.GetChildrenCount(this);
            for(int i=0; i<count; i++)
            {
                var childUI = VisualTreeHelper.GetChild(this, i);
                childUI.Measure(in availableSize);
            }
            return availableSize;
        }
        protected override void ArrangeOverride(in RectangleF arrangeSize)
        {
            var count = VisualTreeHelper.GetChildrenCount(this);
            for(int i=0; i<count; i++)
            {
                var childUI = VisualTreeHelper.GetChild(this, i);
                childUI.Arrange(in arrangeSize);
            }
        }

        public override bool QueryElements<T>(Delegate_QueryProcess<T> queryAction, ref T queryData)
        {
            for(int i=0; i<mChildren.Count; i++)
            {
                if (mChildren[i].QueryElements(queryAction, ref queryData))
                    return true;
            }

            return base.QueryElements(queryAction, ref queryData);
        }

        public override bool IsReadyToDraw()
        {
            var count = VisualTreeHelper.GetChildrenCount(this);
            for(int i=0; i<count; i++)
            {
                var child = VisualTreeHelper.GetChild(this, i);
                if (!child.IsReadyToDraw())
                    return false;
            }
            return true;
        }


        protected override void OnRenderTransformDirtyChanged(bool isDirty)
        {
            base.OnRenderTransformDirtyChanged(isDirty);
            if(isDirty)
            {
                var count = VisualTreeHelper.GetChildrenCount(this);
                for(int i=0; i<count; i++)
                {
                    var child = VisualTreeHelper.GetChild(this, i);
                    child.RenderTransformDirty = true;
                }
            }
        }
        public override UInt16 UpdateTransformIndex(ushort parentTransformIdx)
        {
            var idx = base.UpdateTransformIndex(parentTransformIdx);
            var count = VisualTreeHelper.GetChildrenCount(this);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(this, i);
                child?.UpdateTransformIndex(idx);
            }
            return idx;
        }
    }
}
