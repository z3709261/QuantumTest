

#region quantum_unity/Assets/Photon/Quantum/Editor/Utils/AssetDatabaseExtensions.cs
namespace Quantum.Editor {
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using UnityEditor;
  using UnityEditorInternal;
  using UnityEngine;

  public static class AssetDatabaseExt {
    public static void DeleteNestedAsset(this Object parent, Object child) {
      // destroy child
      Object.DestroyImmediate(child, true);

      // set dirty
      EditorUtility.SetDirty(parent);

      // save
      AssetDatabase.SaveAssets();
    }

    public static void DeleteAllNestedAssets(this Object parent) {
      // get path of parent object
      var path = AssetDatabase.GetAssetPath(parent);

      // LoadAllAssetsAtPath() returns the parent asset AND all of its nested (chidren)
      var assets = AssetDatabase.LoadAllAssetsAtPath(path);
      foreach (var asset in assets) {

        // keep main (parent) asset
        if (AssetDatabase.IsMainAsset(asset))
          continue;

        // delete nested assets
        parent.DeleteNestedAsset(asset);
      }
    }

    public static Object CreateNestedScriptableObjectAsset(this Object parent, System.Type type, System.String name, HideFlags hideFlags = HideFlags.None) {
      // create new asset in memory
      Object asset;

      asset = ScriptableObject.CreateInstance(type);
      asset.name = name;
      asset.hideFlags = hideFlags;

      // add to parent asset
      AssetDatabase.AddObjectToAsset(asset, parent);

      // set dirty
      EditorUtility.SetDirty(parent);

      // save
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      return asset;
    }

    public static Object FindNestedObjectParent(this Object asset) {
      var assetPath = AssetDatabase.GetAssetPath(asset);
      if (string.IsNullOrEmpty(assetPath)) {
        return null;
      }

      return AssetDatabase.LoadMainAssetAtPath(assetPath);
    }

