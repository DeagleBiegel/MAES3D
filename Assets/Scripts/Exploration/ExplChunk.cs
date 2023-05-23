using System.Collections;
using System.Collections.Generic;
using MAES3D;
using UnityEngine;

public class ExplChunk : MonoBehaviour
{
    public bool[,,] Filled { get; set; }
    public Vector3Int Position { get; set; }

    private bool[,,] explored;
    private bool[,,] layout;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    private int vertexIndex = 0;

    private Map cave;

    void Awake()
    {
        Filled = new bool[Explored.CHUNK_SIZE, Explored.CHUNK_SIZE, Explored.CHUNK_SIZE];

        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        // Find the object with the name.
        GameObject simulationObject = GameObject.Find("Simulation(Clone)");
        GameObject chunkObject = GameObject.Find("Map(Clone)");

        // Get the component of the script attached to the object.
        Simulation simulation = simulationObject.GetComponent<Simulation>();
        cave = chunkObject.GetComponent<Map>();

        // Make a reference to the array of explored voxels
        explored = simulation.ExplorationManager.ExploredMap;
    }

    private void CreateMeshData() 
    {
        for (int y = 0; y < Filled.GetLength(1); y++) 
        {
            for (int x = 0; x < Filled.GetLength(0); x++) 
            {
                for (int z = 0; z < Filled.GetLength(2); z++) 
                {
                    if (Filled[x, y, z]) 
                    {
                        AddVoxelDataToChunk(new Vector3(x, y, z));
                    }
                }
            }
        }
    }

    public void UpdateMesh() 
    {
        ClearMeshData();
        CreateMeshData();
        CreateMesh();
    }

    private void ClearMeshData() 
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }

    private bool CheckVoxel(Vector3 pos) 
    {
        int x = Mathf.FloorToInt(Position.x * Explored.CHUNK_SIZE + pos.x);
        int y = Mathf.FloorToInt(Position.y * Explored.CHUNK_SIZE + pos.y);
        int z = Mathf.FloorToInt(Position.z * Explored.CHUNK_SIZE + pos.z);

        
        if (x < 0 || x > explored.GetLength(0) - 1 || y < 0 || y > explored.GetLength(1) - 1 || z < 0 || z > explored.GetLength(2) - 1)
            return true;
        

        return (explored[x, y, z] || cave.IsWall(x, y, z));
    }

    private void AddVoxelDataToChunk(Vector3 pos) 
    {
        for (int p = 0; p < 6; p++) 
        {
            if (CheckVoxel(pos + VoxelData.faceChecks[p])) 
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);
                uvs.Add(VoxelData.voxelUvs[0]);
                uvs.Add(VoxelData.voxelUvs[1]);
                uvs.Add(VoxelData.voxelUvs[2]);
                uvs.Add(VoxelData.voxelUvs[3]);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
                vertexIndex += 4;
            }
        }
    }

    private void CreateMesh() 
    {
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh.vertices = vertices.ToArray();
        meshFilter.mesh.triangles = triangles.ToArray();
        meshFilter.mesh.uv = uvs.ToArray();
        meshFilter.mesh.RecalculateNormals();
    }
}
