

#region quantum_unity/Assets/Photon/Quantum/Editor/Tools/AssetDBInspector.cs
namespace Quantum.Editor {


  using System;
  using System.Collections.Generic;
  using System.IO;
  using Quantum;
  using Quantum.Editor;
  using UnityEditor;
  using UnityEngine;

  public class AssetDBInspector : EditorWindow {
    private static readonly Color Grey = Color.gray * 1.2f;

    private Vector2 _scrollPos;

    private enum SortingOrder {
      Path,
      Filename,
      Guid,
      Type,
      Loaded
    };

    private enum ResourceType {
      Asset,
      View,
      Prototype
    }

    private static SortingOrder Sorting {
      get => (SortingOrder)EditorPrefs.GetInt("Quantum_AssetDBInspector_SortingOrder", 0);
      set => EditorPrefs.SetInt("Quantum_AssetDBInspector_SortingOrder", (int)value);
    }

    private static bool HideUnloaded {
      get => EditorPrefs.GetBool("Quantum_AssetDBInspector_HideUnloaded", false);
      set => EditorPrefs.SetBool("Quantum_AssetDBInspector_HideUnloaded", value);
    }

    private static ResourceType PathToResourceType(string path) {
      var separatorIndex = path.LastIndexOf(AssetBase.NestedPathSeparator);
      if (separatorIndex >= 0) {
        if (path.EndsWith(nameof(Quantum.EntityView)) || path.EndsWith("EntityPrefab")) {
          return ResourceType.View;
        } else if (path.EndsWith(nameof(Quantum.EntityPrototype))) {
          return ResourceType.Prototype;
        }
      }

      return ResourceType.Asset;
    }

    [MenuItem("Window/Quantum/AssetDB Inspector")]
    [MenuItem("Quantum/Show AssetDB Inspector", false, 41)]
    static void Init() {
      AssetDBInspector window = (AssetDBInspector)GetWindow(typeof(AssetDBInspector), false, "Quantum Asset DB");
      window.Show();
    }

    public void OnGUI() {
      using (new GUILayout.HorizontalScope()) {
        if (QuantumRunner.Default == null && GUILayout.Button("Generate Quantum Asset DB")) {
          AssetDBGeneration.Generate();
        } else if (QuantumRunner.Default != null && GUILayout.Button("Dispose Resource Manager")) {
          UnityDB.Dispose();
        }

        using (new EditorGUI.DisabledScope(QuantumRunner.Default != null)) {
          if (GUILayout.Button("Export", GUILayout.Width(50))) {
            AssetDBGeneration.Export();
          }
        }

        EditorGUILayout.LabelField("Sort By", GUILayout.Width(50));
        Sorting = (SortingOrder)EditorGUILayout.EnumPopup(Sorting, new GUILayoutOption[] { GUILayout.Width(70) });

        EditorGUILayout.LabelField("Only Show Loaded", GUILayout.Width(110));
        HideUnloaded = EditorGUILayout.Toggle(HideUnloaded, GUILayout.Width(20));
      }

      using (new GUILayout.VerticalScope())
      using (var scrollView = new GUILayout.ScrollViewScope(_scrollPos)) {
        var resources = new List<AssetResource>(UnityDB.AssetResources);
        switch (Sorting) {
          case SortingOrder.Guid:
            resources.Sort((a, b) => a.Guid.CompareTo(b.Guid));
            break;
          case SortingOrder.Path:
            resources.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.Ordinal));
            break;
          case SortingOrder.Filename:
            // the char | is inside the invalid character list
            resources.Sort((a, b) => string.Compare(Path.GetFileName(a.Path.Replace(AssetBase.NestedPathSeparator, '.')), Path.GetFileName(b.Path.Replace(AssetBase.NestedPathSeparator, '.')), StringComparison.Ordinal));
            break;
          case SortingOrder.Type:
            resources.Sort((a, b) => {
              var resourceTypeA = PathToResourceType(a.Path);
              var resourceTypeB = PathToResourceType(b.Path);
              if (resourceTypeA == resourceTypeB) {
                return string.Compare(a.Path, b.Path, StringComparison.Ordinal);
              }

              return resourceTypeA.CompareTo(resourceTypeB);
            });
            break;
          case SortingOrder.Loaded:
            resources.Sort((a, b) => b.StateValue.CompareTo(a.StateValue));
            break;
        }

        foreach (var resource in resources) {
          // first one is the null asset
          if (resource.Guid == 0)
            continue;

          var loaded = resource.IsLoaded;

          if (HideUnloaded && !loaded)
            continue;

          var rect = EditorGUILayout.GetControlRect();
          var rectIcon = new Rect(rect.position, new Vector2(20.0f, rect.size.y));
          var rectGuid = new Rect(new Vector2(rectIcon.xMax, rect.position.y), new Vector2(200.0f, rect.size.y));
          var rectButton = new Rect(new Vector2(rect.xMax - 60.0f, rect.position.y), new Vector2(60.0f, rect.size.y));
          var rectName = new Rect(new Vector2(rectGuid.xMax, rect.position.y), new Vector2(rect.size.x - rectGuid.xMax - rectButton.size.x, rect.size.y));

          GUI.color = loaded ? Color.green : Color.gray;
          EditorGUI.LabelField(rect, EditorGUIUtility.IconContent("blendSampler"));

          GUI.color = loaded ? Color.white : Grey;
          GUI.Label(rectGuid, resource.Guid.ToString());

          // TODO replace by NestedAsset Utils
          var resourceType = PathToResourceType(resource.Path);
          var resourcePath = resource.Path;
          var separatorIndex = resourcePath.LastIndexOf(AssetBase.NestedPathSeparator);
          if (separatorIndex >= 0) {
            resourcePath = resourcePath.Substring(0, separatorIndex);
          }

          var resourceName = resourcePath;
          if (Sorting == SortingOrder.Filename) {
            resourceName = Path.GetFileName(resourcePath);
          }

          var resourceLabel = loaded ? $"{resourceName} ({resource.AssetObject?.GetType().Name})" : $"{resourceName} ({resourceType})";
          var color = resourceType == ResourceType.View ? Color.cyan : resourceType == ResourceType.Prototype ? Color.yellow : Color.green;

          GUI.color = loaded ? Desaturate(color, 0.75f) : Desaturate(color, 0.25f);
          if (GUI.Button(rectName, new GUIContent(resourceLabel, resource.Path), GUI.skin.label)) {
            var selectCandidates = AssetDatabase.FindAssets(Path.GetFileName(resourcePath)); //, new string[] { $"Assets/Resources/{Path.GetDirectoryName(resource.URL)}"});

            var candidateGuid = string.Empty;
            switch (resourceType) {
              case ResourceType.Asset:
              case ResourceType.Prototype:
                candidateGuid = Array.Find(selectCandidates, c => AssetDatabase.GUIDToAssetPath(c).EndsWith($"{resourcePath}.asset"));
                break;
              case ResourceType.View:
                candidateGuid = Array.Find(selectCandidates, c => AssetDatabase.GUIDToAssetPath(c).EndsWith($"{resourcePath}.prefab"));
                break;
            }

            if (!string.IsNullOrEmpty(candidateGuid)) {
              Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(candidateGuid));
            }
          }

          GUI.color = loaded ? Color.white : Grey;
          if (GUI.Button(rectButton, loaded ? "Dispose" : "Load")) {
            if (loaded) {
              UnityDB.DefaultResourceManager.DisposeAsset(resource.Guid);
              UnityDB.DefaultResourceManager.MainThreadSimulationEnded();
              return;
            } else {
              UnityDB.DefaultResourceManager.GetAsset(resource.Guid, true);
            }
          }
        }

        GUI.color = Color.white;

        _scrollPos = scrollView.scrollPosition;
      }

      if (!Application.isPlaying) {
        UnityDB.Update();
      }
    }

    public void OnInspectorUpdate() {
      this.Repaint();
    }

    private static Color Desaturate(Color c, float t) {
      return Color.Lerp(new Color(c.grayscale, c.grayscale, c.grayscale), c, t);
    }
  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Tools/QuantumFrameDifferWindow.cs
namespace Quantum.Editor {

  using System;
  using System.Linq;
  using System.Collections;
  using System.Collections.Generic;
  using System.IO;
  using UnityEditor;
  using UnityEngine;
  using Quantum;

  public class QuantumFrameDifferWindow : EditorWindow {
    private class StaticFrameStateStorage : ScriptableObject {
      public QuantumFrameDifferGUI.FrameDifferState State = new QuantumFrameDifferGUI.FrameDifferState();
    }

    private static StaticFrameStateStorage _stateStorage;

    private static StaticFrameStateStorage Storage {
      get {
        if (!_stateStorage) {
          _stateStorage = FindObjectOfType<StaticFrameStateStorage>();
          if (!_stateStorage) {
            _stateStorage = ScriptableObject.CreateInstance<StaticFrameStateStorage>();
          }
        }
        return _stateStorage;
      }
    }

    [InitializeOnLoadMethod]
    static void Initialize() {
      QuantumCallback.SubscribeManual((CallbackChecksumErrorFrameDump callback) => {
        Storage.State.AddEntry(QuantumRunner.FindRunner(callback.Game).Id, callback.ActorId, callback.FrameNumber, callback.FrameDump);
        ShowWindow();
      });
    }

    class QuantumFrameDifferGUIEditor : QuantumFrameDifferGUI {
      QuantumFrameDifferWindow _window;

      public override Rect Position {
        get { return _window.position; }
      }

      public override GUIStyle MiniButton {
        get { return EditorStyles.miniButton; }
      }

      public override GUIStyle MiniButtonLeft {
        get { return EditorStyles.miniButtonLeft; }
      }

      public override GUIStyle MiniButtonRight {
        get { return EditorStyles.miniButtonRight; }
      }

      public override GUIStyle BoldLabel {
        get { return EditorStyles.boldLabel; }
      }

      public override GUIStyle DiffHeaderError {
        get { return (GUIStyle)"flow node 6"; }
      }

      public override GUIStyle DiffHeader {
        get { return (GUIStyle)"flow node 1"; }
      }

      public override GUIStyle DiffBackground {
        get { return (GUIStyle)"CurveEditorBackground"; }
      }

      public override GUIStyle DiffLineOverlay {
        get { return (GUIStyle)"ProfilerTimelineBar"; }
      }

      public override bool IsEditor {
        get { return true; }
      }

      public override GUIStyle TextLabel {
        get { return EditorStyles.label; }
      }

      public QuantumFrameDifferGUIEditor(QuantumFrameDifferWindow window, FrameDifferState state) : base(state) {
        _window = window;
      }

      public override void Repaint() {
        _window.Repaint();
      }

      public override void DrawHeader() {
        bool wasEnabled = GUI.enabled;
        GUI.enabled = State.RunnerIds.Any();
        if (GUILayout.Button("Save", MiniButton, GUILayout.Height(16))) {
          var savePath = UnityEditor.EditorUtility.SaveFilePanel("Save", "", "frameDiff", "json");
          if (!string.IsNullOrEmpty(savePath)) {
            File.WriteAllText(savePath, JsonUtility.ToJson(State));
          }
        }
        GUI.enabled = wasEnabled;

        if (GUILayout.Button("Load", MiniButton, GUILayout.Height(16))) {
          var loadPath = UnityEditor.EditorUtility.OpenFilePanel("Load", "", "json");
          if (!string.IsNullOrEmpty(loadPath)) {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(loadPath), State);
          }
        }
      }
    }

    [MenuItem("Window/Quantum/Frame Differ")]
    [MenuItem("Quantum/Show Frame Differ", false, 42)]
    public static void ShowWindow() {
      GetWindow(typeof(QuantumFrameDifferWindow));
    }

    QuantumFrameDifferGUIEditor _gui;

    void OnGUI() {
      titleContent = new GUIContent("Frame Differ");

      if (_gui == null) {
        _gui = new QuantumFrameDifferGUIEditor(this, Storage.State);
      }

      _gui.OnGUI();
    }
  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Tools/QuantumProfilingServer.cs
namespace Quantum.Editor {
  using System;
  using System.Collections.Generic;
  using System.Net;
  using LiteNetLib;
  using LiteNetLib.Utils;
  using UnityEngine;

  public class QuantumProfilingServer {
    public const int PORT = 30000;
    private static QuantumProfilingServer _server;

    private EventBasedNetListener _listener;

    private NetManager _manager;
    private Dictionary<NetPeer, QuantumProfilingClientInfo> _peers = new Dictionary<NetPeer, QuantumProfilingClientInfo>();

    private QuantumProfilingServer() {
      _listener = new EventBasedNetListener();

      _manager = new NetManager(_listener);
      _manager.BroadcastReceiveEnabled = true;
      _manager.Start(PORT);

      _listener.ConnectionRequestEvent += OnConnectionRequest;
      _listener.PeerConnectedEvent += OnPeerConnected;
      _listener.PeerDisconnectedEvent += OnPeerDisconnected;
      _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
      _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;

      Debug.Log($"QuantumProfilingServer: Started @ 0.0.0.0:{PORT}");
    }

    public static event Action<QuantumProfilingClientInfo, Profiling.ProfilerContextData> SampleReceived;

    public static void Update() {
      if (_server == null) {
        _server = new QuantumProfilingServer();
      }

      _server._manager.PollEvents();
    }

    private void OnConnectionRequest(ConnectionRequest request) {
      request.AcceptIfKey(QuantumProfilingClientConstants.CONNECT_TOKEN);
    }

    private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliverymethod) {
      try {

        var msgType = reader.GetByte();
        var text = reader.GetString();

        if (msgType == QuantumProfilingClientConstants.ClientInfoMessage) {
          var data = JsonUtility.FromJson<QuantumProfilingClientInfo>(text);
          _peers[peer] = data;

        } else if (msgType == QuantumProfilingClientConstants.FrameMessage) {
          if (SampleReceived != null) {
            var data = JsonUtility.FromJson<Profiling.ProfilerContextData>(text);
            try {
              if (_peers.TryGetValue(peer, out var info)) {
                SampleReceived(info, data);
              } else {
                Log.Error("Client Info not found for peer {0}", peer.EndPoint);
              }
            } catch (Exception ex) {
              Log.Error($"QuantumProfilingServer: Sample Handler Error: {ex}");
            }
          }
        } else {
          throw new NotSupportedException($"Unknown message type: {msgType}");
        }
      } catch (Exception ex) {
        Log.Error($"QuantumProfilingServer: Receive error: {ex}, disconnecting peer {peer.EndPoint}");
        _manager.DisconnectPeerForce(peer);
      }
    }

    private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteendpoint, NetPacketReader reader, UnconnectedMessageType messagetype) {
      if (reader.GetString() == QuantumProfilingClientConstants.DISCOVER_TOKEN) {
        Log.Info($"QuantumProfilingServer: Discovery Request From {remoteendpoint}");
        _manager.SendUnconnectedMessage(NetDataWriter.FromString(QuantumProfilingClientConstants.DISCOVER_RESPONSE_TOKEN), remoteendpoint);
      }
    }

    private void OnPeerConnected(NetPeer peer) {
      Log.Info($"QuantumProfilingServer: Connection From {peer.EndPoint}");
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
      _peers.Remove(peer);
    }

  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Tools/QuantumStateInspector.cs
namespace Quantum.Editor {
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Reflection;
  using Photon.Deterministic;
  using Quantum.Core;
  using UnityEditor;
  using UnityEditor.IMGUI.Controls;
  using UnityEngine;

  public unsafe partial class QuantumStateInspector : EditorWindow {
    private static Lazy<Skin> _skin = new Lazy<Skin>(() => new Skin());

    private QuantumSimulationObjectInspector _inspector = new QuantumSimulationObjectInspector();
    private Model _model = new Model();
    private Vector2 _inspectorScroll;
    private bool _syncWithActiveGameObject;
    private bool _useVerifiedFrame;

    private ComponentTypeSetSelector _prohibitedComponents = new ComponentTypeSetSelector() { ComponentTypeNames = Array.Empty<string>() };
    private ComponentTypeSetSelector _requiredComponents   = new ComponentTypeSetSelector() { ComponentTypeNames = Array.Empty<string>() };
    
    private TreeViewState _runnersTreeViewState        = new TreeViewState();
    private UnityInternal.SplitterState _splitterState = UnityInternal.SplitterState.FromRelative(new float[] { 0.5f, 0.5f }, new int[] { 100, 100 });
    
