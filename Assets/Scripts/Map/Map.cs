using MAES3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour {

    public int SizeX => Width;
    public int SizeY => Height;
    public int SizeZ => Depth;
    public int Width => _voxelMap.GetLength(0);
    public int Height => _voxelMap.GetLength(1);
    public int Depth => _voxelMap.GetLength(2);
    public List<Cell> SpawnPositions => _spawnPositions;
    public int ExplorableTiles => _explorableTiles;


    private bool[,,] _voxelMap;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private int _vertexIndex = 0;
    private List<Vector3> _vertices = new List<Vector3>();
    private List<int> _triangles = new List<int>();
    private List<Vector2> _uvs = new List<Vector2>();

    private List<Cell> _spawnPositions = new List<Cell>();
    private int _explorableTiles;

    //void Awake() {
    //    _meshRenderer = gameObject.GetComponent<MeshRenderer>();
    //    _meshFilter = gameObject.GetComponent<MeshFilter>();
    //    _meshCollider = gameObject.AddComponent<MeshCollider>();
    //
    //    CreateMesh();
    //}

    public void InitMap(bool[,,] voxelMap) {

        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
        _meshFilter = gameObject.GetComponent<MeshFilter>();
        _meshCollider = gameObject.AddComponent<MeshCollider>();

        this._voxelMap = voxelMap;
        _explorableTiles = CalculateNumberOfExplorableTiles();
        CalculateSpawnPositions();
        CreateMeshData();
        CreateMesh();
    }

    private int CalculateNumberOfExplorableTiles() {
        int explorableTiles = 0;

        for (int x = 0; x < SizeX; x++) {
            for (int y = 0; y < SizeY; y++) {
                for (int z = 0; z < SizeZ; z++) {
                    if (_voxelMap[x, y, z] == false) {
                        explorableTiles++;
                    }
                }
            }
        }

        return explorableTiles;
    }

    private void CalculateSpawnPositions() {
        float distance = 1000f;
        Cell closest = new Cell(0, 0, 0);

        for (int x = 0; x < SizeX - 3; x++) {
            for (int y = 0; y < SizeY - 3; y++) {
                for (int z = 0; z < SizeZ - 3; z++) {
                    float tempDist = Vector3.Distance(new Cell(0, 0, 0).middle, new Cell(x, y, z).middle);

                    if (!IsWall(x, y, z) && tempDist < distance) {
                        bool validSpawn = true;

                        for (int offsetX = 0; offsetX < 3; offsetX++) {
                            for (int offsetY = 0; offsetY < 3; offsetY++) {
                                for (int offsetZ = 0; offsetZ < 3; offsetZ++) {
                                    if (IsWall(x + offsetX, y + offsetY, z + offsetZ)) {
                                        validSpawn = false;
                                    }
                                }
                            }
                        }

                        if (validSpawn) {
                            distance = tempDist;
                            closest = new Cell(x, y, z);
                        }
                    }
                }
            }
        }

        for (int offsetX = 0; offsetX < 3; offsetX++) {
            for (int offsetY = 0; offsetY < 3; offsetY++) {
                for (int offsetZ = 0; offsetZ < 3; offsetZ++) {
                    _spawnPositions.Add(new Cell(closest.x + offsetX, closest.y + offsetY, closest.z + offsetZ));
                }
            }
        }
    }

    private void CreateMeshData() {

        for (int x = 1; x < SizeX - 1; x++) {
            for (int y = 1; y < SizeY - 1; y++) {
                for (int z = 1; z < SizeZ - 1; z++) {
                    if (IsWall(x, y, z)) {
                        AddVoxelDataToChunk(new Vector3(x, y, z));
                    }
                }
            }
        }

    }

    private void AddVoxelDataToChunk(Vector3 pos) {
        for (int p = 0; p < 6; p++) {
            if (!IsWall(pos + VoxelData.faceChecks[p])) {
                _vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                _vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                _vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                _vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);
                _uvs.Add(VoxelData.voxelUvs[0]);
                _uvs.Add(VoxelData.voxelUvs[1]);
                _uvs.Add(VoxelData.voxelUvs[2]);
                _uvs.Add(VoxelData.voxelUvs[3]);
                _triangles.Add(_vertexIndex);
                _triangles.Add(_vertexIndex + 1);
                _triangles.Add(_vertexIndex + 2);
                _triangles.Add(_vertexIndex + 2);
                _triangles.Add(_vertexIndex + 1);
                _triangles.Add(_vertexIndex + 3);
                _vertexIndex += 4;
            }
        }
    }

    private void CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = _vertices.ToArray();
        mesh.triangles = _triangles.ToArray();
        mesh.uv = _uvs.ToArray();

        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;
        _meshCollider.sharedMesh = _meshFilter.mesh;
    }

    public bool IsWall(int x, int y, int z) {
        //If position is out of the map, return wall
        if (x < 0 || y < 0 || z < 0 || x > SizeX - 1 || y > SizeY - 1|| z > SizeZ - 1) {
            return true;
        }
        return _voxelMap[x, y, z];
    }
    public bool IsWall(Cell cell) {
        return IsWall(cell.x, cell.y, cell.z);
    }
    public bool IsWall(Vector3 pos) {
        return IsWall(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
    }
}
