using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;
using UnityEngine.Analytics;

public class JsonWriter2 {
    private string _fileName;
    private string dethele = "";

    public JsonWriter2(GeneratedSettings settings) {
        InitFile(settings);
    }

    public void InitFile(GeneratedSettings settings) {
        _fileName = $"{AlgorithmIndexToString(settings.algorithm)}_{settings.Width}x{settings.Height}x{settings.Depth}_{settings.agentCount}";

        dethele += $"{{\"scenario\" : {{\"algorithm\" : \"{AlgorithmIndexToString(settings.algorithm)}\","
                   + $"\"mapX\" : {settings.Width},"
                   + $"\"mapY\" : {settings.Height},"
                   + $"\"mapZ\" : {settings.Depth},"
                   + $"\"duration\" : {settings.duration},"
                   + $"\"agents\" : {settings.agentCount},"
                   + $"\"simulations\" : [";

    }
    public void InitTest(int discoverableCells, int seed) {
        dethele += $"{{ \"seed\" : {seed}, " +
                      $"\"freeCells\" : {discoverableCells}, " +
                      $"\"data\" : [";
    }

    public void AddData(int time, float progress, bool shouldEnd) {
        dethele += $"{{ \"timestamp\" : {time}, " +
                      $"\"progress\" : {progress} }},";

        if (shouldEnd) {
            Debug.Log("Ending");
            dethele = dethele.Remove(dethele.Length - 1, 1);
            dethele += "]},";
        }
        else {
            Debug.Log("Not ending");
        }
    }

    public void EndFile() {
        dethele = dethele.Remove(dethele.Length - 1, 1);
        dethele += "]}}";

        Debug.Log($"test {dethele.Length}");

        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{_fileName}.json", false);
        textWriter.Write(dethele);
        textWriter.Close();
    }


    private string AlgorithmIndexToString(int algorithmIndex) {
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
