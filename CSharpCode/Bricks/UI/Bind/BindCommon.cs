﻿using EngineNS.EGui.Controls.PropertyGrid;
using EngineNS.Rtti;
using EngineNS.UI.Trigger;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;

namespace EngineNS.UI.Bind
{
    // test only /////////////////////////////////////////////
    public partial class BindA : TtBindableObject
    {
        Guid mId = Guid.NewGuid(); 
        public bool IsSource = false;
        int mBindValueA = 1;
        [BindProperty(IsAutoGen = false)]
        public int BindValueA
        {
            get => mBindValueA;
            set
            {
                OnValueChange(value, mBindValueA);  
                mBindValueA = value;
            }
        }
        static TtBindableProperty BindValueAProperty = UEngine.Instance.UIBindManager.Register<int, BindA>("BindValueA", -1);

        [BindProperty(IsAutoGen = false, IsCallSetProperty = false)]
        public int BindValueA2
        {
            get => GetValue<int>();
            set => SetValue(value);
        }
        static TtBindableProperty BindValueA2Property = UEngine.Instance.UIBindManager.Register<int, BindA>("BindValueA2", -6, 
            (bObj, bp, val)=>
            {
                
            });

        public static TtBindableProperty AttachedProperty_Int = UEngine.Instance.UIBindManager.RegisterAttached<int, BindA>("AttPro_Int",0);
        public static int GetAttPro_Int(IBindableObject target)
        {
            return target.GetValue<int>(AttachedProperty_Int);
        }
        public static void SetAttPro_Int(IBindableObject target, int value)
        {
            target.SetValue<int>(value, AttachedProperty_Int);
        }
    }
    public partial class BindB //: TtBindableObject
    {
        Guid mId = Guid.NewGuid();
        public bool IsSource = false;
        int mBindValueB = 0;
        [BindProperty]
        [Category("CategoryTest")]
        public int BindValueB
        {
            get => mBindValueB;
            set
            {
                OnValueChange(value, mBindValueB);
                mBindValueB = value;
            }
        }

        public BindC InnerObject = new BindC();
    }
    public partial class BindC
    {
        int mBindValueC = 5;
        [BindProperty(DefaultValue = 6)]
        public int BindValueC
        {
            get => mBindValueC;
            set
            {
                OnValueChange(value, mBindValueC);
                mBindValueC = value;
            }
        }

        double mBindValueC2 = 0.1;
        [BindProperty]
        public double BindValueC2
        {
            get => mBindValueC2;
            set
            {
                OnValueChange(value, mBindValueC2);
                mBindValueC2 = value;
            }
        }
    }
    class Int2DoubleConvert : TtBindTypeConvertBase
    {

        public override bool CanConvertTo<TTag, TSrc>()
        {
            if ((typeof(TTag) == typeof(double)) && (typeof(TSrc) == typeof(int)))
                return true;
            return false;
        }
        public override bool CanConvertFrom<TTag, TSrc>()
        {
            if ((typeof(TTag) == typeof(double)) && (typeof(TSrc) == typeof(int)))
                return true;
            return false;
        }

