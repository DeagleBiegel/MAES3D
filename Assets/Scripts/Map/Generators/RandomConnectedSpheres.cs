using MAES3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RandomConnectedSpheres : MapGenerator {

    private int minRadius = 3;
    private int maxRadius = 6;

    private int connectionsToMake = 3;
    private int connectionRadius = 3;

    private float ratioToClear = 0.2f;
    private int smoothingIterations = 2;

    protected override void PopulateVoxelMap() {

        // Create pockets in map
        List<(Cell, int)> pockets = new List<(Cell, int)>();

        int totalVoxels = SizeX * SizeY * SizeZ;
        int clearedVoxels = 0;
        while (((float)clearedVoxels / (float)totalVoxels) < ratioToClear) {
            int radius = Random.Range(minRadius, maxRadius + 1);
            Cell center = new Cell(
                Random.Range(radius + 1, SizeX - 2 - radius),
                Random.Range(radius + 1, SizeY - 2 - radius),
                Random.Range(radius + 1, SizeZ - 2 - radius)
            );

            if (voxelMap[center.x, center.y, center.z] == true) {
                pockets.Add((center, radius));

                for (int dx = -radius; dx <= radius; dx++) {
                    for (int dy = -radius; dy <= radius; dy++) {
                        for (int dz = -radius; dz <= radius; dz++) {
                            Cell c = new Cell(center.x + dx, center.y + dy, center.z + dz);
                            int dist = Mathf.RoundToInt(Vector3.Distance(c.middle, center.middle));
                            if (dist <= radius) {
                                if (voxelMap[c.x, c.y, c.z] == true) {
                                    voxelMap[c.x, c.y, c.z] = false;
                                    clearedVoxels++;
                                }
                            }
                        }
                    }
                }
            }
        }

        //Make connections between pockets
        List<(Cell, Cell)> pocketConnections = new List<(Cell, Cell)>();

        for (int i = 0; i < pockets.Count; i++) {
            Cell pocket = pockets[i].Item1;

            Dictionary<Cell, float> distances = new Dictionary<Cell, float>();

            for (int j = 0; j < pockets.Count; j++) {
                Cell otherPocket = pockets[j].Item1;

                if (pocket != otherPocket) {
                    float distance = Vector3.Distance(pocket.middle, otherPocket.middle);
                    distances.Add(otherPocket, distance);
                }
            }

            var sortedDistances = distances.OrderBy(x => x.Value);
            List<Cell> closestPockets = sortedDistances.Take(connectionsToMake).Select(x => x.Key).ToList();
            foreach (Cell closestPocket in closestPockets) {
                if (!pocketConnections.Contains((pocket, closestPocket)) && !pocketConnections.Contains((closestPocket, pocket))) {
                    pocketConnections.Add((pocket, closestPocket));
                }
            }
        }

        //Free connections
        foreach ((Cell, Cell) pair in pocketConnections) {
            FreeVoxelsAlongLine(pair.Item1, pair.Item2, connectionRadius);
        }

        //Draw connections
        //DEBUG ONLY
        foreach ((Cell, Cell) pair in pocketConnections) {
            Cell origin = pair.Item1;
            Cell target = pair.Item2;

            Debug.DrawLine(origin.middle, target.middle, UnityEngine.Color.blue, 50);
        }

        //Make edge solid
        //Needlessly checks every voxel weh nwe only need the edge
        //Could also be done by only removeing the the faces from the outer walls instead of every face from the outer wall
        for (int x = 0; x < voxelMap.GetLength(0); x++) {
            for (int y = 0; y < voxelMap.GetLength(1); y++) {
                for (int z = 0; z < voxelMap.GetLength(2); z++) {
                    if (x == 1 || x == voxelMap.GetLength(0) - 2 || y == 1 || y == voxelMap.GetLength(1) - 2 || z == 1 || z == voxelMap.GetLength(2) - 2) {
                        voxelMap[x, y, z] = true;
                    }
                }
            }
        }

        SmoothenMap(smoothingIterations);

    }

    private void FreeVoxelsAlongLine(Cell start, Cell end, int distance) {
        Vector3 direction = end.middle - start.middle;
        float magnitude = direction.magnitude;
        Vector3 step = direction / magnitude;

        Vector3 current = start.middle;

        //Find cells along line
        for (float t = 0; t <= magnitude; t += 1f) {

            Cell currentCell = Utility.CoordinateToCell(current);

            for (int x = currentCell.x - distance; x <= currentCell.x + distance; x++) {
                for (int y = currentCell.y - distance; y <= currentCell.y + distance; y++) {
                    for (int z = currentCell.z - distance; z <= currentCell.z + distance; z++) {
                        Cell c = new Cell(x, y, z);
                        if (Vector3.Distance(currentCell.middle, c.middle) <= distance) {
                            voxelMap[x, y, z] = false;
                        }
                    }
                }
            }
            current += step;
        }
    }

}
