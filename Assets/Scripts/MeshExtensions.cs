using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshExtensions
{
    /// <summary>
    /// Parallelogram Method
    /// A = (base*height)
    /// TriArea = A / 2
    /// Triangle is one half of Parallelogram's area
    /// </summary>
    /// <returns></returns>
    public static float CalcMeshArea(this Mesh mesh)
    {
       int[] triangleArr = mesh.triangles;
       Vector3[] vertexArr = mesh.vertices;

        float area = 0f;
        float triArea = 0f;
        for (int i = 0; i < triangleArr.Length; i += 3)
        {
            triArea = Vector3.Cross(vertexArr[triangleArr[i + 1]] - vertexArr[triangleArr[i]], vertexArr[triangleArr[i + 2]] - vertexArr[triangleArr[i]]).magnitude;
            area += triArea;
        }
        return area * 0.5f;
    }
    public static float CalcMeshAreaScaled(this Mesh mesh, Transform transform)
    {
        int[] triangleArr = mesh.triangles;
        Vector3[] vertexArr = mesh.vertices;

        float area = 0f;
        float triArea = 0f;
        Vec3 tempVert;
        for (int i = 0; i < vertexArr.Length; i++)
        {
            tempVert = vertexArr[i];
            vertexArr[i] = tempVert * transform.localScale;
        }

        for (int i = 0; i < triangleArr.Length; i += 3)
        {
            triArea = Vector3.Cross(vertexArr[triangleArr[i + 1]] - vertexArr[triangleArr[i]], vertexArr[triangleArr[i + 2]] - vertexArr[triangleArr[i]]).magnitude;
            area += triArea;
        }
        return area * 0.5f;
    }
}

public class Vec3
{
    Vector3 val;
    public Vec3(Vector3 v)
    {
        val = v;
    }
    public Vec3()
    {
        val = Vector3.zero; 
    }

    public static implicit operator Vector3(Vec3 v)
    {
        return v.val;
    }
    public static implicit operator Vec3(Vector3 v)
    {
        return new Vec3(v);
    }
    public static Vec3 operator *(Vec3 v1, Vec3 v2)
    {
        Vec3 returnVec = new Vec3();
        returnVec.val.x = v1.val.x * v2.val.x;
        returnVec.val.y = v1.val.y * v2.val.y;
        returnVec.val.z = v1.val.z * v2.val.z;
        return returnVec;
    }

   
}

