using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class KDNode
{
    public List<Triangle> Triangles { get; private set; }
    public KDNode Left { get; private set; }
    public KDNode Right { get; private set; }
    public int Axis { get; private set; }

    public Bounds Bounding { get; private set; }

    public KDNode(List<Triangle> triangles)
    {
        Triangles = triangles;
        Bounding = CalculateBoundingBox(Triangles);
    }

    public KDNode(KDNode left, KDNode right, int axis)
    {
        Left = left;
        Right = right;
        Axis = axis;
        Triangles = new List<Triangle>();
        Triangles.AddRange(left.Triangles);
        Triangles.AddRange(right.Triangles);
        Bounding = CalculateBoundingBox(Triangles);
    }

    private Bounds CalculateBoundingBox(List<Triangle> triangles)
    {
        Vector3 min = triangles[0].Min;
        Vector3 max = triangles[0].Max;
        for (int i = 1; i < triangles.Count; i++)
        {
            min = Vector3.Min(min, triangles[i].Min);
            max = Vector3.Max(max, triangles[i].Max);
        }
        return new Bounds(min + (max - min) / 2f, max - min);
    }
    public void DrawGizmos()
    {
        if (Left != null)
        {
            Left.DrawGizmos();
        }
        if (Right != null)
        {
            Right.DrawGizmos();
        }
        if (Triangles != null && Triangles.Count > 0)
        {
            Gizmos.color = Color.white;
            foreach (Triangle triangle in Triangles)
            {
                Gizmos.DrawLine(triangle.V1, triangle.V2);
                Gizmos.DrawLine(triangle.V2, triangle.V3);
                Gizmos.DrawLine(triangle.V3, triangle.V1);
            }
        }
    }
}
public class Triangle
{
    public int Index1 { get; private set; }
    public int Index2 { get; private set; }
    public int Index3 { get; private set; }
    public Vector3 V1 { get; private set; }
    public Vector3 V2 { get; private set; }
    public Vector3 V3 { get; private set; }
    public Vector3 Center { get; private set; }
    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }

    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, int index1, int index2, int index3)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
        this.Index1 = index1;
        this.Index2 = index2;
        this.Index3 = index3;
        Center = (v1 + v2 + v3) / 3f;
        Min = Vector3.Min(Vector3.Min(v1, v2), v3);
        Max = Vector3.Max(Vector3.Max(v1, v2), v3);
    }
}

public static class ClusterSplit
{
    private static int s_maxDepth = 6;
    private static int s_maxTrianglesPerNode = 128;

    static int indext = 0;

   /* public static int[] GetAfreshSoftTriangles(List<KDNode> nodeList)
    {
        List<int> triangless = new List<int>();

        int start = 0;
        //重新排序三角形索引
        foreach (var node in nodeList)
        {
            foreach (var tri in node.Triangles)
            {
                triangless.Add(tri.Index1);
                triangless.Add(tri.Index2);
                triangless.Add(tri.Index3);

            }
            node.StartIndex = start;

            start = start + node.Triangles.Count * 3;
            node.EndIndex = start - 1;
        }
        return triangless.ToArray();
    }*/

    public static List<KDNode> Split(Mesh mesh, int maxDepth = 6, int maxTrianglesPerNode = 128)
    {
        if (mesh == null)
        {
            return null;
        }
        s_maxDepth = maxDepth;
        s_maxTrianglesPerNode = maxTrianglesPerNode;

        List<Triangle> triangles = new List<Triangle>();
        HashSet<Triangle> uniqueTriangles = new HashSet<Triangle>();
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            Vector3 v1 = mesh.vertices[mesh.triangles[i]];
            Vector3 v2 = mesh.vertices[mesh.triangles[i + 1]];
            Vector3 v3 = mesh.vertices[mesh.triangles[i + 2]];
            Triangle triangle = new Triangle(v1, v2, v3, mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2]);
            if (uniqueTriangles.Add(triangle))
            {
                triangles.Add(triangle);
            }
        }
        KDNode rootNode = BuildKDTree(triangles, 0);
        return GetLeaves(rootNode);
    }
    private static KDNode BuildKDTree(List<Triangle> triangles, int depth)
    {
        if (triangles.Count <= s_maxTrianglesPerNode || depth >= s_maxDepth)
        {
            return new KDNode(triangles);
        }

        // Find the longest axis of the bounding box
        Vector3 min = triangles[0].Min;
        Vector3 max = triangles[0].Max;
        for (int i = 1; i < triangles.Count; i++)
        {
            min = Vector3.Min(min, triangles[i].Min);
            max = Vector3.Max(max, triangles[i].Max);
        }
        Vector3 size = max - min;
        int axis = size.x > size.y ? (size.x > size.z ? 0 : 2) : (size.y > size.z ? 1 : 2);

        // Sort the triangles along the axis
        triangles.Sort((t1, t2) => t1.Center[axis].CompareTo(t2.Center[axis]));

        // Split the triangles into two groups
        int mid = triangles.Count / 2;
        List<Triangle> leftTriangles = triangles.GetRange(0, mid);
        List<Triangle> rightTriangles = triangles.GetRange(mid, triangles.Count - mid);

        // Recursively build the left and right subtrees
        KDNode leftNode = BuildKDTree(leftTriangles, depth + 1);
        KDNode rightNode = BuildKDTree(rightTriangles, depth + 1);

        // Create a new node for the current level
        return new KDNode(leftNode, rightNode, axis);
    }
    private static List<KDNode> GetLeaves(KDNode rootNode)
    {
        List<KDNode> leaves = new List<KDNode>();
        GetLeavesSubtree(rootNode, leaves);
        return leaves;
    }
    private static void GetLeavesSubtree(KDNode node, List<KDNode> leaves)
    {
        if (node == null)
        {
            return;
        }

        if (node.Left == null && node.Right == null)
        {
            leaves.Add(node);
        }
        else
        {
            GetLeavesSubtree(node.Left, leaves);
            GetLeavesSubtree(node.Right, leaves);
        }
    }
    public static void OnDrawGizmos(List<KDNode> nodeList)
    {
        if (nodeList != null)
        {
            indext = 0;
            foreach (var node in nodeList)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(node.Bounding.center, node.Bounding.size);
                //node.DrawGizmos();
                indext += node.Triangles.Count;
            }
            Debug.Log(indext);
        }
    }
}
