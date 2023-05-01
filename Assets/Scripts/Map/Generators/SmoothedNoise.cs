using UnityEngine;

public class SmoothedNoise : MapGenerator {
    private int smoothingIterations = 20;
    private float fillRatio = 0.53f;

    protected override void PopulateVoxelMap() {
        for (int x = 0; x < SizeX; x++) {
            for (int y = 0; y < SizeY; y++) {
                for (int z = 0; z < SizeZ; z++) {
                    if (x == 0 || x == SizeX - 1 || y == 0 || y == SizeY - 1 || z == 0 || z == SizeZ - 1) {
                        voxelMap[x, y, z] = true;
                    }
                    else {
                        if (Random.value <= fillRatio) {
                            voxelMap[x, y, z] = true;
                        }
                        else {
                            voxelMap[x, y, z] = false;
                        }
                    }
                }
            }
        }

        SmoothenMap(smoothingIterations);

    }
}