    [NonSerialized] private QuantumRunner _currentRunner;
    [NonSerialized] private bool _needsReload;
    [NonSerialized] private bool _needsSelectionSync;
    [NonSerialized] private bool _addingComponent;
    [NonSerialized] private string _buildEntityRunnerId;
    [NonSerialized] private List<EntityRef> _entityRefBuffer = new List<EntityRef>();
    [NonSerialized] private CommandQueue _pendingDebugCommands = new CommandQueue();
    [NonSerialized] private List<Tuple<string, EntityRef>> _pendingSelection = new List<Tuple<string, EntityRef>>();
    [NonSerialized] private RunnersTreeView _runnersTreeView;

    private static Skin skin => _skin.Value;

    public static QuantumStateInspector ShowWindow(bool newWindow = false) {
      QuantumStateInspector result;
      if (newWindow) {
        result = CreateInstance<QuantumStateInspector>();
      } else {
        result = GetWindow<QuantumStateInspector>();
      }
      result.Show();
      return result;
    }

    public void SelectEntity(global::EntityView view) {
      UpdateState();
      SelectCorrespondingEntityNode(Enumerable.Repeat(view, 1));
    }

    #region Unity Messages

    private void OnDisable() {
      EditorApplication.hierarchyChanged -= DeferredSyncWithSelection;
      Selection.selectionChanged         -= DeferredSyncWithSelection;
      DebugCommand.CommandExecuted       -= CommandExecuted;
    }

    private void OnEnable() {
      _runnersTreeView = new RunnersTreeView(_runnersTreeViewState, _model);
      _runnersTreeView.TreeSelectionChanged += (sender, selection) => {
        _addingComponent = false;
        _buildEntityRunnerId = null;
        _runnersTreeView.SetForceShowItems(null);
        if (_syncWithActiveGameObject) {
          SelectCorrespondingGameObjects(GetEntitiesByTreeId(selection));
        }
      };

      // remove missing component types
      _requiredComponents.ComponentTypeNames   = _requiredComponents.ComponentTypeNames.Where(x => QuantumEditorUtility.TryGetComponentType(x, out _)).ToArray();
      _prohibitedComponents.ComponentTypeNames = _prohibitedComponents.ComponentTypeNames.Where(x => QuantumEditorUtility.TryGetComponentType(x, out _)).ToArray();

      _runnersTreeView.WithComponents = _requiredComponents;
      _runnersTreeView.WithoutComponents = _prohibitedComponents;
      _runnersTreeView.Reload();

      DebugCommand.CommandExecuted       += CommandExecuted;
      Selection.selectionChanged         -= DeferredSyncWithSelection;
      Selection.selectionChanged         += DeferredSyncWithSelection;
      EditorApplication.hierarchyChanged -= DeferredSyncWithSelection;
      EditorApplication.hierarchyChanged += DeferredSyncWithSelection;

      {
        _inspector.EntityMenu = QuantumEditorGUI.BuildMenu<EntityRef>()
          .AddItem("Select View", entity => SelectCorrespondingGameObjects(QTuple.Create(_currentRunner, entity)), _ => _currentRunner)
          .AddItem("Destroy Entity", entity => CommandEnqueue(_currentRunner, DebugCommand.CreateDestroyPayload(entity)), _ => _currentRunner && DebugCommand.IsEnabled);

        _inspector.ComponentMenu = QuantumEditorGUI.BuildMenu<EntityRef, string>()
          .AddItem("Remove Component", (entity, typeName) => CommandEnqueue(_currentRunner, DebugCommand.CreateRemoveComponentPayload(entity, QuantumEditorUtility.GetComponentType(typeName))), (_, __) => _currentRunner && DebugCommand.IsEnabled);
      }

      {
        Func<RunnersTreeViewItemBase, bool> runnerExists = (n) => FindRunnerById(n.RunnerModel.Id);
        Func<RunnersTreeViewItemBase, bool> runnerExistsDebug = (n) => DebugCommand.IsEnabled && FindRunnerById(n.RunnerModel.Id);

        _runnersTreeView.RunnerContextMenu = QuantumEditorGUI.BuildMenu<RunnerTreeViewItem>()
          .AddItem("Create Entity...", (node) => _buildEntityRunnerId = node.State.Id, runnerExistsDebug)
          .AddItem("Save Dump (Small)", (node) => DumpFrame(FindRunnerById(node.State.Id), true), runnerExists)
          .AddItem("Save Dump (Full)", (node) => DumpFrame(FindRunnerById(node.State.Id), false), runnerExists)
          .AddItem("Remove", (node) => {
            var state = node.State;
            _model.Runners.RemoveAll(x => x == state);
            _needsReload = true;
          });

        _runnersTreeView.EntityContextMenu = QuantumEditorGUI.BuildMenu<EntityTreeViewItem>()
          .AddItem("Select View", _ => {
            SelectCorrespondingGameObjects(GetSelectedEntities().ToArray());
          }, runnerExists)
          .AddItem("Destroy Entity", _ => {
            foreach (var group in GetSelectedEntities().GroupBy(x => x.Item0)) {
              var payload = group.Select(x => DebugCommand.CreateDestroyPayload(x.Item1));
              CommandEnqueue(group.Key, payload.ToArray());
            }
          }, runnerExistsDebug)
          .AddGenerator(adder => {
            var commonSet = new ComponentSet();
            var selectedEntities = GetEntityNodes(_runnersTreeView.GetSelection());
            foreach (var node in selectedEntities) {
              commonSet.UnionWith(node.State.EntityComponents);
            }

            foreach (var typeName in _model.GetComponentNames(commonSet)) {
              adder($"Remove Component/{typeName}", (_) => {
                foreach (var group in GetSelectedEntities().GroupBy(x => x.Item0)) {
                  var payload = group.Select(x => DebugCommand.CreateRemoveComponentPayload(x.Item1, QuantumEditorUtility.GetComponentType(typeName)));
                  CommandEnqueue(group.Key, payload.ToArray());
                }
              }, runnerExistsDebug);
            }
          });
      }
    }

    private void OnGUI() {
      //var rect = new Rect(0, 0, position.width, position.height);

      titleContent = skin.titleContent;

      var toolbarRect = position.ZeroXY().SetHeight(skin.toolbarHeight);
      using (new GUILayout.AreaScope(toolbarRect)) {
        using (new GUILayout.HorizontalScope(EditorStyles.toolbar)) {
          _useVerifiedFrame = EditorGUILayout.Popup(_useVerifiedFrame ? 0 : 1, skin.frameTypeOptions, EditorStyles.toolbarPopup, GUILayout.Width(80)) != 0;
          _syncWithActiveGameObject = GUILayout.Toggle(_syncWithActiveGameObject, "Sync Selection", EditorStyles.toolbarButton);
          DrawComponentDropdown(_requiredComponents, "Entities With");
          DrawComponentDropdown(_prohibitedComponents, "Entities Without");
          GUILayout.FlexibleSpace();
        }
      }

      // alloc space for inspector and the tree
      try {
        UnityInternal.SplitterGUILayout.BeginHorizontalSplit(_splitterState, GUIStyle.none);
        using (new EditorGUILayout.VerticalScope()) {
          GUILayout.FlexibleSpace();
        }
        using (new EditorGUILayout.VerticalScope()) {
          GUILayout.FlexibleSpace();
        }
      } finally {
        UnityInternal.SplitterGUILayout.EndHorizontalSplit();
      }

      _runnersTreeView.WithComponents = _requiredComponents;
      _runnersTreeView.WithoutComponents = _prohibitedComponents;
      
      if (_needsReload) {
        _runnersTreeView.Reload();
        _needsReload = false;
      }

      var treeViewRect = new Rect(0, skin.toolbarHeight, _splitterState.realSizes[0], position.height - skin.toolbarHeight);
      _runnersTreeView.OnGUI(treeViewRect);

      var inspectorRect = new Rect(treeViewRect.xMax, skin.toolbarHeight, _splitterState.realSizes[1], treeViewRect.height);

      QuantumSimulationObjectInspectorState currentState = null;

      var firstSelectedNode = _runnersTreeView.GetSelection()
        .Select(x => _runnersTreeView.FindById(x))
        .FirstOrDefault(x => x?.GetInspectorState() != null);

      if (firstSelectedNode != null) {
        currentState = firstSelectedNode.GetInspectorState();
      }

      _currentRunner = null;
      using (new GUILayout.AreaScope(inspectorRect)) {
        // we want to get rid of the top border here, hence the weird -1 space;
        // seems to do the trick
        GUILayout.Space(-1);
        using (new GUILayout.HorizontalScope(skin.inspectorBackground))
        using (var scroll = new EditorGUILayout.ScrollViewScope(_inspectorScroll)) {
          _inspectorScroll = scroll.scrollPosition;
          if (!string.IsNullOrEmpty(_buildEntityRunnerId)) {
            DrawBuildEntityGUI();
          } else if (currentState == null) {
            if (_runnersTreeView.GetSelection().Any()) {
              using (EditorStyles.wordWrappedLabel.FontStyleScope(italic: true)) {
                EditorGUILayout.LabelField("Not available. Only selected entities are fetched from an active runner.", EditorStyles.wordWrappedLabel);
              }
            }
          } else {
            _currentRunner = FindRunnerById(firstSelectedNode.RunnerModel.Id);
            _inspector.DoGUILayout(currentState, useScrollView: false);

            if (_currentRunner) {
              if (firstSelectedNode is EntityTreeViewItem entityNode) {
                EditorGUILayout.Space();
                EditorGUILayout.Separator();
                DrawAddComponentGUI(_currentRunner, entityNode.Entity);
              }
            }
          }
        }
      }
    }

    private void Update() {
      UpdateState();

      if (_syncWithActiveGameObject && _needsSelectionSync) {
        _needsSelectionSync = false;

        var selectedValidViews = Selection.gameObjects
          .Where(x => x.scene.IsValid())
          .Select(x => x.GetComponent<global::EntityView>())
          .Where(x => x?.EntityRef.IsValid == true)
          .ToList();

        if (selectedValidViews.Any()) {
          SelectCorrespondingEntityNode(selectedValidViews);
        }
      }

      if (_pendingSelection.Any()) {
        try {
          List<int> selectionBuffer = new List<int>();
          foreach (var p in _pendingSelection) {
            AppendSelectionBuffer(p.Item1, p.Item2, selectionBuffer);
          }
          if (selectionBuffer.Any()) {
            ApplySelection(selectionBuffer);
          }
        } finally {
          _pendingSelection.Clear();
        }
      }
    }

    private void OnInspectorUpdate() {
      if (_needsReload) {
        Repaint();
      }
    }

    #endregion Unity Messages

    private static QuantumRunner FindRunnerById(string id) {
      return QuantumRunner.FindRunner(id);
    }

    private static long GenerateCommandId() => AssetGuid.NewGuid().Value;

    [MenuItem("Window/Quantum/State Inspector")]
    [MenuItem("Quantum/Show State Inspector", false, 44)]
    private static void ShowWindowMenuItem() => ShowWindow(false);

    private bool AppendSelectionBuffer(string runnerId, EntityRef entityRef, List<int> selectionBuffer) {
      var runnerModel = _model.FindRunner(runnerId);
      if (runnerModel == null) {
        return false;
      }

      foreach (var entity in runnerModel.Entities) {

        if (entity.Entity != entityRef) {
          continue;
        }

        // get the tree item id
        _model.GetFixedTreeNodeIds(runnerId, out var runnerNodeId, out _, out var entitiesNodeId, out _);

        // because of how tree view works, nodes need to be expanded, otherwise children do not exist :(
        if (_runnersTreeView.SetExpanded(runnerNodeId, true)) {
          _runnersTreeView.Reload();
        }
        if (_runnersTreeView.SetExpanded(entitiesNodeId, true)) {
          _runnersTreeView.Reload();
        }

        var entityId = _model.GetTreeNodeIdForEntity(entitiesNodeId, entityRef);
        selectionBuffer.Add(entityId);
        return true;
      }

      return false;
    }

    private void ApplySelection(List<int> selection) {
      if (selection.Any()) {
        try {
          _runnersTreeView.SetForceShowItems(null);
          _runnersTreeView.SetSelection(selection, TreeViewSelectionOptions.RevealAndFrame);
        } catch (ArgumentException) {
          // this error may come from the fact that some entities are hidden due to component filters
          _runnersTreeView.SetForceShowItems(selection);
          _runnersTreeView.Reload();
          _runnersTreeView.SetSelection(selection, TreeViewSelectionOptions.RevealAndFrame);
        }
      }
    }

    private void CommandEnqueue(QuantumRunner runner, params DebugCommand.Payload[] payload) {
      Debug.Assert(DebugCommand.IsEnabled);
      for (int i = 0; i < payload.Length; ++i) {
        payload[i].Id = GenerateCommandId();
        _pendingDebugCommands.Add(Tuple.Create(runner.Id, payload[i]));
      }

      DebugCommand.Send(runner.Game, payload);
    }

    private void CommandExecuted(DebugCommand.Payload payload, Exception error) {
      var cmd = _pendingDebugCommands.RemoveReturn(payload.Id);
      if (cmd != null && payload.Type == DebugCommandType.Create && payload.Entity.IsValid && !cmd.Item2.Entity.IsValid) {
        // make sure the created entity is selected
        _pendingSelection.Add(Tuple.Create(cmd.Item1, payload.Entity));
      }
    }

    private void CommandPending(RunnerState runnerState, DebugCommand.Payload payload) {
      if (payload.Type == DebugCommandType.Create) {
        Func<EntityState, EntityState> processEntity = e => {
          if (e.InspectorId > 0) {
            var inspectorState = runnerState.GetInspector(e.InspectorId);
            foreach (var name in _model.GetComponentNames(payload.Components)) {
              inspectorState.AddComponentPlaceholder(name);
            }
          }
          var components = e.EntityComponents;
          components.UnionWith(payload.Components);
          e.EntityComponents = components;
          return e;
        };

        if (!payload.Entity.IsValid) {
          runnerState.AddPendingEntity(processEntity);
        } else {
          runnerState.UpdateEntityState(payload.Entity, processEntity);
        }
      } else if (payload.Type == DebugCommandType.Destroy) {
        if (payload.Components.IsEmpty) {
          // destroy entity
          runnerState.RemoveEntityState(payload.Entity);
        } else {
          runnerState.UpdateEntityState(payload.Entity, e => {
            if (e.InspectorId > 0) {
              var inspectorState = runnerState.GetInspector(e.InspectorId);
              foreach (var name in _model.GetComponentNames(payload.Components)) {
                inspectorState.Remove($"/{name}");
              }
            }
            // remove thumbnail
            var components = e.EntityComponents;
            components.Remove(payload.Components);
            e.EntityComponents = components;
            return e;
          });
        }
      }
    }

    private void DeferredSyncWithSelection() {
      _needsSelectionSync = true;
    }

    private void DrawAddComponentGUI(QuantumRunner runner, EntityRef entity) {
      if (!_addingComponent) {
        using (new GUILayout.HorizontalScope()) {
          GUILayout.FlexibleSpace();
          if (EditorGUILayout.DropdownButton(skin.addComponentContent, FocusType.Passive, UnityInternal.Styles.AddComponentButton)) {
            _addingComponent = true;
            QuantumEditorUtility.GetPendingEntityPrototypeRoot(clear: true);
          }
          GUILayout.FlexibleSpace();
        }
      } else {
        using (new QuantumEditorGUI.HierarchyModeScope(true))
        using (new QuantumEditorGUI.BoxScope()) {
          QuantumEditorGUI.Inspector(QuantumEditorUtility.GetPendingEntityPrototypeRoot(), skipRoot: true);
          using (new GUILayout.HorizontalScope()) {
            using (new EditorGUI.DisabledScope(runner == null)) {
              if (GUILayout.Button("OK")) {
                CommandEnqueue(runner, DebugCommand.CreateMaterializePayload(entity, QuantumEditorUtility.FinishPendingEntityPrototype(), runner.Game.AssetSerializer));
                _addingComponent = false;
              }
            }
            if (GUILayout.Button("Cancel")) {
              _addingComponent = false;
            }
          }
        }
      }
    }

    private void DrawBuildEntityGUI() {
      var runner = FindRunnerById(_buildEntityRunnerId);
      if (runner == null) {
        _buildEntityRunnerId = null;
        Repaint();
      } else {
        using (new QuantumEditorGUI.HierarchyModeScope(true))
        using (new QuantumEditorGUI.BoxScope()) {
          QuantumEditorGUI.Inspector(QuantumEditorUtility.GetPendingEntityPrototypeRoot(), skipRoot: true);
          using (new GUILayout.HorizontalScope()) {
            using (new EditorGUI.DisabledScope(runner == null)) {
              if (GUILayout.Button("OK")) {
                CommandEnqueue(runner, DebugCommand.CreateMaterializePayload(EntityRef.None, QuantumEditorUtility.FinishPendingEntityPrototype(), runner.Game.AssetSerializer));
                _buildEntityRunnerId = null;
              }
            }
            if (GUILayout.Button("Cancel")) {
              _buildEntityRunnerId = null;
            }
          }
        }
      }
    }

