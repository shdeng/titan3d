﻿using EngineNS.Bricks.StateMachine.SampleSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Bricks.StateMachine.TimedSM
{
    public partial class TtTimedState<S, T> : IState<S, T>
    {
        public S CenterData { get; set; }
        public string Name { get; set; } = "TimedState";
        protected TtTimedStateMachine<S, T> mStateMachine = null;
        public TtTimedStateMachine<S, T> StateMachine { get => mStateMachine; set => mStateMachine = value; }
        protected List<ITransition<S, T>> mTransitions = new List<ITransition<S, T>>();
        public List<ITransition<S, T>> Transitions
        {
            get => mTransitions;
        }
        public bool AddTransition(ITransition<S, T> transition)
        {
            if (mTransitions.Contains(transition))
                return false;
            mTransitions.Add(transition);
            return true;
        }
        public bool RemoveTransition(ITransition<S, T> transition)
        {
            if (!mTransitions.Contains(transition))
                return false;
            mTransitions.Remove(transition);
            return true;
        }
        protected List<IAttachmentRule<S, T>> mAttachments = new List<IAttachmentRule<S, T>>();
        public List<IAttachmentRule<S, T>> Attachments
        {
            get => mAttachments;
        }
        public bool AddAttachment(IAttachmentRule<S, T> attachment)
        {
            if (mAttachments.Contains(attachment))
                return false;
            mAttachments.Add(attachment);
            return true;
        }

        public bool RemoveAttachment(IAttachmentRule<S, T> attachment)
        {
            if (!mAttachments.Contains(attachment))
                return false;
            mAttachments.Remove(attachment);
            return true;
        }
        TtTimedSMClock mClock = new TtTimedSMClock();
        public ETimedSM_ClockWrapMode WrapMode
        {
            get => mClock.WrapMode;
            set => mClock.WrapMode = value;
        }
        public float StateTime { get => mClock.TimeInSecond; }
        public float StateTimeDuration { get => mClock.DurationInSecond; set => mClock.DurationInSecond = value; }
        public float StateMachineTime
        {
            get
            {
                return (mStateMachine as TtTimedStateMachine<S, T>).Clock.TimeInSecond;
            }
        }
        public TtTimedState(string name = "TimedState")
        {
        }
        public TtTimedState(TtTimedStateMachine<S, T> stateMachine,string name = "TimedState")
        {
            mStateMachine = stateMachine;
        }
        public bool IsInitialized = false;
        public virtual bool Initialize()
        {
            return false;
        }
        public virtual void Enter()
        {
            if (!IsInitialized)
            {
                Initialize();
                IsInitialized = true;
            }
            OnEnter();
        }

        public virtual void Exit()
        {
            OnExit();
        }
        public bool EnableTick { get; set; } = true;
        public bool ShouldUpdate()
        {
            return true;
        }
        public virtual void Tick(float elapseSecond, in T context)
        {
            if (!EnableTick)
                return;
            mClock.Advance(elapseSecond);
            if (!ShouldUpdate())
                return;
            Update(elapseSecond, context);
            foreach (var attachment in mAttachments)
            {
                if (attachment.Check())
                {
                    attachment.Tick(elapseSecond, context);
                }
            }
        }
        public bool TryCheckTransitions(in T context, out List<ITransition<S, T>> transitions)
        {
            transitions = new List<ITransition<S, T>>();
            for (int i = 0; i < mTransitions.Count; ++i)
            {
                if (mTransitions[i].Check(in context))
                {
                    transitions.Add(mTransitions[i]);
                }
            }
            if(transitions.Count > 0)
                return true;
            return false;
        }
        public virtual void Update(float elapseSecond, in T context)
        {
            OnUpdate();
        }
        public virtual void OnEnter()
        {

        }
        public virtual void OnExit()
        {

        }
        public virtual void OnUpdate()
        {

        }
    }

    public class TtTimedState<S> : TtTimedState<S, TtStateMachineContext>
    {
        public TtTimedState(string name = "TimedState") : base(name)
        {
        }
        public TtTimedState(TtTimedStateMachine<S, TtStateMachineContext> stateMachine, string name = "TimedState") : base(stateMachine, name)
        {
        }
    }
    public class TtTimedState : TtTimedState<TtDefaultCenterData, TtStateMachineContext>
    {
        public TtTimedState(string name = "TimedState") : base(name)
        {
        }
        public TtTimedState(TtTimedStateMachine<TtDefaultCenterData, TtStateMachineContext> stateMachine, string name = "TimedState") : base(stateMachine, name)
        {
        }
    }
}
