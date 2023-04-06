using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SimulationSettings
{
    public static int Instance = 0;
    public static int Height = 100;
    public static int Width = 100;
    public static int Depth = 100;
    public static int smoothingIterations = 20;
	public static bool useRandomSeed = false;
	public static int seed = 1; 
    public static int agentCount = 1;
    public static int algorithm = 1; // 0 = RBW, 1 = LVD

    public static float duration = 30 * 60; // in minutes

    public static float progress = 0;
}