        public override TTag ConvertTo<TTag, TSrc>(TtBindingExpressionBase bindingExp, TSrc value)
        {
            dynamic valueInt = value;
            dynamic valueDouble = (double)valueInt;
            return valueDouble;
        }
        public double ConvertTo<TTag, TSrc>(TtBindingExpressionBase bindingExp, int value)
        {
            return (double)value;
        }
        public override TSrc ConvertFrom<TTag, TSrc>(TtBindingExpressionBase bindingExp, TTag value)
        {
            dynamic valueDouble = value;
            dynamic valueInt = (int)valueDouble;
            return valueInt;
        }
        public int ConvertFrom(TtBindingExpressionBase bindingExp, double value)
        {
            return (int)value;
        }
    }
    public static class BindTestClass
    {
        public static void BindTest()
        {
            var bindA1 = new BindA();
            var bindA2 = new BindA();
            //bindA2.IsSource = true;
            //TtBindingOperations.SetBinding<int, int>(bindA1, "BindValueA", bindA2, "BindValueA", EBindingMode.OneWay);
            TtBindingOperations.SetBinding<int, int>(bindA1, "BindValueA2", bindA2, "BindValueA", EBindingMode.OneWay);
            bindA2.BindValueA = 10;

            var bindB1 = new BindB();
            var bindB2 = new BindB();
            var bindC1 = new BindC();
            bindB2.IsSource = true;
            TtBindingOperations.SetBinding<int, int>(bindA1, "BindValueA", bindB2, "BindValueB", EBindingMode.TwoWay);
            //TtBindingOperations.SetBinding<BindC, int, BindB, int>(bindC1, "BindValueC", bindB2, "InnerObject.BindValueC", EBindingMode.TwoWay);
            //bindB2.InnerObject.BindValueC = 100;
            //bindC1.BindValueC = 666;
            bindB2.BindValueB = 11;
            bindA1.BindValueA = 22;

            BindA.SetAttPro_Int(bindB1, 6);
            var attVal = BindA.GetAttPro_Int(bindB1);
            //TtBindingOperations.SetBinding<int, int>(bindB1, "BindValueB", bindB2, "BindValueB", EBindingMode.OneWay);
            //TtBindingOperations.SetBinding<int, int>(bindC1, "BindValueC", bindB2, "BindValueB", EBindingMode.OneWay);
            //var binding = new TtBinding();
            //binding.Convert = new Int2DoubleConvert();
            //binding.Mode = EBindingMode.TwoWay;
            //TtBindingOperations.SetBinding<double, int>(bindC1, "BindValueC2", bindB2, "BindValueB", binding);
            //bindB2.BindValueB = 20;
            //bindC1.BindValueC2 = 30;
        }
    }
    // generated code ////////////////////////////////////////
    //public partial class BindB
    //{
    //    static TtBindableProperty BindValueBProperty = UEngine.Instance.UIBindManager.Register<int, BindB>("BindValueB", -10);
    //    public TtBindingBase CreateBinding(string propertyName)
    //    {
    //        switch(propertyName)
    //        {
    //            case "BindValueB":
    //                return new BindB_BindingImp_BindValueB();
    //        }
    //        return null;
    //    }
    //}
    //public class BindB_BindingImp_BindValueB : TtBinding<int, BindB>
    //{
    //    public override TtBindingExpressionBase CreateBindingExpression(IBindableObject targetObject, TtBindableProperty targetProperty)
    //    {
    //        var retVal = new BindB_BindingExprImp_BindValueB(this, null);
    //        retVal.TargetObject = targetObject;
    //        retVal.TargetProperty = targetProperty;
    //        return retVal;
    //    }
    //}
    //public class BindB_BindingExprImp_BindValueB : TtBindingExpression<int, BindB>
    //{

    //    public BindB_BindingExprImp_BindValueB(BindB_BindingImp_BindValueB binding, TtBindingExpressionBase parent)
    //        : base(binding, parent)
    //    {

    //    }
    //    public override void UpdateSource()
    //    {
    //        if(mParentExp != null)
    //        {
    //            mParentExp.UpdateSource();
    //        }
    //        else
    //        {
    //            dynamic obj = TargetObject;
    //            obj.BindValueB = mValueStore;
    //        }
    //    }
    //}

    //public partial class BindC : IBindableObject
    //{
    //    Dictionary<string, List<TtBindingExpressionBase>> mBindExprDic = new Dictionary<string, List<TtBindingExpressionBase>>();

    //    public void OnValueChange<T>(T value, [CallerMemberName] string? propertyName = null)
    //    {

    //    }
    //}
    //////////////////////////////////////////////////////////

