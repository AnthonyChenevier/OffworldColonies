using System.Collections.Generic;
using UnityEngine;

namespace ModUtilities
{
    public static class DrawTools
    {
        private static Material _material;

        private static int _glDepth = 0;

        private static Material Material
        {
            get
            {
                if (_material == null) _material = new Material(Shader.Find("Particles/Alpha Blended"));
                return _material;
            }
        }

        // Ok that's cheap but I did not want to add a bunch 
        // of try catch to make sure the GL calls ends.
        public static void NewFrame()
        {
            if (_glDepth > 0)
                MonoBehaviour.print(_glDepth);
            _glDepth = 0;
        }

        private static void GLStart()
        {
            if (_glDepth == 0)
            {
                GL.PushMatrix();
                Material.SetPass(0);
                GL.LoadPixelMatrix();
                GL.Begin(GL.LINES);
            }
            _glDepth++;
        }

        private static void GLEnd()
        {
            _glDepth--;

            if (_glDepth == 0)
            {
                GL.End();
                GL.PopMatrix();
            }
        }


        private static Camera GetActiveCam()
        {
            Camera cam;

            if (HighLogic.LoadedSceneIsEditor)
                cam = EditorLogic.fetch.editorCamera;
            else if (HighLogic.LoadedSceneIsFlight)
                cam = FlightCamera.fetch.mainCamera;
            else
                cam = Camera.main;
            return cam;
        }

