using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MAES3D 
{
    public class Node 
    {
        // Coordinates of the node
        public int x { get; private set; }
        public int y { get; private set; }
        public int z { get; private set; }

        // GScore and FScore values for the A* algorithm
        public int GScore { get; set; }
        public float FScore { get; set; }

        // Parent node for tracing the path back to the start node
        public Node Parent { get; set; }

        // Constructor for creating a new node with given coordinates
        public Node(int x, int y, int z) 
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Override the Equals method to compare nodes by their coordinates
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Node otherNode = (Node)obj;
            return x == otherNode.x && y == otherNode.y && z == otherNode.z;
        }

        // Override the GetHashCode method to ensure the hash code is based on the node's coordinates
        public override int GetHashCode()
        {
            int hash = 17;

            hash = hash * 23 + x.GetHashCode();
            hash = hash * 23 + y.GetHashCode();
            hash = hash * 23 + z.GetHashCode();

            return hash;
        }

        // Define the equality operator for nodes based on their coordinates
        public static bool operator ==(Node node1, Node node2)
        {
            // If both nodes are null, or if they are the same instance, return true.
            if (ReferenceEquals(node1, node2))
            {
                return true;
            }

            // If one of the nodes is null, return false
            if (node1 is null || node2 is null)
            {
                return false;
            }

            // Return true if the fields match:
            return node1.x == node2.x && node1.y == node2.y && node1.z == node2.z;
        }

        // Define the inequality operator for nodes based on their coordinates
        public static bool operator !=(Node node1, Node node2)
        {
            return !(node1 == node2);
        }
    }
}