using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MapGenerator {

    protected bool[,,] voxelMap;
    protected int SizeX => voxelMap.GetLength(0);
    protected int SizeY => voxelMap.GetLength(1);
    protected int SizeZ => voxelMap.GetLength(2);

    public MapGenerator() {
        voxelMap = new bool[SimulationSettings.Width, SimulationSettings.Height, SimulationSettings.Depth];

        InitializeVoxelMap();
    }

    public bool[,,] GenerateMap() {

        PopulateVoxelMap();

        RemovePockets();

        return voxelMap;
    }

    protected abstract void PopulateVoxelMap();

    protected void SmoothenMap(int iterations) {
        for (int i = 0; i < iterations; i++) {
            SmoothenMapIteration();
        }
    }

    private void InitializeVoxelMap() {
        for (int x = 0; x < SizeX; x++) {
            for (int y = 0; y < SizeY; y++) {
                for (int z = 0; z < SizeZ; z++) {
                    voxelMap[x, y, z] = true;
                }
            }
        }
    }

    private void SmoothenMapIteration() {

        bool[,,] tempMap = (bool[,,])voxelMap.Clone();

        for (int x = 1; x < SizeX - 1; x++) {
            for (int y = 1; y < SizeY - 1; y++) {
                for (int z = 1; z < SizeZ - 1; z++) {

                    int neighbourWallCount = 0;
                    for (int dx = x - 1; dx <= x + 1; dx++) {
                        for (int dy = y - 1; dy <= y + 1; dy++) {
                            for (int dz = z - 1; dz <= z + 1; dz++) {
                                if (voxelMap[dx, dy, dz] == true && !(dx == x && dy == y && dz == z)) {
                                    neighbourWallCount++;
                                }
                            }
                        }
                    }

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

    private void RemovePockets() {
        int[,,] group = new int[voxelMap.GetLength(0), voxelMap.GetLength(1), voxelMap.GetLength(2)];
        int currentGroup = 0;

        for (int x = 1; x < SizeX - 1; x++) {
            for (int y = 1; y < SizeY - 1; y++) {
                for (int z = 1; z < SizeZ - 1; z++) {
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

            for (int x = SizeX - 1; x > 0; x--) {
                for (int y = SizeY - 1; y > 0; y--) {
                    for (int z = SizeZ - 1; z > 0; z--) {
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
        for (int x = 1; x < SizeX - 1; x++) {
            for (int y = 1; y < SizeY - 1; y++) {
                for (int z = 1; z < SizeZ - 1; z++) {
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

        for (int x = 1; x < SizeX - 1; x++) {
            for (int y = 1; y < SizeY - 1; y++) {
                for (int z = 1; z < SizeZ - 1; z++) {
                    if (group[x, y, z] != biggestGroup) {

                        voxelMap[x, y, z] = true;
                    }
                }
            }
        }
    }
}