    private void DrawComponentDropdown(ComponentTypeSetSelector selector, string prefix) {
      GUIContent guiContent;
      float additionalWidth = 0;

      if (selector.ComponentTypeNames.Length == 0) {
        guiContent = new GUIContent($"{prefix}: None");
      } else {
        guiContent = new GUIContent($"{prefix}: ");
        additionalWidth = QuantumEditorGUI.CalcThumbnailsWidth(selector.ComponentTypeNames.Length);
      }

      var buttonSize = EditorStyles.toolbarDropDown.CalcSize(guiContent);
      var originalButtonSize = buttonSize;
      buttonSize.x += additionalWidth;
      buttonSize.x = Math.Max(50, buttonSize.x);

      var buttonRect = GUILayoutUtility.GetRect(buttonSize.x, buttonSize.y);
      bool pressed = GUI.Button(buttonRect, guiContent, EditorStyles.toolbarDropDown);

      var thumbnailRect = buttonRect.AddX(originalButtonSize.x - skin.toolbarDropdownButtonWidth);
      foreach (var name in selector.ComponentTypeNames) {
        thumbnailRect = QuantumEditorGUI.ComponentThumbnailPrefix(thumbnailRect, name, addSpacing: true);
      }

      if (pressed) {
        QuantumEditorGUI.ShowComponentTypesPicker(buttonRect, selector, onChange: () => {
          _needsReload = true;
          Repaint();
        });
      }
    }

    private void DumpFrame(QuantumRunner runner, bool partial = false) {
      var frame = _useVerifiedFrame ? runner.Game.Frames.Verified : runner.Game.Frames.Predicted;

      var path = EditorUtility.SaveFilePanel("Save Frame Dump", string.Empty, $"frame_{frame.Number}.txt", "txt");
      if (!string.IsNullOrEmpty(path)) {
        System.IO.File.WriteAllText(path, frame.DumpFrame((partial ? Frame.DumpFlag_NoHeap : 0)));
      }
    }

    private QTuple<QuantumRunner, EntityRef>[] GetEntitiesByTreeId(IList<int> ids) {
      return GetEntityNodes(ids)
        .Select(n => QTuple.Create(FindRunnerById(n.RunnerModel.Id), n.Entity))
        .Where(n => n.Item0 != null)
        .ToArray();
    }

    private IEnumerable<EntityTreeViewItem> GetEntityNodes(IList<int> ids) {
      return ids
        .Select(id => _runnersTreeView.FindById(id))
        .OfType<EntityTreeViewItem>();
    }

    private QTuple<QuantumRunner, EntityRef>[] GetSelectedEntities() {
      return GetEntitiesByTreeId(_runnersTreeView.GetSelection());
    }

    private void SelectCorrespondingEntityNode(IEnumerable<global::EntityView> views) {
      var newSelection = new List<int>();

      var updaters = GameObject.FindObjectsOfType<EntityViewUpdater>()
        .Select(x => new { Updater = x, RunnerId = QuantumRunner.FindRunner(x.ObservedGame)?.Id })
        .Where(x => !string.IsNullOrEmpty(x.RunnerId))
        .ToList();

      foreach (var view in views) {
        if (view?.EntityRef.IsValid != true) {
          continue;
        }

        var entityRef = view.EntityRef;

        // which updater does this belong to?
        foreach (var updater in updaters) {
          if (updater.Updater.GetView(entityRef) != view) {
            continue;
          }

          AppendSelectionBuffer(updater.RunnerId, entityRef, newSelection);
        }
      }

      ApplySelection(newSelection);
    }

    private void SelectCorrespondingGameObjects(params QTuple<QuantumRunner, EntityRef>[] entities) {
      var updaters = GameObject.FindObjectsOfType<EntityViewUpdater>();

      var newSelection = new List<UnityEngine.Object>();

      foreach (var item in entities) {
        var entityRef = item.Item1;
        Debug.Assert(entityRef.IsValid);

        foreach (var updater in updaters) {
          var runner = QuantumRunner.FindRunner(updater.ObservedGame);
          if (runner != item.Item0) {
            continue;
          }

          var view = updater.GetView(entityRef);
          if (!view) {
            continue;
          }

          newSelection.Add(view.gameObject);
        }
      }

      if (newSelection.Any()) {
        Selection.objects = newSelection.ToArray();
      }
    }

    private bool UpdateRunnersState(Model model, List<int> selectedNodes) {
      bool needsReload = false;

      if (ComponentTypeId.Type?.Length > 0) {
        Array.Resize(ref model.ComponentShortNames, ComponentTypeId.Type.Length);
        for (int i = 0; i < ComponentTypeId.Type.Length; ++i) {
          model.ComponentShortNames[i] = ComponentTypeId.Type[i]?.Name;
        }
      }

      foreach (var runnerState in model.Runners) {
        runnerState.IsActive = false;
        runnerState.IsDefault = false;

        // remove all pending entities
        runnerState.RemovePendingEntities();
      }

      // match actual runners against models
      foreach (var runner in QuantumRunner.ActiveRunners) {
        var frame = _useVerifiedFrame ? runner.Game.Frames.Verified : runner.Game.Frames.Predicted;
        if (frame == null) {
          continue;
        }

        var runnerState = model.Runners.FirstOrDefault(x => x.Id == runner.Id);
        if (runnerState == null) {
          runnerState = new RunnerState() {
            Id = runner.Id
          };
          model.Runners.Add(runnerState);
          needsReload = true;
        } else if ( runnerState.Tick != frame.Number ) {
          needsReload = true;
        }

        runnerState.Tick = frame.Number;
        runnerState.Entities.Clear();
        runnerState.DynamicDB.Clear();
        runnerState.IsActive = true;
        runnerState.IsDefault = (runner == QuantumRunner.Default);

        // always inspect runner and globals
        runnerState.RunnerInspectorState.FromSession(runner.Session);
        runnerState.GlobalsInspectorState.FromStruct(frame, "Globals", frame.Global, typeof(_globals_));

        int dynamicInspectorState = 0;
        model.GetFixedTreeNodeIds(runner.Id, out _, out _, out var entitiesNodeId, out var dynamicDBNodeId);

        frame.GetAllEntityRefs(_entityRefBuffer);
        foreach (var entity in _entityRefBuffer) {
          var entityState = EntityState.FromEntity(frame, entity);
          var entityNodeId = model.GetTreeNodeIdForEntity(entitiesNodeId, entityState);
          if (selectedNodes.Contains(entityNodeId)) {
            runnerState.AcquireInspectorState(ref dynamicInspectorState, out entityState.InspectorId).FromEntity(frame, entity);
          }
          runnerState.Entities.Add(entityState);
        }

        var dynamicDB = frame.DynamicAssetDB;
        foreach (var asset in dynamicDB.Assets) {
          var assetState = new DynamicAssetState() {
            Guid = asset.Guid,
            Path = asset.Path,
            Type = asset.GetType().AssemblyQualifiedName,
          };
          var assetNodeId = model.GetTreeNodeIdForDynamicAsset(dynamicDBNodeId, asset.Guid);
          if (selectedNodes.Contains(assetNodeId)) {
            runnerState.AcquireInspectorState(ref dynamicInspectorState, out assetState.InspectorId).FromAsset(asset);
          }

          runnerState.DynamicDB.Add(assetState);
        }
      }

      foreach (var pair in _pendingDebugCommands) {
        var runner = _model.FindRunner(pair.Item1);
        if (runner == null) {
          continue;
        }
        CommandPending(runner, pair.Item2);
      }

      return needsReload;
    }

    private void UpdateState() {
      _needsReload |= UpdateRunnersState(_model, _runnersTreeViewState.selectedIDs);
    }

    #region Model

    [Serializable]
    private struct DynamicAssetState {
      public AssetGuid Guid;
      public int InspectorId;
      public string Path;
      public string Type;
    }

    [Serializable]
    private struct EntityState {
      public long ComponentSet0;
      public long ComponentSet1;
      public long ComponentSet2;
      public long ComponentSet3;
      public int EntityRefIndex;
      public int EntityRefVersion;
      public int InspectorId;
      public EntityRef Entity => new EntityRef() { Index = EntityRefIndex, Version = EntityRefVersion };

      public ComponentSet EntityComponents {
        get {
          if (ComponentSet.SIZE == sizeof(long) * 4) {
            ComponentSet result = default;
            long* lp = (long*)&result;
            lp[0] = ComponentSet0;
            lp[1] = ComponentSet1;
            lp[2] = ComponentSet2;
            lp[3] = ComponentSet3;
            return result;
          }
        }
        set {
          long* lp = (long*)&value;
          ComponentSet0 = lp[0];
          ComponentSet1 = lp[1];
          ComponentSet2 = lp[2];
          ComponentSet3 = lp[3];
        }
      }

      public bool IsPending => EntityRefIndex < 0;

      public static EntityState FromEntity(FrameBase frame, EntityRef entity) {
        ComponentSet componentSet = frame.GetComponentSet(entity);
        long* lp = (long*)&componentSet;
        return new EntityState() {
          EntityRefIndex = entity.Index,
          EntityRefVersion = entity.Version,
          EntityComponents = componentSet,
        };
      }
    }

    [Serializable]
    private class Model {
      public string[] ComponentShortNames = { };
      public List<RunnerState> Runners = new List<RunnerState>();

      private const int MaxDynamicAssetsPerRunner = RunnerIdSpace - MaxEntitiesPerRunner - 4;
      private const int MaxEntitiesPerRunner = 9000000;
      private const int RunnerIdSpace = 10000000;

      public RunnerState FindRunner(string id) {
        return Runners.FirstOrDefault(x => x.Id == id);
      }

      public string GetComponentName(int typeId) {
        return ComponentShortNames[typeId];
      }

      public IEnumerable<string> GetComponentNames() {
        return ComponentShortNames.Skip(1);
      }

      public IEnumerable<string> GetComponentNames(ComponentSet set) {
        if (ComponentShortNames?.Length <= 0) {
          yield break;
        }

        for (int i = 1; i < ComponentShortNames.Length; ++i) {
          if (!set.IsSet(i))
            continue;

          yield return ComponentShortNames[i];
        }
      }

      public IEnumerable<int> GetComponentTypeIds() {
        if (ComponentShortNames?.Length > 0) {
          return Enumerable.Range(1, ComponentShortNames.Length - 1);
        } else {
          return Array.Empty<int>();
        }
      }

      public void GetFixedTreeNodeIds(string runnerId, out int runnerNodeId, out int globalsNodeId, out int entitiesNodeId, out int dynamicDBNodeId) {
        int index = Runners.FindIndex(x => x.Id == runnerId);
        if (index < 0) {
          throw new InvalidOperationException();
        }

        runnerNodeId = 1 + index * RunnerIdSpace;
        globalsNodeId = runnerNodeId + 1;
        entitiesNodeId = runnerNodeId + 2;
        dynamicDBNodeId = runnerNodeId + 3 + MaxEntitiesPerRunner;
      }

      public int GetTreeNodeIdForDynamicAsset(int dynamicDBNodeId, AssetGuid guid) {
        Debug.Assert(guid.IsDynamic);
        long rawId = guid.Value & (~AssetGuid.DynamicBit);
        Debug.Assert(rawId <= MaxDynamicAssetsPerRunner);
        return dynamicDBNodeId + 1 + ((int)rawId % MaxDynamicAssetsPerRunner);
      }

      public int GetTreeNodeIdForEntity(int entitiesNodeId, EntityRef entity) {
        Debug.Assert(entity.Index <= MaxEntitiesPerRunner);
        return entitiesNodeId + 1 + (entity.Index % MaxEntitiesPerRunner);
      }

      public int GetTreeNodeIdForEntity(int entitiesNodeId, in EntityState entity) {
        if (entity.IsPending) {
          Debug.Assert(entity.EntityRefIndex < 0);
          return entitiesNodeId + 1 + MaxEntitiesPerRunner + entity.EntityRefIndex;
        } else {
          Debug.Assert(entity.EntityRefIndex >= 0 && entity.EntityRefIndex <= MaxEntitiesPerRunner);
          return entitiesNodeId + 1 + (entity.EntityRefIndex % MaxEntitiesPerRunner);
        }
      }
    }

    [Serializable]
    private class RunnerState {

      public List<EntityState> Entities        = new List<EntityState>();
      public List<DynamicAssetState> DynamicDB = new List<DynamicAssetState>();

      public string Id;
      public bool IsActive;
      public bool IsDefault;
      public int Tick;

      public QuantumSimulationObjectInspectorState DynamicDBInspectorState     = new QuantumSimulationObjectInspectorState();
      public QuantumSimulationObjectInspectorState EntitiesInspectorState      = new QuantumSimulationObjectInspectorState();
      public QuantumSimulationObjectInspectorState GlobalsInspectorState       = new QuantumSimulationObjectInspectorState();
      public QuantumSimulationObjectInspectorState RunnerInspectorState        = new QuantumSimulationObjectInspectorState();
      public List<QuantumSimulationObjectInspectorState> DynamicInspectorState = new List<QuantumSimulationObjectInspectorState>();


      public QuantumSimulationObjectInspectorState AcquireInspectorState(ref int state, out int id) {
        var index = state++;
        id = state;
        if (DynamicInspectorState.Count > index) {
          return DynamicInspectorState[index];
        } else {
          var result = new QuantumSimulationObjectInspectorState();
          DynamicInspectorState.Add(result);
          return result;
        }
      }

      public void AddPendingEntity(Func<EntityState, EntityState> func) {
        var entity = new EntityState();

        int pendingCount = 0;
        for (int i = Entities.Count - 1; i >= 0 && Entities[i].EntityRefIndex < 0; --i) {
          ++pendingCount;
        }

        entity.EntityRefIndex = -1 - pendingCount;
        entity.EntityRefVersion = int.MinValue;

        Entities.Add(func(entity));
      }

      public QuantumSimulationObjectInspectorState GetDynamicAssetInspector(int assetStateId) {
        var state = DynamicDB[assetStateId];
        if (state.InspectorId == 0) {
          return null;
        } else {
          return GetInspector(state.InspectorId);
        }
      }

      public QuantumSimulationObjectInspectorState GetEntityInspector(int entityStateId) {
        var state = Entities[entityStateId];
        if (state.InspectorId == 0) {
          return null;
        } else {
          return GetInspector(state.InspectorId);
        }
      }

      public QuantumSimulationObjectInspectorState GetInspector(int inspectorId) {
        return DynamicInspectorState[inspectorId - 1];
      }

      public bool RemoveEntityState(EntityRef entityRef) {
        var entityStateIndex = Entities.FindIndex(x => x.Entity == entityRef);
        if (entityStateIndex < 0) {
          return false;
        }
        Entities.RemoveAt(entityStateIndex);
        return true;
      }

      public bool UpdateEntityState(EntityRef entityRef, Func<EntityState, EntityState> func) {
        var entityStateIndex = Entities.FindIndex(x => x.Entity == entityRef);
        if (entityStateIndex < 0) {
          return false;
        }

        var entityState = Entities[entityStateIndex];
        var newState = func(entityState);
        Entities[entityStateIndex] = newState;
        return true;
      }

      internal void RemovePendingEntities() {
        int pendingCount = 0;
        for (int i = Entities.Count - 1; i >= 0 && Entities[i].IsPending; --i) {
          ++pendingCount;
        }
        if (pendingCount > 0) {
          Entities.RemoveRange(Entities.Count - pendingCount, pendingCount);
        }
      }
    }

    #endregion Model

    #region Tree

    private sealed class DynamicAssetTreeViewItem : RunnersTreeViewItemBase {
      public int StateIndex;

      public DynamicAssetTreeViewItem(int id, int depth, int stateId) : base(id, depth, string.Empty) {
        this.StateIndex = stateId;
      }

      protected override QuantumSimulationObjectInspectorState GetInspectorStateInternal(RunnerState runner) {
        return runner.GetDynamicAssetInspector(StateIndex);
      }
    }

    private sealed class DynamicDBTreeViewItem : RunnersTreeViewItemBase {

      public DynamicDBTreeViewItem(int id, int depth) : base(id, depth, "DynamicDB") {
      }

      protected override QuantumSimulationObjectInspectorState GetInspectorStateInternal(RunnerState runner) {
        return runner.DynamicDBInspectorState;
      }
    }