    public static int DeleteMissingNestedScriptableObjects(string path) {

      var yamlObjectHeader = new Regex("^--- !u!", RegexOptions.Multiline);
     
      // 114 - class id (see https://docs.unity3d.com/Manual/ClassIDReference.html)
      var monoBehaviourRegex = new Regex(@"^114 &(\d+)");

      // if a script is missing, then it will load as null
      List<long> validFileIds = new List<long>();
      foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
        if (asset == null)
          continue;

        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset.GetInstanceID(), out var guid, out long fileId)) {
          validFileIds.Add(fileId);
        }
      }

      var yamlObjects = yamlObjectHeader.Split(System.IO.File.ReadAllText(path)).ToList();

      // now remove all that's missing
      int initialCount = yamlObjects.Count;
      for (int i = 0; i < yamlObjects.Count; ++i) {
        var part = yamlObjects[i];
        var m = monoBehaviourRegex.Match(part);
        if (!m.Success)
          continue;

        var assetFileId = long.Parse(m.Groups[1].Value);
        if (!validFileIds.Remove(assetFileId)) {
          yamlObjects.RemoveAt(i--);
        }
      }

      Debug.Assert(initialCount >= yamlObjects.Count);
      if (initialCount != yamlObjects.Count) {
        System.IO.File.WriteAllText(path, string.Join("--- !u!", yamlObjects));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return initialCount - yamlObjects.Count;
      } else {
        return 0;
      }
    }

    [System.Serializable]
    private class AssemblyDefnitionSurrogate {
      public string[] references = new string[0];
    }

    public static string[] GetReferences(this AssemblyDefinitionAsset assemblyDefinition) {
      var json = assemblyDefinition.text;
      return JsonUtility.FromJson<AssemblyDefnitionSurrogate>(json).references;
    }

    public static void SetReferences(this AssemblyDefinitionAsset assemblyDefinition, string[] references) {
      var json = assemblyDefinition.text;

      string newReferencesString;
      if (references.Length > 0) {
        newReferencesString = string.Join(",", references.Select(x => $"\n        \"{x}\""));
      } else {
        newReferencesString = "";
      }

      var regex = new Regex(@"(""references""\s?:\s?\[).*?(\s*\])", RegexOptions.Singleline);

      if (regex.IsMatch(json)) {
        var fixedJson = regex.Replace(json, $"$1{newReferencesString}$2");
        var path = AssetDatabase.GetAssetPath(assemblyDefinition);
        if (string.IsNullOrEmpty(path)) {
          throw new System.ArgumentException("Not an asset", nameof(assemblyDefinition));
        }
        System.IO.File.WriteAllText(path, fixedJson);
        AssetDatabase.ImportAsset(path);
      } else {
        throw new System.NotImplementedException();
      }
    }

    public static void UpdateReferences(this AssemblyDefinitionAsset asmdef, IEnumerable<(string, string)> referencesToAdd, IEnumerable<(string, string)> referencesToRemove) {
      var existingReferences = asmdef.GetReferences().ToList();

      if (referencesToRemove != null) {
        foreach (var r in referencesToRemove) {
          existingReferences.Remove($"GUID:{r.Item1}");
          existingReferences.Remove(r.Item2);
        }
      }

      if (referencesToAdd != null) {
        foreach (var r in referencesToAdd) {
          existingReferences.Remove($"GUID:{r.Item1}");
          existingReferences.Remove(r.Item2);
        }

        // guess whether to use guids or names
        bool useGuids = true;
        if (existingReferences.FirstOrDefault()?.StartsWith("GUID:", System.StringComparison.OrdinalIgnoreCase) == false) {
          useGuids = false;
        }

        foreach (var r in referencesToAdd) {
          if (useGuids) {
            existingReferences.Add($"GUID:{r.Item1}");
          } else {
            existingReferences.Add(r.Item2);
          }
        }
      }

      asmdef.SetReferences(existingReferences.ToArray());
    }

    public static bool HasScriptingDefineSymbol(BuildTargetGroup group, string value) {
      var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';');
      return System.Array.IndexOf(defines, value) >= 0;
    }

    public static bool? HasScriptingDefineSymbol(string value) {
      bool anyDefined = false;
      bool anyUndefined = false;
      foreach (BuildTargetGroup group in ValidBuildTargetGroups) {
        if (HasScriptingDefineSymbol(group, value)) {
          anyDefined = true;
        } else {
          anyUndefined = true;
        }
      }

      return (anyDefined && anyUndefined) ? (bool?)null : anyDefined;
    }

    public static void UpdateScriptingDefineSymbol(BuildTargetGroup group, string define, bool enable) {
      UpdateScriptingDefineSymbolInternal(new[] { group },
        enable ? new[] { define } : null,
        enable ? null : new[] { define });
    }

    public static void UpdateScriptingDefineSymbol(string define, bool enable) {
      UpdateScriptingDefineSymbolInternal(ValidBuildTargetGroups,
        enable ? new[] { define } : null,
        enable ? null : new[] { define });
    }

    public static void UpdateScriptingDefineSymbol(BuildTargetGroup group, IEnumerable<string> definesToAdd, IEnumerable<string> definesToRemove) {
      UpdateScriptingDefineSymbolInternal(new[] { group },
        definesToAdd,
        definesToRemove);
    }

    public static void UpdateScriptingDefineSymbol(IEnumerable<string> definesToAdd, IEnumerable<string> definesToRemove) {
      UpdateScriptingDefineSymbolInternal(ValidBuildTargetGroups,
        definesToAdd,
        definesToRemove);
    }

    private static void UpdateScriptingDefineSymbolInternal(IEnumerable<BuildTargetGroup> groups, IEnumerable<string> definesToAdd, IEnumerable<string> definesToRemove) {
      EditorApplication.LockReloadAssemblies();
      try {
        foreach (var group in groups) {
          var originalDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
          var defines = originalDefines.Split(';').ToList();

          if (definesToRemove != null) {
            foreach (var d in definesToRemove) {
              defines.Remove(d);
            }
          }

          if (definesToAdd != null) {
            foreach (var d in definesToAdd) {
              defines.Remove(d);
              defines.Add(d);
            }
          }

          var newDefines = string.Join(";", defines);
          PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefines);
        }
      } finally {
        EditorApplication.UnlockReloadAssemblies();
      }
    }

    private static bool IsEnumValueObsolete<T>(string valueName) where T : System.Enum {
      var fi = typeof(T).GetField(valueName);
      var attributes = fi.GetCustomAttributes(typeof(System.ObsoleteAttribute), false);
      return attributes?.Length > 0;
    }

    private static IEnumerable<BuildTargetGroup> ValidBuildTargetGroups {
      get {
        foreach (var name in System.Enum.GetNames(typeof(BuildTargetGroup))) {
          if (IsEnumValueObsolete<BuildTargetGroup>(name))
            continue;
          var group = (BuildTargetGroup)System.Enum.Parse(typeof(BuildTargetGroup), name);
          if (group == BuildTargetGroup.Unknown)
            continue;

          yield return group;
        }
      }
    }
  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Utils/BinarySerializer.cs
namespace Quantum.Editor {
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Text;
  using UnityEngine;

  public class BinarySerializer : IDisposable {

    private readonly BinaryWriterEx _writer;
    private readonly BinaryReaderEx _reader;

    public delegate void ElementSerializer<T>(BinarySerializer serializer, ref T element);

    private sealed class BinaryWriterEx : BinaryWriter {
      public BinaryWriterEx(Stream stream, System.Text.Encoding encoding, bool leaveOpen) : base(stream, encoding, leaveOpen) { }

      public void Write7BitEncoded(int value) => base.Write7BitEncodedInt(value);
      public void Write7BitEncoded(long value) {
        ulong num;
        for (num = (ulong)value; num >= 128; num >>= 7) {
          Write((byte)(num | 0x80));
        }
        Write((byte)num);
      }
    }

    private sealed class BinaryReaderEx : BinaryReader {
      public BinaryReaderEx(Stream stream, System.Text.Encoding encoding, bool leaveOpen) : base(stream, encoding, leaveOpen) { }

      public int Read7BitEncodedInt32() => base.Read7BitEncodedInt();

      public long Read7BitEncodedInt64() {
        long num = 0;
        int num2 = 0;
        byte b;
        do {
          if (num2 == 70) {
            throw new InvalidOperationException();
          }
          b = ReadByte();
          num |= ((long)(b & 0x7F)) << num2;
          num2 += 7;
        }
        while ((b & 0x80) != 0);
        return num;
      }
    }

    public BinarySerializer(Stream stream, bool writing, Encoding encoding, bool leaveOpen) {
      if (writing) {
        _writer = new BinaryWriterEx(stream, encoding, leaveOpen);
      } else {
        _reader = new BinaryReaderEx(stream, encoding, leaveOpen);
      }
    }

