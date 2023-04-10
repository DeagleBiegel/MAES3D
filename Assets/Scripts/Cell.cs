using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

namespace MAES3D {
    public class Cell {
        public int x;
        public int y;
        public int z;

        public Vector3 toVector => new Vector3(x, y, z);
        public Vector3 middle => new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);

        public Cell(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override bool Equals(object other) {
            return Equals(other as Cell);
        }

        public virtual bool Equals(Cell other) {
            if (other == null) { return false; }
            if (object.ReferenceEquals(this, other)) { return true; }
            return this.x == other.x && this.y == other.y && this.z == other.z;
        }

        public static bool operator ==(Cell cell1, Cell cell2) {
            if (object.ReferenceEquals(cell1, cell2)) { return true; }
            if ((object)cell1 == null || (object)cell2 == null) { return false; }
            return cell1.x == cell2.x && cell1.y == cell2.y && cell1.z == cell2.z;
        }

        public static bool operator !=(Cell cell1, Cell cell2) {
            return !(cell1 == cell2);
        }

        public static Cell operator +(Cell cell1, Cell cell2) {
            return new Cell(cell1.x + cell2.x, cell1.y + cell2.y, cell1.z + cell2.z);
        }
        public static Cell operator -(Cell cell1, Cell cell2) {
            return new Cell(cell1.x - cell2.x, cell1.y - cell2.y, cell1.z - cell2.z);
        }

        public override string ToString() {
            return $"({x}, {y}, {z})";
        }

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }

        public static int ManhattanDistance(Cell a, Cell b) {
            int xDistance = Math.Abs(a.x - b.x);
            int yDistance = Math.Abs(a.y - b.y);
            int zDistance = Math.Abs(a.z - b.z);

            return xDistance + yDistance + zDistance;
        }

        public Vector3 ToCoordinate() {
            return new Vector3(x, y, z);
        }

        public void DrawCell(Color? c = null){
                Color color = c ?? Color.red;

                Debug.DrawLine(new Vector3(x+0.25f, y+0.25f, z+0.25f), new Vector3(x+0.75f, y+0.75f, z+0.75f), color, 5);
                Debug.DrawLine(new Vector3(x+0.75f, y+0.25f, z+0.25f), new Vector3(x+0.25f, y+0.75f, z+0.75f), color, 5);
                Debug.DrawLine(new Vector3(x+0.25f, y+0.25f, z+0.75f), new Vector3(x+0.75f, y+0.75f, z+0.25f), color, 5);
                Debug.DrawLine(new Vector3(x+0.75f, y+0.25f, z+0.75f), new Vector3(x+0.25f, y+0.75f, z+0.25f), color, 5);
        }

    }
}