    private sealed class EntitiesTreeViewItem : RunnersTreeViewItemBase {

      public EntitiesTreeViewItem(int id, int depth) : base(id, depth, "Entities") {
      }

      protected override QuantumSimulationObjectInspectorState GetInspectorStateInternal(RunnerState runner) {
        return runner.EntitiesInspectorState;
      }
    }

    private sealed class EntityTreeViewItem : RunnersTreeViewItemBase {
      public bool SholdBeHidden;
      public int StateIndex;

      public EntityTreeViewItem(int id, int depth, int stateIndex) : base(id, depth, string.Empty) {
        StateIndex = stateIndex;
      }

      public EntityRef Entity => State.Entity;
      public EntityState State => this.Runner.State.Entities[StateIndex];
      protected override QuantumSimulationObjectInspectorState GetInspectorStateInternal(RunnerState runner) {
        return runner.GetEntityInspector(StateIndex);
      }
    }

    private sealed class GlobalsTreeViewItem : RunnersTreeViewItemBase {

      public GlobalsTreeViewItem(int id, int depth) : base(id, depth, "Globals") {
      }

      protected override QuantumSimulationObjectInspectorState GetInspectorStateInternal(RunnerState runner) {
        return runner.GlobalsInspectorState;
      }
    }

    private unsafe sealed class RunnersTreeView : TreeView {
      public Action<Rect, EntityTreeViewItem> EntityContextMenu;
      public Action<Rect, RunnerTreeViewItem> RunnerContextMenu;
      private List<int> _forceShowIds = null;
      private Model _model;

      private List<TreeViewItem> _rowsBuffer = new List<TreeViewItem>();

      private GUIContent tempContent = new GUIContent();

      public RunnersTreeView(TreeViewState state, Model model) : base(state) {
        _model = model;
      }

      public event Action<RunnersTreeView, IList<int>> TreeSelectionChanged;
      
      public ComponentTypeSetSelector WithComponents { get; set; }

      public ComponentTypeSetSelector WithoutComponents { get; set; }

      public RunnersTreeViewItemBase FindById(int id) {
        return (RunnersTreeViewItemBase)this.FindItem(id, rootItem);
      }

      internal void SetForceShowItems(List<int> ids) {
        _forceShowIds = ids;
      }

      protected override TreeViewItem BuildRoot() {
        return new TreeViewItem() {
          id = 0,
          depth = -1,
          displayName = "Root"
        };
      }

      protected override IList<TreeViewItem> BuildRows(TreeViewItem root) {
        _rowsBuffer.Clear();

        ComponentSet withComponents = new ComponentSet();
        ComponentSet withoutComponents = new ComponentSet();

        foreach (var id in _model.GetComponentTypeIds()) {
          if (QuantumEditorUtility.TryGetComponentType(_model.GetComponentName(id), out var type)) {
            if (WithComponents?.ComponentTypeNames?.Contains(type.Name) == true) {
              withComponents.Add(id);
            }
            if (WithoutComponents?.ComponentTypeNames?.Contains(type.Name) == true) {
              withoutComponents.Add(id);
            }
          }
        }

        foreach (var runner in _model.Runners) {
          _model.GetFixedTreeNodeIds(runner.Id, out var runnerNodeId, out var globalsNodeId, out var entitiesNodeId, out var dynamicDBNodeId);

          string runnerLabel = $"{runner.Id} @ {runner.Tick}" +
            (runner.IsActive ? (runner.IsDefault ? " [default]" : "") : " [inactive]");

          var runnerRoot = new RunnerTreeViewItem(runnerNodeId, 0, runnerLabel, runner);
          _rowsBuffer.Add(runnerRoot);

          if (IsExpanded(runnerNodeId)) {
            _rowsBuffer.Add(new GlobalsTreeViewItem(globalsNodeId, 1));

            var entities = new EntitiesTreeViewItem(entitiesNodeId, 1);
            _rowsBuffer.Add(entities);

            if (IsExpanded(entitiesNodeId)) {
              int entityStateIndex = -1;
              foreach (var entity in runner.Entities) {
                ++entityStateIndex;

                var entityId = _model.GetTreeNodeIdForEntity(entitiesNodeId, entity);

                bool forcedShow = _forceShowIds?.Contains(entityId) == true;
                bool hiddenDueToComponentFilters = false;

                var componentSet = entity.EntityComponents;

                if (!withComponents.IsEmpty) {
                  if (!componentSet.IsSupersetOf(withComponents)) {
                    hiddenDueToComponentFilters = true;
                  }
                }

                if (!withoutComponents.IsEmpty) {
                  if (componentSet.Overlaps(withoutComponents)) {
                    hiddenDueToComponentFilters = true;
                  }
                }

                if (hiddenDueToComponentFilters && !forcedShow) {
                  continue;
                }

                var entityNode = new EntityTreeViewItem(entityId, 2, entityStateIndex) {
                  SholdBeHidden = hiddenDueToComponentFilters
                };
                _rowsBuffer.Add(entityNode);
              }
            } else if (runner.Entities.Any()) {
              entities.children = CreateChildListForCollapsedParent();
            }

            var dynamicDB = new DynamicDBTreeViewItem(dynamicDBNodeId, 1);
            _rowsBuffer.Add(dynamicDB);

            if (IsExpanded(dynamicDBNodeId)) {
              int dynamicAssetId = 0;
              foreach (var dynamicAsset in runner.DynamicDB) {
                _rowsBuffer.Add(new DynamicAssetTreeViewItem(_model.GetTreeNodeIdForDynamicAsset(dynamicDBNodeId, dynamicAsset.Guid), 2, dynamicAssetId++));
              }
            } else if (runner.DynamicDB.Any()) {
              dynamicDB.children = CreateChildListForCollapsedParent();
            }
          } else {
            runnerRoot.children = CreateChildListForCollapsedParent();
          }
        }
        SetupParentsAndChildrenFromDepths(root, _rowsBuffer);
        return _rowsBuffer;
      }

      protected override float GetCustomRowHeight(int row, TreeViewItem item) {
        return 16.0f;
      }

      protected override void RowGUI(RowGUIArgs args) {
        var item = (RunnersTreeViewItemBase)args.item;
        var rect = args.rowRect;

        RunnerTreeViewItem root = item.Runner;

        if (root == item && !args.selected) {
          GUI.Label(rect, GUIContent.none, UnityInternal.Styles.HierarchyTreeViewSceneBackground);
        }

        Debug.Assert(root != null);

        bool enabledState = GUI.enabled;

        using (new EditorGUI.DisabledScope(false)) {
          var r = rect;
          r.xMin += GetContentIndent(item);

          var style = UnityInternal.Styles.HierarchyTreeViewLine;
          bool shouldBeHidden = false;

          if (Event.current.type == EventType.Repaint) {
            if (item is EntityTreeViewItem entity) {
              if (string.IsNullOrEmpty(entity.displayName)) {
                var state = root.State.Entities[entity.StateIndex];
                if (state.IsPending) {
                  entity.displayName = $"<pending>";
                } else {
                  entity.displayName = state.Entity.ToString();
                }
              }
              shouldBeHidden = entity.SholdBeHidden;
            } else if (item is DynamicAssetTreeViewItem asset) {
              if (string.IsNullOrEmpty(asset.displayName)) {
                var state = root.State.DynamicDB[asset.StateIndex];
                asset.displayName = state.Guid.ToString();
              }
            }

            tempContent.text = item.displayName;

            var hasState = item.GetInspectorState(root.State) != null;

            using (style.FontStyleScope(italic: shouldBeHidden)) {
              GUI.enabled = root.RunnerModel.IsActive || hasState;
              style.Draw(r, tempContent, false, false, args.selected, args.focused);
              GUI.enabled = enabledState;
            }
          }

          var size = style.CalcSize(tempContent);

          if (item is EntityTreeViewItem) {
            // make sure the width is a multiple of thumbnail width; that way they'll align nicely
            size.x = Mathf.Ceil(size.x / QuantumEditorGUI.ThumbnailWidth) * QuantumEditorGUI.ThumbnailWidth;
          }

          r.xMin += size.x;
          rect = r;

          if (item is EntityTreeViewItem e) {
            var state = root.State.Entities[e.StateIndex];
            foreach (var componentName in _model.GetComponentNames(state.EntityComponents)) {
              rect = QuantumEditorGUI.ComponentThumbnailPrefix(rect, componentName);
            }
          } else if ( item is DynamicAssetTreeViewItem asset ) {
            var state = root.State.DynamicDB[asset.StateIndex];
            QuantumEditorGUI.AssetThumbnailPrefix(rect, state.Type);
          }
        }

        if (item is RunnerTreeViewItem runner) {
          QuantumEditorGUI.HandleContextMenu(args.rowRect, runner, RunnerContextMenu);
        } else if (item is EntityTreeViewItem entity) {
          QuantumEditorGUI.HandleContextMenu(args.rowRect, entity, EntityContextMenu, showButton: false);
        }
      }

      protected override void SelectionChanged(IList<int> selectedIds) {
        TreeSelectionChanged?.Invoke(this, selectedIds);
      }
    }

    private abstract class RunnersTreeViewItemBase : TreeViewItem {

      public RunnersTreeViewItemBase(int id, int depth, string name) : base(id, depth, name) {
      }

      public RunnerTreeViewItem Runner {
        get {
          for (TreeViewItem i = this; i != null; i = i.parent) {
            if (i is RunnerTreeViewItem r) {
              return r;
            }
          }
          throw new InvalidOperationException();
        }
      }

      public RunnerState RunnerModel => Runner.State;

      public QuantumSimulationObjectInspectorState GetInspectorState(RunnerState state = null) {
        return GetInspectorStateInternal(state ?? Runner.State);
      }

      protected abstract QuantumSimulationObjectInspectorState GetInspectorStateInternal(RunnerState runner);
    }

    private sealed class RunnerTreeViewItem : RunnersTreeViewItemBase {
      public RunnerState State;

      public RunnerTreeViewItem(int id, int depth, string name, RunnerState runner) : base(id, depth, name) {
        this.State = runner;
      }

      protected override QuantumSimulationObjectInspectorState GetInspectorStateInternal(RunnerState runner) {
        return runner.RunnerInspectorState;
      }
    }

    #endregion

    private sealed class CommandQueue : KeyedCollection<long, Tuple<string, DebugCommand.Payload>> {

      public Tuple<string, DebugCommand.Payload> RemoveReturn(long key) {
        if (!Contains(key)) {
          return null;
        } else {
          var result = this[key];
          this.Remove(key);
          return result;
        }
      }

      protected override long GetKeyForItem(Tuple<string, DebugCommand.Payload> item) {
        return item.Item2.Id;
      }
    }

    private sealed class Skin {
      public readonly GUIStyle inspectorBackground = new GUIStyle(UnityInternal.Styles.BoxWithBorders);

      public readonly GUIStyle inspectorPaddingStyle = new GUIStyle() {
        padding = new RectOffset(1, 0, 0, 0)
      };
      
      public readonly GUIContent titleContent = new GUIContent("Quantum State Inspector");
      public readonly GUIContent addComponentContent = new GUIContent("Add/Override Components");


      public GUIContent[] frameTypeOptions = new[] {
        new GUIContent("Predicted"),
        new GUIContent("Verified")
      };

      public Skin() {
        ++inspectorBackground.padding.left;
      }

      public float errorHeight => 40;
      public float toolbarDropdownButtonWidth => 15;
      public float toolbarHeight => 21;
    }
  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Tools/QuantumTaskProfilerModel.cs
namespace Quantum.Editor {
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.IO;
  using Photon.Deterministic;
  using Quantum.Profiling;
  using UnityEngine;
  using Color = UnityEngine.Color;

  [Serializable]
  public class QuantumTaskProfilerModel : ISerializationCallbackReceiver {

    public static readonly Color DefaultSampleColor = new Color(0.65f, 0.65f, 0.65f, 1.0f);

    public byte FormatVersion = BinaryFormat.Invalid;
    public List<Frame> Frames = new List<Frame>();
    public List<SampleMeta> SamplesMeta = new List<SampleMeta>();
    public List<QuantumProfilingClientInfo> Clients = new List<QuantumProfilingClientInfo>();

    /// <summary>
    /// 'QPRF'
    /// </summary>
    private const int BinaryHeader = 0x46525051;
    private const int InitialStackCapacity = 10;

    private Dictionary<string, int> _clientIdToIndex = new Dictionary<string, int>();
    private Dictionary<string, int> _samplePathToId = new Dictionary<string, int>();
    private Sample[] _samplesStack = new Sample[InitialStackCapacity];
    private int samplesStackCount = 0;

    public static QuantumTaskProfilerModel LoadFromFile(string path) {
      // first, try to read as a binary
      try {
        using (var serializer = new BinarySerializer(File.OpenRead(path), false)) {
          var result = new QuantumTaskProfilerModel();
          result.Serialize(serializer);
          return result;
        }
      } catch (System.InvalidOperationException) {
        // well, try to load as json now
        var text = File.ReadAllText(path);
        var result = JsonUtility.FromJson<QuantumTaskProfilerModel>(text);
        if (result.FormatVersion == 0) {
          var legacySession = JsonUtility.FromJson<LegacySerializableFrames>(text);
          if (legacySession.Frames != null) {
            result = new QuantumTaskProfilerModel();
            foreach (var frame in legacySession.Frames) {
              result.AddFrame(null, frame);
            }
          }
        }

        return result;
      }
    }

    public void AccumulateDurations(BitArray mask, int startFrame, List<float> target) {
      if (Frames.Count == 0)
        return;

      // we need to keep track of the match depth; if we have a match for a parent, 
      // we want to skip all the descendants
      int matchDepthPlusOne = 0;

      for (int frame = startFrame; frame < Frames.Count; ++frame) {
        long totalTicks = 0;
        var f = Frames[frame];
        foreach (var thread in f.Threads) {
          foreach (var sample in thread.Samples) {
            if (matchDepthPlusOne > 0) {
              if (sample.Depth + 1 > matchDepthPlusOne)
                continue;
              else
                matchDepthPlusOne = 0;
            }

            int mod = mask.Get(sample.Id) ? 1 : 0;

            totalTicks += sample.Duration * mod;
            matchDepthPlusOne = sample.Depth * mod;
          }
        }

        target.Add(Mathf.Min((float)(totalTicks * f.TicksToMS), f.DurationMS));
      }
    }

    public void AddFrame(QuantumProfilingClientInfo clientInfo, ProfilerContextData data) {
      var frame = new Frame();

      GetStartEndRange(data, out frame.Start, out frame.Duration);
      frame.TickFrequency = data.Frequency;
      frame.Number = data.Frame;
      frame.IsVerified = data.IsVerified;
      frame.SimulationId = data.SimulationId;
      if (clientInfo != null) {
        frame.ClientId = GetOrAddClientInfo(clientInfo);
      }

      foreach (var sourceThread in data.Profilers) {
        var thread = new Thread() {
          Name = sourceThread.Name
        };

        foreach (var sourceSample in sourceThread.Samples) {
          switch (sourceSample.Type) {
            case SampleType.Begin: {
                var sample = new Sample() {
                  Id = GetOrAddMetaId(sourceSample.Name),
                  Start = sourceSample.Time,
                };

                PushSample(sample);
              }
              break;

            case SampleType.End: {
                var sample = PopSample();
                var duration = sourceSample.Time - sample.Start;
                sample.Duration = duration;
                sample.Start -= frame.Start;
                sample.Depth = samplesStackCount;
                thread.Samples.Add(sample);
              }
              break;

            case SampleType.Event: {
                // events have duration of 0 and depth is always 0
                var sample = new Sample() {
                  Id = GetOrAddMetaId(sourceSample.Name),
                  Start = sourceSample.Time - frame.Start,
                  Duration = 0,
                  Depth = 0
                };

                thread.Samples.Add(sample);
              }
              break;

            default:
              break;
          }
        }

        frame.Threads.Add(thread);
      }

      Frames.Add(frame);
    }

    public void CreateSearchMask(string pattern, BitArray bitArray) {
      if (bitArray.Length < SamplesMeta.Count) {
        bitArray.Length = SamplesMeta.Count;
      }
      for (int i = 0; i < SamplesMeta.Count; ++i) {
        var name = SamplesMeta[i].Name;
        bitArray.Set(i, name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);
      }
    }

    public Frame FindPrevSafe(int index, bool verified = true) {
      if (index > Frames.Count || index <= 0)
        return null;

      for (int i = index - 1; i >= 0; --i) {
        if (Frames[i].IsVerified == verified)
          return Frames[i];
      }

      return null;
    }

