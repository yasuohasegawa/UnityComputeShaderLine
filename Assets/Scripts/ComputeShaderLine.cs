using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

struct LineData
{
    public Vector3 pos0;
    public Vector3 pos1;
    public Vector3 pos2;
    public Vector3 pos3;
}

public class ComputeShaderLine : MonoBehaviour {
    const int BLOCK_SIZE = 256;
    public static uint numeberOfLines = 10000;

    private bool isLoop = true;
    private int numberOfSegments = 5;
    private float lineWidth = 0.5f;

    public List<Vector3> controlPoints { get; set; }
    public Material material;
    public ComputeShader computeShader;

    private Mesh mesh;
    private Bounds bounds;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer visibleBuffer;
    private ComputeBuffer lineDataBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5];
    

    private Vector3[] positions;
    private int[] visible;
    private LineData[] ld;

    private int pointCount = 0;
    private List<Vector3> points = new List<Vector3>();

    // Use this for initialization
    void Awake()
    {
        controlPoints = new List<Vector3>();
        mesh = CreateMesh();

        // 12 = float (4 byte x 3）
        positionBuffer = new ComputeBuffer((int)numeberOfLines, 12);
        visibleBuffer = new ComputeBuffer((int)numeberOfLines, sizeof(int));
        lineDataBuffer = new ComputeBuffer((int)numeberOfLines, Marshal.SizeOf(typeof(LineData)));
        positions = new Vector3[numeberOfLines];

        visible = new int[numeberOfLines];
        ld = new LineData[numeberOfLines];

        for (int i = 0; i < numeberOfLines; i++)
        {
            LineData ldata = new LineData();
            ldata.pos0 = Vector3.zero;
            ldata.pos1 = Vector3.zero;
            ldata.pos2 = Vector3.zero;
            ldata.pos2 = Vector3.zero;

            visible[i] = 0;
            positions[i] = Vector3.zero;
            ld[i] = ldata;
        }

        positionBuffer.SetData(positions);
        visibleBuffer.SetData(visible);
        lineDataBuffer.SetData(ld);

        material.SetBuffer("PositionBuffer", positionBuffer);
        material.SetBuffer("VisibleBuffer", visibleBuffer);
        material.SetBuffer("LineDataBuffer", lineDataBuffer);

        bounds = new Bounds(Vector3.zero, new Vector3(numeberOfLines / 3, numeberOfLines / 3, numeberOfLines / 3));

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = mesh.GetIndexCount(0);
        args[1] = numeberOfLines;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;
        argsBuffer.SetData(args);
    }

    
    public void AddPosition(Vector3 pos)
    {
        Vector3 p0;
        Vector3 p1;
        Vector3 m0;
        Vector3 m1;

        controlPoints.Add(pos);

        pointCount = 0;
        points.Clear();

        if (controlPoints.Count < 2)
        {
            return;
        }

        int last = controlPoints.Count - 1;
        int ind = 0;
        int len = (numberOfSegments * last);

        // https://en.wikibooks.org/wiki/Cg_Programming/Unity/Hermite_Curves
        for (int j = 0; j < last; j++)
        {
            if (controlPoints[j] == null ||
                controlPoints[j + 1] == null ||
                (j > 0 && controlPoints[j - 1] == null) ||
                (j < controlPoints.Count - 2 &&
                    controlPoints[j + 2] == null))
            {
                return;
            }

            p0 = controlPoints[j];
            p1 = controlPoints[j + 1];
            if (j > 0)
            {
                m0 = 0.5f * (controlPoints[j + 1] - controlPoints[j - 1]);
            }
            else
            {
                m0 = controlPoints[j + 1] - controlPoints[j];
            }

            if (j < controlPoints.Count - 2)
            {
                m1 = 0.5f * (controlPoints[j + 2] - controlPoints[j]);
            }
            else
            {
                m1 = controlPoints[j + 1] - controlPoints[j];
            }

            Vector3 position;

            float t;
            float pointStep = 1.0f / numberOfSegments;
            if (j == controlPoints.Count - 2)
            {
                pointStep = 1.0f / (numberOfSegments - 1.0f);
            }

            for (int i = 0; i < numberOfSegments; i++)
            {
                t = i * pointStep;
                position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * p0
                    + (t * t * t - 2.0f * t * t + t) * m0
                    + (-2.0f * t * t * t + 3.0f * t * t) * p1
                    + (t * t * t - t * t) * m1;

                if (ind < len)
                {
                    AddLine(position);
                    ind++;
                }
            }
        }

        visible[pointCount] = 0;
        visible[pointCount - 1] = 0;
    }

    private void AddLine(Vector3 pos)
    {
        LineData ldata = new LineData();
        visible[pointCount] = 1;
        ld[pointCount] = ldata;

        points.Add(pos);

        if (points.Count < 2)
        {
            pointCount++;
            return;
        }

        Vector3 left, right;

        Vector3 direction = Vector3.up;
        Vector3 forward = Vector3.Normalize(points[pointCount - 1] - points[pointCount]);
        Vector3 r = Vector3.Cross(direction, forward);

        // note: Below is how to calculate up vector.
        // Vector3 up = Vector3.Cross(forward, right); 

        right = points[pointCount - 1] + r * lineWidth / 2f;
        left = points[pointCount - 1] - r * lineWidth / 2f;
        ld[pointCount-1].pos0 = left;
        ld[pointCount - 1].pos1 = right;

        forward = Vector3.Normalize(points[points.Count - 1] - points[points.Count - 2]);
        r = Vector3.Cross(direction, forward);

        right = points[points.Count - 1] + r * lineWidth / 2f;
        left = points[points.Count - 1] - r * lineWidth / 2f;
        ld[pointCount - 1].pos2 = left;
        ld[pointCount - 1].pos3 = right;

        pointCount++;
    }

    // Update is called once per frame
    void Update()
    {
        visibleBuffer.SetData(visible);
        material.SetBuffer("VisibleBuffer", visibleBuffer);

        for (int i = 1; i < pointCount; i++)
        {
            ld[i].pos0 = ld[(i - 1)].pos3;
            ld[i].pos1 = ld[(i - 1)].pos2;
        }

        if (isLoop)
        {
            ld[pointCount-2].pos3 = ld[0].pos0;
            ld[pointCount-2].pos2 = ld[0].pos1;
        }

        lineDataBuffer.SetData(ld);
        material.SetBuffer("LineDataBuffer", lineDataBuffer);

        int kernelId = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelId, "PositionBuffer", positionBuffer);
        computeShader.SetBuffer(kernelId, "VisibleBuffer", visibleBuffer);
        computeShader.SetBuffer(kernelId, "LineDataBuffer", lineDataBuffer);
        int groupSize = Mathf.CeilToInt(numeberOfLines / BLOCK_SIZE); // (number of objects / thread group)*(number of objects / thread group)
        computeShader.Dispatch(kernelId, groupSize, 1, 1);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    // quad
    private Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];

        float size = 0.5f;

        vertices[0] = new Vector3(-size, 0, size);
        vertices[1] = new Vector3(size, 0, size);
        vertices[2] = new Vector3(size, 0, -size);
        vertices[3] = new Vector3(-size, 0, -size);

        int[] tri = new int[6];
        tri[0] = 0;
        tri[1] = 1;
        tri[2] = 2;

        tri[3] = 0;
        tri[4] = 2;
        tri[5] = 3;

        Vector3[] normals = new Vector3[4];
        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;

        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(0, 1);

        Vector2[] uv2 = new Vector2[4];
        uv2[0] = new Vector2(0, 0);
        uv2[1] = new Vector2(1, 0);
        uv2[2] = new Vector2(2, 0);
        uv2[3] = new Vector2(3, 0);

        mesh.vertices = vertices;
        mesh.triangles = tri;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.uv2 = uv2;

        return mesh;
    }

    void OnDestroy()
    {
        visibleBuffer.Release();
        lineDataBuffer.Release();
        positionBuffer.Release();
        argsBuffer.Release();
    }
}
