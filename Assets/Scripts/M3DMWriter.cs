using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class M3DMWriter {
    List<bool> fileContent = new List<bool>();

    public void WriteBools(List<bool> boolList) {
        fileContent.AddRange(boolList);
    }

    public void WriteInt(int number, int numBits) {

        if (numBits == 0) {
            return;
        }

        if (number >= (1 << numBits)) {
            throw new ArgumentException("Number is too large for the specified number of bits");
        }

        bool[] bits = new bool[numBits];

        for (int i = 0; i < numBits; i++) {
            bits[i] = ((number >> i) & 1) == 1;
        }

        Array.Reverse(bits);

        fileContent.AddRange(bits);
    }

    public void CommitToFile(string filePath) {
        if (fileContent == null || fileContent.Count < 0) {
            throw new InvalidOperationException("No data to write");
        }

        BitArray bitArray = new BitArray(fileContent.ToArray());

        int byteCount = (bitArray.Length + 7) / 8;
        byte[] byteArray = new byte[byteCount];

        for (int i = 0; i < bitArray.Length; i++) {
            if (bitArray[i]) {
                int byteIndex = i / 8;
                int innerIndex = i % 8;
                byteArray[byteIndex] |= (byte)(1 << (7 - innerIndex));
            }
        }

        File.WriteAllBytes(filePath, byteArray);
    }
}