    public int FrameIndexToSimulationIndex(int index) {
      if (Frames.Count == 0)
        return 0;

      int currentSimulation = Frames[0].SimulationId;
      int simulationIndex = 0;

      for (int i = 0; i < Frames.Count; ++i) {
        var frame = Frames[i];
        if (frame.SimulationId != currentSimulation) {
          ++simulationIndex;
          currentSimulation = frame.SimulationId;
        }

        if (i == index) {
          return simulationIndex;
        }
      }

      throw new InvalidOperationException();
    }

    public QuantumProfilingClientInfo GetClientInfo(Frame frame) {
      if (frame.ClientId < 0)
        return null;
      return Clients[frame.ClientId];
    }

    public void GetFrameDurations(List<float> values) {
      foreach (var f in Frames) {
        values.Add(f.DurationMS);
      }
    }

    public void GetSampleMeta(Sample s, out Color color, out string text) {
      var meta = SamplesMeta[s.Id];
      color = meta.Color;
      text = meta.Name;
    }

    public void GroupBySimulationId(List<float> values, List<float> grouped, List<float> counts = null) {
      Debug.Assert(values == null || values.Count == Frames.Count);
      if (Frames.Count == 0)
        return;

      int currentSimulation = Frames[0].SimulationId;
      float total = 0.0f;
      int count = 0;

      for (int i = 0; i < Frames.Count; ++i) {
        var frame = Frames[i];
        if (frame.SimulationId != currentSimulation) {
          grouped.Add(total);
          counts?.Add((float)count);
          count = 0;
          total = 0.0f;
          currentSimulation = frame.SimulationId;
        }
        ++count;
        total += values == null ? Frames[i].DurationMS : values[i];
      }

      counts?.Add((float)count);
      grouped.Add(total);
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() {

      _samplePathToId.Clear();
      for (int i = 0; i < SamplesMeta.Count; ++i) {
        _samplePathToId.Add(SamplesMeta[i].FullName, i);
      }

      _clientIdToIndex.Clear();
      for (int i = 0; i < Clients.Count; ++i) {
        _clientIdToIndex.Add(Clients[i].ProfilerId, i);
      }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() {
      FormatVersion = BinaryFormat.Latest;
    }

    public void Serialize(BinarySerializer serializer) {

      if (!serializer.IsReading) {
        ((ISerializationCallbackReceiver)this).OnBeforeSerialize();
      }

      int header = BinaryHeader;
      serializer.Serialize(ref header);
      if (header != BinaryHeader) {
        throw new InvalidOperationException("Invalid header");
      }

      serializer.Serialize(ref FormatVersion);
      if (FormatVersion > BinaryFormat.Latest || FormatVersion == 0) {
        throw new InvalidOperationException($"Version not supported: {FormatVersion}");
      }

      serializer.SerializeList(ref SamplesMeta, Serialize);
      serializer.SerializeList(ref Frames, Serialize);

      if (FormatVersion >= BinaryFormat.WithClientInfo) {
        serializer.SerializeList(ref Clients, Serialize);
      }

      if (serializer.IsReading) {
        ((ISerializationCallbackReceiver)this).OnAfterDeserialize();
      }
    }

    public int SimulationIndexToFrameIndex(int index, out int frameCount) {
      frameCount = 0;
      if (Frames.Count == 0)
        return 0;

      if (index == 0)
        return 0;

      int currentSimulation = Frames[0].SimulationId;
      int simulationIndex = 0;

      int i;
      for (i = 0; i < Frames.Count; ++i) {
        var frame = Frames[i];
        if (frame.SimulationId != currentSimulation) {
          ++simulationIndex;
          currentSimulation = frame.SimulationId;
          if (index == simulationIndex) {
            break;
          }
        }
      }

      var frameIndex = i;

      for (; i < Frames.Count; ++i) {
        var frame = Frames[i];
        if (frame.SimulationId == currentSimulation) {
          ++frameCount;
        } else {
          break;
        }
      }

      return frameIndex;
    }

    private static void GetStartEndRange(ProfilerContextData sourceFrame, out long min, out long range) {
      min = long.MaxValue;
      var max = long.MinValue;

      for (int i = 0; i < sourceFrame.Profilers.Length; ++i) {
        var p = sourceFrame.Profilers[i];
        if (p.Samples.Length > 0) {
          min = Math.Min(min, p.Samples[0].Time);
          max = Math.Max(max, p.Samples[p.Samples.Length - 1].Time);
        }
      }

      range = max - min;
    }

    private static string ProcessName(string name, out Color color) {
      if (name.Length >= 7 && name[name.Length - 7] == '#') {
        // possibly hex encoded color
        var hex = name.Substring(name.Length - 7);
        if (ColorUtility.TryParseHtmlString(hex, out color)) {
          return name.Substring(0, name.Length - 7).Trim();
        }
      }

      color = DefaultSampleColor;
      return name;
    }

    private int GetOrAddClientInfo(QuantumProfilingClientInfo info) {
      if (_clientIdToIndex.TryGetValue(info.ProfilerId, out int id)) {
        return id;
      }

      _clientIdToIndex.Add(info.ProfilerId, Clients.Count);
      Clients.Add(info);

      return Clients.Count - 1;
    }

    private int GetOrAddMetaId(string name) {
      if (_samplePathToId.TryGetValue(name, out int id)) {
        return id;
      }

      var shortName = ProcessName(name, out var color);

      _samplePathToId.Add(name, SamplesMeta.Count);
      SamplesMeta.Add(new SampleMeta() {
        Name = shortName,
        Color = color,
        FullName = name,
      });

      return SamplesMeta.Count - 1;
    }

    private Sample PopSample() {
      Debug.Assert(samplesStackCount > 0);
      return _samplesStack[--samplesStackCount];
    }

    private void PushSample(Sample sample) {
      Debug.Assert(samplesStackCount <= _samplesStack.Length);
      if (samplesStackCount + 1 >= _samplesStack.Length) {
        Array.Resize(ref _samplesStack, samplesStackCount + 10);
      }
      _samplesStack[samplesStackCount++] = sample;
    }

    private void Serialize(BinarySerializer serializer, ref SampleMeta meta) {
      serializer.Serialize(ref meta.FullName);
      serializer.Serialize(ref meta.Name);
      serializer.Serialize(ref meta.Color);
    }

    private void Serialize(BinarySerializer serializer, ref QuantumProfilingClientInfo info) {
      serializer.Serialize(ref info.ProfilerId);
      serializer.Serialize(ref info.Config, DeterministicSessionConfig.ToByteArray, DeterministicSessionConfig.FromByteArray);

      serializer.SerializeList(ref info.Properties, Serialize);
    }

    private void Serialize(BinarySerializer serializer, ref QuantumProfilingClientInfo.CustomProperty info) {
      serializer.Serialize(ref info.Name);
      serializer.Serialize(ref info.Value);
    }

    private void Serialize(BinarySerializer serializer, ref Frame frame) {
      if (FormatVersion < BinaryFormat.WithClientInfo) {
        string oldDeviceId = "";
        serializer.Serialize(ref oldDeviceId);
      } else {
        serializer.Serialize7BitEncoded(ref frame.ClientId);
      }

      serializer.Serialize7BitEncoded(ref frame.Duration);
      serializer.Serialize7BitEncoded(ref frame.TickFrequency);
      serializer.Serialize(ref frame.IsVerified);
      serializer.Serialize(ref frame.Start);
      serializer.Serialize(ref frame.Number);
      serializer.Serialize(ref frame.SimulationId);
      serializer.SerializeList(ref frame.Threads, Serialize);
    }

    private void Serialize(BinarySerializer serializer, ref Thread thread) {
      serializer.Serialize(ref thread.Name);
      serializer.SerializeList(ref thread.Samples, Serialize);
    }

    private void Serialize(BinarySerializer serializer, ref Sample sample) {
      serializer.Serialize7BitEncoded(ref sample.Id);
      serializer.Serialize7BitEncoded(ref sample.Start);
      serializer.Serialize7BitEncoded(ref sample.Duration);
      serializer.Serialize7BitEncoded(ref sample.Depth);
    }

    [Serializable]
    public struct Sample {
      public int Depth;
      public long Duration;
      public int Id;
      public long Start;
    }

    public static class BinaryFormat {
      public const byte Initial = 1;
      public const byte Invalid = 0;
      public const byte Latest = WithClientInfo;
      public const byte WithClientInfo = 2;
    }

    [Serializable]
    public class Frame {
      public int ClientId = -1;
      public long Duration;
      public bool IsVerified;
      public int Number;
      public int SimulationId;
      public long Start;
      public List<Thread> Threads = new List<Thread>();
      public long TickFrequency;
      public float DurationMS => (float)(Duration * TicksToMS);
      public double TicksToMS => 1000.0 / TickFrequency;
    }

    [Serializable]
    public class SampleMeta {
      public Color Color;
      public string FullName;
      public string Name;
    }

    [Serializable]
    public class Thread {
      public string Name;
      public List<Sample> Samples = new List<Sample>();
    }

    [Serializable]
    private class LegacySerializableFrames {
      public ProfilerContextData[] Frames = Array.Empty<ProfilerContextData>();
    }
  }
}

#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Tools/QuantumTaskProfilerWindow.cs
namespace Quantum.Editor {
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using Quantum.Profiling;
  using UnityEditor;
  using UnityEngine;
  using Debug = UnityEngine.Debug;

  public partial class QuantumTaskProfilerWindow : EditorWindow, ISerializationCallbackReceiver {

    [SerializeField]
    private QuantumTaskProfilerModel _session = new QuantumTaskProfilerModel();
    [SerializeField]
    private List<float> _cumulatedMatchingSamples = new List<float>();
    [SerializeField]
    private NavigationState _navigationState = new NavigationState();

    [SerializeField]
    private bool _groupBySimulationId;
    [SerializeField]
    private bool _isPlaying;
    [SerializeField]
    private bool _isRecording;

    [SerializeField]
    private float _lastSamplesHeight = 100.0f;
    [SerializeField]
    private float _navigationHeight = 100.0f;
    [SerializeField]
    private string _searchPhrase = "";
    [SerializeField]
    private List<DeviceEntry> _sources = new List<DeviceEntry>() {
      new DeviceEntry() { id = "Auto", lastAlive = 0, label = new GUIContent("Auto") },
      new DeviceEntry() { id = "Editor", lastAlive = 0, label = new GUIContent("Editor") },
    };
    private int _selectedSourceIndex = 0;


    [SerializeField]
    private ZoomPanel _navigationPanel = new ZoomPanel() {
      controlId = "_navigationPanel".GetHashCode(),
      minRange = 500.0f,
      start = 0,
      range = 1000.0f,
      verticalScroll = 1.0f,
    };

    [SerializeField]
    private ZoomPanel _samplesPanel = new ZoomPanel() {
      controlId = "_samplesPanel".GetHashCode(),
      minRange = Styles.MinVisibleRange,
      start = 0,
      range = 0.2f,
      allowScrollPastLimits = true,
      enableRangeSelect = true,
    };

    [SerializeField]
    private bool _showFullClientInfo;
    [SerializeField]
    private Vector2 _clientInfoScroll;

    [NonSerialized]
    private BitArray _searchMask = new BitArray(0);
    [NonSerialized]
    private float _lastUpdate;
    [NonSerialized]
    private SelectionInfo _selectionInfo;
    [NonSerialized]
    private TickHandler _ticks = new TickHandler();
    [NonSerialized]
    private List<QuantumTaskProfilerModel.Frame> _visibleFrames = new List<QuantumTaskProfilerModel.Frame>();
    [NonSerialized]
    private List<WeakReference<QuantumRunner>> _tracedRunners = new List<WeakReference<QuantumRunner>>();


    [MenuItem("Window/Quantum/Task Profiler")]
    [MenuItem("Quantum/Show Task Profiler", false, 45)]
    public static void ShowWindow() {
      GetWindow(typeof(QuantumTaskProfilerWindow));
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() {
      if (!string.IsNullOrEmpty(_searchPhrase)) {
        _session.CreateSearchMask(_searchPhrase, _searchMask);
      }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() {
    }

    public void OnProfilerSample(QuantumProfilingClientInfo clientInfo, ProfilerContextData data) {

      // find the source
      int sourceIndex = 1;
      for (; sourceIndex < _sources.Count; ++sourceIndex) {
        if (_sources[sourceIndex].id == clientInfo.ProfilerId)
          break;
      }

      // add the new one if needed
      if (sourceIndex >= _sources.Count) {
        sourceIndex = _sources.Count;

        _sources.Add(new DeviceEntry() {
          id = clientInfo.ProfilerId,
          label = new GUIContent($"{clientInfo.GetProperty("MachineName")} ({clientInfo.ProfilerId})")
        });
      }

      // touch
      _sources[sourceIndex].lastAlive = DateTime.Now.ToFileTime();
      if (_isRecording == false) {
        return;
      }

      // auto select source
      if (_selectedSourceIndex == 0) {
        _selectedSourceIndex = sourceIndex;
      } else if (_selectedSourceIndex != sourceIndex) {
        return;
      }

      _session.AddFrame(clientInfo, data);

      if (_navigationState.IsCurrent) {
        _navigationPanel.start = _navigationState.durations.Count - _navigationPanel.range;
      }

      RefreshSearch(startFrame: _session.Frames.Count - 1);
      Repaint();
    }

    private void Clear() {
      _selectionInfo = new SelectionInfo();
      _samplesPanel.start = 0;
      _samplesPanel.range = 0.2f;
      _session = new QuantumTaskProfilerModel();
      _cumulatedMatchingSamples.Clear();
      _samplesPanel.verticalScroll = 0.0f;
      _navigationPanel.start = 0;
      _navigationPanel.range = 100.0f;
      _navigationState.selectedIndex = -1;
    }

    private void DoGridGUI(Rect rect, float frameTime) {
      if (Event.current.type != EventType.Repaint)
        return;

      Styles.profilerGraphBackground.Draw(rect, false, false, false, false);

      using (new GUI.ClipScope(rect)) {
        rect.x = rect.y = 0;

        Color tickColor = Styles.timelineTick.normal.textColor;
        tickColor.a = 0.1f;

        GL.Begin(GL.LINES);

        for (int l = 0; l < _ticks.VisibleLevelsCount; l++) {
          var strength = _ticks.GetStrengthOfLevel(l) * .9f;
          if (strength > 0.5f) {
            foreach (var tick in _ticks.GetTicksAtLevel(l, true)) {
              var x = _samplesPanel.TimeToPixel(tick, rect);
              DrawVerticalLineFast(x, 0, rect.height, tickColor);
            }
          }
        }

        // Draw frame start and end delimiters
        DrawVerticalLineFast(_samplesPanel.TimeToPixel(0, rect), 0, rect.height, Styles.frameDelimiterColor);
        DrawVerticalLineFast(_samplesPanel.TimeToPixel(frameTime, rect), 0, rect.height, Styles.frameDelimiterColor);

        GL.End();
      }
    }

    private void DoNavigationGUI(Rect rect, NavigationState info, float maxY) {
      using (new GUI.GroupScope(rect, Styles.profilerGraphBackground)) {
        rect = rect.ZeroXY();
        var r = rect.Adjust(0, 3, 0, -4);

        float timeToY = r.height / maxY;

        if (info.highlight.Count > 0) {
          int highlightStart = 0;

          if (Event.current.type == EventType.Repaint) {
            UnityInternal.HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.QUADS);
            try {
              for (int i = 0; i < info.highlight.Count; ++i) {
                var x = _navigationPanel.TimeToPixel(i, rect);
                if (info.highlight[i]) {
                  if (highlightStart < 0) {
                    highlightStart = i;
                  }
                } else {
                  if (highlightStart >= 0) {

                    var width = x - _navigationPanel.TimeToPixel(highlightStart, rect);
                    var highlightRect = rect;

                    highlightRect.x = x - width;
                    highlightRect.width = width;

                    DrawRectFast(highlightRect, new Color(0, 0, 0, Mathf.Lerp(0.02f, 0.1f, width)));
                    highlightStart = -1;
                  }
                }

                if (x > rect.width) {
                  // we also want the fist point outside of the visible scope, then we're done
                  break;
                }
              }
            } finally {
              GL.End();
            }
          }
        }

        if (info.durations.Count > 0) {
          DrawGraph(rect, info.durations, _navigationPanel, maxY, color: Color.yellow, lineWidth: 2);
        }

        if (info.searchResults.Count > 0) {
          DrawGraph(rect, info.searchResults, _navigationPanel, maxY, color: Styles.SearchHighlightColor, lineWidth: 3);
        }

        using (new Handles.DrawingScope(new Color(1, 1, 1, 0.2f))) {
          foreach (var gridLine in Styles.NavigationGridLines) {
            if (gridLine > maxY)
              continue;
            var labelRect = DrawDropShadowLabelWithMargins(r, gridLine, maxY, 0);
            var y = (maxY - gridLine) * timeToY + r.y;
            Handles.DrawLine(new Vector2(r.xMin + labelRect.xMax, y), new Vector2(r.xMax, y));
          }
        }

        if (_navigationPanel.selectionRange.HasValue) {
          info.selectedIndex = Mathf.Clamp(Mathf.RoundToInt(_navigationPanel.selectionRange.Value.x), 0, info.durations.Count - 1);
          _navigationPanel.selectionRange = null;
        }

        if (info.selectedIndex > 0) {
          using (new Handles.DrawingScope(Styles.selectedFrameColor)) {
            var x = _navigationPanel.TimeToPixel(info.selectedIndex, rect);
            Handles.DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax));

            var oldContentColor = GUI.contentColor;
            try {
              DrawDropShadowLabelWithMargins(r, info.durations[info.selectedIndex], maxY, x, -1.0f, color: Color.yellow);

              if (info.searchResults.Count > info.selectedIndex) {
                DrawDropShadowLabelWithMargins(r, Mathf.Min(info.durations[info.selectedIndex], info.searchResults[info.selectedIndex]), maxY, x, color: Styles.SearchHighlightColor);
              }
            } finally {
              GUI.contentColor = oldContentColor;
            }
          }
        }
      }
    }

