using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

public static class StatWriter {
    // Start is called before the first frame update

    public static void InitializeStatFile(int instance) {
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/test{instance}.csv", false);
        textWriter.WriteLine($"Algorithm,{(SimulationSettings.algorithm)}");
        textWriter.WriteLine($"MapSize,x={SimulationSettings.Width},y={SimulationSettings.Height},z={SimulationSettings.Depth}");
        textWriter.WriteLine($"AgentAmount,{SimulationSettings.agentCount}");
        textWriter.WriteLine($"Duration(min),{SimulationSettings.duration / 60}");
        textWriter.WriteLine($"Seed,{SimulationSettings.seed}");
        textWriter.WriteLine("Timestamp(s),Progress(%),");
        textWriter.WriteLine("0, 0");
        textWriter.Close();
    }

    public static void AddResults(int instance, float time, float progress) {
        TextWriter tw = new StreamWriter(Application.dataPath + $"/Results/test{instance}.csv", true);
        tw.WriteLine($"{(int)time},{progress}");
        tw.Close();
    }

    private static string AlgorithmIndexToString(int algorithmIndex) {
        switch (algorithmIndex) {
            case 0:
                return "RBW";
            case 1:
                return "LVD";
            case 2:
                return "DSVP";
            default:
                return "No AlgorithmIndex Given";
        }
    }
}
