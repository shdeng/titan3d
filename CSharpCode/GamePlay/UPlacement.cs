﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.GamePlay
{
    public partial class UPlacementBase : IO.ISerializer
    {
        public UPlacementBase()
        {
            //HostNode = node;
        }
        public virtual void OnPreRead(object tagObject, object hostObject, bool fromXml)
        {
            HostNode = tagObject as Scene.UNode;
        }
        public virtual void OnPropertyRead(object root, System.Reflection.PropertyInfo prop, bool fromXml) { }
        public virtual DVector3 Position
        {
            get
            {
                return DVector3.Zero;
            }
            set
            {
            }
        }
        public virtual Vector3 Scale
        {
            get
            {
                return Vector3.One;
            }
            set
            {

            }
        }
        public virtual Quaternion Quat
        {
            get
            {
                return Quaternion.Identity;
            }
            set
            {

            }
        }        
        public virtual bool IsIdentity
        {
            get { return true; }
        }
        public virtual bool HasScale
        {
            get { return false; }
        }
        public bool InheritScale { get; set; } = false;
        public virtual void SetTransform(in DVector3 pos, in Vector3 scale, in Quaternion quat)
        {
            
        }
        public virtual void SetTransform(in FTransform data)
        {

        }
        public virtual FTransform TransformData
        {
            get
            {
                return FTransform.Identity;
            }
            set
            {

            }
        }
        public virtual ref FTransform TransformRef
        {
            get
            {
                return ref FTransform.IdentityForRef;
            }
        }

        public Scene.UNode HostNode { get; set; }
        public virtual ref FTransform AbsTransform
        {
            get
            {
                return ref FTransform.IdentityForRef;
            }
        }
    }

    public partial class UIdentityPlacement : UPlacementBase
    {
        public UIdentityPlacement()
        {
            
        }
        public FTransform mAbsTransform = FTransform.Identity;
        public override ref FTransform AbsTransform
        {
            get
            {
                return ref mAbsTransform;
            }
        }
    }

    public partial class UPlacement : UIdentityPlacement
    {
        public UPlacement()
        {
            mTransformData.InitData();
            mIsIdentity = true;
        }
        protected bool mIsIdentity;
        public override bool IsIdentity
        {
            get { return mIsIdentity; }
        }
        public FTransform mTransformData;
        [Rtti.Meta(Order = 0)]
        public override FTransform TransformData
        {
            get => mTransformData;
            set
            {
                mTransformData = value;
                UpdateData();
            }
        }
        public override ref FTransform TransformRef
        {
            get
            {
                return ref mTransformData;
            }
        }
        public override DVector3 Position
        {
            get => mTransformData.mPosition;
            set
            {
                mTransformData.mPosition = value;
                UpdateData();
            }
        }
        public override Vector3 Scale
        {
            get => mTransformData.mScale;
            set
            {
                mTransformData.mScale = value;
                UpdateData();
            }
        }
        public override Quaternion Quat
        {
            get => mTransformData.mQuat;
            set
            {
                mTransformData.mQuat = value;
                UpdateData();
            }
        }
        public override bool HasScale
        {
            get { return mTransformData.HasScale; }
        }
        private void UpdateData()
        {
            mIsIdentity = mTransformData.IsIdentity;
            HostNode.UpdateAbsTransform();
            HostNode.UpdateAABB();
            if (HostNode.Parent != null)
                HostNode.Parent.UpdateAABB();
        }

        public override void SetTransform(in DVector3 pos, in Vector3 scale, in Quaternion quat)
        {
            mTransformData.Position = pos;
            mTransformData.Scale = scale;
            mTransformData.Quat = quat;

            UpdateData();
        }
        public override void SetTransform(in FTransform data)
        {
            mTransformData = data;
            UpdateData();
        }
    }
}