    public BinarySerializer(Stream stream, bool writing) : this(stream, writing, Encoding.Default, false) { }

    public bool IsWriting => _writer != null;
    public bool IsReading => !IsWriting;

    public void Dispose() {
      if (_writer != null) {
        _writer.Dispose();
      } else {
        _reader.Dispose();
      }
    }

    public void Serialize(ref byte value) {
      if (_writer != null) {
        _writer.Write(value);
      } else {
        value = _reader.ReadByte();
      }
    }

    public void Serialize(ref bool value) {
      if (_writer != null) {
        _writer.Write(value);
      } else {
        value = _reader.ReadBoolean();
      }
    }

    public void Serialize(ref float value) {
      if (_writer != null) {
        _writer.Write(value);
      } else {
        value = _reader.ReadSingle();
      }
    }

    public void Serialize(ref Color value) {
      Serialize(ref value.r);
      Serialize(ref value.g);
      Serialize(ref value.b);
      Serialize(ref value.a);
    }



    public void Serialize(ref string value) {
      if (_writer != null) {
        _writer.Write(value);
      } else {
        value = _reader.ReadString();
      }
    }

    public void Serialize(ref int value) {
      if (_writer != null) {
        _writer.Write(value);
      } else {
        value = _reader.ReadInt32();
      }
    }

    public void Serialize(ref long value) {
      if (_writer != null) {
        _writer.Write(value);
      } else {
        value = _reader.ReadInt64();
      }
    }

    public void Serialize(ref byte[] value) {
      if (_writer != null) {
        _writer.Write7BitEncoded(value.Length);
        _writer.Write(value);
      } else {
        int count = _reader.Read7BitEncodedInt32();
        value = _reader.ReadBytes(count);
      }
    }

    public void Serialize<T>(ref T value, Func<T, int> toInt, Func<int, T> fromInt) {
      if (_writer != null) {
        _writer.Write(toInt(value));
      } else {
        value = fromInt(_reader.ReadInt32());
      }
    }

    public void Serialize<T>(ref T value, Func<T, byte[]> toBytes, Func<byte[], T> fromBytes) {
      if (_writer != null) {
        var bytes = toBytes(value);
        _writer.Write7BitEncoded(bytes.Length);
        _writer.Write(bytes);
      } else {
        int count = _reader.Read7BitEncodedInt32();
        var bytes = _reader.ReadBytes(count);
        value = fromBytes(bytes);
      }
    }

    public void Serialize7BitEncoded(ref int value) {
      if (_writer != null) {
        _writer.Write7BitEncoded(value);
      } else {
        value = _reader.Read7BitEncodedInt32();
      }
    }

    public void Serialize7BitEncoded(ref long value) {
      if (_writer != null) {
        _writer.Write7BitEncoded(value);
      } else {
        value = _reader.Read7BitEncodedInt64();
      }
    }

    public void SerializeList<T>(ref List<T> list, ElementSerializer<T> serializer) where T : new() {
      if (_writer != null) {
        _writer.Write(list != null ? list.Count : 0);
        if (list != null) {
          for (int i = 0; i < list.Count; ++i) {
            var element = list[i];
            serializer(this, ref element);
          }
        }
      } else {

        if (list == null) {
          list = new List<T>();
        } else {
          list.Clear();
        }

        var count = _reader.ReadInt32();
        list.Capacity = count;

        for (int i = 0; i < count; ++i) {
          var element = new T();
          serializer(this, ref element);
          list.Add(element);
        }
      }
    }
  }
}



#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Utils/GUIStyleExtensions.cs
namespace Quantum.Editor {
  using System;
  using UnityEngine;

  public static class GUIStyleExtensions {

    public static IDisposable ContentOffsetScope(this GUIStyle style, Vector2 offset) {
      var result = new DisposableAction<Vector2>(x => style.contentOffset = x, style.contentOffset);
      style.contentOffset = offset;
      return result;
    }

    public static IDisposable ContentOffsetScope(this GUIStyle style, float x) => ContentOffsetScope(style, new Vector2(x, 0));

    public static IDisposable FontStyleScope(this GUIStyle style, FontStyle fontStyle) {
      if (style.fontStyle == fontStyle) {
        return null;
      }

      var result = new DisposableAction<FontStyle>(x => style.fontStyle = x, style.fontStyle);
      style.fontStyle = fontStyle;
      return result;
    }

    public static IDisposable FontStyleScope(this GUIStyle style, bool italic = false, bool bold = false) {
      FontStyle fontStyle;
      if (italic) {
        fontStyle = bold ? FontStyle.BoldAndItalic : FontStyle.Italic;
      } else {
        fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
      }

      return FontStyleScope(style, fontStyle);
    }

    public static IDisposable WordWrapScope(this GUIStyle style, bool wordWrap) {
      var result = new DisposableAction<bool>(x => style.wordWrap = x, style.wordWrap);
      style.wordWrap = wordWrap;
      return result;
    }

    public static IDisposable MarginScope(GUIStyle style, RectOffset margin) {
      var result = new DisposableAction<RectOffset>(x => style.margin = x, style.margin);
      style.margin = margin;
      return result;
    }

    public static IDisposable MarginScope(this GUIStyle style, int margin) => MarginScope(style, new RectOffset(margin, margin, margin, margin));

    private sealed class DisposableAction<T> : IDisposable {
      private T oldValue;
      private Action<T> setter;

