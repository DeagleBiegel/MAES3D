using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SimulationSettings
{
    //Advanced Settings
    public static int AStarIterations = 100;
    public static int UnexploredChunkSize = 32;
    public static int CommunicationRange = 50;
    public static int SensingFOV = 360;
    //Base Settings
    public static int mapGen = 0; //0 = RandomConnectedSpheres(), 1 = SmoothedNoise()
    public static int Instance = 0;
    public static int agentCount = 3;
    public static float duration = 30 * 60; // in minutes
    public static int algorithm = 0; // 0 = RBW, 1 = LVD, 2 = DSVP
    public static float progress = 0;
	public static bool useRandomSeed = true;
	public static int seed = 1; 

    //SmoothedNoise()
    public static int Height = 50;
    public static int Width = 50;
    public static int Depth = 50;
    public static int smoothingIterations = 20;
    public static int SN_initialFillRatio = 53;


    //RandomConnectedSpheres()
    public static int RCS_smoothingIterations = 2;
    public static int RCS_minRadius = 3;
    public static int RCS_maxRadius = 6;
    public static int RCS_connectionsToMake = 3;
    public static int RCS_ratioToClear = 20;
}
