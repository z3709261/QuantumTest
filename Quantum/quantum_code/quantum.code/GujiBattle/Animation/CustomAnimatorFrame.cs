using System;
using Photon.Deterministic;

namespace Quantum.Custom.Animator
{
    [Serializable]
    public struct AnimatorFrame
    {
        public int id;
        public FP time;
        public FPVector3 position;
        public FPQuaternion rotation;
        public FP rotationY;//radians

        public static AnimatorFrame operator +(AnimatorFrame a, AnimatorFrame b)
        {
            return new AnimatorFrame()
            {
                id = a.id + b.id,
                time = a.time + b.time,
                position = a.position + b.position,
                rotation = a.rotation + b.rotation,
                rotationY = a.rotationY + b.rotationY
            };
        }

        public static AnimatorFrame operator -(AnimatorFrame a, AnimatorFrame b)
        {
            return new AnimatorFrame()
            {
                id = b.id - a.id,
                time = b.time - a.time,
                position = b.position - a.position,
                rotation = b.rotation - a.rotation,
                rotationY = b.rotationY - a.rotationY
            };
        }

        public static AnimatorFrame operator *(AnimatorFrame a, AnimatorFrame b)
        {
            return new AnimatorFrame()
            {
                id = a.id * b.id,
                time = a.time * b.time,
                position = FPVector3.Scale(a.position, b.position),
                rotation = a.rotation * b.rotation,
                rotationY = a.rotationY * b.rotationY
            };
        }

        public static AnimatorFrame operator *(AnimatorFrame a, FP b)
        {
            return new AnimatorFrame()
            {
                id = (a.id * b).AsInt,
                time = a.time * b,
                position = a.position * b,
                rotation = a.rotation * b,
                rotationY = a.rotationY * b
            };
        }

        public static AnimatorFrame Lerp(AnimatorFrame a, AnimatorFrame b, FP value)
        {
            AnimatorFrame output = new AnimatorFrame();

            output.id = a.id;
            output.time = FPMath.Lerp(a.time, b.time, value);
            output.position = FPVector3.Lerp(a.position, b.position, value);
            output.rotationY = FPMath.Lerp(a.rotationY, b.rotationY, value);

            try
            {
                output.rotation = FPQuaternion.Slerp(a.rotation, b.rotation, value);
            }
            catch (Exception e)
            {
                Log.Info("quaternion slerp divByzero : " + value + " " + ToString(a.rotation) + " " + ToString(b.rotation) + " \n" + e);
                output.rotation = a.rotation;
            }

            return output;
        }

        public override string ToString()
        {
            return string.Format("Animator Frame id: " + id + " time: " + time.AsFloat + " position " + position.ToString() + " rotation " + rotation.AsEuler.ToString() + " rotationY " + rotationY);
        }

        public static string ToString(FPQuaternion q)
        {
            return string.Format("{0}, {1}, {2}", q.AsEuler.Z.AsFloat, q.AsEuler.Y.AsFloat, q.AsEuler.Z.AsFloat);
        }
    }
}
