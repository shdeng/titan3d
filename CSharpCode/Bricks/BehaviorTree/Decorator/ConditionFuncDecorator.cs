﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.BehaviorTree.Decorator
{
    public class ConditionFuncDecorator : ConditionDecorator
    {
        public ConditionFuncDecorator()
        {
            BehaviorEvent = new Event.ConditionEvent();
            BehaviorEvent.Behavior = this;
        }
        public Func<long, GamePlay.UCenterData, bool> Func { get; set; } = null;
        public override BehaviorStatus Update(long timeElapse, GamePlay.UCenterData context)        {            if (Func != null)
            {
                var result = Func.Invoke(timeElapse, context);
                if (result == false && Child.Status != BehaviorStatus.Running)
                {
                    if (Inverse == false)
                        return BehaviorStatus.Failure;
                    else
                        return BehaviorStatus.Success;
                }
            }
            return Child.Tick(timeElapse, context);
        }
        public override bool EventEvaluate(long timeElapse, GamePlay.UCenterData context)
        {
            if (Func != null)
                return Func.Invoke(timeElapse, context);
            return false;
        }
    }
}
