using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using Photon.Deterministic;
using UnityEngine;

public class ExportSceneObjAnimationClip : MonoBehaviour
{
    public static void CreateAsset(SceneObjAnimationClipAsset dataAsset, AnimationClip clip)
    {
        if (!clip)
        {
            return;
        }

        if (!dataAsset)
        {
            return;
        }

        QuantumRunner.Init(); //make sure we can get debug calls from Quantum

        var quantumAnimationClip = new Quantum.SceneObjAnimationClip
        {
            Path = dataAsset.Settings.Path,
            Guid = dataAsset.Settings.Guid,
        };


        quantumAnimationClip.clipName = clip.name;


        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);

        float usedTime = settings.stopTime - settings.startTime;

        quantumAnimationClip.frameRate = Mathf.RoundToInt(clip.frameRate);
        quantumAnimationClip.length = FP.FromFloat_UNSAFE(usedTime);
        quantumAnimationClip.frameCount = Mathf.RoundToInt(clip.frameRate * usedTime);
        quantumAnimationClip.frames = new Quantum.SceneObjAnimationFrame[quantumAnimationClip.frameCount];
        quantumAnimationClip.looped = clip.isLooping && settings.loopTime;
        quantumAnimationClip.mirror = settings.mirror;

        int frameCount = quantumAnimationClip.frameCount;
        int curveBindingsLength = curveBindings.Length;
        if (curveBindingsLength > 0)
        {
            AnimationCurve curveTx = null, curveTy = null, curveTz = null, curveRx = null, curveRy = null, curveRz = null, curveRw = null;

            for (int c = 0; c < curveBindingsLength; c++)
            {
                string propertyName = curveBindings[c].propertyName;
                if (propertyName == "m_LocalPosition.x" || propertyName == "RootT.x")
                    curveTx = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
                if (propertyName == "m_LocalPosition.y" || propertyName == "RootT.y")
                    curveTy = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
                if (propertyName == "m_LocalPosition.z" || propertyName == "RootT.z")
                    curveTz = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);

                if (propertyName == "m_LocalRotation.x" || propertyName == "RootQ.x")
                    curveRx = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
                if (propertyName == "m_LocalRotation.y" || propertyName == "RootQ.y")
                    curveRy = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
                if (propertyName == "m_LocalRotation.z" || propertyName == "RootQ.z")
                    curveRz = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
                if (propertyName == "m_LocalRotation.w" || propertyName == "RootQ.w")
                    curveRw = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
            }

            quantumAnimationClip.havePosition = curveTx != null || curveTy != null || curveTz != null;
            quantumAnimationClip.haveRotate = curveRx != null || curveRy != null || curveRz != null || curveRw != null;

            if (!quantumAnimationClip.havePosition) Debug.LogWarning("No movement data was found in the animation: " + clip.name);
            if (!quantumAnimationClip.haveRotate) Debug.LogWarning("No rotation data was found in the animation: " + clip.name);

            Quaternion startRotUq = Quaternion.identity;
            FPQuaternion startRot = FPQuaternion.Identity;
            if (quantumAnimationClip.haveRotate)
            {
                float srotxu = curveRx.Evaluate(settings.startTime);
                float srotyu = curveRy.Evaluate(settings.startTime);
                float srotzu = curveRz.Evaluate(settings.startTime);
                float srotwu = curveRw.Evaluate(settings.startTime);

                FP srotx = FP.FromFloat_UNSAFE(srotxu);
                FP sroty = FP.FromFloat_UNSAFE(srotyu);
                FP srotz = FP.FromFloat_UNSAFE(srotzu);
                FP srotw = FP.FromFloat_UNSAFE(srotwu);

                startRotUq = new Quaternion(srotxu, srotyu, srotzu, srotwu);
                startRot = new FPQuaternion(srotx, sroty, srotz, srotw);
            }

            Quaternion offsetRotUq = Quaternion.Inverse(startRotUq);
            FPQuaternion offsetRot = FPQuaternion.Inverse(startRot);

            for (int i = 0; i < frameCount; i++)
            {
                var frameData = new Quantum.SceneObjAnimationFrame();
                frameData.id = i;
                float percent = i / (frameCount - 1f);
                float frameTime = usedTime * percent;
                frameData.time = FP.FromFloat_UNSAFE(frameTime);
                float clipTIme = settings.startTime + percent * (settings.stopTime - settings.startTime);

                if (quantumAnimationClip.havePosition)
                {
                    FP posx = FP.FromFloat_UNSAFE(i > 0 ? curveTx.Evaluate(clipTIme) - curveTx.Evaluate(settings.startTime) : 0);
                    FP posy = FP.FromFloat_UNSAFE(i > 0 ? curveTy.Evaluate(clipTIme) - curveTy.Evaluate(settings.startTime) : 0);
                    FP posz = FP.FromFloat_UNSAFE(i > 0 ? curveTz.Evaluate(clipTIme) - curveTz.Evaluate(settings.startTime) : 0);
                    FPVector3 newPosition = offsetRot * new FPVector3(posx, posy, posz);
                    if (settings.mirror) newPosition.X = -newPosition.X;
                    frameData.position = newPosition;
                }

                if (quantumAnimationClip.haveRotate)
                {
                    float curveRxEval = curveRx.Evaluate(clipTIme);
                    float curveRyEval = curveRy.Evaluate(clipTIme);
                    float curveRzEval = curveRz.Evaluate(clipTIme);
                    float curveRwEval = curveRw.Evaluate(clipTIme);
                    Quaternion curveRotation = offsetRotUq * new Quaternion(curveRxEval, curveRyEval, curveRzEval, curveRwEval);
                    if (settings.mirror)//mirror the Y axis rotation
                    {
                        Quaternion mirrorRotation = new Quaternion(curveRotation.x, -curveRotation.y, -curveRotation.z, curveRotation.w);

                        if (Quaternion.Dot(curveRotation, mirrorRotation) < 0)
                        {
                            mirrorRotation = new Quaternion(-mirrorRotation.x, -mirrorRotation.y, -mirrorRotation.z, -mirrorRotation.w);
                        }

                        curveRotation = mirrorRotation;
                    }

                    FP rotx = FP.FromFloat_UNSAFE(curveRotation.x);
                    FP roty = FP.FromFloat_UNSAFE(curveRotation.y);
                    FP rotz = FP.FromFloat_UNSAFE(curveRotation.z);
                    FP rotw = FP.FromFloat_UNSAFE(curveRotation.w);
                    FPQuaternion newRotation = new FPQuaternion(rotx, roty, rotz, rotw);
                    frameData.rotation = FPQuaternion.Product(offsetRot, newRotation);

                    float rotY = curveRotation.eulerAngles.y * Mathf.Deg2Rad;
                    while (rotY < -Mathf.PI) rotY += Mathf.PI * 2;
                    while (rotY > Mathf.PI) rotY += -Mathf.PI * 2;
                    frameData.rotationY = FP.FromFloat_UNSAFE(rotY);
                }

                quantumAnimationClip.frames[i] = frameData;
            }
        }
        dataAsset.Settings = quantumAnimationClip;
        EditorUtility.SetDirty(dataAsset);
    }
}