        private static Vector3 Tangent(Vector3 normal)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.right);
            if (tangent.sqrMagnitude <= float.Epsilon)
                tangent = Vector3.Cross(normal, Vector3.up);
            return tangent;
        }

        private static void DrawLine(Vector3 origin, Vector3 destination, Color color)
        {
            Camera cam = GetActiveCam();

            Vector3 screenPoint1 = cam.WorldToScreenPoint(origin);
            Vector3 screenPoint2 = cam.WorldToScreenPoint(destination);

            GL.Color(color);
            GL.Vertex3(screenPoint1.x, screenPoint1.y, 0);
            GL.Vertex3(screenPoint2.x, screenPoint2.y, 0);
        }

        private static void DrawRay(Vector3 origin, Vector3 direction, Color color)
        {
            Camera cam = GetActiveCam();

            Vector3 screenPoint1 = cam.WorldToScreenPoint(origin);
            Vector3 screenPoint2 = cam.WorldToScreenPoint(origin + direction);

            GL.Color(color);
            GL.Vertex3(screenPoint1.x, screenPoint1.y, 0);
            GL.Vertex3(screenPoint2.x, screenPoint2.y, 0);
        }

        public static void DrawTransform(Transform t, float scale = 1.0f)
        {
            GLStart();

            DrawRay(t.position, t.up * scale, Color.green);
            DrawRay(t.position, t.right * scale, Color.red);
            DrawRay(t.position, t.forward * scale, Color.blue);

            GLEnd();
        }
        public static void DrawPoint(Vector3 position, Color color, float scale = 1.0f)
        {
            GLStart();
            GL.Color(color);

            DrawRay(position + Vector3.up * (scale * 0.5f), -Vector3.up * scale, color);
            DrawRay(position + Vector3.right * (scale * 0.5f), -Vector3.right * scale, color);
            DrawRay(position + Vector3.forward * (scale * 0.5f), -Vector3.forward * scale, color);

            GLEnd();
        }

        public static void DrawArrow(Vector3 position, Vector3 direction, Color color)
        {
            GLStart();
            GL.Color(color);

            DrawRay(position, direction, color);

            GLEnd();

            DrawCone(position + direction, -direction * 0.333f, color, 15);
        }

        public static void DrawCone(Vector3 position, Vector3 direction, Color color, float angle = 45)
        {
            float length = direction.magnitude;

            Vector3 forward = direction;
            Vector3 up = Tangent(forward).normalized;
            Vector3 right = Vector3.Cross(forward, up).normalized;

            float radius = length * Mathf.Tan(Mathf.Deg2Rad * angle);

            GLStart();
            GL.Color(color);

            DrawRay(position, direction + radius * up, color);
            DrawRay(position, direction - radius * up, color);
            DrawRay(position, direction + radius * right, color);
            DrawRay(position, direction - radius * right, color);

            GLEnd();

            DrawCircle(position + forward, direction, color, radius);
            DrawCircle(position + forward * 0.5f, direction, color, radius * 0.5f);
        }

        public static void DrawLocalMesh(Transform transform, Mesh mesh, Color color)
        {
            if (mesh == null || mesh.triangles == null || mesh.vertices == null)
                return;
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            GLStart();
            GL.Color(color);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 p1 = transform.TransformPoint(vertices[triangles[i]]);
                Vector3 p2 = transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 p3 = transform.TransformPoint(vertices[triangles[i + 2]]);
                DrawLine(p1, p2, color);
                DrawLine(p2, p3, color);
                DrawLine(p3, p1, color);
            }

            GLEnd();
        }

        public static void DrawBounds(Transform boundTransform, Bounds bounds, Color color) {
            List<Vector3> corners = bounds.Corners();
            for (int i = 0; i < corners.Count; i++) 
                corners[i] = boundTransform.TransformPoint(corners[i]);
            DrawBox(corners, color);
        }

        public static void DrawBox(List<Vector3> corners, Color color) {
            GLStart();
            // Top
            DrawLine(corners[0], corners[2], color);
            DrawLine(corners[0], corners[1], color);
            DrawLine(corners[2], corners[3], color);
            DrawLine(corners[1], corners[3], color);
            // Sides
            DrawLine(corners[0], corners[4], color);
            DrawLine(corners[1], corners[5], color);
            DrawLine(corners[2], corners[6], color);
            DrawLine(corners[3], corners[7], color);
            // Bottom
            DrawLine(corners[4], corners[6], color);
            DrawLine(corners[4], corners[5], color);
            DrawLine(corners[6], corners[7], color);
            DrawLine(corners[7], corners[5], color);

            GLEnd();
        }

        /// <summary>
        /// Draws a GL primitive cube on the given transform.
        /// </summary>
        /// <param name="transform">The transform the cube is relative to</param>
        /// <param name="size">size of the cube</param>
        /// <param name="color">cube colour</param>
        /// <param name="offset">cube centre offset in local space (default is (0,0,0))</param>
        public static void DrawLocalBox(Transform transform, Vector3 size, Color color, Vector3 offset = default(Vector3)) {
            DrawBox(new List<Vector3> {
                transform.TransformPoint(offset + new Vector3(-size.x, size.y, -size.z)*0.5f),
                transform.TransformPoint(offset + new Vector3(size.x, size.y, -size.z)*0.5f),
                transform.TransformPoint(offset + new Vector3(size.x, size.y, size.z)*0.5f),
                transform.TransformPoint(offset + new Vector3(-size.x, size.y, size.z)*0.5f),
                transform.TransformPoint(offset + new Vector3(-size.x, -size.y, -size.z)*0.5f),
                transform.TransformPoint(offset + new Vector3(size.x, -size.y, -size.z)*0.5f),
                transform.TransformPoint(offset + new Vector3(size.x, -size.y, size.z)*0.5f),
                transform.TransformPoint(offset + new Vector3(-size.x, -size.y, size.z)*0.5f)
            }, color);
        }

        public static void DrawCapsule(Vector3 start, Vector3 end, Color color, float radius = 1)
        {
            int segments = 18;
            float segmentsInv = 1f / segments;

            Vector3 up = (end - start).normalized * radius;
            Vector3 forward = Tangent(up).normalized * radius;
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            float height = (start - end).magnitude;
            float sideLength = Mathf.Max(0, height * 0.5f - radius);
            Vector3 middle = (end + start) * 0.5f;

            start = middle + (start - middle).normalized * sideLength;
            end = middle + (end - middle).normalized * sideLength;

            //Radial circles
            DrawCircle(start, up, color, radius);
            DrawCircle(end, -up, color, radius);

            GLStart();
            GL.Color(color);

            //Side lines
            DrawLine(start + right, end + right, color);
            DrawLine(start - right, end - right, color);

            DrawLine(start + forward, end + forward, color);
            DrawLine(start - forward, end - forward, color);

            for (int i = 1; i <= segments; i++)
            {
                float stepFwd = i * segmentsInv;
                float stepBck = (i - 1) * segmentsInv;
                //Start endcap
                DrawLine(Vector3.Slerp(right, -up, stepFwd) + start, Vector3.Slerp(right, -up, stepBck) + start, color);
                DrawLine(Vector3.Slerp(-right, -up, stepFwd) + start, Vector3.Slerp(-right, -up, stepBck) + start, color);
                DrawLine(Vector3.Slerp(forward, -up, stepFwd) + start, Vector3.Slerp(forward, -up, stepBck) + start, color);
                DrawLine(Vector3.Slerp(-forward, -up, stepFwd) + start, Vector3.Slerp(-forward, -up, stepBck) + start, color);

                //End endcap
                DrawLine(Vector3.Slerp(right, up, stepFwd) + end, Vector3.Slerp(right, up, stepBck) + end, color);
                DrawLine(Vector3.Slerp(-right, up, stepFwd) + end, Vector3.Slerp(-right, up, stepBck) + end, color);
                DrawLine(Vector3.Slerp(forward, up, stepFwd) + end, Vector3.Slerp(forward, up, stepBck) + end, color);
                DrawLine(Vector3.Slerp(-forward, up, stepFwd) + end, Vector3.Slerp(-forward, up, stepBck) + end, color);
            }

            GLEnd();
        }

        public static void DrawCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f)
        {
            int segments = 36;
            float step = Mathf.Deg2Rad * 360f / segments;

            Vector3 upNormal = up.normalized * radius;
            Vector3 forwardNormal = Tangent(upNormal).normalized * radius;
            Vector3 rightNormal = Vector3.Cross(upNormal, forwardNormal).normalized * radius;

            Matrix4x4 matrix = new Matrix4x4();

            matrix[0] = rightNormal.x;
            matrix[1] = rightNormal.y;
            matrix[2] = rightNormal.z;

            matrix[4] = upNormal.x;
            matrix[5] = upNormal.y;
            matrix[6] = upNormal.z;

            matrix[8] = forwardNormal.x;
            matrix[9] = forwardNormal.y;
            matrix[10] = forwardNormal.z;

            Vector3 lastPoint = position + matrix.MultiplyPoint3x4(Vector3.right);

            GLStart();
            GL.Color(color);

            for (int i = 0; i <= segments; i++)
            {
                Vector3 nextPoint;
                var angle = i * step;
                nextPoint.x = Mathf.Cos(angle);
                nextPoint.z = Mathf.Sin(angle);
                nextPoint.y = 0;

                nextPoint = position + matrix.MultiplyPoint3x4(nextPoint);

                DrawLine(lastPoint, nextPoint, color);
                lastPoint = nextPoint;
            }
            GLEnd();
        }

        public static void DrawSphere(Vector3 position, Color color, float radius = 1.0f)
        {
            int segments = 36;
            float step = Mathf.Deg2Rad * 360f / segments;

            Vector3 x = new Vector3(position.x, position.y, position.z + radius);
            Vector3 y = new Vector3(position.x + radius, position.y, position.z);
            Vector3 z = new Vector3(position.x + radius, position.y, position.z);

            GLStart();
            GL.Color(color);

            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                Vector3 nextX = new Vector3(position.x, position.y + radius * Mathf.Sin(angle), position.z + radius * Mathf.Cos(angle));
                Vector3 nextY = new Vector3(position.x + radius * Mathf.Cos(angle), position.y, position.z + radius * Mathf.Sin(angle));
                Vector3 nextZ = new Vector3(position.x + radius * Mathf.Cos(angle), position.y + radius * Mathf.Sin(angle), position.z);

                DrawLine(x, nextX, color);
                DrawLine(y, nextY, color);
                DrawLine(z, nextZ, color);

                x = nextX;
                y = nextY;
                z = nextZ;
            }
            GLEnd();
        }

        public static void DrawCylinder(Vector3 start, Vector3 end, Color color, float radius = 1)
        {
            Vector3 up = (end - start).normalized * radius;
            Vector3 forward = Tangent(up);
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            //Radial circles
            DrawCircle(start, up, color, radius);
            DrawCircle(end, -up, color, radius);
            DrawCircle((start + end) * 0.5f, up, color, radius);

            GLStart();
            GL.Color(color);

            //Sides
            DrawLine(start + right, end + right, color);
            DrawLine(start - right, end - right, color);

            DrawLine(start + forward, end + forward, color);
            DrawLine(start - forward, end - forward, color);

            //Top
            DrawLine(start - right, start + right, color);
            DrawLine(start - forward, start + forward, color);

            //Bottom
            DrawLine(end - right, end + right, color);
            DrawLine(end - forward, end + forward, color);
            GLEnd();
        }
    }
}