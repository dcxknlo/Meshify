/* -List of vertices should really be LinkedList with implicit prev and next
 * -General organisation is unsatisfactory. A lot of lists need to be passed around.
 * -Requires a separate vertices list and a 'Vertex' List with indices and positions.
 * -Accounts for self intersections but at present does not remove or stop user from entering them.
 * 
 * + Handles concave polygons based on user input boundary edge. 
 * + Detects self intersections.
 * + Corrects winding order to always display clockwise;
 * + Is able to generate simple polygons but not complex or nested ones with holes
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CreateMesh : MonoBehaviour {


     List<Vector3> m_vertices;
     List<Vector3> m_normals;
     List<int> m_triangle_indices;
     List<Vector2> m_uvs;


    public MeshFilter m_mesh_filter;
    public Mesh m_mesh;
    public MeshRenderer m_renderer;
    public Meshify meshify;

    [SerializeField] Color color;
    private void OnEnable()
    {
        meshify = new Meshify();
        m_renderer = this.GetComponent<MeshRenderer>();
        m_mesh_filter = this.GetComponent<MeshFilter>();
        m_mesh_filter.sharedMesh = m_mesh = InitMesh(ref m_vertices, ref m_normals, ref m_triangle_indices, ref m_uvs);

    }
    #region Mesh Helper Functions
    public Mesh InitMesh(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangleIndices, ref List<Vector2> uvs)
    {
        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        triangleIndices = new List<int>();
        uvs = new List<Vector2>();
        Mesh m = new Mesh();


        m.SetVertices(vertices);
        m.SetTriangles(triangleIndices, 0);
        m.SetNormals(normals);
        m.SetUVs(0, uvs);

        return m;
    }
    public void AddPoint(Vector3 v)
    {
        
        m_vertices.Add(v);
        m_normals.Add(-Vector3.forward);
        UpdateMesh(m_mesh, m_vertices, m_normals, m_triangle_indices, m_uvs);
    }
    private void AddPoint(Mesh m, Vector3 v, List<Vector3> vertices, List<Vector3> normals, List<int> triangleIndices, List<Vector2> uvs)
    {       
        vertices.Add(v);
        normals.Add(-Vector3.forward);
        UpdateMesh(m, vertices, normals, triangleIndices, uvs);
    }
    public void UpdateMesh(Mesh m, List<Vector3> vertices, List<Vector3> normals, List<int> triangleIndices, List<Vector2> uvs)
    {
        m.SetVertices(vertices);
        m.SetTriangles(triangleIndices, 0);
        m.SetNormals(normals);
        m.SetUVs(0, uvs);

    }
    public void ClearMesh(Mesh m, List<Vector3> vertices, List<Vector3> normals, List<int> triangleIndices, List<Vector2> uvs)
    {
        triangleIndices.Clear();
        vertices.Clear();
        normals.Clear();
        m.SetTriangles(triangleIndices, 0);
        m.SetVertices(vertices);
        m.SetNormals(normals);
    }
    #endregion
    #region Unity Debug
    private void OnGUI()
    {
       
        if (GUI.Button(new Rect(new Vector2(10, 10), new Vector2(70, 20)), "Meshify"))
        {
            m_mesh.SetTriangles(meshify.EarClipping(m_mesh, m_vertices, m_triangle_indices), 0);
        }
        
        if (GUI.Button(new Rect(new Vector2(10, 35), new Vector2(70, 20)), "Clear"))
        {
            ClearMesh(m_mesh, m_vertices, m_normals, m_triangle_indices, m_uvs);

        }
        if (GUI.Button(new Rect(new Vector2(10, 70), new Vector2(70, 20)), "Color"))
        {
            int colorID = Shader.PropertyToID("_Color");
            MaterialPropertyBlock mp = new MaterialPropertyBlock();
            mp.SetColor(colorID, color);
            m_renderer.SetPropertyBlock(mp);

        }
        if (GUI.Button(new Rect(new Vector2(10, 95), new Vector2(70, 40)), "Clear\nColor"))
        {
            m_renderer.SetPropertyBlock(null);
        }
        GUI.TextField(new Rect(Screen.width /2, 10, 100, 20),  m_mesh.CalcMeshArea().ToString() + "m^2");


    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && m_vertices.Count > 0)
        {
            for (int i = 0; i < m_vertices.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(m_vertices[i], 0.1f);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(m_vertices[i], m_vertices[(i + 1) % m_vertices.Count]);
                UnityEditor.Handles.Label(m_vertices[i] + Vector3.back * 0.1f, i.ToString());
                UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;        
            }
              
        }
    }
#endif
    #endregion
}
public class Vertex
{
    public Vector3 point;
    public Vertex prev;
    public Vertex next;
    public int index;
};

public class Meshify {

    #region Triangulation
    
    public List<int> EarClipping(Mesh m, List<Vector3> vertices, List<int> triangleIndices)
    {
        int vCount = vertices.Count;

        List<Vertex> vertIndices = new List<Vertex>(); // current verts;
        List<int> reflexVerts = new List<int>(); // empty list for storing reflex verts
        for (int i = 0; i < vCount; i++)
        {
            Vertex v = new Vertex();
            v.point = vertices[i];
            v.index = i;
            vertIndices.Add(v);

        }
        ConnectVertexes( vertIndices);

        // m.GetVertices(verts);
        if (vCount <= 2)
            return triangleIndices;

        if (vCount == 3)
        {
            AddTriangle(triangleIndices, vertIndices[0]);
            return triangleIndices;
        }
        /////// check triangulation
        

        int leftMost = GetLeftMost(vertices);
        bool isPolyCCW = IsCCW(vertices[PrevIndex(leftMost, vCount)], vertices[leftMost], vertices[NextIndex(leftMost, vCount)]);
        SelfIntersect(vertIndices, vertIndices[vCount - 1], isPolyCCW);

        Vector3[] tri = new Vector3[3];
        while (vertIndices.Count > 3)
        {
            int earIndex = -1;
            // Loop checks if angle is reflex then adds it to reflexVerts list;
            for (int i = 0; i < vertIndices.Count; i++)
            {

                if (earIndex >= 0) // eartip index found so break;
                    break;
                // triangle of current index

                tri[0] = vertices[vertIndices[i].prev.index];
                tri[1] = vertices[vertIndices[i].index];
                tri[2] = vertices[vertIndices[i].next.index];

                bool vertOrientation = IsCCW(tri[0], tri[1], tri[2]); // gets vertex orientation
                // if vertex is not same order as polygon then it is reflex
                if (isPolyCCW != vertOrientation)
                {
                    reflexVerts.Add(vertIndices[i].index);
                    continue;
                }
                // Possible ear identified. Testing below to see if any vertices in the triangle;
                else
                {
                    // Loop goes through existing reflex vertices
                    bool isEar = true;

                    for (int j = 0; j < reflexVerts.Count; j++)
                    {
                        Vector3 reflexVert = vertices[j];
                        if (tri[0] == reflexVert || tri[2] == reflexVert)
                        {
                            continue;
                        } // no self testing
                        else if (!PointNotInTriangle(reflexVert, tri))
                        {
                            isEar = false;
                            break;
                        } // if point is in triangle then there is no ear!
                    }
                    // if it is still an ear candidate then check rest of vertices
                    if (isEar)
                    {
                        for (int k = i + 2; k < vertIndices.Count; k++)
                        {

                            Vector3 vert = vertIndices[k].point;
                            if (tri[0] == vert || tri[2] == vert)
                            {
                                continue;
                            } // no self testing
                            else if (!PointNotInTriangle(vert, tri))
                            {
                                isEar = false;
                                break;
                            }
                        }
                        if (isEar)
                        {
                            earIndex = i;
                        }
                    }
                }
            }
            if (earIndex == -1) { Debug.Log("No Eartips found"); break; }
            // Vertex v = vertIndices[earIndex];
            Vertex v;
            RemoveVertex(ref vertIndices, earIndex, out v);
            //vertIndices.Remove(v);

            //v.prev.next = v.next;
            //v.next.prev = v.prev;
            AddTriangle(triangleIndices, v);
        }
        AddTriangle(triangleIndices, vertIndices[0]);
        if (isPolyCCW) // if polyOrientation is counter clockwise tris must be reversed due to unity's clockwise winding pattern. 
            triangleIndices.Reverse();
        return triangleIndices;
    }
    #endregion
    #region Meshify Helper Functions
    int GetLeftMost(List<Vector3> vertList)
    {
        int leftMost = 0;
        for (int i = 0; i < vertList.Count; i++)
        {
            if (vertList[i].x > vertList[leftMost].x || (vertList[i].x == vertList[leftMost].x && vertList[i].y < vertList[leftMost].y))
            {
                leftMost = i;
            }
        }
        return leftMost;
    }
    int PrevIndex(int curr, int vCount)
    {
        return curr - 1 < 0 ? vCount - 1 : curr - 1;
    }
    int NextIndex(int curr, int vCount)
    {
        return curr + 1 > vCount - 1 ? 0 : curr + 1;
    }
    // Is it counter-clockwise?
    bool IsCCW(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.z - b.z) - (c.x - b.x) * (b.z - a.z) > 0.0f; 
    }

    bool SegmentIntersect(Vertex v1, Vertex v2, bool isPolyCCW)
    {
        Vector2 intersection;

        float denom = ((v2.next.point.z - v2.point.z) * (v1.next.point.x - v1.point.x)) -
                     ((v2.next.point.x - v2.point.x) * (v1.next.point.z - v1.point.z));

        float nume_a = ((v2.next.point.x - v2.next.point.x) * (v1.point.z - v2.point.z)) -
                       ((v2.next.point.z - v2.next.point.z) * (v1.point.x - v2.point.x));

        float nume_b = ((v1.next.point.x - v1.point.x) * (v1.point.z - v2.point.z)) -
                       ((v1.next.point.z - v1.point.z) * (v1.point.x - v2.point.x));

        // Parallel;
        if (denom == 0.0f)
        {   
            return false;
        }

        float ua = nume_a / denom;
        float ub = nume_b / denom;

        int intersectionPoints = EndPointOverlap(v1, v2);
        if(intersectionPoints >= 1)
        {
            return false;
        }
        
        if (ua >= 0.0f && ua <= 1.0f && ub >= 0.0f && ub <= 1.0f)
        {
            // Get the intersection point            
            intersection.x = v1.point.x + ua * (v1.next.point.x - v1.point.x);
            intersection.y = v1.point.z + ua * (v1.next.point.z - v1.point.z);
            return true;
        }
    
        return false;
    }
    int EndPointOverlap(Vertex v1, Vertex v2)
    {
        int overlapPoints = 0;
        if (v1 == v2)
        {
            overlapPoints++;
        }
        else if (v1.next == v2)
        {
            overlapPoints++;
        }
        else if (v1.next == v2.next)
        {
            overlapPoints++;
        }
        else if (v2.next == v1)
        {
            overlapPoints++;
        }
        return overlapPoints;
    }
    bool SelfIntersect(List<Vertex> vertIndices, Vertex v,  bool isPolyCCW)
    {
        int vCount = vertIndices.Count;
        for (int i = 0; i < vCount; i++)
        {           
            Vertex nextVert = vertIndices[i];
            if (v != nextVert)
            {
                if (SegmentIntersect(v, nextVert, isPolyCCW) && SegmentIntersect(nextVert, v, isPolyCCW)) // non optimal but works
                {                   
                    Debug.Log("Segment " + v.index + "---" + v.next.index + " intersects " + nextVert.index + "---" + nextVert.next.index);                  
                    return true;
                }
            }
        }
        return false;
    }
    bool IsReflex(bool polyOrient, bool vert)
    {
        return vert != polyOrient ? true : false;
    }
    void ConnectVertexes(List<Vertex> verts)
    {
        int vCount = verts.Count;
        for (int x = 0; x < vCount; x++)
        {
            verts[x].prev = verts[PrevIndex(x, vCount)];
            verts[x].next = verts[NextIndex(x, vCount)];
        }
    }
    public void AddVertex(List<Vertex> packedVertices, Vector3 v)
    {
        Vertex vert = new Vertex();
        vert.point = v;
        vert.index = packedVertices.Count;
        packedVertices.Add(vert);

    }
    public void RemoveVertex(ref List<Vertex> vertIndices, int vertIndex, out Vertex v)
    {
        v = vertIndices[vertIndex];
        vertIndices.Remove(v);

        v.prev.next = v.next;
        v.next.prev = v.prev;
    }
    void AddTriangle(List<int> triangleIndices, List<int> vertList, int earIndex)
    {
        triangleIndices.Add(vertList[PrevIndex(earIndex, vertList.Count)]);
        triangleIndices.Add(vertList[earIndex]);
        triangleIndices.Add(vertList[NextIndex(earIndex, vertList.Count)]);
    }
    void AddTriangle(List<int> triangleIndices, Vertex earVert)
    {
        triangleIndices.Add(earVert.prev.index);
        triangleIndices.Add(earVert.index);
        triangleIndices.Add(earVert.next.index);
    }
    bool PointNotInTriangle(Vector3 point, Vector3[] tri)
    {
        float denom = ((tri[1].z - tri[2].z) * (tri[0].x - tri[2].x) +
                       (tri[2].x - tri[1].x) * (tri[0].z - tri[2].z));
        if (denom == 0)
            return true;
        denom = 1.0f / denom;
        float alpha = denom * ((tri[1].z - tri[2].z) * (point.x - tri[2].x) +
                                 (tri[2].x - tri[1].x) * (point.z - tri[2].z));
        float beta = denom * ((tri[2].z - tri[0].z) * (point.x - tri[2].x) +
                                 (tri[0].x - tri[2].x) * (point.z - tri[2].z));
        float gamma = 1.0f - alpha - beta;
        return (gamma < 0 || alpha < 0 || beta < 0);
    }
    #endregion
  

}


