using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Networking;
using System;
using System.Drawing;

public class MapImporter : MapGenerator {

    string filePath = "maps/map.m3dm";

    protected override void PopulateVoxelMap() {

        M3DMReader fileReader = new M3DMReader();
        fileReader.OpenFile(filePath);

        SimulationSettings.Width = fileReader.ReadBitsAsInt(7);
        SimulationSettings.Height = fileReader.ReadBitsAsInt(7);
        SimulationSettings.Depth = fileReader.ReadBitsAsInt(7);

        int width = SimulationSettings.Width;
        int height = SimulationSettings.Height;
        int depth = SimulationSettings.Depth;

        if (width < 30 || width > 100 || height < 30 || height > 100 || depth < 30 || depth > 100) {
            Exception e = new Exception("File indicates incompatible map size");
            Debug.LogException(e);
            Debug.Break();
        }

        int offset = fileReader.ReadBitsAsInt(3);

        int dataLength = fileReader.GetLength() - 3;
        int expectedDataLength = (width * height * depth) + offset;

        if (expectedDataLength % 8 != 0) {
            Exception e = new Exception("File header indicates an uneven amount of bytes.");
            Debug.LogException(e);
            Debug.Break();
        }

        if ((expectedDataLength / 8) != fileReader.GetLength() - 3) {
            Exception e = new Exception($"File header indicates a wrong amount of data bytes. Expected: {dataLength}. Actual: {expectedDataLength / 8}");
            Debug.LogException(e);
            Debug.Break();
        }

        fileReader.SkipBits(offset);
        bool[] dataArray = fileReader.ReadBits(dataLength * 8 - offset);

        fileReader.CloseFile();

        voxelMap = new bool[SimulationSettings.Width, SimulationSettings.Height, SimulationSettings.Depth];

        for (int i = 0; i < dataArray.Length; i++) {
            int x = i / (width * height);
            int y = (i % (width * height)) / width;
            int z = (i % (width * height)) % width;

            voxelMap[x, y, z] = dataArray[i];
        }
    }
}
