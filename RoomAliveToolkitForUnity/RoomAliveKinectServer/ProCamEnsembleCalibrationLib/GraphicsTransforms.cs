using SharpDX;
using System;

namespace RoomAliveToolkit
{
    public class GraphicsTransforms
    {
        public static SharpDX.Matrix ProjectionMatrixFromCameraMatrix(float fx, float fy, float cx, float cy, float w, float h, float near, float far)
        {
            // fx, fy, cx, cy are in pixels
            // input coordinate sysem is x left, y up, z foward (right handed)
            // project to view volume where x, y in [-1, 1], z in [0, 1], x right, y up, z forward
            // pre-multiply matrix

            // -(2 * fx / w),           0,   -(2 * cx / w - 1),                           0,
            //             0,  2 * fy / h,      2 * cy / h - 1,                           0,
            //             0,           0,  far / (far - near),  -near * far / (far - near),
            //             0,           0,                   1,                           0

            return new SharpDX.Matrix(
                -(2 * fx / w), 0, -(2 * cx / w - 1), 0,
                0, 2 * fy / h, 2 * cy / h - 1, 0,
                0, 0, far / (far - near), -near * far / (far - near),
                0, 0, 1, 0
                );
        }

        public static SharpDX.Matrix PerspectiveFov(float fieldOfViewY, float aspectRatio, float near, float far)
        {
            // right handed, pre multiply, x left, y up, z forward

            float h = 1f / (float)Math.Tan(fieldOfViewY / 2f);
            float w = h / aspectRatio;

            return new SharpDX.Matrix(
                -w, 0, 0, 0,
                0, h, 0, 0,
                0, 0, far / (far - near), -near * far / (far - near),
                0, 0, 1, 0
                );
        }

        public static SharpDX.Matrix LookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            // right handed, pre multiply, x left, y up, z forward

            var zaxis = Vector3.Normalize(cameraTarget - cameraPosition);
            var xaxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zaxis));
            var yaxis = Vector3.Cross(zaxis, xaxis);

            return new SharpDX.Matrix(
                xaxis.X, xaxis.Y, xaxis.Z, -Vector3.Dot(xaxis, cameraPosition),
                yaxis.X, yaxis.Y, yaxis.Z, -Vector3.Dot(yaxis, cameraPosition),
                zaxis.X, zaxis.Y, zaxis.Z, -Vector3.Dot(zaxis, cameraPosition),
                0, 0, 0, 1
            );
        }
    }
}