    // 用于自动代码生成
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class BindPropertyAttribute : Attribute
    {
        public EBindingMode DefaultMode = EBindingMode.OneWay;
        public EUpdateSourceTrigger UpdateSourceTrigger = EUpdateSourceTrigger.PropertyChanged;
        public bool IsAutoGen = true;
        public bool IsCallSetProperty = true;
        public object DefaultValue;
        //public TtBindPropertyAttribute

        public BindPropertyAttribute()
        {

        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AttachedPropertyAttribute : Attribute
    {
        public string Name;
        public string Category;
        public object DefaultValue;
    }
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class BindPropertyDisplayNameAttribute : Attribute
    {
        public virtual string GetDisplayName(IBindableObject element)
        {
            return "";
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BindableObjectAttribute : Attribute
    {

    }
    public class TtBindableProperty
    {
        public string Name;
        public string Category;
        public UTypeDesc PropertyType;
        public UTypeDesc HostType;
        public PGCustomValueEditorAttribute CustomValueEditor;
        public BindPropertyDisplayNameAttribute DisplayNameAtt;

        [Flags]
        internal enum EFlags : UInt64
        {
            GlobalIndexMask = 0xFFFF,
            UpdateDefault = 1 << 16,
            UpdateOnPropertyChanged = 1 << 17,
            UpdateOnLostFocus = 1 << 18,
            UpdateExplicitly = 1 << 19,
            UpdateMask = UpdateDefault | UpdateOnPropertyChanged | UpdateOnLostFocus | UpdateExplicitly,

            ModeDefault = 1 << 20,
            ModeOneTime = 1 << 21,
            ModeOneWay = 1 << 22,
            ModeOneWayToSource = 1 << 23,
            ModeTwoWay = ModeOneWay | ModeOneWayToSource,
            ModeMask = ModeDefault | ModeOneTime | ModeTwoWay,

            IsReadonly          = 1 << 24,
            AutoGen             = 1 << 25,
            CallSetProperty     = 1 << 26,
            AttachedProperty  = 1 << 27,
        }
        EFlags mFlags;
        public UInt16 GlobalIndex
        {
            get => (UInt16)(mFlags & EFlags.GlobalIndexMask);
        }
        public bool IsReadonly
        {
            get => (mFlags & EFlags.IsReadonly) != 0;
            set
            {
                if (value)
                    mFlags |= EFlags.IsReadonly;
                else
                    mFlags &= ~EFlags.IsReadonly;
            }
        }
        public bool IsAutoGen
        {
            get => (mFlags & EFlags.AutoGen) != 0;
            internal set
            {
                if(value)
                    mFlags |= EFlags.AutoGen;
                else
                    mFlags &= ~EFlags.AutoGen;
            }
        }
        public bool IsCallSetProperty
        {
            get => (mFlags & EFlags.CallSetProperty) != 0;
            internal set
            {
                if (value)
                    mFlags |= EFlags.CallSetProperty;
                else
                    mFlags &= ~EFlags.CallSetProperty;
            }
        }
        public bool IsAttachedProperty
        {
            get => (mFlags & EFlags.AttachedProperty) != 0;
            internal set
            {
                if (value)
                    mFlags |= EFlags.AttachedProperty;
                else
                    mFlags &= ~EFlags.AttachedProperty;
            }
        }
        public EUpdateSourceTrigger UpdateSourceTrigger
        {
            get
            {
                switch(mFlags & EFlags.UpdateMask)
                {
                    case EFlags.UpdateDefault: return EUpdateSourceTrigger.Default;
                    case EFlags.UpdateOnPropertyChanged: return EUpdateSourceTrigger.PropertyChanged;
                    case EFlags.UpdateOnLostFocus: return EUpdateSourceTrigger.LostFocus;
                    case EFlags.UpdateExplicitly: return EUpdateSourceTrigger.Explicit;
                }
                return EUpdateSourceTrigger.Default;
            }
            set
            {
                EFlags flag = EFlags.UpdateDefault;
                switch(value)
                {
                    case EUpdateSourceTrigger.Default: 
                        flag = EFlags.UpdateDefault; 
                        break;
                    case EUpdateSourceTrigger.PropertyChanged:
                        flag = EFlags.UpdateOnPropertyChanged;
                        break;
                    case EUpdateSourceTrigger.LostFocus:
                        flag = EFlags.UpdateOnLostFocus;
                        break;
                    case EUpdateSourceTrigger.Explicit:
                        flag = EFlags.UpdateExplicitly;
                        break;
                }
                mFlags = (mFlags & ~EFlags.UpdateMask) | (flag & EFlags.UpdateMask);
            }
        }
        public EBindingMode BindingMode
        {
            get
            {
                switch(mFlags & EFlags.ModeMask)
                {
                    case EFlags.ModeDefault: return EBindingMode.Default;
                    case EFlags.ModeOneTime: return EBindingMode.OneTime;
                    case EFlags.ModeOneWay: return EBindingMode.OneWay;
                    case EFlags.ModeOneWayToSource: return EBindingMode.OneWayToSource;
                    case EFlags.ModeTwoWay: return EBindingMode.TwoWay;
                }
                return EBindingMode.Default;
            }
            set
            {
                EFlags flag = EFlags.ModeDefault;
                switch(value)
                {
                    case EBindingMode.Default:
                        flag = EFlags.ModeDefault;
                        break;
                    case EBindingMode.OneTime:
                        flag = EFlags.ModeOneTime;
                        break;
                    case EBindingMode.OneWay:
                        flag = EFlags.ModeOneWay;
                        break;
                    case EBindingMode.TwoWay:
                        flag = EFlags.ModeTwoWay;
                        break;
                    case EBindingMode.OneWayToSource:
                        flag = EFlags.ModeOneWayToSource;
                        break;
                }
                mFlags = (mFlags & ~EFlags.ModeMask) | (flag & EFlags.ModeMask);
            }
        }
        internal struct NameKey
        {
            private string mName;
            private UTypeDesc mHostType;
            private int mHashCode;

            public NameKey(string name, UTypeDesc hostType)
            {
                mName = name;
                mHostType = hostType;
                mHashCode = mName.GetHashCode() ^ mHostType.GetHashCode();
            }
            public override int GetHashCode()
            {
                return mHashCode;
            }
            public override bool Equals(object obj)
            {
                if ((obj != null) && (obj is NameKey))
                    return Equals((NameKey)obj);
                else
                    return false;
            }
            public bool Equals(NameKey key)
            {
                return (mName.Equals(key.mName)) && (mHostType.Equals(key.mHostType));
            }
        }

        protected TtBindableProperty()
        {
            mFlags = (EFlags)TtBindManager.GetBindablePropertyUniqueGlobalIndex();
        }
        public void CallOnValueChanged<T>(IBindableObject obj, TtBindableProperty property, in T value) 
        {
            var bp = this as TtBindableProperty<T>;
            if (bp != null)
                bp.OnValueChanged(obj, property, value);
        }

        public override int GetHashCode()
        {
            return GlobalIndex;
        }
        public override bool Equals(object obj)
        {
            var pro = obj as TtBindableProperty;
            if (pro == null)
                return false;
            return (this.GlobalIndex == pro.GlobalIndex);
        }
    }
    public class TtBindableProperty<TProperty> : TtBindableProperty
    {
        public TProperty DefaultValue;
        public Action<IBindableObject, TtBindableProperty, TProperty> OnValueChanged;
    }
    public interface IBindableObject
    {
#nullable enable
        public T GetValue<T>([CallerMemberName] string? propertyName = null);
        public T GetValue<T>(TtBindableProperty bp);
        public void SetValue<T>(in T value, [CallerMemberName] string? propertyName = null);
        public void SetValue<T>(in T value, TtBindableProperty bp);
        public void SetBindExpression(TtBindableProperty bp, TtBindingExpressionBase expr);
        public void OnValueChange<T>(in T value, in T oldValue, [CallerMemberName] string? propertyName = null);
        public TtBindingExpressionBase CreateBindingExpression<TProperty>(string propertyName, TtBindingBase binding, TtBindingExpressionBase parent);
        public void ClearBindExpression(TtBindableProperty bp);
        public void RemoveAttachedProperties(Type propertiesHostType);
        public void RemoveAttachedProperty(TtBindableProperty property);
        public void SetAttachedProperties(IBindableObject target);
        public TtBindableProperty FindBindableProperty(string propertyName);
#nullable disable
    }
    public class TtBindableObject : IBindableObject, IPropertyCustomization
    {
        protected Dictionary<TtBindableProperty, TtBindablePropertyValueBase> mBindExprDic = new Dictionary<TtBindableProperty, TtBindablePropertyValueBase>();
        protected TtTriggerCollection mTriggers = new TtTriggerCollection();
        [Browsable(false)]
        public TtTriggerCollection Triggers => mTriggers;

        public virtual TtBindableProperty FindBindableProperty(string propertyName)
        {
            lock(mBindExprDic)
            {
                foreach(var key in mBindExprDic.Keys)
                {
                    if (key.Name == propertyName)
                        return key;
                }
            }
            return UEngine.Instance.UIBindManager.FindBindableProperty(propertyName, UTypeDesc.TypeOf(GetType())); 
        }
        //public TtBindingExpressionBase SetBinding(TtBindableProperty bp, TtBindingBase binding)
        //{
        //    return TtBindingOperations.SetBinding(this, bp, binding);
        //}

        public virtual void SetBindExpression(TtBindableProperty bp, TtBindingExpressionBase expr)
        {
            TtBindablePropertyValueBase bpVal = null;
            lock(mBindExprDic)
            {
                if(!mBindExprDic.TryGetValue(bp, out bpVal))
                {
                    bpVal = new TtExpressionValues();
                    mBindExprDic[bp] = bpVal;
                }
            }
            ((TtExpressionValues)bpVal).Expressions.Add(expr);
        }
#nullable enable
        public virtual void SetValue<T>(in T value, [CallerMemberName] string? propertyName = null)
#nullable disable
        {
            var bp = UEngine.Instance.UIBindManager.FindBindableProperty(propertyName, UTypeDesc.TypeOf(GetType()));
            SetValue<T>(value, bp);
        }
        public virtual void SetValue<T>(in T value, TtBindableProperty bp)
        {
            TtBindablePropertyValueBase bpVal = null;
            lock (mBindExprDic)
            {
                if(bp.IsAttachedProperty && !mBindExprDic.TryGetValue(bp, out bpVal))
                {
                    bpVal = new TtAttachedValue<IBindableObject, T>(this);
                    mBindExprDic[bp] = bpVal;
                }
            }
            if (bpVal == null)
                return;
            if(mTriggers.HasTrigger(bp))
            {
                var oldVal = bpVal.GetValue<T>(bp);
                bpVal.SetValue<T>(this, bp, in value);
                mTriggers.InvokeTriggers(this, bp, oldVal, value);
            }
            else
                bpVal.SetValue<T>(this, bp, in value);
        }
#nullable enable
        public virtual T GetValue<T>([CallerMemberName] string? propertyName = null)
#nullable disable
        {
            var bp = UEngine.Instance.UIBindManager.FindBindableProperty(propertyName, UTypeDesc.TypeOf(GetType()));
            return GetValue<T>(bp);
        }
        public virtual T GetValue<T>(TtBindableProperty bp)
        {
            if (bp == null)
                return default(T);
            TtBindablePropertyValueBase bpVal = null;
            lock (mBindExprDic)
            {
                if (!mBindExprDic.TryGetValue(bp, out bpVal))
                    return default(T);
            }
            return bpVal.GetValue<T>(bp);            
        }
#nullable enable
        public virtual void OnValueChange<T>(in T value, in T oldValue, [CallerMemberName] string? propertyName = null)
#nullable disable
        {
            var bp = FindBindableProperty(propertyName);
            if (bp == null)
                return;
            TtBindablePropertyValueBase bpVal = null;
            lock (mBindExprDic)
            {
                mBindExprDic.TryGetValue(bp, out bpVal);
            }
            if (bpVal == null)
            {
                mTriggers.InvokeTriggers(this, bp, oldValue, value);
            }
            else
            {
                if (mTriggers.HasTrigger(bp))
                {
                    var oldVal = bpVal.GetValue<T>(bp);
                    bpVal.SetValue<T>(this, bp, in value);
                    mTriggers.InvokeTriggers(this, bp, oldVal, value);
                }
                else
                    bpVal.SetValue<T>(this, bp, in value);
            }
        }
        public virtual TtBindingExpressionBase CreateBindingExpression<TProperty>(string propertyName, TtBindingBase binding, TtBindingExpressionBase parent)
        {
            var retVal = new TtBindingExpression<TProperty>(binding, parent);
            retVal.TargetObject = this;
            retVal.TargetProperty = (TtBindableProperty<TProperty>)UEngine.Instance.UIBindManager.FindBindableProperty(propertyName, UTypeDesc.TypeOf(GetType()));
            return retVal;
        }
        public virtual void ClearBindExpression(TtBindableProperty bp)
        {
            lock(mBindExprDic)
            {
                mBindExprDic.Remove(bp);
            }
        }
        public virtual void RemoveAttachedProperties(Type propertiesHostType)
        {
            System.Collections.Generic.HashSet<TtBindableProperty> removePros = new HashSet<TtBindableProperty>();
            foreach (var data in mBindExprDic)
            {
                if(data.Key.HostType.IsEqual(propertiesHostType))
                {
                    removePros.Add(data.Key);
                }
            }
            foreach(var key in removePros)
            {
                RemoveAttachedProperty(key);
            }
        }
        public virtual void RemoveAttachedProperty(TtBindableProperty property)
        {
            mBindExprDic.Remove(property);
        }
        public virtual void SetAttachedProperties(IBindableObject target)
        {
        }


        [Browsable(false)]
        public bool IsPropertyVisibleDirty
        {
            get;
            set;
        } = false;

        public virtual void GetProperties(ref CustomPropertyDescriptorCollection collection, bool parentIsValueType)
        {
            var pros = TypeDescriptor.GetProperties(this);
            collection.InitValue(this, Rtti.UTypeDesc.TypeOf(this.GetType()), pros, parentIsValueType);

            // attached properties
            foreach(var bindData in mBindExprDic)
            {
                if(bindData.Value.Type == TtBindablePropertyValueBase.EType.AttachedValue)
                {
                    var proDesc = PropertyCollection.PropertyDescPool.QueryObjectSync();
                    proDesc.Name = bindData.Key.Name;
                    if(bindData.Key.DisplayNameAtt != null)
                        proDesc.DisplayName = bindData.Key.DisplayNameAtt.GetDisplayName(this);
                    proDesc.PropertyType = bindData.Key.PropertyType;
                    proDesc.Category = bindData.Key.Category;
                    proDesc.CustomValueEditor = bindData.Key.CustomValueEditor;
                    collection.Add(proDesc);
                }
            }
        }

        public virtual object GetPropertyValue(string propertyName)
        {
            var pro = this.GetType().GetProperty(propertyName);
            if (pro != null)
                return pro.GetValue(this);

            foreach(var bindData in mBindExprDic)
            {
                if(bindData.Key.Name == propertyName)
                {
                    return bindData.Value.GetValue<object>(bindData.Key);
                }
            }

            return null;
        }

        public virtual void SetPropertyValue(string propertyName, object value)
        {
            var pro = this.GetType().GetProperty(propertyName);
            if (pro != null)
                pro.SetValue(this, value);
            else
            {
                foreach(var bindData in mBindExprDic)
                {
                    if(bindData.Key.Name == propertyName)
                    {
                        bindData.Value.SetValue<object>(this, bindData.Key, in value);
                        break;
                    }
                }
            }
        }
    }
}
