/* Derived from https://github.com/landonk89/Buffed-Presents/blob/main/Source/AccessExtensions.cs */

using System.Reflection;
using System;

using LogLevel = BepInEx.Logging.LogLevel;

namespace LC_GiftBox_Config.libs.ReflectionsAccess
{
    public static class ReflectionsAccess
    {
        //call nonpublic methods using reflection, ex. SomeClass.CallMethod("MethodInClass", param1, param2, ...);
        public static object CallMethod(this object methodHolder, string methodName, params object[] args)
        {
            var methodInfo = methodHolder.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (methodInfo == null)
            {
                throw new ArgumentException($"[libs.ReflectionsAccess.CallMethod] Couldn't call nonpublic method {methodHolder}.{methodName}");
            }

            Plugin.Log(LogLevel.Debug, $"[libs.ReflectionsAccess.CallMethod] Calling nonpublic method {methodHolder}.{methodName}");
            return methodInfo.Invoke(methodHolder, args);
        }

        //Get nonpublic fields, ex. someVar = GetFieldValue<SomeClass>(someClassInstance, "someField");
        //returns Value of Type T contained in the nonprivate field
        public static T GetFieldValue<T>(object fieldHolder, string fieldName)
        {
            FieldInfo fieldInfo = fieldHolder.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (fieldInfo == null)
            {
                throw new ArgumentException($"[libs.ReflectionsAccess.GetFieldValue] Field {fieldHolder}.{fieldName} not found!");
            }

            var fieldValue = (T)fieldInfo.GetValue(fieldHolder);
            
            Plugin.Log(LogLevel.Debug, $"[libs.ReflectionsAccess.GetFieldValue] Field {fieldHolder}.{fieldName} found, returning {fieldValue}");
            return fieldValue;
        }

        //Set nonpublic fields, ex. SetFieldValue(someClassInstance, "fieldToSet", valueToSet);
        public static void SetFieldValue(object fieldHolder, string fieldName, object fieldValue)
        {
            FieldInfo fieldInfo = fieldHolder.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                throw new ArgumentException($"[libs.ReflectionsAccess.SetFieldValue] Field {fieldHolder}.{fieldName} not found!");
            }

            Plugin.Log(LogLevel.Debug, $"[libs.ReflectionsAccess.SetFieldValue] Field {fieldHolder}.{fieldName} found, assigning to {fieldValue}");
            fieldInfo.SetValue(fieldHolder, fieldValue);
        }
    }
}