      public DisposableAction(Action<T> setter, T oldValue) {
        this.oldValue = oldValue;
        this.setter = setter;
      }

      void IDisposable.Dispose() {
        setter(oldValue);
      }
    }
  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Utils/ReplayMenu.cs
namespace Quantum.Editor {
  using System;
  using System.IO;
  using UnityEditor;
  using UnityEngine;

  public class ReplayMenu {
    private static string ReplayLocation {
      get => EditorPrefs.GetString("Quantum_Export_LastReplayLocation");
      set => EditorPrefs.SetString("Quantum_Export_LastReplayLocation", value);
    }

    private static string SavegameLocation {
      get => EditorPrefs.GetString("Quantum_Export_LastSavegameLocation");
      set => EditorPrefs.SetString("Quantum_Export_LastSavegameLocation", value);
    }

    [MenuItem("Quantum/Export/Replay (JSON) %#r", true, 3)]
    public static bool ExportReplayAndDBToJSONCheck() {
      return Application.isPlaying;
    }

    [MenuItem("Quantum/Export/Replay (JSON) %#r", false, 3)]
    public static void ExportReplayAndDBToJSON() {
      ExportDialogReplayAndDB(QuantumRunner.Default.Game, new QuantumUnityJsonSerializer(), ".json");
    }

    [MenuItem("Quantum/Export/Savegame (JSON)", true, 3)]
    public static bool SaveGameCheck() {
      return Application.isPlaying;
    }

    [MenuItem("Quantum/Export/Savegame (JSON)", false, 3)]
    public static void SaveGame() {
      ExportDialogSavegame(QuantumRunner.Default.Game, new QuantumUnityJsonSerializer(), ".json");
    }

    // TODO: add interface for replay serializer
    public static void ExportDialogReplayAndDB(QuantumGame game, QuantumUnityJsonSerializer serializer, string ext) {
      Assert.Check(serializer != null, "Serializer is invalid");

      var defaultName = "replay";
      var directory = Application.dataPath;
      if (!string.IsNullOrEmpty(ReplayLocation)) {
        defaultName = Path.GetFileName(ReplayLocation);
        directory   = ReplayLocation.Remove(ReplayLocation.IndexOf(defaultName, StringComparison.Ordinal));
      }

      var folderPath = EditorUtility.SaveFolderPanel("Save replay and db to..", directory, defaultName);
      if (!string.IsNullOrEmpty(folderPath)) {

        var replay = game.GetRecordedReplay();
        Debug.Assert(replay != null);

        File.WriteAllBytes(Path.Combine(folderPath, "replay" + ext),  serializer.SerializeReplay(replay));
        File.WriteAllBytes(Path.Combine(folderPath, "db" + ext), serializer.SerializeAssets(UnityDB.DefaultResourceManager.LoadAllAssets(true)));

        if (game.RecordedChecksums != null) {
          File.WriteAllBytes(Path.Combine(folderPath, "checksum" + ext), serializer.SerializeChecksum(game.RecordedChecksums));
        }

        Debug.Log("Saved replay to " + folderPath);
        AssetDatabase.Refresh();

        ReplayLocation = folderPath;
      }
    }

    public static void ExportDialogSavegame(QuantumGame game, QuantumUnityJsonSerializer serializer, string ext) {
      Assert.Check(serializer != null, "Serializer is invalid");

      var defaultName = "savegame";
      var directory = Application.dataPath;
      if (!string.IsNullOrEmpty(SavegameLocation)) {
        defaultName = Path.GetFileName(SavegameLocation);
        directory = SavegameLocation.Remove(SavegameLocation.IndexOf(defaultName, StringComparison.Ordinal));
      }

      var folderPath = EditorUtility.SaveFolderPanel("Save game to..", directory, defaultName);
      if (!string.IsNullOrEmpty(folderPath)) {
        var replay = game.CreateSavegame();
        File.WriteAllBytes(Path.Combine(folderPath, "savegame" + ext), serializer.SerializeReplay(replay));
        File.WriteAllBytes(Path.Combine(folderPath, "db" + ext),  serializer.SerializeAssets(UnityDB.DefaultResourceManager.LoadAllAssets(true)));

        Debug.Log("Saved game to " + folderPath);
        AssetDatabase.Refresh();
      }

      SavegameLocation = folderPath;
    }
  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Utils/UnityInternal.cs
namespace Quantum.Editor {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using UnityEditor;
  using UnityEngine;

  public static class UnityInternal {

    public sealed class InternalStyles {
      public readonly GUIStyle InspectorTitlebar = GetStyle("IN Title");
      public readonly GUIStyle FoldoutTitlebar = GetStyle("Titlebar Foldout", "Foldout");
      public readonly GUIStyle BoxWithBorders = GetStyle("OL Box");
      public readonly GUIStyle HierarchyTreeViewLine = GetStyle("TV Line");
      public readonly GUIStyle HierarchyTreeViewSceneBackground = GetStyle("SceneTopBarBg", "ProjectBrowserTopBarBg");
      public readonly GUIStyle OptionsButtonStyle = GetStyle("PaneOptions");
      public readonly GUIStyle AddComponentButton = GetStyle("AC Button");
      public readonly GUIStyle AnimationEventTooltip = GetStyle("AnimationEventTooltip");
      public readonly GUIStyle AnimationEventTooltipArrow = GetStyle("AnimationEventTooltipArrow");