    private void DoSampleGUI(QuantumTaskProfilerModel.Sample sample, float durationMS, Rect rect, Rect clippedRect, bool selected) {
      GetDrawData(sample, out Color color, out string label);

      if (selected) {
        color = Color.Lerp(color, Color.white, 0.25f);
      }

      if (_searchMask.Count > sample.Id && _searchMask.Get(sample.Id) == false) {
        color = Color.Lerp(color, new Color(0, 0, 0, 0.1f), 0.75f);
      }

      DrawSolidRectangleWithOutline(rect, color, Color.Lerp(color, Color.black, 0.25f));

      if (Event.current.type != EventType.Repaint)
        return;

      if (clippedRect.width > 5.0f) {
        Styles.sampleStyle.Draw(clippedRect, string.Format("{0} ({1:F3}ms)", label, durationMS), false, false, false, false);
      }
    }

    private float DoSamplesGUI(Rect samplesRect, Rect legendRect, IEnumerable<QuantumTaskProfilerModel.Frame> frames) {
      var baseY = Styles.SampleSpacing - _samplesPanel.verticalScroll;
      var startY = baseY;

      var threadsLookup = frames.SelectMany(x => x.Threads).Select(x => x.Name).Distinct().ToDictionary(x => x, x => 0.0f);

      using (new GUI.GroupScope(samplesRect)) {
        samplesRect = samplesRect.ZeroXY();

        Rect tooltipRect = new Rect();
        GUIContent tooltipContent = GUIContent.none;

        foreach (var threadName in threadsLookup.Keys.ToList()) {
          float frameStartTime = 0.0f;
          float initialY = baseY;

          baseY += Styles.EventHeight;
          int maxDepth = 1;

          foreach (var frame in frames) {
            var ticksToMS = frame.TicksToMS;

            for (int i = 0; i < frame.Threads.Count; ++i) {
              var thread = frame.Threads[i];
              if (thread.Name != threadName)
                continue;

              for (int j = 0; j < thread.Samples.Count; ++j) {
                var sample = thread.Samples[j];

                bool isSelected = _selectionInfo.thread == thread && _selectionInfo.sample == j;
                float time = 0;
                Rect sampleRect;

                maxDepth = Mathf.Max(maxDepth, sample.Depth + 1);

                if (sample.Duration == 0) {
                  GetDrawData(sample, out Color color, out string label);
                  time = (float)(sample.Start * ticksToMS) + frameStartTime;
                  sampleRect = new Rect(_samplesPanel.TimeToPixel(time, samplesRect) - Styles.eventMarker.width / 2, baseY - Styles.EventHeight + 1, Styles.eventMarker.width, Styles.eventMarker.height);
                  GUI.DrawTexture(sampleRect, Styles.eventMarker, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
                } else {
                  var x = _samplesPanel.TimeToPixel((float)(sample.Start * ticksToMS) + frameStartTime, samplesRect);
                  var duration = (float)(sample.Duration * ticksToMS);
                  var width = _samplesPanel.DurationToPixelLength(duration, samplesRect);
                  var r = new Rect(x, baseY + sample.Depth * (Styles.SampleHeight + Styles.SampleSpacing), width, Styles.SampleHeight);

                  time = duration;
                  sampleRect = Rect.MinMaxRect(Mathf.Max(r.x, 0.0f), Mathf.Max(r.y, 0.0f), Mathf.Min(r.xMax, samplesRect.width), Mathf.Min(r.yMax, samplesRect.height));
                  DoSampleGUI(sample, duration, r, sampleRect, isSelected);
                }

                if (Event.current.type == EventType.MouseUp && GUIUtility.hotControl == 0 && sampleRect.Contains(Event.current.mousePosition)) {
                  isSelected = true;
                  Event.current.Use();
                }

                if (isSelected) {
                  _selectionInfo = new SelectionInfo() {
                    thread = thread,
                    sample = j,
                  };

                  tooltipRect = sampleRect;
                  GetDrawData(sample, out var dummy, out var name);
                  tooltipContent = new GUIContent(string.Format("{0}\n{1}", name, FormatTime(time)));
                }
              }
            }

            frameStartTime += frame.DurationMS;
          }

          baseY += maxDepth * Styles.SampleHeight + (maxDepth - 1) * Styles.SampleSpacing + Styles.ThreadSpacing;

          using (new Handles.DrawingScope(Color.black)) {
            Handles.DrawLine(new Vector3(0, baseY), new Vector3(samplesRect.width, baseY));
          }

          threadsLookup[threadName] = baseY - initialY;
        }

        if (tooltipRect.width > 0) {
          QuantumEditorGUI.LargeTooltip(samplesRect, tooltipRect, tooltipContent);
        }

        {
          float frameStartTime = 0.0f;

          foreach (var frame in frames) {
            var x = _samplesPanel.TimeToPixel(frameStartTime, samplesRect);
            using (new Handles.DrawingScope(Color.gray)) {
              Handles.DrawLine(new Vector3(x, 0), new Vector3(x, samplesRect.height));
            }
            frameStartTime += frame.DurationMS;
          }
        }
      }

      using (new GUI.GroupScope(legendRect)) {
        float y = Styles.SampleSpacing - _samplesPanel.verticalScroll;

        foreach (var kv in threadsLookup.OrderBy(x => x.Key)) {
          DrawLegendLabel(new Rect(0, y, legendRect.width, kv.Value), kv.Key);
          y += kv.Value;
        }
      }

      return baseY - startY;
    }

    private void DoSelectionGUI(Rect samplesRect, Rect timelineRect) {
      if (_samplesPanel.selectionRange != null) {
        var timeRange = _samplesPanel.selectionRange.Value;
        using (new GUI.ClipScope(samplesRect)) {
          samplesRect = samplesRect.ZeroXY();
          var xMin = _samplesPanel.TimeToPixel(timeRange.x, samplesRect);
          var xMax = _samplesPanel.TimeToPixel(timeRange.y, samplesRect);
          EditorGUI.DrawRect(Rect.MinMaxRect(xMin, samplesRect.yMin, xMax, samplesRect.yMax), Styles.rangeSelectionColor);
        }

        using (new GUI.ClipScope(timelineRect)) {
          timelineRect = timelineRect.ZeroXY();
          var xMin = _samplesPanel.TimeToPixel(timeRange.x, timelineRect);
          var xMax = _samplesPanel.TimeToPixel(timeRange.y, timelineRect);
          var xCentre = (xMax + xMin) / 2.0f;
          DrawDropShadowLabel(timeRange.y - timeRange.x, xCentre, timelineRect.yMax, -0.5f, -1.0f);
        }
      }
    }

    private void DoFitButtonGUI(Rect rect, float visibleRange) {

      try {
        GUI.BeginClip(rect);
        rect = rect.ZeroXY();

        
        if (GUI.Button(rect, GUIContent.none, EditorStyles.toolbarButton)) {
          // add tiny margin
          _samplesPanel.start = -visibleRange * 0.02f;
          _samplesPanel.range = visibleRange * 1.04f;
          GUIUtility.ExitGUI();
        }
        
        // the label is so small it needs to be drawn on top with the "small" style
        var labelSize = EditorStyles.miniLabel.CalcSize(Styles.fitButtonContent);
        var labelOffset = new Vector2(labelSize.x - rect.width, labelSize.y - rect.height);
        var labelRect = new Rect(-labelOffset.x / 2, -labelOffset.y / 2, labelSize.x, labelSize.y);
        GUI.Label(labelRect, Styles.fitButtonContent, EditorStyles.miniLabel);

      } finally {
        GUI.EndClip();
      }
    }

    private void DoTickbarGUI(Rect rect, Color tickColor) {
      if (Event.current.type != EventType.Repaint) {
        return;
      }

      GUI.Box(rect, GUIContent.none, EditorStyles.toolbarButton);
      GUI.BeginClip(rect);
      try {
        var clipRect = rect.ZeroXY();
        UnityInternal.HandleUtility.ApplyWireMaterial();
        GL.Begin(GL.LINES);
        try {
          for (int i = 0; i < _ticks.VisibleLevelsCount; i++) {
            float strength = _ticks.GetStrengthOfLevel(i) * 0.8f;
            if (!(strength < 0.1f)) {
              foreach (float tick in _ticks.GetTicksAtLevel(i, excludeTicksFromHigherLevels: true)) {
                float x = _samplesPanel.TimeToPixel(tick, clipRect);
                var height = clipRect.height * Mathf.Min(1, strength) * Styles.MaxTickHeight;
                DrawVerticalLineFast(x, clipRect.height - height + 0.5f, clipRect.height - 0.5f, tickColor);
              }
            }
          }
        } finally {
          GL.End();
        }

        int labelLevel = _ticks.GetLevelWithMinSeparation(Styles.TickLabelWidth);
        foreach (var tick in _ticks.GetTicksAtLevel(labelLevel, false)) {
          float labelpos = Mathf.Floor(_samplesPanel.TimeToPixel(tick, clipRect));
          string label = FormatTickLabel(tick, labelLevel);
          GUI.Label(new Rect(labelpos + 3, -3, Styles.TickLabelWidth, 20), label, Styles.timelineTick);
        }
      } finally {
        GUI.EndClip();
      }
    }

    private void DoToolbarGUI(Rect toolbarRect, NavigationState state, string selectedLabel) {
      using (new GUI.GroupScope(toolbarRect, EditorStyles.toolbar)) {
        using (new GUILayout.HorizontalScope()) {
          _isRecording = GUILayout.Toggle(_isRecording, "Record", EditorStyles.toolbarButton);

          _selectedSourceIndex = EditorGUILayout.Popup(_selectedSourceIndex, _sources.Select(x => x.label).ToArray(), EditorStyles.toolbarPopup, GUILayout.MaxWidth(60));

          if (GUILayout.Button("New Window", EditorStyles.toolbarButton)) {
            CreateInstance<QuantumTaskProfilerWindow>().Show();
          }

          GUILayout.FlexibleSpace();

          if (state.EffectiveSelectedIndex > 0) {
            var selectedDuration = state.durations[state.EffectiveSelectedIndex];
            CalculateMeanStdDev(state.durations, out var mean, out var stdDev);

            GUILayout.Label(selectedLabel, Styles.toolbarLabel);
            GUILayout.Label(string.Format("CPU: {0}", FormatTime(state.durations[state.EffectiveSelectedIndex])), Styles.toolbarLabel);
            GUILayout.Label(string.Format("Mean: {0}", FormatTime((float)mean)), Styles.toolbarLabel);
            GUILayout.Label(string.Format("σ: {0}", FormatTime((float)stdDev)), Styles.toolbarLabel);
          }

          GUILayout.FlexibleSpace();

          if (GUILayout.Button("Clear", EditorStyles.toolbarButton)) {
            Clear();
            GUIUtility.ExitGUI();
          }

          if (GUILayout.Button("Load", EditorStyles.toolbarButton)) {
            var path = EditorUtility.OpenFilePanelWithFilters("Open Profiler Report", ".", new[] { "Profiler Report", "dat,json" });
            if (!string.IsNullOrEmpty(path)) {
              LoadFile(path);
              GUIUtility.ExitGUI();
            }
          }

          using (new EditorGUI.DisabledScope(_session.Frames.Count == 0)) {
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) {
              string fileName;
              if (_selectedSourceIndex == 0) {
                fileName = "ProfilerReport";
              } else {
                fileName = $"ProfilerReport_{_sources[_selectedSourceIndex].id}";
              }
              var target = EditorUtility.SaveFilePanel("Save Profiler Report", ".", fileName, "dat,json");
              if (!string.IsNullOrEmpty(target)) {
                if (Path.GetExtension(target).Equals(".json", StringComparison.InvariantCultureIgnoreCase)) {
                  File.WriteAllText(target, JsonUtility.ToJson(_session));
                } else {
                  using (var serializer = new BinarySerializer(File.Create(target), true)) {
                    _session.Serialize(serializer);
                  }
                }
              }
            }
          }

          {
            GUILayout.Label("Frame:", Styles.toolbarLabel);
            var frameLabel = "Current";
            if (state.selectedIndex >= 0) {
              frameLabel = string.Format("   {0} / {1}", state.selectedIndex + 1, state.durations.Count);
            }
            GUILayout.Label(frameLabel, Styles.toolbarLabel, GUILayout.Width(100));
          }

          // Previous/next/current buttons
          using (new EditorGUI.DisabledScope(!state.CanSelectPreviousFrame)) {
            if (GUILayout.Button(Styles.prevFrame, EditorStyles.toolbarButton))
              state.SelectPrevFrame();
          }

          using (new EditorGUI.DisabledScope(!state.CanSelectNextFrame)) {
            if (GUILayout.Button(Styles.nextFrame, EditorStyles.toolbarButton))
              state.SelectNextFrame();
          }

          GUILayout.Space(10);
          if (GUILayout.Button(Styles.currentFrame, EditorStyles.toolbarButton)) {
            state.SelectCurrentFrame();
          }

          GUILayout.Space(5);
        }
      }
    }

    private string FormatTickLabel(float time, int level) {
      string format = "{0}ms";
      float periodOfLevel = _ticks.GetPeriodOfLevel(level);
      int log10 = Mathf.FloorToInt(Mathf.Log10(periodOfLevel));
      if (log10 >= 3) {
        time /= 1000f;
        format = "{0}s";
      }
      return string.Format(format, time.ToString("N" + Mathf.Max(0, -log10)));
    }

    private void GetDrawData(QuantumTaskProfilerModel.Sample s, out Color color, out string text) {
      _session.GetSampleMeta(s, out color, out text);
    }

    private void LoadFile(string file) {
      if (!string.IsNullOrWhiteSpace(file)) {
        try {
          _session = QuantumTaskProfilerModel.LoadFromFile(file);
          _navigationPanel.start = 0;
          _navigationPanel.range = _session.Frames.Count;
          RefreshSearch();
        } catch (Exception exn) {
          Debug.LogException(exn);
        }
      }
    }

    private void OnEnable() {
      titleContent.text = "Quantum Task Profiler";
      minSize = new Vector2(200, 200);
    }

