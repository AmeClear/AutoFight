using UnityEngine;
using System.Collections.Generic;

public static class RangeMeshGenerator
{
    public static Mesh GenerateCircle(float radius, int resolution)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[resolution + 1];
        int[] triangles = new int[resolution * 3];
        Vector2[] uv = new Vector2[vertices.Length];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        float angleStep = 2 * Mathf.PI / resolution;
        for (int i = 1; i <= resolution; i++)
        {
            float angle = (i - 1) * angleStep;
            vertices[i] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            uv[i] = new Vector2((Mathf.Cos(angle) + 1) * 0.5f, (Mathf.Sin(angle) + 1) * 0.5f);
        }

        for (int i = 0; i < resolution; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2 > resolution ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }

    public static Mesh GenerateSector(float radius, float angle, int resolution)
    {
        Mesh mesh = new Mesh();
        int actualResolution = Mathf.Max(2, Mathf.RoundToInt(resolution * angle / 360f));
        Vector3[] vertices = new Vector3[actualResolution + 1];
        int[] triangles = new int[(actualResolution - 1) * 3];
        Vector2[] uv = new Vector2[vertices.Length];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        float startAngle = -angle / 2 * Mathf.Deg2Rad;
        float angleStep = angle * Mathf.Deg2Rad / (actualResolution - 1);

        for (int i = 1; i <= actualResolution; i++)
        {
            float currentAngle = startAngle + (i - 1) * angleStep;
            vertices[i] = new Vector3(Mathf.Cos(currentAngle) * radius, 0, Mathf.Sin(currentAngle) * radius);
            uv[i] = new Vector2((Mathf.Cos(currentAngle) + 1) * 0.5f, (Mathf.Sin(currentAngle) + 1) * 0.5f);
        }

        for (int i = 0; i < actualResolution - 1; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }

    public static Mesh GenerateRing(float outerRadius, float innerRadius, int resolution)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[resolution * 2];
        int[] triangles = new int[resolution * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        float angleStep = 2 * Mathf.PI / resolution;

        for (int i = 0; i < resolution; i++)
        {
            float angle = i * angleStep;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices[i] = new Vector3(cos * outerRadius, 0, sin * outerRadius);
            uv[i] = new Vector2((float)i / resolution, 1);

            vertices[i + resolution] = new Vector3(cos * innerRadius, 0, sin * innerRadius);
            uv[i + resolution] = new Vector2((float)i / resolution, 0);
        }

        for (int i = 0; i < resolution; i++)
        {
            int nextIndex = (i + 1) % resolution;

            int triangleIndex = i * 6;
            triangles[triangleIndex] = i;
            triangles[triangleIndex + 1] = nextIndex;
            triangles[triangleIndex + 2] = i + resolution;

            triangles[triangleIndex + 3] = nextIndex;
            triangles[triangleIndex + 4] = nextIndex + resolution;
            triangles[triangleIndex + 5] = i + resolution;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }

    public static Mesh GenerateRectangle(Vector2 size)
    {
        Mesh mesh = new Mesh();
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-halfWidth, 0, -halfHeight),
            new Vector3( halfWidth, 0, -halfHeight),
            new Vector3( halfWidth, 0,  halfHeight),
            new Vector3(-halfWidth, 0,  halfHeight)
        };

        int[] triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3
        };

        Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }

    // 自定义多边形生成（简单示例，可用于扩展）
    public static Mesh GenerateCustomPolygon(Vector3[] points)
    {
        Mesh mesh = new Mesh();
        // 这里需要实现三角剖分，可以使用Triangulator或简单凸多边形剖分
        // 为简化，返回空网格，实际使用时请实现
        Debug.LogWarning("Custom polygon generation not fully implemented");
        return mesh;
    }
}