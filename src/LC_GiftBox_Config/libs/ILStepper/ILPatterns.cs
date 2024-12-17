using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using StackBehaviour = System.Reflection.Emit.StackBehaviour;

namespace LC_GiftBox_Config.libs.ILStepper;
public static class ILPatterns
{
    #region Stack Size Patterns
        public static int? StackSizeDelta(StackBehaviour stackBehaviour)
        {
            return stackBehaviour switch
            {
                StackBehaviour.Pop0 => 0,
                StackBehaviour.Pop1 => -1,
                StackBehaviour.Pop1_pop1 => -2,
                StackBehaviour.Popi => -1,
                StackBehaviour.Popi_pop1 => -2,
                StackBehaviour.Popi_popi => -2,
                StackBehaviour.Popi_popi8 => -2,
                StackBehaviour.Popi_popi_popi => -3,
                StackBehaviour.Popi_popr4 => -2,
                StackBehaviour.Popi_popr8 => -2,
                StackBehaviour.Popref => -1,
                StackBehaviour.Popref_pop1 => -2,
                StackBehaviour.Popref_popi => -2,
                StackBehaviour.Popref_popi_popi => -3,
                StackBehaviour.Popref_popi_popi8 => -3,
                StackBehaviour.Popref_popi_popr4 => -3,
                StackBehaviour.Popref_popi_popr8 => -3,
                StackBehaviour.Popref_popi_popref => -3,
                StackBehaviour.Push0 => 0,
                StackBehaviour.Push1 => 1,
                StackBehaviour.Push1_push1 => 2,
                StackBehaviour.Pushi => 1,
                StackBehaviour.Pushi8 => 1,
                StackBehaviour.Pushr4 => 1,
                StackBehaviour.Pushr8 => 1,
                StackBehaviour.Pushref => 1,
                StackBehaviour.Popref_popi_pop1 => -3,

                StackBehaviour.Varpop or StackBehaviour.Varpush => null,
                _ => throw new ArgumentOutOfRangeException($"stackBehaviour {stackBehaviour} is an invalid value")
            };
        }

        // Derived from https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Cil/CodeWriter.cs#L434
        public static int? StackSizeDelta(OpCode opCode, object operand)
        {
            if (opCode == OpCodes.Calli) return null; // should be impossible, calli is not supported by HarmonyX

            if (opCode.FlowControl == FlowControl.Call) // methods / properties
            {
                MethodInfo? methodInfo = operand as MethodInfo;
                if (methodInfo == null) return null;

                int delta = -methodInfo.GetParameters().Count(); // Pop explicit parameters

                // Pop implicit this
                if (opCode != OpCodes.Newobj && methodInfo.CallingConvention.HasFlag(CallingConventions.HasThis) && !methodInfo.CallingConvention.HasFlag(CallingConventions.ExplicitThis))
                    delta -= 1;

                // Push return value
                if (opCode == OpCodes.Newobj || methodInfo.ReturnType != typeof(void))
                    delta += 1;

                return delta;
            }

            int? pushes = StackSizeDelta(opCode.StackBehaviourPush);
            int? pops = StackSizeDelta(opCode.StackBehaviourPop);

            if (pushes == null || pops == null) return null; // should be impossible if EmptiesStack() is false
            return pushes + pops;
        }

        public static int? StackSizeDelta(CodeInstruction code)
        {
            return StackSizeDelta(code.opcode, code.operand);
        }

        public static bool EmptiesStack(OpCode opcode)
        {
            return opcode.FlowControl switch {
                FlowControl.Branch or FlowControl.Throw or FlowControl.Return => true,
                _ => false
            };
        }

        public static bool EmptiesStack(CodeInstruction code)
        {
            return EmptiesStack(code.opcode);
        }

        public static Func<CodeInstruction, int, bool> NextEmptyStack(int startSize = 0) 
        {
            int stackSize = startSize;

            return (CodeInstruction code, int index) => {
                if (EmptiesStack(code)) return true;

                int delta = StackSizeDelta(code)
                    ?? throw new ArgumentException($"[libs.ILPatterns.NextEmptyStack] Encountered uncountable instruction [{index}] {code}");

                return (stackSize += delta) == 0;
            };
        }
    #endregion
}