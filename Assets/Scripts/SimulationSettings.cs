using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SimulationSettings
{
    public static int Instance = 0;
    public static int Height = 50;
    public static int Width = 50;
    public static int Depth = 50;
    public static int smoothingIterations = 20;
	public static bool useRandomSeed = true;
	public static int seed = 1; 
    public static int agentCount = 3;
    public static int algorithm = 0; // 0 = RBW, 1 = LVD, 2 = DSVP

    public static float duration = 30 * 60; // in minutes

    public static float progress = 0;
}
