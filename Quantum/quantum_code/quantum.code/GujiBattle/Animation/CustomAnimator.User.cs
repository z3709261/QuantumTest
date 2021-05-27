using System;
using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Extension struct for the CustomAnimator component
    /// Used mainly to get/set variable values and to initialize an entity's CustomAnimator
    /// </summary>
    public unsafe partial struct CustomAnimator
    {
        // A static variable that allows transition interrupt.
        public static readonly bool allowTransitionInterruption = true;

        internal static CustomAnimatorRuntimeVariable* Variable(Frame f, CustomAnimator* ptr, int index)
        {
            var variablesList = f.ResolveList<CustomAnimatorRuntimeVariable>(ptr->AnimatorVariables);
            Assert.Check(index >= 0 && index < variablesList.Count);
            return variablesList.GetPointer(index);
        }

        internal static CustomAnimatorRuntimeVariable* VariableByName(Frame f, CustomAnimator* a, string name, out int variableId)
        {
            variableId = -1;
            if (a->animatorGraph.Equals(default) == false)
            {
                CustomAnimatorGraph graph = f.FindAsset<CustomAnimatorGraph>(a->animatorGraph.Id);
                variableId = graph.VariableIndex(name);
                if (variableId >= 0)
                {
                    return Variable(f, a, variableId);
                }
            }
            return null;
        }

        /// <summary>
        /// Initializes the passed CustomAnimator component based on the CustomAnimatorGraph passed
        /// This is how timers are initialized and variables are set to default
        /// </summary>
        public static void SetCustomAnimatorGraph(Frame f, CustomAnimator* a, CustomAnimatorGraph graph)
        {
            graph.Initialise(f, a);
        }

        static void SetRuntimeVariable(Frame f, CustomAnimator* a, CustomAnimatorRuntimeVariable* variable, int variableId)
        {
            Assert.Check(variable != null);
            Assert.Check(variableId >= 0);

            var paramsList = f.ResolveList<CustomAnimatorRuntimeVariable>(a->AnimatorVariables);
            *paramsList.GetPointer(variableId) = *variable;
        }

        internal static FP GetActiveTime(CustomAnimator* animator)
        {
            return animator->to_state_id == 0 ? animator->time : animator->to_state_time;
        }

        internal static FP GetActiveWorldTime(Frame f, CustomAnimator* animator)
        {
            var state = GetCurrentState(f, animator);
            FP d = state == null ? FP._1 : state.speed;
            return (animator->to_state_id == 0 ? animator->time : animator->to_state_time) / d;
        }

        internal static FP GetLastActiveTime(CustomAnimator* animator)
        {
            return animator->to_state_id == 0 ? animator->last_time : animator->to_state_last_time;
        }

        internal static FP GetLastActiveWorldTime(Frame f, CustomAnimator* animator)
        {
            var state = GetCurrentState(f, animator);
            FP d = state == null ? FP._1 : state.speed;
            return (animator->to_state_id == 0 ? animator->last_time : animator->to_state_last_time) / d;
        }

        #region FixedPoint

        static void SetFixedPointValue(Frame f, CustomAnimator* a, CustomAnimatorRuntimeVariable* variable, int variableId, FP value)
        {
            if (variable == null)
            {
                return;
            }

            *variable->FPValue = value;
            SetRuntimeVariable(f, a, variable, variableId);
        }

        public static void SetFixedPoint(Frame f, CustomAnimator* a, string name, FP value)
        {
            var variable = VariableByName(f, a, name, out var variableId);
            SetFixedPointValue(f, a, variable, variableId, value);
        }

        public static void SetFixedPoint(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name, FP value)
        {
            Assert.Check(a->animatorGraph == g);

            var variableId = g.VariableIndex(name);
            SetFixedPoint(f, a, variableId, value);
        }

        public static void SetFixedPoint(Frame f, CustomAnimator* a, int variableId, FP value)
        {
            if (variableId < 0)
            {
                return;
            }

            var variable = Variable(f, a, variableId);
            SetFixedPointValue(f, a, variable, variableId, value);
        }

        public static FP GetFixedPoint(Frame f, CustomAnimator* a, string name)
        {
            var variable = VariableByName(f, a, name, out _);
            if (variable != null)
            {
                return *variable->FPValue;
            }

            return FP.PiOver4;
        }

        public static FP GetFixedPoint(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name)
        {
            Assert.Check(a->animatorGraph == g);

            var variableId = g.VariableIndex(name);
            return GetFixedPoint(f, a, variableId);
        }

        public static FP GetFixedPoint(Frame f, CustomAnimator* a, int variableId)
        {
            if (variableId < 0)
            {
                return FP.PiOver4;
            }

            var variable = Variable(f, a, variableId);
            if (variable != null)
            {
                return *variable->FPValue;
            }

            return FP.PiOver4;
        }

        #endregion

        #region Integer

        static void SetIntegerValue(Frame f, CustomAnimator* a, CustomAnimatorRuntimeVariable* variable, int variableId, int value)
        {
            if (variable == null)
            {
                return;
            }

            *variable->IntegerValue = value;
            SetRuntimeVariable(f, a, variable, variableId);
        }

        // DeLucas:  Added add methods for integers
        public static void AddToInteger(Frame f, CustomAnimator* a, string name, int value)
        {
            var v = GetInteger(f, a, name);
            v += value;
            SetInteger(f, a, name, v);
        }

        public static void AddToInteger(Frame f, CustomAnimator* a, int index, int value)
        {
            var v = GetInteger(f, a, index);
            v += value;
            SetInteger(f, a, index, v);
        }

        public static void AddToFixedPoint(Frame f, CustomAnimator* a, string name, FP value)
        {
            var v = GetFixedPoint(f, a, name);
            v += value;
            SetFixedPoint(f, a, name, v);
        }

        public static void AddToFixedPoint(Frame f, CustomAnimator* a, int index, FP value)
        {
            var v = GetFixedPoint(f, a, index);
            v += value;
            SetFixedPoint(f, a, index, v);
        }

        public static void SetInteger(Frame f, CustomAnimator* a, string name, int value)
        {
            var variable = VariableByName(f, a, name, out var variableId);
            SetIntegerValue(f, a, variable, variableId, value);
        }

        public static void SetInteger(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name, int value)
        {
            Assert.Check(a->animatorGraph == g);

            var variableId = g.VariableIndex(name);
            SetInteger(f, a, variableId, value);
        }

        public static void SetInteger(Frame f, CustomAnimator* a, int variableId, int value)
        {
            if (variableId < 0)
            {
                return;
            }

            var variable = Variable(f, a, variableId);
            SetIntegerValue(f, a, variable, variableId, value);
        }

        public static int GetInteger(Frame f, CustomAnimator* a, string name)
        {
            var variable = VariableByName(f, a, name, out _);
            if (variable != null)
            {
                return *variable->IntegerValue;
            }

            return 0;
        }

        public static Custom.Animator.AnimatorState GetCurrentState(Frame f, CustomAnimator* anim)
        {
            if (anim->to_state_id == 0)
                return f.Assets.CustomAnimatorGraph(anim->animatorGraph).GetState(anim->current_state_id);
            else
                return f.Assets.CustomAnimatorGraph(anim->animatorGraph).GetState(anim->to_state_id);
        }

        public static int GetInteger(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name)
        {
            Assert.Check(a->animatorGraph == g);

            var variableId = g.VariableIndex(name);
            return GetInteger(f, a, variableId);
        }

        public static int GetInteger(Frame f, CustomAnimator* a, int variableId)
        {
            if (variableId < 0)
            {
                return 0;
            }

            var variable = Variable(f, a, variableId);
            if (variable != null)
            {
                return *variable->IntegerValue;
            }

            return 0;
        }

        #endregion

        #region Boolean

        static void SetBooleanValue(Frame f, CustomAnimator* a, CustomAnimatorRuntimeVariable* variable, int variableId, bool value)
        {
            if (variable == null)
            {
                return;
            }

            *variable->BooleanValue = value;
            SetRuntimeVariable(f, a, variable, variableId);
        }

        public static void SetBoolean(Frame f, CustomAnimator* a, string name, bool value)
        {
            var variable = VariableByName(f, a, name, out var variableId);
            SetBooleanValue(f, a, variable, variableId, value);
        }

        public static void SetBoolean(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name, bool value)
        {
            Assert.Check(a->animatorGraph == g);

            var variableId = g.VariableIndex(name);
            SetBoolean(f, a, variableId, value);
        }

        public static void SetBoolean(Frame f, CustomAnimator* a, int variableId, bool value)
        {
            if (variableId < 0)
            {
                return;
            }

            var variable = Variable(f, a, variableId);
            SetBooleanValue(f, a, variable, variableId, value);
        }

        public static bool GetBoolean(Frame f, CustomAnimator* a, string name)
        {
            var variable = VariableByName(f, a, name, out _);
            if (variable != null)
            {
                return *variable->BooleanValue;
            }

            return false;
        }

        public static bool GetBoolean(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name)
        {
            Assert.Check(a->animatorGraph == g);

            var variableId = g.VariableIndex(name);
            return GetBoolean(f, a, variableId);
        }

        public static bool GetBoolean(Frame f, CustomAnimator* a, int variableId)
        {
            if (variableId < 0)
            {
                return false;
            }

            var variable = Variable(f, a, variableId);
            if (variable != null)
            {
                return *variable->BooleanValue;
            }

            return false;
        }

        #endregion

        #region Trigger

        public static void SetTrigger(Frame f, CustomAnimator* a, string name)
        {
            SetBoolean(f, a, name, true);
        }

        public static void SetTrigger(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name)
        {
            SetBoolean(f, a, g, name, true);
        }

        public static void SetTrigger(Frame f, CustomAnimator* a, int variableId)
        {
            SetBoolean(f, a, variableId, true);
        }

        public static void ResetTrigger(Frame f, CustomAnimator* a, string name)
        {
            SetBoolean(f, a, name, false);
        }

        public static void ResetTrigger(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name)
        {
            SetBoolean(f, a, g, name, false);
        }

        public static void ResetTrigger(Frame f, CustomAnimator* a, int variableId)
        {
            SetBoolean(f, a, variableId, false);
        }

        public static bool IsTriggerActive(Frame f, CustomAnimator* a, string name)
        {
            return GetBoolean(f, a, name);
        }

        public static bool IsTriggerActive(Frame f, CustomAnimator* a, CustomAnimatorGraph g, string name)
        {
            return GetBoolean(f, a, g, name);
        }

        public static bool IsTriggerActive(Frame f, CustomAnimator* a, int variableId)
        {
            return GetBoolean(f, a, variableId);
        }

        #endregion
    }
}
