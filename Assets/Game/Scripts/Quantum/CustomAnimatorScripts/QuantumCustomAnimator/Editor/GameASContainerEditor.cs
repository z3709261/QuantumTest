using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Editor extension for the State Behaviour Container.
/// </summary>
[CustomEditor(typeof(GameASBContainer))]
public class QTASContainerEditor : Editor
{
    GameAnimatorStateAssetEditor e;
    bool showE = true;
    bool showEHeader = true;

    ReorderableList behaviourList = null;

    private void OnDestroy()
    {
        DestroyImmediate(e);
        behaviourList = null;
    }

    private void OnEnable()
    {
        GameASBContainer t = target as GameASBContainer;
        behaviourList = null;
    }

    void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
    {
        try
        {
            GameASBContainer t = target as GameASBContainer;

            t.behaviourAssets[index] = (GameAnimatorBehaviourAsset)EditorGUI.ObjectField(rect,
                t.behaviourAssets[index], typeof(GameAnimatorBehaviourAsset), false);
        }
        catch
        { }
    }

    void DrawHeader(Rect rect)
    {
        GameASBContainer t = target as GameASBContainer;
        if (t == null || t.stateAsset == null)
        {
            EditorGUI.LabelField(rect, "NA");
            return;
        }

        rect.width *= 0.25f;
        EditorGUI.LabelField(rect, "Behaviours");

        rect.x += rect.width;
        rect.width *= 3;
        CustomAnimatorBehaviourAsset caba = null;
        caba = (CustomAnimatorBehaviourAsset)EditorGUI.ObjectField(rect, "Add Behaviour", caba, typeof(CustomAnimatorBehaviourAsset), false);
        if (caba != null)
        {
            t.behaviourAssets.Add(caba);
            t.OnValidate();
        }
    }



    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameASBContainer t = target as GameASBContainer;
        if (t == null || t.stateAsset == null)
            return;

        for (int i = 0; t.stateAsset.stateBehaviours != null && i < t.stateAsset.stateBehaviours.Count; i++)
        {
            EditorGUILayout.ObjectField(t.stateAsset.stateBehaviours[i], typeof(GameAnimatorStateAsset), false);
        }

        showE = EditorGUILayout.Foldout(showE, "Show State Asset", true);
        if (showE)
        {
            if (e == null && t.stateAsset != null)
            {
                e = Editor.CreateEditor(t.stateAsset) as GameAnimatorStateAssetEditor;
                e.disableAnimPreview = true;
            }

            if (e != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                showEHeader = EditorGUILayout.Foldout(showEHeader, "Show " + e.target.name + " Header", true);
                if (showEHeader)
                    e.DrawHeader();
                EditorGUILayout.EndHorizontal();
                e.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }

        if (behaviourList == null)
        {
            behaviourList = new ReorderableList(t.behaviourAssets, typeof(GameAnimatorBehaviourAsset));
            behaviourList.drawHeaderCallback = DrawHeader;
            behaviourList.drawElementCallback = DrawListItems;

            behaviourList.onChangedCallback += OnChange;
            behaviourList.onAddDropdownCallback = AddItemD;
        }
        else
        {
            behaviourList.DoLayoutList();
        }
    }

    private void OnChange(ReorderableList list)
    {
        GameASBContainer t = target as GameASBContainer;
        if (t.stateAsset == null)
            return;

        t.stateAsset.stateBehaviours = new List<CustomAnimatorBehaviourAsset>(t.behaviourAssets);
        t.stateAsset.Settings.behaviours = new List<Quantum.AssetRefCustomAnimatorBehaviour>();
        for (int i = 0; i < t.stateAsset.stateBehaviours.Count; i++)
        {
            t.stateAsset.Settings.behaviours.Add(new Quantum.AssetRefCustomAnimatorBehaviour() { Id = t.stateAsset.stateBehaviours[i].AssetObject.Guid });
        }

        t.OnValidate();
        EditorUtility.SetDirty(t.stateAsset);
    }

    private void TestAdd(ReorderableList list)
    {
        for (int i = 0; i < Selection.objects.Length; i++)
            Debug.Log(Selection.objects[i].name);
    }

    public static Type[] GetAllDerivedTypes(AppDomain aAppDomain, Type aType)
    {
        var result = new List<System.Type>();
        var assemblies = aAppDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.IsSubclassOf(aType))
                    result.Add(type);
            }
        }
        return result.ToArray();
    }

    private void AddItemD(Rect buttonRect, ReorderableList list)
    {
        GameASBContainer t = target as GameASBContainer;
        if (t.stateAsset == null)
            return;


        GenericMenu menu = new GenericMenu();

        Type[] possible = GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(GameAnimatorBehaviourAsset));

        for (int i = 0; i < possible.Length; i++)
        {
            if (possible[i].IsAbstract)
                continue;

            menu.AddItem(new GUIContent(possible[i].Name), false, AddBehaviour, possible[i]);
        }

        menu.ShowAsContext();
    }

    private void AddBehaviour(object obj)
    {
        GameASBContainer t = target as GameASBContainer;
        if (t.stateAsset == null)
            return;

        Type type = (Type)obj;
        Debug.Log(type);

        var newType = CreateInstance(type);

        string path = AssetDatabase.GetAssetPath(t.stateAsset).Replace(".asset", "") + "_" + type.Name + ".asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        AssetDatabase.CreateAsset(newType, path);
        AssetDatabase.Refresh();

        t.behaviourAssets.Add((CustomAnimatorBehaviourAsset)newType);

        EditorUtility.SetDirty(t);

        t.OnValidate();
    }

    private void AddItem(ReorderableList list)
    {
        GameASBContainer t = target as GameASBContainer;
        t.behaviourAssets.Add(null);

        t.OnValidate();
    }
}


