/* Derived from:
    https://github.com/BepInEx/HarmonyX/blob/master/Harmony/Public/CodeInstruction.cs
    https://github.com/BepInEx/HarmonyX/blob/master/Harmony/Tools/Extensions.cs
    https://github.com/BepInEx/HarmonyX/blob/master/Harmony/Tools/CodeMatcher/CodeMatch.cs
*/

using HarmonyLib;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace LC_GiftBox_Config.libs.HarmonyXExtensions;

public static class CodeInstructionPolyfills
{
    public static readonly OpCode[] ARGUMENTED_LOCAL_INSTRUCTS = [OpCodes.Ldloc, OpCodes.Stloc, OpCodes.Ldloca, OpCodes.Ldloc_S, OpCodes.Stloc_S, OpCodes.Ldloca_S];
    public static readonly OpCode[] LOAD_FIELD_INSTRUCTS = [OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda];
    public static readonly OpCode[] STORE_FIELD_INSTRUCTS = [OpCodes.Stfld, OpCodes.Stsfld];

    /// <summary>Returns if an <see cref="OpCode"/> is initialized and valid</summary>
    /// <param name="code">The <see cref="OpCode"/></param>
    /// <returns></returns>
    public static bool IsValid(this OpCode code) => code.Size > 0;

    // --- CALLING

    /// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
    /// <param name="type">The class/type where the method is declared</param>
    /// <param name="name">The name of the method (case sensitive)</param>
    /// <param name="parameters">Optional parameters to target a specific overload of the method</param>
    /// <param name="generics">Optional list of types that define the generic version of the method</param>
    /// <returns>A code instruction that calls the method matching the arguments</returns>
    ///
    public static CodeInstruction Call(Type type, string name, Type[]? parameters = null, Type[]? generics = null)
    {
        var method = AccessTools.Method(type, name, parameters, generics) 
            ?? throw new ArgumentException($"No method found for type={type}, name={name}, parameters={parameters.Description()}, generics={generics.Description()}");
        return new CodeInstruction(OpCodes.Call, method);
    }

    /// <summary>Tests if the code instruction calls the method/constructor</summary>
    /// <param name="codeInstruction">The HarmonyLib.CodeInstruction</param>
    /// <param name="type">The class/type where the method is declared</param>
    /// <param name="name">The name of the method (case sensitive)</param>
    /// <param name="parameters">Optional parameters to target a specific overload of the method</param>
    /// <param name="generics">Optional list of types that define the generic version of the method</param>
    /// <returns>True if the instruction calls the method or constructor</returns>
    ///
    public static bool Calls(this CodeInstruction codeInstruction, Type type, string name, Type[]? parameters = null, Type[]? generics = null)
    {
        var method = AccessTools.Method(type, name, parameters, generics) 
            ?? throw new ArgumentException($"No method found for type={type}, name={name}, parameters={parameters.Description()}, generics={generics.Description()}");
        return codeInstruction.Calls(method);
    }

    /// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
    /// <param name="typeColonMethodname">The target method in the form <c>TypeFullName:MethodName</c>, where the type name matches a form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
    /// <param name="parameters">Optional parameters to target a specific overload of the method</param>
    /// <param name="generics">Optional list of types that define the generic version of the method</param>
    /// <returns>A code instruction that calls the method matching the arguments</returns>
    ///
    public static CodeInstruction Call(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null)
    {
        var method = AccessTools.Method(typeColonMethodname, parameters, generics) 
            ?? throw new ArgumentException($"No method found for {typeColonMethodname}, parameters={parameters.Description()}, generics={generics.Description()}");
        return new CodeInstruction(OpCodes.Call, method);
    }

