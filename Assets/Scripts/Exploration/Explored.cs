using System.Collections;
using System.Collections.Generic;
using MAES3D;
using UnityEngine;

public class Explored : MonoBehaviour
{
    public GameObject ChunkPrefab;

    private List<Vector3Int> _newlyExplored;

    private GameObject[,,] chunks;

    public const int CHUNK_SIZE = 32;

    private int delay = 0;

    // Start is called before the first frame update
    void Awake()
    {
        // Find the object with the name.
        GameObject simulationObject = GameObject.Find("Simulation(Clone)");
        GameObject chunkObject = GameObject.Find("Chunk(Clone)");

        // Get the component of the script attached to the object.
        Simulation simulation = simulationObject.GetComponent<Simulation>();
        Chunk cave = chunkObject.GetComponent<Chunk>();

        // Make a reference to the array of explored voxels
        _newlyExplored = simulation.ExplorationManager.NewlyExplored;
        var layout = cave.GetVoxelMap();

        int width =  Mathf.CeilToInt(layout.GetLength(0) / (float) CHUNK_SIZE);
        int height = Mathf.CeilToInt(layout.GetLength(1) / (float) CHUNK_SIZE);
        int depth =  Mathf.CeilToInt(layout.GetLength(2) / (float) CHUNK_SIZE);

        chunks = new GameObject[width, height, depth];

        for (int x = 0; x < width; x++) 
        {
            for (int y = 0; y < height; y++) 
            {
                for (int z = 0; z < depth; z++) 
                {
                    chunks[x, y, z] = Instantiate(ChunkPrefab, new Vector3(x * CHUNK_SIZE, y * CHUNK_SIZE, z * CHUNK_SIZE), Quaternion.identity, transform);
                    ExplChunk chunk = chunks[x, y, z].GetComponent<ExplChunk>();

                    chunk.Position = new Vector3Int(x, y, z);

                    for (int _x = 0; _x < CHUNK_SIZE; _x++) 
                    {
                        for (int _y = 0; _y < CHUNK_SIZE; _y++) 
                        {
                            for (int _z = 0; _z < CHUNK_SIZE; _z++) 
                            {
                                if (chunk.Position.x * CHUNK_SIZE + _x > layout.GetLength(0) - 1 || chunk.Position.y * CHUNK_SIZE + _y > layout.GetLength(1) - 1 || chunk.Position.z * CHUNK_SIZE + _z > layout.GetLength(2) - 1)
                                    continue;

                                chunk.Filled[_x, _y, _z] = !layout[chunk.Position.x * CHUNK_SIZE + _x, chunk.Position.y * CHUNK_SIZE + _y, chunk.Position.z * CHUNK_SIZE + _z];
                            } 
                        }
                    }

                    chunk.UpdateMesh();
                }
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (delay <= 10) 
        {
            delay++;
            return;
        }
        else 
        {
            delay = 0;
        }

        if (_newlyExplored.Count > 0) 
        {
            HashSet<ExplChunk> updatedChunks = new HashSet<ExplChunk>();

            foreach (var nExplored in _newlyExplored) 
            {
                int chunkX = Mathf.FloorToInt(nExplored.x / (float) CHUNK_SIZE);
                int chunkY = Mathf.FloorToInt(nExplored.y / (float) CHUNK_SIZE);
                int chunkZ = Mathf.FloorToInt(nExplored.z / (float) CHUNK_SIZE);

                GameObject chunkObject = chunks[chunkX, chunkY, chunkZ];
                ExplChunk chunk = chunkObject.GetComponent<ExplChunk>();

                int chunkPosX = nExplored.x % CHUNK_SIZE;
                int chunkPosY = nExplored.y % CHUNK_SIZE;
                int chunkPosZ = nExplored.z % CHUNK_SIZE;

                chunk.Filled[chunkPosX, chunkPosY, chunkPosZ] = false;

                if (chunkPosX == 0 && chunkX != 0) 
                {
                    updatedChunks.Add(GetChunk(chunkX - 1, chunkY, chunkZ));
                }

                if (chunkPosX == CHUNK_SIZE && chunkX != chunks.GetLength(0) - 1) 
                {
                    updatedChunks.Add(GetChunk(chunkX + 1, chunkY, chunkZ));
                }

                if (chunkPosY == 0 && chunkY != 0) 
                {
                    updatedChunks.Add(GetChunk(chunkX, chunkY - 1, chunkZ));
                }

                if (chunkPosY == CHUNK_SIZE && chunkY != chunks.GetLength(1) - 1) 
                {
                    updatedChunks.Add(GetChunk(chunkX, chunkY + 1, chunkZ));
                }

                if (chunkPosZ == 0 && chunkZ != 0) 
                {
                    updatedChunks.Add(GetChunk(chunkX, chunkY, chunkZ - 1));
                }

                if (chunkPosZ == CHUNK_SIZE && chunkZ != chunks.GetLength(2) - 1) 
                {
                    updatedChunks.Add(GetChunk(chunkX, chunkY, chunkZ + 1));
                }

                updatedChunks.Add(chunk);      
            }

            foreach(var chunk in updatedChunks) 
            {
                chunk.UpdateMesh();
            }

            _newlyExplored.Clear();
        }
    }

    private ExplChunk GetChunk(int x, int y, int z) 
    {
        GameObject chunkObject = chunks[x, y, z];
        return chunkObject.GetComponent<ExplChunk>();
    }
}
