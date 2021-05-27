using System.Collections.Generic;
using UnityEngine;
using Quantum.Custom.Animator;
using Photon.Deterministic;

#if UNITY_EDITOR
using UnityEditor.IMGUI.Controls;
using UnityEditor;
#endif


[ExecuteInEditMode()]
public class GameCharacterEditor : MonoBehaviour
{
#if UNITY_EDITOR
    public CustomAnimatorGraphAsset dataAsset;

    public int stateIndex;

    public int frame;

    [HideInInspector()]
    public float previousTime;
    public float time;
    public int oldIndex;

    public void OnValidate()
    {
        if (dataAsset == default)
        {
            return;
        }
        stateIndex = Mathf.Clamp(stateIndex, 0, dataAsset.Settings.layers[0].states.Length - 1);
    }

    public void Update()
    {
        if(dataAsset == default)
        {
            return;
        }
        if (oldIndex != stateIndex)
        {
            UpdateAnimation();
        }

        Animator animator = this.gameObject.GetComponent<Animator>();
        float length = 0f;
        GetMotionLength(ref length, dataAsset.Settings.layers[0].states[stateIndex].motion,
            dataAsset.Settings.layers[0].states[stateIndex].speed);

        frame = Mathf.Clamp(frame, 0, Mathf.CeilToInt(60 * length));
        time = frame / 60f;
        animator?.Update(time - previousTime);
        previousTime = time;
    }

    private void GetMotionLength(ref float length, AnimatorMotion motion, FP speed)
    {
        if (motion == null)
        {
            return;
        }
        if (motion.isTree)
        {
            var tree = motion as Quantum.Custom.Animator.AnimatorBlendTree;
            for (int i = 0; i < tree.motions.Length; i++)
            {
                GetMotionLength(ref length, tree.motions[i], speed);
            }
        }
        else
        {
            var clip = motion as Quantum.Custom.Animator.AnimatorClip;
            length = Mathf.Max(clip.data.length.AsFloat / speed.AsFloat, length);
        }
    }

    public void UpdateAnimation()
    {
        Animator animator = this.gameObject.GetComponent<Animator>();
        animator.PlayInFixedTime(dataAsset.Settings.layers[0].states[stateIndex].name, 0, 0f);
        oldIndex = stateIndex;

        frame = 0;
        previousTime = 0;
        time = 0;

        animator.Update(0f);

        animator.transform.position = Vector3.zero;
    }

#endif
}


[CustomEditor(typeof(GameCharacterEditor))]
public class GameCharacterEditorTools : Editor
{

    private BoxBoundsHandle mainExtentHandle = new BoxBoundsHandle();

    private List<BoxBoundsHandle> hurtboxHandles = new List<BoxBoundsHandle>();

    private List<BoxBoundsHandle> hitBoxHandles = new List<BoxBoundsHandle>();

    int oldIndex = -1;


    int oldBoxIndex;
    int hurtBoxIndex;

    public string[] stateNames;

    int oldAtkFrame = -1;
    public GameAttackStateAsset currentAtk = null;

