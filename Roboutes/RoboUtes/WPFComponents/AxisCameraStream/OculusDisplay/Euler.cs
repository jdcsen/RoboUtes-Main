using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDisplay
{
    public class Euler
    {
        public double X;
        public double Y;
        public double Z;

        public Euler(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        private const float Singularity = 0.499f;
        public static Euler FromQuaternion(Quaternion q)
        {
            float ww = q.W * q.W;
            float xx = q.X * q.X;
            float yy = q.Y * q.Y;
            float zz = q.Z * q.Z;
            float lengthSqd = xx + yy + zz + ww;
            float singularityTest = q.Y * q.W - q.X * q.Z;
            float singularityValue = Singularity * lengthSqd;
            return singularityTest > singularityValue
                ? new Euler(-2 * MathExtensions.Atan2(q.Z, q.W), 90.0f, 0.0f)
                : singularityTest < -singularityValue
                    ? new Euler(2 * MathExtensions.Atan2(q.Z, q.W), -90.0f, 0.0f)
                    : new Euler(MathExtensions.Atan2(2.0f * (q.Y * q.Z + q.X * q.W), 1.0f - 2.0f * (xx + yy)),
                        MathExtensions.Asin(2.0f * singularityTest / lengthSqd),
                        MathExtensions.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (yy + zz)));
        }
    }
}