    /// <summary>Tests if the code instruction calls the method/constructor</summary>
    /// <param name="codeInstruction">The HarmonyLib.CodeInstruction</param>
    /// <param name="typeColonMethodname">The target method in the form <c>TypeFullName:MethodName</c>, where the type name matches a form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
    /// <param name="parameters">Optional parameters to target a specific overload of the method</param>
    /// <param name="generics">Optional list of types that define the generic version of the method</param>
    /// <returns>True if the instruction calls the method or constructor</returns>
    ///
    public static bool Calls(this CodeInstruction codeInstruction, string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null)
    {
        var method = AccessTools.Method(typeColonMethodname, parameters, generics) 
            ?? throw new ArgumentException($"No method found for {typeColonMethodname}, parameters={parameters.Description()}, generics={generics.Description()}");
        return codeInstruction.Calls(method);
    }

    /// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
    /// <param name="expression">The lambda expression using the method</param>
    /// <returns></returns>
    ///
    public static CodeInstruction Call(Expression<Action> expression) => new(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));

    /// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
    /// <param name="expression">The lambda expression using the method</param>
    /// <returns></returns>
    ///
    public static CodeInstruction Call<T>(Expression<Action<T>> expression) => new(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));

    /// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
    /// <param name="expression">The lambda expression using the method</param>
    /// <returns></returns>
    ///
    public static CodeInstruction Call<T, TResult>(Expression<Func<T, TResult>> expression) => new(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));

    /// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
    /// <param name="expression">The lambda expression using the method</param>
    /// <returns></returns>
    ///
    public static CodeInstruction Call(LambdaExpression expression) => new(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));

    /// <summary>Returns an instruction to call the specified closure</summary>
    /// <typeparam name="T">The delegate type to emit</typeparam>
    /// <param name="closure">The closure that defines the method to call</param>
    /// <returns>A <see cref="CodeInstruction"/> that calls the closure as a method</returns>
    ///
    public static CodeInstruction CallClosure<T>(T closure) where T : Delegate
    {
        return Transpilers.EmitDelegate(closure);
    }

    // --- FIELDS

    /// <summary>Creates a CodeInstruction loading a field (LD[S]FLD[A])</summary>
    /// <param name="type">The class/type where the field is defined</param>
    /// <param name="name">The name of the field (case sensitive)</param>
    /// <param name="useAddress">Use address of field</param>
    /// <returns>The HarmonyLib.CodeInstruction</returns>
    public static CodeInstruction LoadField(Type type, string name, bool useAddress = false)
    {
        var field = AccessTools.Field(type, name) ?? throw new ArgumentException($"No field found for {type} and {name}");
        return new CodeInstruction(useAddress ? (field.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda) : (field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld), field);
    }

    /// <summary>Creates a CodeInstruction storing to a field (ST[S]FLD)</summary>
    /// <param name="type">The class/type where the field is defined</param>
    /// <param name="name">The name of the field (case sensitive)</param>
    /// <returns>The HarmonyLib.CodeInstruction</returns>
    public static CodeInstruction StoreField(Type type, string name)
    {
        var field = AccessTools.Field(type, name) ?? throw new ArgumentException($"No field found for {type} and {name}");
        return new CodeInstruction(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
    }

    //
    // Summary:
    //     Tests if the code instruction loads a field
    //
    // Parameters:
    //   code:
    //     The HarmonyLib.CodeInstruction
    //
    //   type:
    //     The type to which the given field belongs
    //
    //   name:
    //     The name of the FieldInfo
    //
    //   byAddress:
    //     Set to true if the address of the field is loaded
    //
    // Returns:
    //     True if the instruction loads this field
    public static bool LoadsField(this CodeInstruction codeInstruction, Type type, string name, bool byAddress = false)
    {
        FieldInfo? field = AccessTools.DeclaredField(type, name);

        return field != null && codeInstruction.LoadsField(field, byAddress);
    }

    //
    // Summary:
    //     Tests if the code instruction stores a field
    //
    // Parameters:
    //   code:
    //     The HarmonyLib.CodeInstruction
    //
    //   type:
    //     The type to which the given field belongs
    //
    //   name:
    //     The name of the FieldInfo
    //
    // Returns:
    //     True if the instruction stores this field
    public static bool StoresField(this CodeInstruction codeInstruction, Type type, string name)
    {
        FieldInfo? field = AccessTools.DeclaredField(type, name);

        return field != null && codeInstruction.StoresField(field);
    }

    // --- LOCALS

    /// <summary>Returns the index targeted by this <c>ldloc</c>, <c>ldloca</c>, or <c>stloc</c></summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <returns>The index it targets</returns>
    /// <seealso cref="LoadLocal(int, bool)"/>
    /// <seealso cref="StoreLocal(int)"/>
    public static int LocalIndex(this CodeInstruction code)
    {
        if (code.opcode == OpCodes.Ldloc_0 || code.opcode == OpCodes.Stloc_0) return 0;
        else if (code.opcode == OpCodes.Ldloc_1 || code.opcode == OpCodes.Stloc_1) return 1;
        else if (code.opcode == OpCodes.Ldloc_2 || code.opcode == OpCodes.Stloc_2) return 2;
        else if (code.opcode == OpCodes.Ldloc_3 || code.opcode == OpCodes.Stloc_3) return 3;
        else if (ARGUMENTED_LOCAL_INSTRUCTS.Contains(code.opcode)) return Convert.ToInt32((code.operand as LocalVariableInfo)?.LocalIndex ?? code.operand);
        else throw new ArgumentException("Instruction is not a load or store", nameof(code));
    }

    /// <summary>Creates a CodeInstruction loading a local with the given index, using the shorter forms when possible</summary>
    /// <param name="index">The index where the local is stored</param>
    /// <param name="useAddress">Use address of local</param>
    /// <returns>The HarmonyLib.CodeInstruction</returns>
    /// <seealso cref="LocalIndex(CodeInstruction)"/>
    public static CodeInstruction LoadLocal(int index, bool useAddress = false)
    {
        if (useAddress)
        {
            return index switch
            {
                < 256 => new CodeInstruction(OpCodes.Ldloca_S, Convert.ToByte(index)),
                _ => new CodeInstruction(OpCodes.Ldloca, index)
            };
        }
        else
        {
            return index switch
            {
                0 => new CodeInstruction(OpCodes.Ldloc_0),
                1 => new CodeInstruction(OpCodes.Ldloc_1),
                2 => new CodeInstruction(OpCodes.Ldloc_2),
                3 => new CodeInstruction(OpCodes.Ldloc_3),
                < 256 => new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(index)),
                _ => new CodeInstruction(OpCodes.Ldloc, index)
            };
        }
    }

    /// <summary>Creates a CodeInstruction loading a local with the given LocalVariableInfo, using the shorter forms when possible</summary>
    /// <param name="local">The LocalVariableInfo of the stored local</param>
    /// <param name="useAddress">Use address of local</param>
    /// <returns>The HarmonyLib.CodeInstruction</returns>
    /// <seealso cref="LocalIndex(CodeInstruction)"/>
    public static CodeInstruction LoadLocal(LocalVariableInfo local, bool useAddress = false)
    {
        return LoadLocal(local.LocalIndex, useAddress);
    }

    /// <summary>Creates a CodeInstruction storing to a local with the given index, using the shorter forms when possible</summary>
    /// <param name="index">The index where the local is stored</param>
    /// <returns>The HarmonyLib.CodeInstruction</returns>
    /// <seealso cref="LocalIndex(CodeInstruction)"/>
    public static CodeInstruction StoreLocal(int index)
    {
        return index switch
        {
            0 => new CodeInstruction(OpCodes.Stloc_0),
            1 => new CodeInstruction(OpCodes.Stloc_1),
            2 => new CodeInstruction(OpCodes.Stloc_2),
            3 => new CodeInstruction(OpCodes.Stloc_3),
            < 256 => new CodeInstruction(OpCodes.Stloc_S, Convert.ToByte(index)),
            _ => new CodeInstruction(OpCodes.Stloc, index)
        };
    }

    /// <summary>Creates a CodeInstruction storing to a local with the given LocalVariableInfo, using the shorter forms when possible</summary>
    /// <param name="local">The LocalVariableInfo of the stored local</param>
    /// <returns>The HarmonyLib.CodeInstruction</returns>
    /// <seealso cref="LocalIndex(CodeInstruction)"/>
    public static CodeInstruction StoreLocal(LocalVariableInfo local)
    {
        return StoreLocal(local.LocalIndex);
    }

    //
    // Summary:
    //     Tests for any form of Ldloc*
    //
    // Parameters:
    //   code:
    //     The HarmonyLib.CodeInstruction
    //
    //   variable:
    //     The optional local variable
    //
    // Returns:
    //     True if it matches one of the variations
    public static bool LoadsLocal(this CodeInstruction code, LocalVariableInfo? local = null)
    {
        return code.IsLdloc() && (local == null || code.LocalIndex() == local.LocalIndex);
    }

    //
    // Summary:
    //     Tests for any form of Ldloc*
    //
    // Parameters:
    //   code:
    //     The HarmonyLib.CodeInstruction
    //
    //   variable:
    //     The optional local variable
    //
    // Returns:
    //     True if it matches one of the variations
    public static bool LoadsLocal(this CodeInstruction code, int index)
    {
        return code.IsLdloc() && code.LocalIndex() == index;
    }

    //
    // Summary:
    //     Tests for any form of Stloc*
    //
    // Parameters:
    //   code:
    //     The HarmonyLib.CodeInstruction
    //
    //   variable:
    //     The optional local variable
    //
    // Returns:
    //     True if it matches one of the variations
    public static bool StoresLocal(this CodeInstruction code, LocalVariableInfo? local = null)
    {
        return code.IsStloc() && (local == null || code.LocalIndex() == local.LocalIndex);
    }

    //
    // Summary:
    //     Tests for any form of Stloc*
    //
    // Parameters:
    //   code:
    //     The HarmonyLib.CodeInstruction
    //
    //   variable:
    //     The optional local variable
    //
    // Returns:
    //     True if it matches one of the variations
    public static bool StoresLocal(this CodeInstruction code, int index)
    {
        return code.IsStloc() && code.LocalIndex() == index;
    }

    // --- ARGUMENTS

    /// <summary>Creates a CodeInstruction loading an argument with the given index, using the shorter forms when possible</summary>
    /// <param name="index">The index of the argument</param>
    /// <param name="useAddress">Use address of argument</param>
    /// <returns></returns>
    /// <seealso cref="ArgumentIndex(CodeInstruction)"/>
    public static CodeInstruction LoadArgument(int index, bool useAddress = false)
    {
        if (useAddress)
        {
            if (index < 256) return new CodeInstruction(OpCodes.Ldarga_S, Convert.ToByte(index));
            else return new CodeInstruction(OpCodes.Ldarga, index);
        }
        else
        {
            if (index == 0) return new CodeInstruction(OpCodes.Ldarg_0);
            else if (index == 1) return new CodeInstruction(OpCodes.Ldarg_1);
            else if (index == 2) return new CodeInstruction(OpCodes.Ldarg_2);
            else if (index == 3) return new CodeInstruction(OpCodes.Ldarg_3);
            else if (index < 256) return new CodeInstruction(OpCodes.Ldarg_S, Convert.ToByte(index));
            else return new CodeInstruction(OpCodes.Ldarg, index);
        }
    }

    /// <summary>Returns the index targeted by this <c>ldarg</c>, <c>ldarga</c>, or <c>starg</c></summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <returns>The index it targets</returns>
    /// <seealso cref="LoadArgument(int, bool)"/>
    /// <seealso cref="StoreArgument(int)"/>
    public static int ArgumentIndex(this CodeInstruction code)
    {
        if (code.opcode == OpCodes.Ldarg_0) return 0;
        else if (code.opcode == OpCodes.Ldarg_1) return 1;
        else if (code.opcode == OpCodes.Ldarg_2) return 2;
        else if (code.opcode == OpCodes.Ldarg_3) return 3;
        else if (code.opcode == OpCodes.Ldarg_S || code.opcode == OpCodes.Ldarg) return Convert.ToInt32(code.operand);
        else if (code.opcode == OpCodes.Starg_S || code.opcode == OpCodes.Starg) return Convert.ToInt32(code.operand);
        else if (code.opcode == OpCodes.Ldarga_S || code.opcode == OpCodes.Ldarga) return Convert.ToInt32(code.operand);
        else throw new ArgumentException("Instruction is not a load or store", nameof(code));
    }

    /// <summary>Creates a CodeInstruction storing to an argument with the given index, using the shorter forms when possible</summary>
    /// <param name="index">The index of the argument</param>
    /// <returns></returns>
    /// <seealso cref="ArgumentIndex(CodeInstruction)"/>
    public static CodeInstruction StoreArgument(int index)
    {
        if (index < 256) return new CodeInstruction(OpCodes.Starg_S, Convert.ToByte(index));
        else return new CodeInstruction(OpCodes.Starg, index);
    }

    // --- CONSTANTS

    /// <summary>Tests if the code instruction loads a string constant</summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <param name="str">The string</param>
    /// <returns>True if the instruction loads the constant</returns>
    ///
    public static bool LoadsString(this CodeInstruction code, string str)
    {
        if (code.opcode != OpCodes.Ldstr) return false;
        var val = Convert.ToString(code.operand);
        return val == str;
    }

    //
    // Summary:
    //     Returns a code instruction that loads a short integer constant
    //
    // Parameters:
    //   number:
    //     The short integer constant
    //
    // Returns:
    //     The HarmonyLib.CodeInstruction
    public static CodeInstruction LoadConstant(sbyte number)
    {
        return number switch {
            -1 => new CodeInstruction(OpCodes.Ldc_I4_M1),
            0 => new CodeInstruction(OpCodes.Ldc_I4_0),
            1 => new CodeInstruction(OpCodes.Ldc_I4_1),
            2 => new CodeInstruction(OpCodes.Ldc_I4_2),
            3 => new CodeInstruction(OpCodes.Ldc_I4_3),
            4 => new CodeInstruction(OpCodes.Ldc_I4_4),
            5 => new CodeInstruction(OpCodes.Ldc_I4_5),
            6 => new CodeInstruction(OpCodes.Ldc_I4_6),
            7 => new CodeInstruction(OpCodes.Ldc_I4_7),
            8 => new CodeInstruction(OpCodes.Ldc_I4_8),

            _ => new CodeInstruction(OpCodes.Ldc_I4_S, number),
        };
    }

    //
    // Summary:
    //     Returns a code instruction that loads an integer constant
    //
    // Parameters:
    //   number:
    //     The integer constant
    //
    // Returns:
    //     The HarmonyLib.CodeInstruction
    public static CodeInstruction LoadConstant(int number)
    {
        return number switch {
            >= sbyte.MinValue and <= sbyte.MaxValue => LoadConstant(Convert.ToSByte(number)),
            _ => new CodeInstruction(OpCodes.Ldc_I4, number)
        };
    }

    //
    // Summary:
    //     Returns a code instruction that loads a long integer constant (less optimal than LoadLongOptimally)
    //
    // Parameters:
    //   number:
    //     The long integer constant
    //
    // Returns:
    //     The HarmonyLib.CodeInstruction
    public static CodeInstruction LoadConstant(long number)
    {
        return new CodeInstruction(OpCodes.Ldc_I8, number);
    }

    //
    // Summary:
    //     Returns a code instruction that loads an enum constant
    //
    // Parameters:
    //   e:
    //     The enum
    //
    // Returns:
    //     The HarmonyLib.CodeInstruction
    public static CodeInstruction LoadConstant(Enum e)
    {
        return e.GetTypeCode() switch
        {
            TypeCode.Int64 => LoadConstant(Convert.ToInt64(e)),
            TypeCode.SByte => LoadConstant(Convert.ToSByte(e)),
            _ => LoadConstant(Convert.ToInt32(e))
        };
    }

    //
    // Summary:
    //     Returns an array of code instructions that optimally loads a long integer constant
    //
    // Parameters:
    //   number:
    //     The long integer constant
    //
    // Returns:
    //     The array of HarmonyLib.CodeInstruction
    public static CodeInstruction[] LoadLongOptimally(long number)
    {
        return number switch {
            >= sbyte.MinValue and <= sbyte.MaxValue => [LoadConstant(Convert.ToSByte(number)), new CodeInstruction(OpCodes.Conv_I8)],
            >= int.MinValue and <= int.MaxValue => [LoadConstant(Convert.ToInt32(number)), new CodeInstruction(OpCodes.Conv_I8)],
            _ => [new CodeInstruction(OpCodes.Ldc_I8, number)]
        };
    }

    //
    // Summary:
    //     Returns an array of code instructions that optimally loads an enum constant
    //
    // Parameters:
    //   e:
    //     The enum constant
    //
    // Returns:
    //     The array of HarmonyLib.CodeInstruction
    public static CodeInstruction[] LoadEnumOptimally(Enum e)
    {
        return e.GetTypeCode() switch
        {
            TypeCode.Int64 => LoadLongOptimally(Convert.ToInt64(e)),
            TypeCode.SByte => [LoadConstant(Convert.ToSByte(e))],
            _ => [LoadConstant(Convert.ToInt32(e))]
        };
    }

    //
    // Summary:
    //     Returns a code instruction that loads a string constant
    //
    // Parameters:
    //   string:
    //     The string constant
    //
    // Returns:
    //     The HarmonyLib.CodeInstruction
    public static CodeInstruction LoadString(string str)
    {
        return new CodeInstruction(OpCodes.Ldstr, str);
    }

    //
    // Summary:
    //     Returns a code instruction that loads null
    //
    // Parameters:
    //
    // Returns:
    //     The HarmonyLib.CodeInstruction
    public static CodeInstruction LoadNull()
    {
        return new CodeInstruction(OpCodes.Ldnull);
    }

    /// <summary>Tests if the code instruction loads null</summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <returns>True if the instruction loads null</returns>
    ///
    public static bool LoadsNull(this CodeInstruction code)
    {
        return Equals(code.opcode, OpCodes.Ldnull);
    }

    // PROPERTIES

    /// <summary>Tests if the code instruction loads a property</summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <param name="property">The <see cref="PropertyInfo"/></param>
    /// <returns>True if the instruction loads the given property</returns>
    ///
    public static bool LoadsProperty(this CodeInstruction code, PropertyInfo property)
    {
        return code.Calls(property.GetGetMethod(true));
    }

    /// <summary>Tests if the code instruction loads a property</summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <param name="type">The <see cref="Type"/> to which the property belongs</param> 
    /// <param name="name">The name of the <see cref="PropertyInfo"/></param>
    /// <returns>True if the instruction loads a property with the given name</returns>
    ///
    public static bool LoadsProperty(this CodeInstruction code, Type type, string name)
    {
        PropertyInfo? property = AccessTools.DeclaredProperty(type, name);
        
        return property != null && LoadsProperty(code, property);
    }

    /// <summary>Tests if the code instruction stores a property</summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <param name="property">The <see cref="PropertyInfo"/></param>
    /// <returns>True if the instruction stores the given property</returns>
    ///
    public static bool StoresProperty(this CodeInstruction code, PropertyInfo property)
    {
        return code.Calls(property.GetSetMethod(true));
    }

    /// <summary>Tests if the code instruction stores a property</summary>
    /// <param name="code">The <see cref="CodeInstruction"/></param>
    /// <param name="type">The <see cref="Type"/> to which the property belongs</param> 
    /// <param name="name">The name of the <see cref="PropertyInfo"/></param>
    /// <returns>True if the instruction stores a property with the given name</returns>
    ///
    public static bool StoresProperty(this CodeInstruction code, Type type, string name)
    {
        PropertyInfo? property = AccessTools.DeclaredProperty(type, name);
        
        return property != null && StoresProperty(code, property);
    }
}