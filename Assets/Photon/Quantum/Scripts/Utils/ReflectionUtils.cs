using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Quantum {

  public static class ReflectionUtils {
    public const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    public static Type GetUnityLeafType(this Type type) {
      if (type.HasElementType) {
        type = type.GetElementType();
      } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
        type = type.GetGenericArguments()[0];
      }
      return type;
    }


#if UNITY_EDITOR
    public static T CreateEditorMethodDelegate<T>(string editorAssemblyTypeName, string methodName, BindingFlags flags = DefaultBindingFlags) where T : Delegate {
      return CreateMethodDelegate<T>(typeof(UnityEditor.Editor).Assembly, editorAssemblyTypeName, methodName, flags);
    } 

    public static Delegate CreateEditorMethodDelegate(string editorAssemblyTypeName, string methodName, BindingFlags flags, Type delegateType) {
      return CreateMethodDelegate(typeof(UnityEditor.Editor).Assembly, editorAssemblyTypeName, methodName, flags, delegateType);
    }
#endif

    public static T CreateMethodDelegate<T>(this Type type, string methodName, BindingFlags flags = DefaultBindingFlags) where T : Delegate {
      try {
        return CreateMethodDelegateInternal<T>(type, methodName, flags);
      } catch (System.Exception ex) {
        throw new InvalidOperationException(CreateExceptionMessage<T>(type.Assembly, type.FullName, methodName, flags), ex);
      }
    }

    public static Delegate CreateMethodDelegate(this Type type, string methodName, BindingFlags flags, Type delegateType) {
      try {
        return CreateMethodDelegateInternal(type, methodName, flags, delegateType);
      } catch (System.Exception ex) {
        throw new InvalidOperationException(CreateExceptionMessage(type.Assembly, type.FullName, methodName, flags, delegateType), ex);
      }
    }

    public static T CreateMethodDelegate<T>(Assembly assembly, string typeName, string methodName, BindingFlags flags = DefaultBindingFlags) where T : Delegate {
      try {
        var type = assembly.GetType(typeName, true);
        return CreateMethodDelegateInternal<T>(type, methodName, flags);
      } catch (System.Exception ex) {
        throw new InvalidOperationException(CreateExceptionMessage<T>(assembly, typeName, methodName, flags), ex);
      }
    }

    public static Delegate CreateMethodDelegate(Assembly assembly, string typeName, string methodName, BindingFlags flags, Type delegateType) {
      try {
        var type = assembly.GetType(typeName, true);
        return CreateMethodDelegateInternal(type, methodName, flags, delegateType);
      } catch (System.Exception ex) {
        throw new InvalidOperationException(CreateExceptionMessage(assembly, typeName, methodName, flags, delegateType), ex);
      }
    }

    public static FieldInfo GetFieldOrThrow(this Type type, string fieldName, BindingFlags flags = DefaultBindingFlags) {
      var field = type.GetField(fieldName, flags);
      if (field == null) {
        throw new ArgumentOutOfRangeException(nameof(fieldName), CreateExceptionMessage(type.Assembly, type.FullName, fieldName, flags));
      }
      return field;
    }

    public static FieldInfo GetFieldOrThrow<T>(this Type type, string fieldName, BindingFlags flags = DefaultBindingFlags) {
      return GetFieldOrThrow(type, fieldName, typeof(T), flags);
    }


    public static FieldInfo GetFieldOrThrow(this Type type, string fieldName, Type fieldType, BindingFlags flags = DefaultBindingFlags) {
      var field = type.GetField(fieldName, flags);
      if (field == null) {
        throw new ArgumentOutOfRangeException(nameof(fieldName), CreateExceptionMessage(type.Assembly, type.FullName, fieldName, flags));
      }
      if ( field.FieldType != fieldType) {
        throw new InvalidProgramException($"Field {type.FullName}.{fieldName} is of type {field.FieldType}, not expected {fieldType}");
      }
      return field;
    }

    public static PropertyInfo GetPropertyOrThrow<T>(this Type type, string propertyName, BindingFlags flags = DefaultBindingFlags) {
      return GetPropertyOrThrow(type, propertyName, typeof(T), flags);
    }

    public static PropertyInfo GetPropertyOrThrow(this Type type, string propertyName, Type propertyType, BindingFlags flags = DefaultBindingFlags) {
      var property = type.GetProperty(propertyName, flags);
      if (property == null) {
        throw new ArgumentOutOfRangeException(nameof(propertyName), CreateExceptionMessage(type.Assembly, type.FullName, propertyName, flags));
      }
      if (property.PropertyType != propertyType) {
        throw new InvalidProgramException($"Property {type.FullName}.{propertyName} is of type {property.PropertyType}, not expected {propertyType}");
      }
      return property;
    }

    public static Type GetNestedTypeOrThrow(this Type type, string name, BindingFlags flags) {
      var result = type.GetNestedType(name, flags);
      if (result == null) {
        throw new ArgumentOutOfRangeException(nameof(name), CreateExceptionMessage(type.Assembly, type.FullName, name, flags));
      }
      return result;
    }

    private static string CreateExceptionMessage<T>(Assembly assembly, string typeName, string methodName, BindingFlags flags) {
      return CreateExceptionMessage(assembly, typeName, methodName, flags, typeof(T));
    }

    private static string CreateExceptionMessage(Assembly assembly, string typeName, string methodName, BindingFlags flags, Type delegateType) {
      return $"{assembly.FullName}.{typeName}.{methodName} with flags: {flags} and type: {delegateType}";
    }

    private static string CreateExceptionMessage(Assembly assembly, string typeName, string fieldName, BindingFlags flags) {
      return $"{assembly.FullName}.{typeName}.{fieldName} with flags: {flags}";
    }

    private static T CreateMethodDelegateInternal<T>(this Type type, string name, BindingFlags flags) where T : Delegate {
      return (T)CreateMethodDelegateInternal(type, name, flags, typeof(T));
    }


    private static Delegate CreateMethodDelegateInternal(this Type type, string name, BindingFlags flags, Type delegateType) {

      IEnumerable<ParameterInfo> delegateParamters = delegateType.GetMethod("Invoke").GetParameters();

      if (!flags.HasFlag(BindingFlags.Static)) {
        delegateParamters = delegateParamters.Skip(1);
      }
        
      var method = type.GetMethod(name, flags, null, delegateParamters.Select(x => x.ParameterType).ToArray(), null);

      if (method == null) {
        var methods = type.GetMethods(flags);
        throw new ArgumentOutOfRangeException(nameof(name), $"No method found matching name \"{name}\", " +
          $"signature \"{delegateType}\", " +
          $"flags \"{flags}\" and " +
          $"params: {string.Join(", ", delegateParamters)}" +
          $", candidates are\n: {(string.Join("\n", methods.Select(x => x.ToString())))}");
      }

      return System.Delegate.CreateDelegate(delegateType, null, method);
    }

    public static bool IsArrayOrList(this Type listType) {
      if (listType.IsArray) {
        return true;
      } else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>)) {
        return true;
      }
      return false;
    }

    public static Type GetArrayOrListElementType(this Type listType) {
      if (listType.IsArray) {
        return listType.GetElementType();
      } else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>)) {
        return listType.GetGenericArguments()[0];
      }
      return null;
    }

    public static Type MakeFuncType(params Type[] types) {
      return GetFuncType(types.Length).MakeGenericType(types);
    }

    private static Type GetFuncType(int argumentCount) {
      switch (argumentCount) {
        case 1: return typeof(Func<>);
        case 2: return typeof(Func<,>);
        case 3: return typeof(Func<,,>);
        case 4: return typeof(Func<,,,>);
        case 5: return typeof(Func<,,,,>);
        case 6: return typeof(Func<,,,,,>);
        default: throw new ArgumentOutOfRangeException(nameof(argumentCount));
      }
    }

    public static Type MakeActionType(params Type[] types) {
      if (types.Length == 0) return typeof(Action);
      return GetActionType(types.Length).MakeGenericType(types);
    }

    private static Type GetActionType(int argumentCount) {
      switch (argumentCount) {
        case 1: return typeof(Action<>);
        case 2: return typeof(Action<,>);
        case 3: return typeof(Action<,,>);
        case 4: return typeof(Action<,,,>);
        case 5: return typeof(Action<,,,,>);
        case 6: return typeof(Action<,,,,,>);
        default: throw new ArgumentOutOfRangeException(nameof(argumentCount));
      }
    }

  }
}