    private void OnEnable()
    {
        GameCharacterEditor b = target as GameCharacterEditor;

        if (b.dataAsset == default)
        {
            return;
        }
        stateNames = new string[b.dataAsset.Settings.layers[0].states.Length];

        for (int i = 0; i < b.dataAsset.Settings.layers[0].states.Length; i++)
        {
            stateNames[i] = b.dataAsset.Settings.layers[0].states[i].name;
        }
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameCharacterEditor b = target as GameCharacterEditor;
        if (b.dataAsset == default)
        {
            return;
        }

        if(stateNames == null)
        {
            stateNames = new string[b.dataAsset.Settings.layers[0].states.Length];

            for (int i = 0; i < b.dataAsset.Settings.layers[0].states.Length; i++)
            {
                stateNames[i] = b.dataAsset.Settings.layers[0].states[i].name;
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<<"))
        {
            b.stateIndex--;
            b.stateIndex = Mathf.Clamp(b.stateIndex, 0, b.dataAsset.Settings.layers[0].states.Length - 1);
        }
        b.stateIndex = EditorGUILayout.Popup(b.stateIndex, stateNames);
        if (GUILayout.Button(">>"))
        {
            b.stateIndex++;
            b.stateIndex = Mathf.Clamp(b.stateIndex, 0, b.dataAsset.Settings.layers[0].states.Length - 1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<<"))
        {
            b.frame--;
            b.frame = Mathf.Max(0, b.frame);
        }
        b.frame = EditorGUILayout.IntField("Frame", b.frame);
        if (GUILayout.Button(">>"))
        {
            b.frame++;
        }
        b.OnValidate();
        b.Update();
        EditorGUILayout.EndHorizontal();

        if (oldIndex != b.stateIndex)
        {
            b.UpdateAnimation();
            oldAtkFrame = -1;
            oldBoxIndex = -1;
        }

        var state = UnityDB.FindAsset<GameAnimatorStateAsset>(b.dataAsset.Settings.layers[0].states[b.stateIndex].StateAsset.Id);
        if (state == null)
            return;

        mainExtentHandle.SetColor(Color.green);
        mainExtentHandle.center = state.Settings.mainExtents.Center.ToUnityVector3();
        mainExtentHandle.size = state.Settings.mainExtents.Extents.ToUnityVector3() * 2f;

        for (int i = 0; i < state.Settings.hurtBoxSets.Count; i++)
        {
            if (b.frame < state.Settings.hurtBoxSets[i].frameIndex)
                break;
            hurtBoxIndex = i;
        }

        if (oldBoxIndex != hurtBoxIndex)
        {
            oldIndex = b.stateIndex;

            hurtboxHandles.Clear();
            for (int i = 0; i < state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes.Count; i++)
            {
                BoxBoundsHandle bbh = new BoxBoundsHandle();
                bbh.SetColor(Color.blue);
                bbh.center = state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes[i].Center.ToUnityVector3();
                bbh.size = state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes[i].Extents.ToUnityVector3() * 2f;

                hurtboxHandles.Add(bbh);
            }

            oldBoxIndex = hurtBoxIndex;
        }

        if (GUILayout.Button("Add Hurt Box Set"))
        {
            var current = state.Settings.hurtBoxSets[hurtBoxIndex];

            if (!state.Settings.hurtBoxSets.Exists(x => x.frameIndex == b.frame))
            {

                Quantum.HurtBoxSet hbx = new Quantum.HurtBoxSet() { frameIndex = b.frame, hurtBoxes = new List<Photon.Deterministic.FPBounds3>(current.hurtBoxes) };
                state.Settings.hurtBoxSets.Add(hbx);
                state.Settings.hurtBoxSets.Sort((x, y) => x.frameIndex - y.frameIndex);

                hurtBoxIndex = state.Settings.hurtBoxSets.FindIndex(x => x.frameIndex == b.frame);

                hurtboxHandles.Clear();
            }
        }

        if (GUILayout.Button("Remove Hurt Box Set"))
        {
            if (state.Settings.hurtBoxSets.Count > 0)
            {
                state.Settings.hurtBoxSets.RemoveAt(hurtBoxIndex);

                hurtBoxIndex = Mathf.Max(0, hurtBoxIndex - 1);
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Add Hurt Box on " + hurtBoxIndex + " frame:  " + state.Settings.hurtBoxSets[hurtBoxIndex].frameIndex))
        {
            int c = state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes.Count;
            state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes.Add(new Photon.Deterministic.FPBounds3()
            {
                Center = Vector3.zero.ToFPVector3(),
                Extents = new Vector3(0.5f, 0.5f, 0.5f).ToFPVector3()
            });

            BoxBoundsHandle bbh = new BoxBoundsHandle();
            bbh.SetColor(Color.blue);
            bbh.center = state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes[c].Center.ToUnityVector3();
            bbh.size = state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes[c].Extents.ToUnityVector3() * 2f;

            hurtboxHandles.Add(bbh);
        }

        if (GUILayout.Button("Delete Last Hurt Box"))
        {
            int c = state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes.Count;
            if (c > 0)
            {
                state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes.RemoveAt(state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes.Count - 1);

                oldBoxIndex = -1;
            }
        }
        if(oldAtkFrame != b.frame)
        {
            SetupAtkStates(b.frame, state.Settings.behaviours);
            oldAtkFrame = b.frame;
        }
        
    }


    private void SetupAtkStates(int frame, List<Quantum.AssetRefCustomAnimatorBehaviour> stateBehaviours)
    {
        hitBoxHandles.Clear();
        for (int i = 0; i < stateBehaviours.Count; i++)
        {
            var behaviour = stateBehaviours[i];
            var b = UnityDB.FindAsset<CustomAnimatorBehaviourAsset>(behaviour.Id);
            if (b is GameAttackStateAsset)
            {
                var atkAsset = (b as GameAttackStateAsset);
                if (frame >= atkAsset.Settings.startFrame && frame <= atkAsset.Settings.endFrame)
                {
                    BoxBoundsHandle bbh = new BoxBoundsHandle();
                    bbh.SetColor(Color.red);
                    bbh.center = atkAsset.Settings.bounds3D.Center.ToUnityVector3();

                    bbh.size = atkAsset.Settings.bounds3D.Extents.ToUnityVector3() * 2f;
                    hitBoxHandles.Add(bbh);
                    currentAtk = atkAsset;
                    break;
                }
            }
        }
    }


    private void OnSceneGUI()
    {
        GameCharacterEditor b = target as GameCharacterEditor;
        if(b.dataAsset == default)
        {
            return;
        }
        var state = UnityDB.FindAsset<GameAnimatorStateAsset>(b.dataAsset.Settings.layers[0].states[b.stateIndex].StateAsset.Id);

        var handle = mainExtentHandle;

        handle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Change Bounds");

            Bounds bb = new Bounds();

            var center = handle.center;
            handle.center = center;

            bb.center = handle.center;
            bb.size = handle.size;

            state.Settings.mainExtents.Center = new FPVector3(FP.FromFloat_UNSAFE(bb.center.x), FP.FromFloat_UNSAFE(bb.center.y), FP.FromFloat_UNSAFE(bb.center.z));
            state.Settings.mainExtents.Extents = new FPVector3(FP.FromFloat_UNSAFE(bb.extents.x), FP.FromFloat_UNSAFE(bb.extents.y), FP.FromFloat_UNSAFE(bb.extents.z));

            EditorUtility.SetDirty(b.dataAsset);
            EditorUtility.SetDirty(state);
        }

        for (int i = 0; i < hurtboxHandles.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            handle = hurtboxHandles[i];

            handle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Bounds");

                Bounds bb = new Bounds();

                bb.center = handle.center;
                bb.size = handle.size;

                var hhb = state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes[i];
                hhb.Center = new FPVector3(FP.FromFloat_UNSAFE(bb.center.x), FP.FromFloat_UNSAFE(bb.center.y), FP.FromFloat_UNSAFE(bb.center.z));
                hhb.Extents = new FPVector3(FP.FromFloat_UNSAFE(bb.extents.x), FP.FromFloat_UNSAFE(bb.extents.y), FP.FromFloat_UNSAFE(bb.extents.z));

                state.Settings.hurtBoxSets[hurtBoxIndex].hurtBoxes[i] = hhb;

                EditorUtility.SetDirty(b.dataAsset);
                EditorUtility.SetDirty(state);
            }
        }

        for (int i = 0; i < hitBoxHandles.Count && currentAtk != null; i++)
        {
            EditorGUI.BeginChangeCheck();
            handle = hitBoxHandles[i];

            handle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Bounds");

                Bounds bb = new Bounds();

                bb.center = handle.center;
                bb.size = handle.size;

                var hhb = currentAtk.Settings.bounds3D;
                hhb.Center = new FPVector3(FP.FromFloat_UNSAFE(bb.center.x), FP.FromFloat_UNSAFE(bb.center.y), FP.FromFloat_UNSAFE(bb.center.z));
                hhb.Extents = new FPVector3(FP.FromFloat_UNSAFE(bb.extents.x), FP.FromFloat_UNSAFE(bb.extents.y), FP.FromFloat_UNSAFE(bb.extents.z));

                currentAtk.Settings.bounds3D = hhb;

                EditorUtility.SetDirty(currentAtk);
            }
        }
    }
}