    private void OnGUI() {
      string toolbarLabel;

      toolbarLabel = "";

      _visibleFrames.Clear();

      RemoveDeadSources();

      QuantumTaskProfilerModel.Frame currentFrame;

      _navigationState.Refresh(_session, _cumulatedMatchingSamples, _groupBySimulationId);
      if (_navigationState.EffectiveSelectedIndex > 0) {
        if (_groupBySimulationId) {
          var frameIndex = _session.SimulationIndexToFrameIndex(_navigationState.EffectiveSelectedIndex, out var frameCount);
          currentFrame = _session.Frames[frameIndex];
          for (int i = 0; i < frameCount; ++i) {
            _visibleFrames.Add(_session.Frames[frameIndex + i]);
          }
          toolbarLabel = $"Frames#: ({currentFrame.Number}-{currentFrame.Number + frameCount})";
        } else {
          var frameIndex = _navigationState.EffectiveSelectedIndex;
          currentFrame = _session.Frames[frameIndex];
          _visibleFrames.Add(currentFrame);

          if (currentFrame.IsVerified) {
            toolbarLabel = $"Frame#: {currentFrame.Number}";
          } else {
            var prevVerified = _session.FindPrevSafe(frameIndex);
            if (prevVerified != null) {
              toolbarLabel = $"Frame#: {prevVerified.Number} (+{currentFrame.Number - prevVerified.Number})";
            } else {
              toolbarLabel = $"Frame#: ? + {currentFrame.Number}";
            }
          }
        }
      } else {
        currentFrame = null;
      }

      var toolbarRect = new Rect(0, 0, position.width, Styles.ToolbarHeight);
      DoToolbarGUI(toolbarRect, _navigationState, toolbarLabel);

      var clientInfoRect = new Rect(0, toolbarRect.yMax, position.width, _showFullClientInfo ? 200 : Styles.ToolbarHeight);
      if (currentFrame != null) {
        var clientInfo = _session.GetClientInfo(currentFrame);
        if (clientInfo != null) {
          DoClientInfoGUI(clientInfoRect, clientInfo);
        } else {
          clientInfoRect.height = 0;
        }
      } else {
        clientInfoRect.height = 0;
      }

      var navigationLabelRect = new Rect(0, clientInfoRect.yMax, Styles.LeftPaneWidth, _navigationHeight);
      DrawLegendLabel(navigationLabelRect, "CPU Usage");

      EditorGUI.BeginChangeCheck();
      _groupBySimulationId = EditorGUI.ToggleLeft(navigationLabelRect.AddLine().SetLineHeight().Adjust(5, 5, 0, 0), "Group By Simulation", _groupBySimulationId);
      if (EditorGUI.EndChangeCheck() && _navigationState.selectedIndex > 0) {
        // translate index
        if (_groupBySimulationId) {
          _navigationState.selectedIndex = _session.FrameIndexToSimulationIndex(_navigationState.selectedIndex);
        } else {
          _navigationState.selectedIndex = _session.SimulationIndexToFrameIndex(_navigationState.selectedIndex, out var frameCount);
        }

        // make sure the selection is visible
        _navigationPanel.start = _navigationState.selectedIndex - _navigationPanel.range / 2.0f;
        GUIUtility.ExitGUI();
      }

      var navigationBarRect = new Rect(Styles.LeftPaneWidth, clientInfoRect.yMax, position.width - Styles.LeftPaneWidth, _navigationHeight);

      _navigationPanel.minRange = Mathf.Min(_navigationState.durations.Count, (navigationBarRect.width - Styles.ScrollBarWidth) * 0.33f);
      _navigationPanel.OnGUI(navigationBarRect, 0.0f, _navigationState.durations.Count, out bool dummy, verticalSlider: true, minY: Styles.MinYRange, maxY: Styles.MaxYRange);

      DoNavigationGUI(_navigationPanel.areaRect, _navigationState, _navigationPanel.verticalScroll);

      var samplesRect = navigationBarRect.AddY(navigationBarRect.height + Styles.TimelineHeight).SetXMax(position.width).SetYMax(position.height);
      var visibleRange = _visibleFrames.Any() ? _visibleFrames.Sum(x => x.DurationMS) : 1.0f;

      _samplesPanel.OnGUI(samplesRect, 0.0f, visibleRange, out bool unselect, maxY: _lastSamplesHeight);
      if (unselect) {
        _selectionInfo = new SelectionInfo();
      }

      var samplesAreaRect = _samplesPanel.areaRect;
      var timelineRect = navigationBarRect.AddY(navigationBarRect.height).SetHeight(Styles.TimelineHeight).SetWidth(samplesAreaRect.width);

      _ticks.Refresh(_samplesPanel.start, _samplesPanel.range, timelineRect.width);
      DoTickbarGUI(timelineRect, Styles.timelineTick.normal.textColor);
      DoFitButtonGUI(timelineRect.SetXMin(timelineRect.xMax).SetWidth(Styles.ScrollBarWidth), visibleRange);

      _navigationHeight += DrawSplitter(timelineRect.SetHeight(5));
      _navigationHeight = Mathf.Clamp(_navigationHeight, 50.0f, position.height - 100);

      EditorGUI.BeginChangeCheck();
      _searchPhrase = UnityInternal.EditorGUI.ToolbarSearchField(Styles.ToolbarSearchFieldId, timelineRect.SetX(0).SetWidth(Styles.LeftPaneWidth).Adjust(1, 1, -2, -2), _searchPhrase, false);
      if (EditorGUI.EndChangeCheck()) {
        RefreshSearch();
      }

      DoGridGUI(samplesAreaRect, 0);

      if (_visibleFrames.Any()) {
        var minX = _samplesPanel.TimeToPixel(0.0f);
        var maxX = _samplesPanel.TimeToPixel(visibleRange);

        if (minX > samplesAreaRect.xMin) {
          EditorGUI.DrawRect(samplesAreaRect.SetXMax(minX), Styles.outOfRangeColor);
        }

        if (maxX < samplesAreaRect.xMax) {
          EditorGUI.DrawRect(samplesAreaRect.SetX(maxX).SetWidth(samplesAreaRect.xMax - maxX), Styles.outOfRangeColor);
        }
      }

      var legendRect = samplesRect.SetX(0).SetWidth(Styles.LeftPaneWidth);
      _lastSamplesHeight = DoSamplesGUI(_samplesPanel.areaRect, legendRect, _visibleFrames);
      DoSelectionGUI(samplesAreaRect, timelineRect);
    }

    private void DoClientInfoGUI(Rect rect, QuantumProfilingClientInfo clientInfo) {
      using (new GUILayout.AreaScope(rect, GUIContent.none, EditorStyles.toolbar)) {
        using (new GUILayout.HorizontalScope()) {

          GUILayout.FlexibleSpace();

          foreach (var inline in Styles.InlineProperties) {
            GUILayout.Label($"{inline}: {clientInfo.GetProperty(inline)}", Styles.toolbarLabel);
          }
          GUILayout.FlexibleSpace();

          EditorGUI.BeginChangeCheck();
          _showFullClientInfo = GUILayout.Toggle(_showFullClientInfo, "More", EditorStyles.toolbarButton);
          if (EditorGUI.EndChangeCheck()) {
            GUIUtility.ExitGUI();
          }
        }

        if (_showFullClientInfo) {
          using (new QuantumEditorGUI.LabelWidthScope(220)) {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_clientInfoScroll)) {
              _clientInfoScroll = scroll.scrollPosition;
              EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
              foreach (var prop in clientInfo.Properties) {
                using (new EditorGUILayout.HorizontalScope()) {
                  var r = EditorGUILayout.GetControlRect(true);
                  r = EditorGUI.PrefixLabel(r, new GUIContent(prop.Name));
                  EditorGUI.SelectableLabel(r, prop.Value);
                }
              }

              EditorGUILayout.Space();
              EditorGUILayout.LabelField("DeterministicConfig", EditorStyles.boldLabel);
              if (clientInfo.Config != null) {
                foreach (var f in clientInfo.Config.GetType().GetFields()) {
                  using (new EditorGUILayout.HorizontalScope()) {
                    var r = EditorGUILayout.GetControlRect(true);
                    r = EditorGUI.PrefixLabel(r, new GUIContent(f.Name));
                    EditorGUI.SelectableLabel(r, f.GetValue(clientInfo.Config)?.ToString());
                  }
                }

              }
            }
          }
        }
      }
    }

    private void RemoveDeadSources() {
      var sourceTTL = TimeSpan.FromSeconds(120);
      // ignore the first two ones (any and the editor)
      for (int i = 2, originalIndex = i; i < _sources.Count; ++i, ++originalIndex) {
        var source = _sources[i];
        var lastAlive = DateTime.FromFileTime(source.lastAlive);
        if (DateTime.Now - lastAlive > sourceTTL) {
          if (_selectedSourceIndex == originalIndex) {
            // don't remove the selected one
            continue;
          }
          _sources.RemoveAt(i--);
        }
      }
    }

    private void RefreshSearch(int? startFrame = null) {
      if (startFrame == null) {
        _cumulatedMatchingSamples.Clear();
        startFrame = 0;
      }

      if (!string.IsNullOrWhiteSpace(_searchPhrase)) {
        _session.CreateSearchMask(_searchPhrase, _searchMask);
        _session.AccumulateDurations(_searchMask, startFrame.Value, _cumulatedMatchingSamples);
      } else {
        _searchMask.Length = 0;
        _cumulatedMatchingSamples.Clear();
      }
    }

    private void Update() {

      // hook up in editor profiling
      {
        foreach (var runner in QuantumRunner.ActiveRunners) {
          if (runner.Game == null || !runner.IsRunning)
            continue;

          foreach (var weakRef in _tracedRunners) {
            if (weakRef.TryGetTarget(out var target) && target == runner) {
              goto Next;
            }
          }

          Debug.Log($"Attaching to a local runner {runner}");
          var info = new QuantumProfilingClientInfo(runner.Id, runner.Session.SessionConfig, runner.Session.PlatformInfo);
          info.ProfilerId = "Editor";

          runner.Game.ProfilerSampleGenerated += (data) => OnProfilerSample(info, data);
          _tracedRunners.Add(new WeakReference<QuantumRunner>(runner));
        Next:;
        }
        // clean up all dead ones
        _tracedRunners.RemoveAll(x => !x.TryGetTarget(out var dummy));
      }

      QuantumProfilingServer.SampleReceived -= OnProfilerSample;
      QuantumProfilingServer.SampleReceived += OnProfilerSample;
      QuantumProfilingServer.Update();

      if (EditorApplication.isPlaying != _isPlaying) {
        _lastUpdate = 0;
        _isPlaying = EditorApplication.isPlaying;
      }

      var now = Time.realtimeSinceStartup;
      if (now > (_lastUpdate + (1f / 30f))) {
        _lastUpdate = now;
        Repaint();
      }
    }
    private struct SelectionInfo {
      public int sample;
      public QuantumTaskProfilerModel.Thread thread;
    }

    private static class Styles {

#if UNITY_2019_3_OR_NEWER
      public static GUIStyle toolbarLabel => EditorStyles.label;
      public const float ToolbarHeight = 21.0f;
#else
      public static GUIStyle toolbarLabel => EditorStyles.miniLabel;
      public const float ToolbarHeight = 18.0f;
#endif

      public static readonly GUIContent fitButtonContent = new GUIContent("↔");

      public const float MinYRange = 0.2f;
      public const float MaxYRange = 1000.0f;

      public const float DragPixelsThreshold = 5.0f;
      public const float EventHeight = 16.0f;
      public const float LeftPaneWidth = 180.0f;
      public const float MaxTickHeight = 0.7f;
      public const float MinVisibleRange = 0.001f;
      public const float NavigationBarHeight = 90.0f;
      public const float SampleHeight = 16.0f;
      public const float SampleSpacing = 1.0f;
      public const float ScrollBarWidth = 16.0f;
      public const float ThreadSpacing = 10.0f;
      public const float TickLabelWidth = 60.0f;
      public const float TimelineHeight = 16.0f;
      public static readonly GUIContent currentFrame = EditorGUIUtility.TrTextContent("Current", "Go to current frame");
      public static readonly Texture eventMarker = EditorGUIUtility.IconContent("Animation.EventMarker").image;
      public static readonly Color frameDelimiterColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
      public static readonly GUIStyle leftPane = "ProfilerTimelineLeftPane";
      public static readonly GUIStyle legendBackground = "ProfilerLeftPane";
      public static readonly GUIContent nextFrame = EditorGUIUtility.TrIconContent("Profiler.NextFrame", "Go one frame forwards");
      public static readonly Color outOfRangeColor = new Color(0, 0, 0, 0.1f);
      public static readonly GUIContent prevFrame = EditorGUIUtility.TrIconContent("Profiler.PrevFrame", "Go back one frame");

      public static readonly GUIStyle profilerGraphBackground = new GUIStyle("ProfilerGraphBackground") {
        overflow = new RectOffset()
      };

      public static readonly Color rangeSelectionColor = new Color32(200, 200, 200, 40);
      public static readonly int SamplesControlId = "Samples".GetHashCode();

      public static readonly GUIStyle sampleStyle = new GUIStyle() {
        alignment = TextAnchor.MiddleCenter,
        clipping = TextClipping.Clip,
        fontSize = 9,
        normal = new GUIStyleState() {
          textColor = Color.white,
        }
      };

      public static readonly Color SearchHighlightColor = new Color(0.075f, 0.627f, 0.812f);
      public static readonly Color selectedFrameColor = new Color(1, 1, 1, 0.6f);
      public static readonly int SplitterControlId = "Splitter".GetHashCode();
      public static readonly int TimelineControlId = "Timeline".GetHashCode();
      public static readonly GUIStyle timelineTick = "AnimationTimelineTick";
      public static readonly int ToolbarSearchFieldId = "ToolbarSearchField".GetHashCode();
      public static readonly GUIStyle whiteLabel = "ProfilerBadge";
      public static float[] NavigationGridLines = new[] { 1 / 6.0f, 1 / 3.0f, 2 / 3.0f, 2.0f, 5.0f, 10.0f, 20.0f, 50.0f, 100.0f, 200.0f, 500.0f };
      public static Vector2 FramesYRange => new Vector2(0.022f, 1.0f);
      public static Vector2 SimulationsYRange => new Vector2(0.022f, 5.0f);

      public static readonly string[] InlineProperties = new[] {
        "MachineName",
        "Platform",
        "Runtime",
        "RuntimeHost",
        "UnityVersion",
        "ProcessorType",
      };
    }

    [Serializable]
    private class DeviceEntry {
      public string id;
      public GUIContent label;
      public long lastAlive;
    }

    [Serializable]
    private class NavigationState {
      [NonSerialized]
      public List<float> durations = new List<float>();

      [NonSerialized]
      public List<bool> highlight = new List<bool>();

      [NonSerialized]
      public List<float> searchResults = new List<float>();

      public int selectedIndex;

      public bool CanSelectNextFrame => selectedIndex < durations.Count - 1;

      public bool CanSelectPreviousFrame => selectedIndex > 0;

      public int EffectiveSelectedIndex => selectedIndex > 0 ? selectedIndex : (durations.Count - 1);

      public bool IsCurrent => selectedIndex < 0;

      public void ClearBuffers() {
        durations.Clear();
        highlight.Clear();
        searchResults.Clear();
      }
      public void Refresh(QuantumTaskProfilerModel session, List<float> searchResults, bool groupBySimulationId) {
        ClearBuffers();

        if (session.Frames.Count > 0) {
          if (groupBySimulationId) {
            session.GroupBySimulationId(null, durations);
            if (searchResults.Count > 0) {
              session.GroupBySimulationId(searchResults, this.searchResults);
            }
          } else {
            session.GetFrameDurations(durations);
            this.searchResults.AddRange(searchResults);

            var prevSimulationId = session.Frames[0].SimulationId;
            bool highlightState = false;

            foreach (var f in session.Frames) {
              if (f.SimulationId != prevSimulationId) {
                highlightState = !highlightState;
                prevSimulationId = f.SimulationId;
              }
              highlight.Add(highlightState);
            }

            //for (int i = 0; i < session.Frames.Count; ++i) {
            //  highlight.Add(session.Frames[i].isVerified);
            //}
          }
        }

        if (selectedIndex > durations.Count) {
          // it's fine if it becomes -1
          selectedIndex = durations.Count - 1;
        }
      }

      public void SelectCurrentFrame() {
        selectedIndex = -1;
      }

      public void SelectNextFrame() {
        if (CanSelectNextFrame) {
          ++selectedIndex;
        }
      }

      public void SelectPrevFrame() {
        if (CanSelectPreviousFrame) {
          --selectedIndex;
        }
      }
    }
  }
}
#endregion

#region quantum_unity/Assets/Photon/Quantum/Editor/Tools/QuantumTaskProfilerWindow.Utils.cs
namespace Quantum.Editor {
  using System;
  using System.Collections.Generic;
  using UnityEditor;
  using UnityEngine;

  public partial class QuantumTaskProfilerWindow {

    private static Vector3[] _rectVertices = new Vector3[4];
    private static Vector3[] _graphPoints = new Vector3[1024];

    public static void DrawSolidRectangleWithOutline(Rect rect, Color faceColor, Color outlineColor) {

      _rectVertices[0] = new Vector3(rect.xMin, rect.yMin, 0f);
      _rectVertices[1] = new Vector3(rect.xMax, rect.yMin, 0f);
      _rectVertices[2] = new Vector3(rect.xMax, rect.yMax, 0f);
      _rectVertices[3] = new Vector3(rect.xMin, rect.yMax, 0f);
      Handles.DrawSolidRectangleWithOutline(_rectVertices, faceColor, outlineColor);
    }