      private static GUIStyle GetStyle(params string[] names) {
        var skin = GUI.skin;

        foreach (var name in names) {
          var result = skin.FindStyle(name);
          if (result != null) {
            return result;
          }
        }

        throw new ArgumentOutOfRangeException($"Style not found: {string.Join(", ", names)}", nameof(names));
      }
    }

    private static class InternalStylesHolder {
      public static InternalStyles Instance = new InternalStyles();
    }

    public static InternalStyles Styles => InternalStylesHolder.Instance;

    [InitializeOnLoad]
    public static class Editor {
      public delegate void BoolSetterDelegate(UnityEditor.Editor editor, bool value);
      public static readonly BoolSetterDelegate InternalSetHidden = typeof(UnityEditor.Editor).CreateMethodDelegate<BoolSetterDelegate>("InternalSetHidden", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [InitializeOnLoad]
    public static class ScriptAttributeUtility {

      private static readonly Type InternalType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility", true);

      public delegate FieldInfo GetFieldInfoFromPropertyDelegate(SerializedProperty property, out Type type);

      public static readonly GetFieldInfoFromPropertyDelegate GetFieldInfoFromProperty =
        Quantum.ReflectionUtils.CreateEditorMethodDelegate<GetFieldInfoFromPropertyDelegate>(
        "UnityEditor.ScriptAttributeUtility",
        "GetFieldInfoFromProperty",
        BindingFlags.Static | BindingFlags.NonPublic);

      public delegate Type GetDrawerTypeForTypeDelegate(Type type);

      public static readonly GetDrawerTypeForTypeDelegate GetDrawerTypeForType =
        Quantum.ReflectionUtils.CreateEditorMethodDelegate<GetDrawerTypeForTypeDelegate>(
        "UnityEditor.ScriptAttributeUtility",
        "GetDrawerTypeForType",
        BindingFlags.Static | BindingFlags.NonPublic);

      private static readonly Delegate _GetHandler = ReflectionUtils.CreateMethodDelegate(InternalType, "GetHandler", BindingFlags.NonPublic | BindingFlags.Static,
        ReflectionUtils.MakeFuncType(typeof(SerializedProperty), PropertyHandler.InternalType)
      );




      internal static readonly PropertyInfo _propertyHandlerCache = InternalType.GetPropertyOrThrow("propertyHandlerCache", PropertyHandlerCache.InternalType);
      public static PropertyHandlerCache propertyHandlerCache => new PropertyHandlerCache { internalObject = _propertyHandlerCache.GetValue(null) };

      private static readonly FieldInfo s_SharedNullHandler = InternalType.GetFieldOrThrow("s_SharedNullHandler", PropertyHandler.InternalType);
      public static PropertyHandler sharedNullHandler => new PropertyHandler(s_SharedNullHandler.GetValue(null));
      public static PropertyHandler GetHandler(SerializedProperty property) => new PropertyHandler(_GetHandler.DynamicInvoke(property));
    }

    public struct PropertyHandler : IEquatable<PropertyHandler> {
      public static readonly Type InternalType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.PropertyHandler", true);

      private static readonly Delegate _OnGUI = ReflectionUtils.CreateMethodDelegate(InternalType, "OnGUI", BindingFlags.Public | BindingFlags.Instance,
        ReflectionUtils.MakeFuncType(InternalType, typeof(Rect), typeof(SerializedProperty), typeof(GUIContent), typeof(bool), typeof(bool))
      );

      private static readonly Delegate _GetHeight = ReflectionUtils.CreateMethodDelegate(InternalType, "GetHeight", BindingFlags.Public | BindingFlags.Instance,
        ReflectionUtils.MakeFuncType(InternalType, typeof(SerializedProperty), typeof(GUIContent), typeof(bool), typeof(float))
      );

      private static readonly Delegate _HandleAttribute = ReflectionUtils.CreateMethodDelegate(InternalType, "HandleAttribute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
        ReflectionUtils.MakeActionType(InternalType,
#if UNITY_2019_3_OR_NEWER
          typeof(UnityEditor.SerializedProperty),
#endif
          typeof(PropertyAttribute), typeof(FieldInfo), typeof(Type)));


      private static readonly PropertyInfo _empty = InternalType.GetPropertyOrThrow<bool>(nameof(empty));
      
      private static readonly FieldInfo m_DecoratorDrawers = InternalType.GetFieldOrThrow<List<DecoratorDrawer>>(nameof(m_DecoratorDrawers));
      private static readonly FieldInfo _tooltip = InternalType.GetFieldOrThrow<string>(nameof(tooltip));
      private static readonly PropertyInfo _isCurrentlyNested = InternalType.GetPropertyOrThrow<bool>(nameof(isCurrentlyNested));

#if UNITY_2021_1_OR_NEWER
      private static readonly PropertyInfo _propertyDrawer = InternalType.GetPropertyOrThrow<UnityEditor.PropertyDrawer>("propertyDrawer");
      private static readonly FieldInfo m_NestingLevel = InternalType.GetFieldOrThrow<int>(nameof(m_NestingLevel));
      private static readonly FieldInfo m_PropertyDrawers = InternalType.GetFieldOrThrow<List<UnityEditor.PropertyDrawer>>(nameof(m_PropertyDrawers));
#else
      private static readonly FieldInfo m_PropertyDrawer = InternalType.GetFieldOrThrow<UnityEditor.PropertyDrawer>(nameof(m_PropertyDrawer));
#endif

      public object internalObject;


      public PropertyHandler(object instance) {
        internalObject = instance;
      }


      public void HandleAttribute(UnityEditor.SerializedProperty property, UnityEngine.PropertyAttribute attribute, FieldInfo field, Type propertyType) {
        _HandleAttribute.DynamicInvoke(internalObject,
#if UNITY_2019_3_OR_NEWER
          property,
#endif
          attribute, field, propertyType);
      }


      public bool OnGUI(Rect position, SerializedProperty property, GUIContent label, bool includeChildren) {
        return (bool)_OnGUI.DynamicInvoke(internalObject, position, property, label, includeChildren);
      }

      public float GetHeight(SerializedProperty property, GUIContent label, bool includeChildren) {
        return (float)_GetHeight.DynamicInvoke(internalObject, property, label, includeChildren);
      }

      public bool isCurrentlyNested => (bool)_isCurrentlyNested.GetValue(internalObject);

#if UNITY_2021_1_OR_NEWER
      public UnityEditor.PropertyDrawer currentPropertyDrawer => (UnityEditor.PropertyDrawer)_propertyDrawer.GetValue(internalObject);

      public int nestingLevel {
        get => (int)m_NestingLevel.GetValue(internalObject);
        set => m_NestingLevel.SetValue(internalObject, value);
      }

      public bool HasPropertyDrawer<T>() where T : UnityEditor.PropertyDrawer {
        var list = (List<UnityEditor.PropertyDrawer>)m_PropertyDrawers.GetValue(internalObject);
        return list?.OfType<T>().Any() ?? false;
      }
#else
      public bool HasPropertyDrawer<T>() where T : UnityEditor.PropertyDrawer => m_PropertyDrawer.GetValue(internalObject) is T;
      public UnityEditor.PropertyDrawer currentPropertyDrawer => isCurrentlyNested ? null : (UnityEditor.PropertyDrawer)m_PropertyDrawer.GetValue(internalObject);
      public int nestingLevel {
        get => 0;
        set { }
      }
#endif

      public List<UnityEditor.DecoratorDrawer> decoratorDrawers {
        get => (List<UnityEditor.DecoratorDrawer>)m_DecoratorDrawers.GetValue(internalObject);
        set => m_DecoratorDrawers.SetValue(internalObject, value);
      }
      public string tooltip {
        get => (string)_tooltip.GetValue(internalObject);
        set => _tooltip.SetValue(internalObject, value);
      }

      public bool empty {
        get => (bool)_empty.GetValue(internalObject);
      }

      public override bool Equals(object other) {
        if (other is PropertyHandler otherHandler) {
          return otherHandler.internalObject == internalObject;
        }
        return false;
      }

      public override int GetHashCode() {
        return internalObject?.GetHashCode() ?? 0;
      }

      public bool Equals(PropertyHandler other) {
        return other.internalObject == internalObject;
      }

      public static implicit operator bool(PropertyHandler handler) {
        return handler.internalObject != null;
      }

      public static PropertyHandler New() {
        return new PropertyHandler(Activator.CreateInstance(InternalType));
      }

      public static bool operator ==(PropertyHandler a, PropertyHandler b) {
        return a.internalObject == b.internalObject;
      }

      public static bool operator !=(PropertyHandler a, PropertyHandler b) {
        return a.internalObject != b.internalObject;
      }
    }

    public struct PropertyHandlerCache {
      public static readonly Type InternalType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.PropertyHandlerCache", true);

      public delegate int GetPropertyHashDelegate(UnityEditor.SerializedProperty property);
      public static readonly GetPropertyHashDelegate GetPropertyHash = InternalType.CreateMethodDelegate<GetPropertyHashDelegate>(nameof(GetPropertyHash));

      public static readonly Delegate _GetHandler = InternalType.CreateMethodDelegate(nameof(GetHandler),
        BindingFlags.NonPublic | BindingFlags.Instance,
        ReflectionUtils.MakeFuncType(InternalType, typeof(SerializedProperty), PropertyHandler.InternalType)
      );

      public static readonly Delegate _SetHandler = InternalType.CreateMethodDelegate(nameof(SetHandler),
        BindingFlags.NonPublic | BindingFlags.Instance,
        ReflectionUtils.MakeActionType(InternalType, typeof(SerializedProperty), PropertyHandler.InternalType)
      );

      public object internalObject;

      public PropertyHandler GetHandler(SerializedProperty property) {
        return new PropertyHandler(_GetHandler.DynamicInvoke(internalObject, property));
      }

      public void SetHandler(SerializedProperty property, PropertyHandler newHandler) {
        _SetHandler.DynamicInvoke(internalObject, property, newHandler.internalObject);
      }
    }

    [InitializeOnLoad]
    public static class PropertyDrawer {
      public delegate float GetPropertyHeightSafeDelegate(UnityEditor.PropertyDrawer drawer, UnityEditor.SerializedProperty property, GUIContent label);
      public static readonly GetPropertyHeightSafeDelegate GetPropertyHeightSafe = ReflectionUtils.CreateMethodDelegate<GetPropertyHeightSafeDelegate>(typeof(UnityEditor.PropertyDrawer), nameof(GetPropertyHeightSafe), BindingFlags.Instance | BindingFlags.NonPublic);

      public delegate void OnGUISafeDelegate(UnityEditor.PropertyDrawer drawer, Rect rect, UnityEditor.SerializedProperty property, GUIContent label);
      public static readonly OnGUISafeDelegate OnGUISafe = ReflectionUtils.CreateMethodDelegate<OnGUISafeDelegate>(typeof(UnityEditor.PropertyDrawer), nameof(OnGUISafe), BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [InitializeOnLoad]
    public static class EditorGUI {
      public delegate Rect MultiFieldPrefixLabelDelegate(Rect totalPosition, int id, GUIContent label, int columns);
      public static readonly MultiFieldPrefixLabelDelegate MultiFieldPrefixLabel = typeof(UnityEditor.EditorGUI).CreateMethodDelegate<MultiFieldPrefixLabelDelegate>("MultiFieldPrefixLabel");

      public delegate string ToolbarSearchFieldDelegate(int id, Rect position, string text, bool showWithPopupArrow);
      public static readonly ToolbarSearchFieldDelegate ToolbarSearchField = typeof(UnityEditor.EditorGUI).CreateMethodDelegate<ToolbarSearchFieldDelegate>("ToolbarSearchField");

      public delegate string TextFieldInternalDelegate(int id, Rect position, string text, GUIStyle style);
      public static readonly TextFieldInternalDelegate TextFieldInternal = typeof(UnityEditor.EditorGUI).CreateMethodDelegate<TextFieldInternalDelegate>("TextFieldInternal");

      private static readonly FieldInfo s_TextFieldHash = typeof(UnityEditor.EditorGUI).GetFieldOrThrow(nameof(s_TextFieldHash));
      public static int TextFieldHash => (int)s_TextFieldHash.GetValue(null);

      public delegate bool HasVisibleChildFieldsDelegate(UnityEditor.SerializedProperty property
#if UNITY_2020_2_OR_NEWER
          , bool uiElements = false
#endif
        );
      public static readonly HasVisibleChildFieldsDelegate HasVisibleChildFields = typeof(UnityEditor.EditorGUI).CreateMethodDelegate<HasVisibleChildFieldsDelegate>("HasVisibleChildFields");

      public delegate bool DefaultPropertyFieldDelegate(Rect position, UnityEditor.SerializedProperty property, GUIContent label);
      public static readonly DefaultPropertyFieldDelegate DefaultPropertyField = typeof(UnityEditor.EditorGUI).CreateMethodDelegate<DefaultPropertyFieldDelegate>("DefaultPropertyField");
    }

    [InitializeOnLoad]
    public static class EditorGUIUtility {
      public delegate UnityEngine.Object GetScriptDelegate(string scriptClass);
      public readonly static GetScriptDelegate GetScript = typeof(UnityEditor.EditorGUIUtility).CreateMethodDelegate<GetScriptDelegate>(nameof(GetScript));


      public delegate Texture2D GetIconForObjectDelegate(UnityEngine.Object obj);
      public readonly static GetIconForObjectDelegate GetIconForObject = typeof(UnityEditor.EditorGUIUtility).CreateMethodDelegate<GetIconForObjectDelegate>(nameof(GetIconForObject));


      public delegate GUIContent TempContentDelegate(string text);
      public readonly static TempContentDelegate TempContent = typeof(UnityEditor.EditorGUIUtility).CreateMethodDelegate<TempContentDelegate>(nameof(TempContent));
    }

    [InitializeOnLoad]
    public static class EditorUtility {
      public delegate void DisplayCustomMenuDelegate(Rect position, string[] options, int[] selected, UnityEditor.EditorUtility.SelectMenuItemFunction callback, object userData);

      public static DisplayCustomMenuDelegate DisplayCustomMenu =
        ReflectionUtils.CreateMethodDelegate<DisplayCustomMenuDelegate>(typeof(UnityEditor.EditorUtility), nameof(DisplayCustomMenu), BindingFlags.NonPublic | BindingFlags.Static);
    }

    [InitializeOnLoad]
    public static class HandleUtility {
      public static readonly Action ApplyWireMaterial = typeof(UnityEditor.HandleUtility).CreateMethodDelegate<Action>(nameof(ApplyWireMaterial));
    }

    [InitializeOnLoad]
    public static class LayerMatrixGUI {

      private static readonly Type InternalType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LayerMatrixGUI", true);
      private static readonly Type InternalGetValueFuncType = InternalType.GetNestedTypeOrThrow(nameof(GetValueFunc), BindingFlags.Public);
      private static readonly Type InternalSetValueFuncType = InternalType.GetNestedTypeOrThrow(nameof(SetValueFunc), BindingFlags.Public);

      private const bool _doGUIUsesGUIContent =
#if UNITY_2020_2_OR_NEWER && !UNITY_2020_2_0 && !UNITY_2020_2_1
          true;
#else
          false;
#endif

      private static readonly Delegate _doGUI = ReflectionUtils.CreateMethodDelegate(InternalType, nameof(DoGUI), BindingFlags.Public | BindingFlags.Static,
        typeof(Ref2Action<,,,>).MakeGenericType(
          _doGUIUsesGUIContent ? typeof(GUIContent) : typeof(string),
          typeof(bool), InternalGetValueFuncType, InternalSetValueFuncType)
      );

      private delegate void Ref2Action<T1, T2, T3, T4>(T1 t1, ref T2 t2, T3 t3, T4 t4);

      public delegate bool GetValueFunc(int layerA, int layerB);
      public delegate void SetValueFunc(int layerA, int layerB, bool val);

      public static void DoGUI(string title, ref bool show, GetValueFunc getValue, SetValueFunc setValue) {
        var getter = Delegate.CreateDelegate(InternalGetValueFuncType, getValue.Target, getValue.Method);
        var setter = Delegate.CreateDelegate(InternalSetValueFuncType, setValue.Target, setValue.Method);

        var args = new object[] { title, show, getter, setter };
        if (_doGUIUsesGUIContent) {
#pragma warning disable CS0162 // Unreachable code detected
          args[0] = new GUIContent(title);
#pragma warning restore CS0162 // Unreachable code detected
        }

        _doGUI.DynamicInvoke(args);
        show = (bool)args[1];
      }
    }

    [InitializeOnLoad]
    public static class ReorderableList {
      public static readonly FieldInfo m_SerializedObject = typeof(UnityEditorInternal.ReorderableList).GetFieldOrThrow(nameof(m_SerializedObject));

      public static SerializedObject GetSerializedObject(UnityEditorInternal.ReorderableList list) => (SerializedObject)m_SerializedObject.GetValue(list);
      public static void SetSerializedObject(UnityEditorInternal.ReorderableList list, SerializedObject obj) => m_SerializedObject.SetValue(list, obj);
    }


    [InitializeOnLoad]
    public static class SplitterGUILayout {
      public static readonly Action EndHorizontalSplit = ReflectionUtils.CreateMethodDelegate<Action>(typeof(UnityEditor.Editor).Assembly,
        "UnityEditor.SplitterGUILayout", "EndHorizontalSplit", BindingFlags.Public | BindingFlags.Static
      );

      public static readonly Action EndVerticalSplit = ReflectionUtils.CreateMethodDelegate<Action>(typeof(UnityEditor.Editor).Assembly,
        "UnityEditor.SplitterGUILayout", "EndVerticalSplit", BindingFlags.Public | BindingFlags.Static
      );

      public static void BeginHorizontalSplit(SplitterState splitterState, GUIStyle style, params GUILayoutOption[] options) {
        _beginHorizontalSplit.DynamicInvoke(splitterState.InternalState, style, options);
      }

      public static void BeginVerticalSplit(SplitterState splitterState, GUIStyle style, params GUILayoutOption[] options) {
        _beginVerticalSplit.DynamicInvoke(splitterState.InternalState, style, options);
      }

      private static readonly Delegate _beginHorizontalSplit = ReflectionUtils.CreateMethodDelegate(typeof(UnityEditor.Editor).Assembly,
        "UnityEditor.SplitterGUILayout", "BeginHorizontalSplit", BindingFlags.Public | BindingFlags.Static,
        typeof(Action<,,>).MakeGenericType(SplitterState.InternalType, typeof(GUIStyle), typeof(GUILayoutOption[]))
      );

      private static readonly Delegate _beginVerticalSplit = ReflectionUtils.CreateMethodDelegate(typeof(UnityEditor.Editor).Assembly,
        "UnityEditor.SplitterGUILayout", "BeginVerticalSplit", BindingFlags.Public | BindingFlags.Static,
        typeof(Action<,,>).MakeGenericType(SplitterState.InternalType, typeof(GUIStyle), typeof(GUILayoutOption[]))
      );
    }

    [InitializeOnLoad]
    [Serializable]
    public class SplitterState : ISerializationCallbackReceiver {

      public static readonly Type InternalType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SplitterState", true);
      private static readonly FieldInfo _relativeSizes = InternalType.GetFieldOrThrow("relativeSizes");
      private static readonly FieldInfo _realSizes = InternalType.GetFieldOrThrow("realSizes");
      private static readonly FieldInfo _splitSize = InternalType.GetFieldOrThrow("splitSize");

      public string Json = "{}";

      [NonSerialized]
      public object InternalState = FromRelativeInner(new[] { 1.0f });

      void ISerializationCallbackReceiver.OnAfterDeserialize() {
        InternalState = JsonUtility.FromJson(Json, InternalType);
      }

      void ISerializationCallbackReceiver.OnBeforeSerialize() {
        Json = JsonUtility.ToJson(InternalState);
      }

      public static SplitterState FromRelative(float[] relativeSizes, int[] minSizes = null, int[] maxSizes = null, int splitSize = 0) {
        var result = new SplitterState();
        result.InternalState = FromRelativeInner(relativeSizes, minSizes, maxSizes, splitSize);
        return result;
      }


      private static object FromRelativeInner(float[] relativeSizes, int[] minSizes = null, int[] maxSizes = null, int splitSize = 0) {
        return Activator.CreateInstance(InternalType, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
          null,
          new object[] { relativeSizes, minSizes, maxSizes, splitSize },
          null, null);
      }

      public float[] realSizes => ConvertArray((Array)_realSizes.GetValue(InternalState));
      public float[] relativeSizes => ConvertArray((Array)_relativeSizes.GetValue(InternalState));
      public float splitSize => Convert.ToSingle(_splitSize.GetValue(InternalState));

      private static float[] ConvertArray(Array value) {
        float[] result = new float[value.Length];
        for (int i = 0; i < value.Length; ++i) {
          result[i] = Convert.ToSingle(value.GetValue(i));
        }
        return result;
      }
    }
  }
}
#endregion