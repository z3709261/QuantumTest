using System;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe partial class SceneObjAnimationClip
    {
        public string clipName;
        public FP length;
        public int index;
        public int frameRate;
        public int frameCount;
        public SceneObjAnimationFrame[] frames;

        public bool looped;
        public bool mirror;

        public bool havePosition;
        public bool haveRotate;

        public SceneObjAnimationFrame CalculateDelta(FP lastTime, FP currentTime)
        {
            return GetFrameAtTime(lastTime) - GetFrameAtTime(currentTime);
        }

        public SceneObjAnimationFrame GetFrameAtTime(FP time)
        {
            SceneObjAnimationFrame output = new SceneObjAnimationFrame();
            if (length == FP._0)
                return frames[0];

            while (time > length)
            {
                time -= length;
                output += frames[frameCount - 1];
            }

            int timeIndex = frameCount - 1;
            for (int f = 1; f < frameCount; f++)
            {
                if (frames[f].time > time)
                {
                    timeIndex = f;
                    break;
                }
            }
            SceneObjAnimationFrame frameA = frames[timeIndex - 1];
            SceneObjAnimationFrame frameB = frames[timeIndex];
            FP currentTime = time - frameA.time;
            FP frameTime = frameB.time - frameA.time;
            FP lerp = currentTime / frameTime;
            return output + SceneObjAnimationFrame.Lerp(frameA, frameB, lerp);
        }
    }
}
