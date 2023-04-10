using MAES3D.Agent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MAES3D {

    public class Chunk : MonoBehaviour {

        private int ChunkHeight;
        private int ChunkWidth;

        private int ChunkDepth;
        private int smoothingIterations;

        private bool useRandomSeed = true;
        private int seed;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        int vertexIndex = 0;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        private bool[,,] voxelMap;

        public List<Cell> SpawnPositions = new List<Cell>();

        void Awake() {

            ChunkHeight = SimulationSettings.Height;
            ChunkWidth = SimulationSettings.Width;
            ChunkDepth = SimulationSettings.Depth;

            smoothingIterations = SimulationSettings.smoothingIterations;

            useRandomSeed = SimulationSettings.useRandomSeed;
            seed = SimulationSettings.seed;


            voxelMap = new bool[ChunkWidth, ChunkHeight, ChunkDepth];
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshFilter = gameObject.GetComponent<MeshFilter>();
            meshCollider = gameObject.AddComponent<MeshCollider>();

            PopulateVoxelMap();

            for (int i = 0; i < smoothingIterations; i++)
                SmoothenVoxelMap();

            RemovePockets();

            RemoveCorners();

            FindSpawnPositions();

            CreateMeshData();
            CreateMesh();
        }

        public bool[,,] GetVoxelMap() {
            return voxelMap;
        }

        public int GetNumberOfExplorableTiles() {
            int explorableTiles = 0;

            for (int x = 0; x < voxelMap.GetLength(0); x++) {
                for (int y = 0; y < voxelMap.GetLength(1); y++) {
                    for (int z = 0; z < voxelMap.GetLength(2); z++) {
                        if (voxelMap[x, y, z] == false) {
                            explorableTiles++;
                        }
                    }
                }
            }

            return explorableTiles;
        }

        private void PopulateVoxelMap() {

            if (useRandomSeed) {
                seed = Mathf.Abs((int)System.DateTime.Now.Ticks);
                SimulationSettings.seed = seed;
            }

            Random.InitState((seed));

            for (int y = 0; y < ChunkHeight; y++) {
                for (int x = 0; x < ChunkWidth; x++) {
                    for (int z = 0; z < ChunkDepth; z++) {
                        if (x == 0 || x == ChunkWidth - 1 || y == 0 || y == ChunkHeight - 1 || z == 0 || z == ChunkDepth - 1) {
                            voxelMap[x, y, z] = true;
                        }
                        else {
                            if (Random.Range(0, 100) <= 53) {
                                voxelMap[x, y, z] = true;
                            }
                            else {
                                voxelMap[x, y, z] = false;
                            }
                        }
                    }
                }
            }
        }

        private void SmoothenVoxelMap() {

            bool[,,] tempMap = (bool[,,])voxelMap.Clone();

            for (int y = 1; y < ChunkHeight - 1; y++) {
                for (int x = 1; x < ChunkWidth - 1; x++) {
                    for (int z = 1; z < ChunkDepth - 1; z++) {

                        int neighbourWallCount = GetNeighbourWallCount(x, y, z);

                        if (neighbourWallCount > 13) {
                            tempMap[x, y, z] = true;
                        }
                        else {
                            tempMap[x, y, z] = false;
                        }
                    }
                }
            }

            voxelMap = tempMap;
        }

        private int GetNeighbourWallCount(int _x, int _y, int _z) {

            int wallCount = 0;

            for (int x = _x - 1; x <= _x + 1; x++) {
                for (int y = _y - 1; y <= _y + 1; y++) {
                    for (int z = _z - 1; z <= _z + 1; z++) {
                        if (voxelMap[x, y, z] && !(x == _x && y == _y && z == _z)) {
                            wallCount++;
                        }
                    }
                }
            }

            return wallCount;
        }

        private void CreateMeshData() {

            for (int y = 1; y < ChunkHeight - 1; y++) {
                for (int x = 1; x < ChunkWidth - 1; x++) {
                    for (int z = 1; z < ChunkDepth - 1; z++) {

                        if (voxelMap[x, y, z]) {
                            AddVoxelDataToChunk(new Vector3(x, y, z));
                        }
                    }
                }
            }

        }

        private bool CheckVoxel(Vector3 pos) {

            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);

            if (x < 0 || x > ChunkWidth - 1 || y < 0 || y > ChunkHeight - 1 || z < 0 || z > ChunkDepth - 1)
                return false;

            return voxelMap[x, y, z];

        }

        private void AddVoxelDataToChunk(Vector3 pos) {
            for (int p = 0; p < 6; p++) {

                if (!CheckVoxel(pos + VoxelData.faceChecks[p])) {

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

        private void CreateMesh() {

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();

            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = meshFilter.mesh;
        }

        private void RemovePockets() {
            int[,,] group = new int[voxelMap.GetLength(0), voxelMap.GetLength(1), voxelMap.GetLength(2)];
            int currentGroup = 0;

            for (int x = 1; x < voxelMap.GetLength(0) - 1; x++) {
                for (int y = 1; y < voxelMap.GetLength(1) - 1; y++) {
                    for (int z = 1; z < voxelMap.GetLength(2) - 1; z++) {
                        if (voxelMap[x, y, z] == false) {

                            int[] neighborGroups = {
                                group[x + 1, y, z], group[x - 1, y, z],
                                group[x, y + 1, z], group[x, y - 1, z],
                                group[x, y, z + 1], group[x, y, z - 1] };
                            int lowestNeighborGroup = 0;

                            foreach (int neighborGroup in neighborGroups) {
                                if (neighborGroup != 0) {

                                    if (lowestNeighborGroup == 0) {
                                        lowestNeighborGroup = neighborGroup;
                                    }

                                    if (neighborGroup < lowestNeighborGroup) {
                                        lowestNeighborGroup = neighborGroup;
                                    }
                                }
                            }

                            if (lowestNeighborGroup == 0) {
                                currentGroup++;
                                group[x, y, z] = currentGroup;
                            }
                            else {
                                group[x, y, z] = lowestNeighborGroup;
                            }
                        }
                    }
                }
            }

            bool stateHasChanged = true;
            while (stateHasChanged == true) {
                stateHasChanged = false;

                for (int x = voxelMap.GetLength(0) - 1; x > 0; x--) {
                    for (int y = voxelMap.GetLength(1) - 1; y > 0; y--) {
                        for (int z = voxelMap.GetLength(2) - 1; z > 0; z--) {
                            if (voxelMap[x, y, z] == false) {

                                int lowestNeighborGroup = 0;
                                int[] neighborGroups = {
                                group[x + 1, y, z], group[x - 1, y, z],
                                group[x, y + 1, z], group[x, y - 1, z],
                                group[x, y, z + 1], group[x, y, z - 1] };

                                foreach (int neighborGroup in neighborGroups) {
                                    if (neighborGroup != 0) {

                                        if (lowestNeighborGroup == 0) {
                                            lowestNeighborGroup = neighborGroup;
                                        }

                                        if (neighborGroup < lowestNeighborGroup) {
                                            lowestNeighborGroup = neighborGroup;
                                        }
                                    }
                                }

                                if (group[x, y, z] != lowestNeighborGroup) {
                                    group[x, y, z] = lowestNeighborGroup;
                                    stateHasChanged = true;
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<int, int> groupSizeMap = new Dictionary<int, int>();
            for (int x = 1; x < voxelMap.GetLength(0) - 1; x++) {
                for (int y = 1; y < voxelMap.GetLength(1) - 1; y++) {
                    for (int z = 1; z < voxelMap.GetLength(2) - 1; z++) {
                        if (group[x, y, z] != 0) {

                            if (!groupSizeMap.ContainsKey(group[x, y, z])) {
                                groupSizeMap.Add(group[x, y, z], 0);
                            }
                            groupSizeMap[group[x, y, z]]++;
                        }
                    }
                }
            }

            int biggestGroup = 0;
            int biggestGroupSize = 0;

            foreach (KeyValuePair<int, int> pair in groupSizeMap) {
                if (pair.Value > biggestGroupSize) {
                    biggestGroup = pair.Key;
                    biggestGroupSize = pair.Value;
                }
            }

            for (int x = 1; x < voxelMap.GetLength(0) - 1; x++) {
                for (int y = 1; y < voxelMap.GetLength(1) - 1; y++) {
                    for (int z = 1; z < voxelMap.GetLength(2) - 1; z++) {
                        if (group[x, y, z] != biggestGroup) {

                            voxelMap[x, y, z] = true;
                        }
                    }
                }
            }
        }

        private void RemoveCorners() {
            for (int x = 1; x < voxelMap.GetLength(0) - 1; x++) {
                for (int y = 1; y < voxelMap.GetLength(1) - 1; y++) {
                    for (int z = 1; z < voxelMap.GetLength(2) - 1; z++) {

                        for (int i = -1; i <= 1; i += 2) {
                            for (int j = -1; j <= 1; j += 2) {

                                if (voxelMap[x + i, y, z] &&
                                    voxelMap[x, y + j, z] &&
                                    voxelMap[x, y, z] == false &&
                                    voxelMap[x + i, y + j, z] == false) {

                                    voxelMap[x + i, y + j, z] = true;
                                    voxelMap[x, y, z] = true;
                                }

                                if (voxelMap[x + i, y, z] &&
                                    voxelMap[x, y, z + j] &&
                                    voxelMap[x, y, z] == false &&
                                    voxelMap[x + i, y, z + j] == false) {

                                    voxelMap[x + i, y, z + j] = true;
                                    voxelMap[x, y, z] = true;
                                }

                                if (voxelMap[x, y + i, z + j] &&
                                    voxelMap[x, y + i, z + j] &&
                                    voxelMap[x, y, z] == false &&
                                    voxelMap[x, y + i, z + j] == false) {

                                    voxelMap[x, y + i, z + j] = true;
                                    voxelMap[x, y, z] = true;
                                }

                            }
                        }

                    }
                }
            }
        }

        private void FindSpawnPositions() {
            float distance = 1000f;
            Cell closest = new Cell(0, 0, 0);

            for (int x = 0; x < ChunkWidth - 3; x++) {
                for (int y = 0; y < ChunkHeight - 3; y++) {
                    for (int z = 0; z < ChunkDepth - 3; z++) {
                        float tempDist = Vector3.Distance(new Cell(0, 0, 0).middle, new Cell(x, y, z).middle);

                        if (!voxelMap[x, y, z] && tempDist < distance) {
                            bool validSpawn = true;

                            for (int offsetX = 0; offsetX < 3; offsetX++) {
                                for (int offsetY = 0; offsetY < 3; offsetY++) {
                                    for (int offsetZ = 0; offsetZ < 3; offsetZ++) {
                                        if (voxelMap[x + offsetX, y + offsetY, z + offsetZ]) {
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
                        SpawnPositions.Add(new Cell(closest.x + offsetX, closest.y + offsetY, closest.z + offsetZ));
                    }
                }
            }
        }
    }
}