    public static void DrawRectFast(Rect r, Color color) {
      GL.Color(color);
      GL.Vertex(new Vector3(r.xMin, r.yMin, 0f));
      GL.Vertex(new Vector3(r.xMax, r.yMin, 0f));
      GL.Vertex(new Vector3(r.xMax, r.yMax, 0f));
      GL.Vertex(new Vector3(r.xMin, r.yMax, 0f));
    }

    public static void DrawVerticalLineFast(float x, float minY, float maxY, Color color) {
      GL.Color(color);
      GL.Vertex(new Vector3(x, minY, 0f));
      GL.Vertex(new Vector3(x, maxY, 0f));
    }
    private static void CalculateMeanStdDev(List<float> values, out double mean, out double stdDev) {
      mean = 0;
      foreach (var v in values)
        mean += v;
      mean /= values.Count;

      stdDev = 0;
      foreach (var v in values) {
        stdDev += (v - mean) * (v - mean);
      }
      stdDev = Math.Sqrt(stdDev / values.Count);
    }

    private static Rect DrawDropShadowLabel(float time, float x, float y, float sizeXMul, float sizeYMul) {
      var content = new GUIContent(FormatTime(time));
      var size = Styles.whiteLabel.CalcSize(content);
      var rect = new Rect(x + size.x * sizeXMul, y + size.y * sizeYMul, size.x, size.y);
      EditorGUI.DropShadowLabel(rect, content, Styles.whiteLabel);
      return rect;
    }

    private static Rect DrawDropShadowLabelWithMargins(Rect r, float time, float maxTime, float x, float sizeXMul = 0.0f, float sizeYMul = -0.5f, Color? color = null) {
      var content = new GUIContent(FormatTime(time));
      var size = Styles.whiteLabel.CalcSize(content);

      var y = (maxTime - time) * r.height / maxTime;
      y += size.y * sizeYMul;
      y = Mathf.Clamp(y, 0.0f, r.height - size.y) + r.y;

      x += size.x * sizeXMul;
      x = Mathf.Clamp(x, 0.0f, r.width - size.x) + r.x;

      var rect = new Rect(x, y, size.x, size.y);

      var oldContentColor = GUI.contentColor;
      try {
        if (color != null) {
          GUI.contentColor = color.Value;
        }
        EditorGUI.DropShadowLabel(rect, content, Styles.whiteLabel);
        return rect;
      } finally {
        GUI.contentColor = oldContentColor;
      }
    }

    private static float LinearRoot(float x, float y, float dx, float dy) {
      return x - y * dx / dy;
    }

    private static void DrawGraph(Rect rect, List<float> durations, ZoomPanel panel, float maxDuration, Color? color = null, float lineWidth = 2) {
      var r = rect.Adjust(0, 3, 0, -4);

      int p = 0;
      var durationToY = r.height / maxDuration;

      float dx = rect.width / panel.range;
      var start = Mathf.FloorToInt(panel.start);
      var end = Mathf.Min(durations.Count-1, Mathf.CeilToInt(panel.start + panel.range));
      var x = panel.TimeToPixel(start, rect);

      for (int i = start; i <= end; ++i, ++p, x += dx) {
        if (_graphPoints.Length - 1 <= p) {
          Array.Resize(ref _graphPoints, p * 2);
        }

        var d = durations[i];
        var y = (maxDuration - d);

        _graphPoints[p].x = x;
        _graphPoints[p].y = (maxDuration - d) * durationToY + r.y;
      }

      using (new Handles.DrawingScope(color ?? Color.white)) {
        Handles.DrawAAPolyLine(lineWidth, p, _graphPoints);
      }
    }

    private static void DrawLegendLabel(Rect rect, string label) {
      GUI.Box(rect, GUIContent.none, Styles.legendBackground);
      rect = rect.Adjust(5, 5, 0, 0);
      EditorGUI.LabelField(rect, label);
    }

    private static float DrawSplitter(Rect rect) {
      float delta = 0.0f;
      var controlId = GUIUtility.GetControlID(Styles.SplitterControlId, FocusType.Passive);
      switch (Event.current.GetTypeForControl(controlId)) {
        case EventType.MouseDown:
          if ((Event.current.button == 0) && (Event.current.clickCount == 1) && rect.Contains(Event.current.mousePosition)) {
            GUIUtility.hotControl = controlId;
          }
          break;

        case EventType.MouseDrag:
          if (GUIUtility.hotControl == controlId) {
            delta = Event.current.delta.y;
            Event.current.Use();
          }
          break;

        case EventType.MouseUp:
          if (GUIUtility.hotControl == controlId) {
            GUIUtility.hotControl = 0;
            Event.current.Use();
          }
          break;

        case EventType.Repaint:
          EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical, controlId);
          break;
      }
      return delta;
    }

    private static string FormatTime(float time) {
      return string.Format("{0:F4}ms", time);
    }
    internal sealed class TickHandler {

      private readonly float[] _tickModulos = new float[] {
        0.00001f,
        0.00005f,
        0.0001f,
        0.0005f,
        0.001f,
        0.005f,
        0.01f,
        0.05f,
        0.1f,
        0.5f,
        1f,
        5f,
        10f,
        50f,
        100f,
        500f,
        1000f,
        5000f,
        10000f,
      };

      private readonly float[] _tickStrengths;

      private int _maxVisibleLevel = -1;
      private int _minVisibleLevel = 0;
      private float _timeMin = 0;
      private float _timeRange = 1;
      private float _timeToPixel = 1;

      private List<float> m_TickList = new List<float>(1000);

      public TickHandler() {
        _tickStrengths = new float[_tickModulos.Length];
      }

      public int VisibleLevelsCount => _maxVisibleLevel - _minVisibleLevel + 1;

      public int GetLevelWithMinSeparation(float pixelSeparation) {
        for (int i = 0; i < _tickModulos.Length; i++) {
          float tickSpacing = _tickModulos[i] * _timeToPixel;
          if (tickSpacing >= pixelSeparation)
            return i - _minVisibleLevel;
        }
        return -1;
      }

      public float GetPeriodOfLevel(int level) {
        return _tickModulos[Mathf.Clamp(_minVisibleLevel + level, 0, _tickModulos.Length - 1)];
      }

      public float GetStrengthOfLevel(int level) {
        return _tickStrengths[_minVisibleLevel + level];
      }

      public List<float> GetTicksAtLevel(int level, bool excludeTicksFromHigherLevels) {
        m_TickList.Clear();

        if (level > 0) {
          GetTicksAtLevel(level, excludeTicksFromHigherLevels, m_TickList);
        }

        return m_TickList;
      }

      public void Refresh(float minTime, float timeRange, float pixelWidth, float minTickSpacing = 3.0f, float maxTickSpacing = 80.0f) {
        _timeMin = minTime;
        _timeRange = timeRange;
        _timeToPixel = pixelWidth / timeRange;

        _minVisibleLevel = 0;
        _maxVisibleLevel = _tickModulos.Length - 1;

        for (int i = _tickModulos.Length - 1; i >= 0; i--) {
          // how far apart (in pixels) these modulo ticks are spaced:
          float tickSpacing = _tickModulos[i] * _timeToPixel;

          // calculate the strength of the tick markers based on the spacing:
          _tickStrengths[i] = (tickSpacing - minTickSpacing) / (maxTickSpacing - minTickSpacing);

          if (_tickStrengths[i] >= 1) {
            _maxVisibleLevel = i;
          }

          if (tickSpacing <= minTickSpacing) {
            _minVisibleLevel = i;
            break;
          }
        }

        for (int i = _minVisibleLevel; i <= _maxVisibleLevel; i++) {
          _tickStrengths[i] = Mathf.Sqrt(Mathf.Clamp01(_tickStrengths[i]));
        }
      }
      private void GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels, List<float> list) {
        if (list == null)
          throw new System.ArgumentNullException("list");

        int l = Mathf.Clamp(_minVisibleLevel + level, 0, _tickModulos.Length - 1);
        int startTick = Mathf.FloorToInt(_timeMin / _tickModulos[l]);
        int endTick = Mathf.FloorToInt((_timeMin + _timeRange) / _tickModulos[l]);
        for (int i = startTick; i <= endTick; i++) {
          // return if tick mark is at same time as larger tick mark
          if (excludeTicksFromHigherlevels
              && l < _maxVisibleLevel
              && (i % Mathf.RoundToInt(_tickModulos[l + 1] / _tickModulos[l]) == 0))
            continue;
          list.Add(i * _tickModulos[l]);
        }
      }
    }

    [Serializable]
    internal class ZoomPanel {
      public bool allowScrollPastLimits;
      public bool enableRangeSelect;

      public int controlId;

      public Rect areaRect;

      public float minRange;
      public float range;
      public float start;
      public float verticalScroll;

      public Vector2? selectionRange;
      private Vector2? _dragStart;

      public float DurationToPixelLength(float duration, Rect rect) {
        return (duration) / range * rect.width;
      }


      public void OnGUI(Rect r, float minValue, float maxValue, out bool unselect, float minY = 0.0f, float maxY = 1.0f, bool verticalSlider = false) {

        unselect = false;

        var areaRect = r.Adjust(0, 0, -Styles.ScrollBarWidth, -Styles.ScrollBarWidth);
        this.areaRect = areaRect;

        var hScrollbarRect = r.SetY(r.yMax - Styles.ScrollBarWidth).SetHeight(Styles.ScrollBarWidth).AddWidth(-Styles.ScrollBarWidth);
        DrawHorizontalScrollbar(hScrollbarRect, maxValue, ref start, ref range);

        var vScrollbarRect = r.SetX(r.xMax - Styles.ScrollBarWidth).SetWidth(Styles.ScrollBarWidth).AddHeight(-Styles.ScrollBarWidth);
        if (verticalSlider) {
          DrawPowerSlider(vScrollbarRect, minY, maxY, 4.0f, ref verticalScroll);
        } else {
          Debug.Assert(minY == 0.0f);
          DrawVerticalScrollbar(vScrollbarRect, maxY < 0 ? areaRect.height : maxY, areaRect.height, ref verticalScroll);
        }
        verticalScroll = Mathf.Clamp(verticalScroll, minY, maxY);

        //GUI.Box(hScrollbarRect.SetX(0).SetWidth(Styles.LeftPaneWidth), GUIContent.none, EditorStyles.toolbar);

        var id = GUIUtility.GetControlID(controlId, FocusType.Passive);



        using (new GUI.GroupScope(areaRect)) {
          if (Event.current.isMouse || Event.current.isScrollWheel) {
            bool doingSelect = Event.current.button == 0 && !Event.current.modifiers.HasFlag(EventModifiers.Alt);
            bool doingDragScroll = Event.current.button == 2 || Event.current.button == 0 && !doingSelect;
            bool doingZoom = Event.current.button == 1 && Event.current.modifiers.HasFlag(EventModifiers.Alt);
            var inRect = r.ZeroXY().Contains(Event.current.mousePosition);

            switch (Event.current.type) {
              case EventType.ScrollWheel:
                if (inRect) {
                  if (Event.current.modifiers.HasFlag(EventModifiers.Shift)) {
                    if (verticalSlider) {
                      var delta = Event.current.delta.x + Event.current.delta.y;
                      var amount = Mathf.Clamp(delta * 0.01f, -0.9f, 0.9f);
                      verticalScroll *= (1 - amount);
                      verticalScroll = Mathf.Clamp(verticalScroll, minY, maxY);
                      Event.current.Use();
                    }
                  } else {
                    PerfomFocusedZoom(Event.current.mousePosition, r.ZeroXY(), -Event.current.delta.x - Event.current.delta.y, minRange,
                      ref start, ref range);
                    Event.current.Use();
                  }
                }
                break;

              case EventType.MouseDown:
                if (inRect && (doingDragScroll || doingSelect || doingZoom)) {
                  _dragStart = Event.current.mousePosition;
                  selectionRange = null;
                  if (doingDragScroll || doingZoom) {
                    GUIUtility.hotControl = id;
                  } else if (!enableRangeSelect) {
                    GUIUtility.hotControl = id;
                    var x = PixelToTime(Event.current.mousePosition.x, areaRect.ZeroXY());
                    selectionRange = new Vector2(x, x);
                  } else {
                    // wait with tracking as this might as well be click-select
                  }
                  Event.current.Use();
                }
                break;

              case EventType.MouseDrag:
                if (_dragStart.HasValue) {
                  if (inRect && GUIUtility.hotControl != id) {
                    var deltaPixels = Event.current.mousePosition - _dragStart.Value;
                    if (Mathf.Abs(deltaPixels.x) > Styles.DragPixelsThreshold) {
                      GUIUtility.hotControl = id;
                      unselect = true;
                    }
                  }

                  if (GUIUtility.hotControl == id) {
                    if (doingSelect) {
                      if (enableRangeSelect) {
                        var minX = Mathf.Min(_dragStart.Value.x, Event.current.mousePosition.x);
                        var maxX = Mathf.Max(_dragStart.Value.x, Event.current.mousePosition.x);
                        selectionRange = new Vector2(minX, maxX) / r.width * range + new Vector2(start, start);
                      } else {
                        var x = PixelToTime(Event.current.mousePosition.x, areaRect.ZeroXY());
                        selectionRange = new Vector2(x, x);
                      }
                    } else if (doingDragScroll) {
                      var deltaTime = (Event.current.delta.x / r.width) * (range);
                      start -= deltaTime;
                    } else if (doingZoom) {
                      PerfomFocusedZoom(_dragStart.Value, r.ZeroXY(), Event.current.delta.x, minRange,
                        ref start, ref range);
                    }

                    Event.current.Use();
                  }
                }
                break;

              case EventType.MouseUp:
                _dragStart = null;
                if (GUIUtility.hotControl == id) {
                  GUIUtility.hotControl = 0;
                  Event.current.Use();
                } else {
                  selectionRange = null;
                  unselect = true;
                }
                break;
            }
          }
        }

        if (!allowScrollPastLimits) {
          range = Mathf.Clamp(range, minRange, maxValue - minValue);
          start = Mathf.Clamp(start, minValue, maxValue - range);
        }
      }

      public float PixelToTime(float pixel, Rect rect) {
        return (pixel - rect.x) * (range / rect.width) + start;
      }

      public float TimeToPixel(float time) => TimeToPixel(time, areaRect);

      public float TimeToPixel(float time, Rect rect) {
        return (time - start) / range * rect.width + rect.x;
      }
      private static void DrawHorizontalScrollbar(Rect rect, float maxValue, ref float start, ref float range) {
        var minScrollbarValue = 0.0f;

        maxValue = Mathf.Max(start + range, maxValue);
        minScrollbarValue = Mathf.Min(start, minScrollbarValue);

        if (Mathf.Abs((maxValue - minScrollbarValue) - range) <= 0.001f) {
          // fill scrollbar
          GUI.HorizontalScrollbar(rect, 0.0f, 1.0f, 0.0f, 1.0f);
        } else {
          // a workaround for
          maxValue += 0.00001f;
          start = GUI.HorizontalScrollbar(rect, start, range, minScrollbarValue, maxValue);
        }
      }

      private static void DrawVerticalScrollbar(Rect rect, float workspaceHeightNeeded, float workspaceHeight, ref float scroll) {
        if (workspaceHeight > workspaceHeightNeeded) {
          scroll = 0.0f;
          GUI.VerticalScrollbar(rect, 0, 1, 0, 1);
        } else {
          scroll = Mathf.Min(scroll, workspaceHeightNeeded - workspaceHeight);
          scroll = GUI.VerticalScrollbar(rect, scroll, workspaceHeight, 0, workspaceHeightNeeded);
        }
      }

      private static void DrawPowerSlider(Rect rect, float min, float max, float power, ref float scroll) {

        var pmin = Mathf.Pow(min, 1f / power);
        var pmax = Mathf.Pow(max, 1f / power);
        var pval = Mathf.Pow(scroll, 1f / power);

        pval = GUI.VerticalSlider(rect, pval, pmax, pmin);

        scroll = Mathf.Pow(pval, power);
      }


      private static void PerfomFocusedZoom(Vector2 zoomAround, Rect rect, float delta, float minRange, ref float start, ref float range) {
        var amount = Mathf.Clamp(delta * 0.01f, -0.9f, 0.9f);

        var oldRange = range;
        range *= (1 - amount);

        if (range < minRange) {
          range = minRange;
          amount = 1.0f - range / oldRange;
        }

        var pivot = zoomAround.x / rect.width;
        start += pivot * oldRange * amount;
      }
    }
  }
}
#endregion