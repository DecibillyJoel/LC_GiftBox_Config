/* Derived from https://github.com/Malcolm-Q/LC-LateGameUpgrades/blob/main/MoreShipUpgrades/Misc/Util/Tools.cs */

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace LC_GiftBox_Config.libs.ILTools
{
    public static class ILTools
    {
        public static bool FindCodeInstruction(ref int index, ref List<CodeInstruction> codes, object findValue, bool reverse = false, string? errorMessage = "Not found")
        {
            for (index = Math.Clamp(index, 0, codes.Count); index >= 0 && index < codes.Count; index += reverse ? -1 : 1)
            {
                if (CheckCodeInstruction(codes[index], findValue)) return true;
            }

            if (errorMessage != null) {
                throw new Exception($"[libs.ILTools.FindCodeInstruction] [{findValue}] | {errorMessage}");
            }

            return false;
        }
        public static bool FindCodeInstruction(ref int index, ref List<CodeInstruction> codes, short localIndex, bool instructionIsStore = false, bool reverse = false, string? errorMessage = "Not found")
        {
            for (index = Math.Clamp(index, 0, codes.Count); index >= 0 && index < codes.Count; index += reverse ? -1 : 1)
            {
                if (CheckCodeInstruction(codes[index], localIndex, instructionIsStore)) return true;
            }

            if (errorMessage != null) throw new Exception($"[libs.ILTools.FindCodeInstruction] {{{localIndex}, {instructionIsStore}}}] | {errorMessage}");

            return false;
        }
        public static bool FindCodeInstruction(ref int index, ref List<CodeInstruction> codes, FieldInfo fieldInfo, bool instructionIsStore = false, bool reverse = false, string? errorMessage = "Not found")
        {
            for (index = Math.Clamp(index, 0, codes.Count); index >= 0 && index < codes.Count; index += reverse ? -1 : 1)
            {
                if (CheckCodeInstruction(codes[index], fieldInfo, instructionIsStore)) return true;
            }

            if (errorMessage != null) throw new Exception($"[libs.ILTools.FindCodeInstruction] {{{fieldInfo}, {instructionIsStore}}}] | {errorMessage}");

            return false;
        }
        private static readonly OpCode[] ArgumentedLoadLocalCodeInstructions = [OpCodes.Ldloc, OpCodes.Ldloc_S];
        private static readonly OpCode[] ArgumentedStoreLocalCodeInstructions = [OpCodes.Stloc, OpCodes.Stloc_S];
        private static bool CheckCodeInstruction(CodeInstruction code, int localIndex, bool instructionIsStore)
        {
            return instructionIsStore switch
            {
                // Load local
                false => ArgumentedLoadLocalCodeInstructions.Contains(code.opcode) ? (code.operand as LocalBuilder)?.LocalIndex == localIndex : (localIndex switch
                {
                    0 => code.opcode == OpCodes.Ldloc_0,
                    1 => code.opcode == OpCodes.Ldloc_1,
                    2 => code.opcode == OpCodes.Ldloc_2,
                    3 => code.opcode == OpCodes.Ldloc_3,
                    _ => false,
                }),
                // Store local
                true => ArgumentedStoreLocalCodeInstructions.Contains(code.opcode) ? (code.operand as LocalBuilder)?.LocalIndex == localIndex : (localIndex switch
                {
                    0 => code.opcode == OpCodes.Stloc_0,
                    1 => code.opcode == OpCodes.Stloc_1,
                    2 => code.opcode == OpCodes.Stloc_2,
                    3 => code.opcode == OpCodes.Stloc_3,
                    _ => false,
                })
            };
        }
        private static bool CheckCodeInstruction(CodeInstruction code, FieldInfo fieldInfo, bool instructionIsStore)
        {
            return instructionIsStore switch
            {
                // Load field
                false => (code.opcode == OpCodes.Ldfld || code.opcode == OpCodes.Ldsfld) && (FieldInfo)code.operand == fieldInfo,
                // Store field
                true => (code.opcode == OpCodes.Stfld || code.opcode == OpCodes.Stsfld) && (FieldInfo)code.operand == fieldInfo
            };
        }
        private static bool CheckCodeInstruction(CodeInstruction code, object findValue)
        {
            if (findValue == null) 
            {
                throw new ArgumentNullException("CheckCodeInstruction(code, findValue) findValue should not be null; use CheckCodeInstruction(code, OpCodes.Ldnull) if searching for null");
            }

            if (findValue is byte || findValue is sbyte || findValue is short || findValue is ushort || findValue is int || findValue is uint || findValue is long || findValue is ulong)
            {
                return CheckIntegerCodeInstruction(code, findValue);
            }

            if (findValue is float || findValue is double || findValue is decimal)
            {
                return CheckFloatCodeInstruction(code, findValue);
            }

            if (findValue is string) 
            {
                return code.opcode == OpCodes.Ldstr && code.operand.Equals(findValue);
            }

            if (findValue is MethodInfo)
            {
                return (code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) && code.operand == findValue;
            }

            if (findValue is OpCode) 
            {
                return code.opcode == (OpCode)findValue;
            }

            if (findValue is FieldInfo)
            {
                throw new ArgumentException("CheckCodeInstruction(code, findValue) findValue should not be FieldInfo; use CheckCodeInstruction(code, fieldInfo, instructionIsStore) instead");
            }
            
            return false;
        }

        private static readonly OpCode[] ArgumentedIntegerCodeInstructions = [OpCodes.Ldc_I4, OpCodes.Ldc_I4_S, OpCodes.Ldc_I8];
        private static bool CheckIntegerCodeInstruction(CodeInstruction code, object findValue)
        {
            if (ArgumentedIntegerCodeInstructions.Contains(code.opcode))
            {
                return code.operand == findValue;
            }

            if (findValue is ulong maybeTooBig && maybeTooBig > 8) {
                return false;
            }

            return (long)findValue switch
            {
                0   => code.opcode == OpCodes.Ldc_I4_0,
                1   => code.opcode == OpCodes.Ldc_I4_1,
                2   => code.opcode == OpCodes.Ldc_I4_2,
                3   => code.opcode == OpCodes.Ldc_I4_3,
                4   => code.opcode == OpCodes.Ldc_I4_4,
                5   => code.opcode == OpCodes.Ldc_I4_5,
                6   => code.opcode == OpCodes.Ldc_I4_6,
                7   => code.opcode == OpCodes.Ldc_I4_7,
                8   => code.opcode == OpCodes.Ldc_I4_8,
                -1  => code.opcode == OpCodes.Ldc_I4_M1,

                _   => false
            };
        }

        private static readonly OpCode[] ArgumentedFloatCodeInstructions = [OpCodes.Ldc_R4, OpCodes.Ldc_R8];
        private static bool CheckFloatCodeInstruction(CodeInstruction code, object findValue)
        {
            return ArgumentedFloatCodeInstructions.Contains(code.opcode) && code.operand == findValue;
        }
    }
}
