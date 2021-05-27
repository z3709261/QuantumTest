using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Quantum.Editor {

  internal static class QuantumCodeIntegration {
    private const string AssemblyPath     = "Library/ScriptAssemblies/PhotonQuantumCode.dll";
    private const string CodeGenPath      = "codegen/quantum.codegen.host.exe";
    private const int CodegenTimeout      = 10000;
    private const string CodeProjectName  = "PhotonQuantumCode";
    private const string QuantumCodePath  = "Assets/Photon/QuantumCode";
    private const string QuantumToolsPath = "../tools";
    private const string UnityCodeGenPath = "codegen_unity/quantum.codegen.unity.host.exe";
    private const int MenuItemPriority    = 200;
    private static readonly MD5 _md5 = MD5.Create();
    
    [MenuItem("Quantum/Code Integration/Run All CodeGen", priority = MenuItemPriority)]
    public static void RunAllCodeGen() {
      RunCodeGenTool(CodeGenPath, QuantumCodePath);
      AssetDatabase.ImportAsset($"{QuantumCodePath}/Core/CodeGen.cs", ImportAssetOptions.ForceUpdate);
      AssetDatabase.Refresh();
    }

    [MenuItem("Quantum/Code Integration/Run Qtn CodeGen", priority = MenuItemPriority + 11)]
    public static void RunQtnCodeGen() {
      RunCodeGenTool(CodeGenPath, QuantumCodePath);
      AssetDatabase.Refresh();
    }

    [MenuItem("Quantum/Code Integration/Run Unity CodeGen", priority = MenuItemPriority + 12)]
    public static void RunUnityCodeGen() {
      RunCodeGenTool(UnityCodeGenPath, AssemblyPath, "Assets");
      AssetDatabase.Refresh();
    }

    private static string GetConsistentSlashes(string path) {
      path = path.Replace('/', Path.DirectorySeparatorChar);
      path = path.Replace('\\', Path.DirectorySeparatorChar);
      return path;
    }

    private static string GetToolPath(string toolName) {
      var toolPath = Path.Combine(QuantumToolsPath, toolName);
      toolPath = GetConsistentSlashes(toolPath);
      toolPath = Path.GetFullPath(toolPath);
      return toolPath;
    }

    private static string Enquote(string str) {
      return $"\"{str.Trim('\'', '"')}\"";
    }

    private static void RunCodeGenTool(string toolName, params string[] args) {
      var output = new StringBuilder();
      var hadStdErr = false;

      var path = GetToolPath(toolName);

      if (UnityEngine.SystemInfo.operatingSystemFamily != UnityEngine.OperatingSystemFamily.Windows) {
        ArrayUtility.Insert(ref args, 0, path);
        path = "mono";
      }

      var startInfo = new ProcessStartInfo() {
        FileName = path,
        Arguments = string.Join(" ", args.Select(Enquote)),
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
      };

      using (var proc = new Process()) {
        proc.StartInfo = startInfo;

        proc.OutputDataReceived += (sender, e) => {
          if (e.Data != null) {
            output.AppendLine(e.Data);
          }
        };

        proc.ErrorDataReceived += (sender, e) => {
          if (e.Data != null) {
            output.AppendLine(e.Data);
            hadStdErr = true;
          }
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        if (!proc.WaitForExit(CodegenTimeout)) {
          throw new TimeoutException($"{toolName} timed out");
        }

        if (proc.ExitCode != 0) {
          throw new InvalidOperationException($"{toolName} failed with {proc.ExitCode}:\n{output}");
        } else if (hadStdErr) {
          Debug.LogWarning($"{toolName} succeeded, but there were problems.\n{output}");
        } else {
          Debug.Log($"{toolName} succeeded.\n{output}");
        }
      }
    }

    private class CodeDllWatcher {

      const string DelayedUnityCodeGenSentinel = "Temp/RunUnityCodeGen";

      [InitializeOnLoadMethod]
      private static void Initialize() {
        UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (path, messages) => {
          if (!IsPathThePhotonQuantumCodeAssembly(path)) {
            return;
          }

#if UNITY_2020_3_OR_NEWER
          File.WriteAllText(DelayedUnityCodeGenSentinel, path);
        };

        if (File.Exists(DelayedUnityCodeGenSentinel)) {
          var path = File.ReadAllText(DelayedUnityCodeGenSentinel);
          File.Delete(DelayedUnityCodeGenSentinel);
          RunCodeGenTool(UnityCodeGenPath, path, "Assets");
          AssetDatabase.Refresh();
        }
#else
          RunCodeGenTool(UnityCodeGenPath, path, "Assets");
          AssetDatabase.Refresh();
        };
#endif

      }

      private static bool IsPathThePhotonQuantumCodeAssembly(string path) {
        return string.Equals(Path.GetFileNameWithoutExtension(path), CodeProjectName, StringComparison.OrdinalIgnoreCase);
      }
    }

    private class QtnPostprocessor : AssetPostprocessor {

      [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Undocummented AssetPostprocessor callback")]
      private static string OnGeneratedCSProject(string path, string content) {
        if (Path.GetFileNameWithoutExtension(path) != CodeProjectName) {
          return content;
        }

        return AddQtnFilesToCsproj(content);
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "AssetPostprocessor callback")]
      private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        if (importedAssets.Any(IsValidQtnPath) || deletedAssets.Any(IsValidQtnPath) || movedAssets.Any(IsValidQtnPath) || movedFromAssetPaths.Any(IsValidQtnPath)) {
          RunQtnCodeGen();
          DeferredAssetDatabaseRefresh();
        }
      }

      private static string AddQtnFilesToCsproj(string content) {
        // find all the qtns
        var qtns = Directory.GetFiles(QuantumCodePath, "*.qtn", SearchOption.AllDirectories);
        if (qtns.Length == 0) {
          return content;
        }

        XDocument doc = XDocument.Load(new StringReader(content));
        var ns = doc.Root.Name.Namespace;

        var group = new XElement(ns + "ItemGroup");
        foreach (var qtn in qtns) {
          group.Add(new XElement(ns + "None", new XAttribute("Include", GetConsistentSlashes(qtn))));
        }

        doc.Root.Add(group);
        using (var writer = new StringWriter()) {
          doc.Save(writer);
          writer.Flush();
          return writer.GetStringBuilder().ToString();
        }
      }

      private static void DeferredAssetDatabaseRefresh() {
        EditorApplication.update -= DeferredAssetDatabaseRefreshHandler;
        EditorApplication.update += DeferredAssetDatabaseRefreshHandler;
      }

      private static void DeferredAssetDatabaseRefreshHandler() {
        EditorApplication.update -= DeferredAssetDatabaseRefreshHandler;
        AssetDatabase.Refresh();
      }

      private static bool IsValidQtnPath(string path) {
        if (!string.Equals(Path.GetExtension(path), ".qtn", StringComparison.OrdinalIgnoreCase)) {
          return false;
        }
        if (!path.StartsWith(QuantumCodePath)) {
          return false;
        }
        return true;
      }
    }
  }
}