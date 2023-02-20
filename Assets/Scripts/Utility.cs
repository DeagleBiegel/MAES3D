using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MAES3D {
    public static class Utility {
        public static Cell CoordinateToCell(Vector3 coordinate) {
            return new Cell(
                (int) Mathf.Floor(coordinate.x),
                (int) Mathf.Floor(coordinate.y),
                (int) Mathf.Floor(coordinate.z)
            );
        }
    }
}
