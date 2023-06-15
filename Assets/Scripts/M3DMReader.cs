using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class M3DMReader {
    private byte[] buffer = new byte[1];
    private int bitOffset = 0;
    private FileStream fileStream;

    public bool OpenFile(string filePath) {
        try {
            fileStream = new FileStream(filePath, FileMode.Open);
            return true;
        }
        catch {
            Exception e = new Exception("There is no map.m3dm in the maps folder");
            Debug.LogException(e);
            Debug.Break();
            return false;
        }
    }

    public void CloseFile() {
        if (fileStream != null) {
            fileStream.Close();
            fileStream = null;
        }
    }

    public bool[] ReadBits(int numBits) {
        if (fileStream == null) {
            throw new InvalidOperationException("No file is open.");
        }

        bool[] bits = new bool[numBits];
        int bitsRead = 0;

        while (bitsRead < numBits) {
            if (bitOffset == 0) {
                int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0) {
                    break; // End of file
                }
            }

            bits[bitsRead] = ((buffer[0] >> (7 - bitOffset)) & 1) == 1;
            bitOffset = (bitOffset + 1) % 8;
            bitsRead++;
        }

        return bits;
    }

    public void SkipBits(int numBits) {
        if (fileStream == null) {
            throw new InvalidOperationException("No file is open.");
        }
        int bitsRead = 0;

        while (bitsRead < numBits) {
            if (bitOffset == 0) {
                int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0) {
                    break; // End of file
                }
            }

            bitOffset = (bitOffset + 1) % 8;
            bitsRead++;
        }
    }

    public bool[] ReadRemainingBits() {
        if (fileStream == null) {
            throw new InvalidOperationException("No file is open.");
        }

        const int bufferSize = 4096; // Adjust the buffer size as needed
        byte[] buffer = new byte[bufferSize];
        bool[] bits = new bool[bufferSize * 8];
        int bitsRead = 0;

        while (true) {
            int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

            if (bytesRead == 0) {
                break; // End of file
            }

            for (int i = 0; i < bytesRead; i++) {
                for (int j = 7; j >= 0; j--) {
                    bits[bitsRead] = ((buffer[i] >> j) & 1) == 1;
                    bitsRead++;

                    if (bitsRead >= bits.Length) {
                        return bits; // Return the bits if the array is full
                    }
                }
            }
        }

        // Trim the bits array to remove unused elements
        Array.Resize(ref bits, bitsRead);

        return bits;
    }

    public int ReadBitsAsInt(int numBits) {
        bool[] bits = ReadBits(numBits);
        int result = 0;

        foreach (bool bit in bits) {
            result = result * 2 + (bit ? 1 : 0);
        }

        return result;
    }

    public int GetLength() {
        return (int)fileStream.Length;
    }
}

