using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JsonWriter
{
    public static void InitFile(int discoverableCells) 
    {
        string fileName = $"{SimulationSettings.Width}_{AlgorithmIndexToString(SimulationSettings.algorithm)}_{SimulationSettings.agentCount}";  
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{fileName}.json", false);

        string header = $"{{\n\t\"scenario\" : \n\t{{\n\t\t\"algorithm\" : \"{AlgorithmIndexToString(SimulationSettings.algorithm)}\",\n"
                      + $"\t\t\"mapX\" : {SimulationSettings.Width},\n"
                      + $"\t\t\"mapY\" : {SimulationSettings.Height},\n"
                      + $"\t\t\"mapZ\" : {SimulationSettings.Depth},\n"
                      + $"\t\t\"duration\" : {SimulationSettings.duration},\n"
                      + $"\t\t\"agents\" : {SimulationSettings.agentCount},\n"
                      + $"\t\t\"discoverableCells\" : {discoverableCells},";

        textWriter.WriteLine(header);
        
        textWriter.Close();

        InitTest(true);
    }

    public static void InitTest(bool initialTest = false) 
    {
        string fileName = $"{SimulationSettings.Width}_{AlgorithmIndexToString(SimulationSettings.algorithm)}_{SimulationSettings.agentCount}";  
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{fileName}.json", true);

        if (!initialTest) 
        {
           textWriter.Write($",\t\t\t\n\t\t\t{{\n\t\t\t\t\"seed\" : {SimulationSettings.seed},\n\t\t\t\t\"data\" : \n\t\t\t\t[");
        }
        else 
        {
            textWriter.WriteLine($"\t\t\"simulations\": \n\t\t[\n\t\t\t{{\n\t\t\t\t\"seed\" : {SimulationSettings.seed},\n\t\t\t\t\"data\" : \n\t\t\t\t[");
        }


        textWriter.Close();
    }

    public static void AddData(int time, float progress) 
    {
        string fileName = $"{SimulationSettings.Width}_{AlgorithmIndexToString(SimulationSettings.algorithm)}_{SimulationSettings.agentCount}";  
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{fileName}.json", true);

        textWriter.WriteLine($"\t\t\t\t\t{{");
        textWriter.WriteLine($"\t\t\t\t\t\t\"timestamp\" : {time},");
        textWriter.WriteLine($"\t\t\t\t\t\t\"progress\" : {progress}");

        if (time >= SimulationSettings.duration) 
        {
            textWriter.Write($"\t\t\t\t\t}}\n\t\t\t\t]\n\t\t\t}}");
        }
        else 
        {
            textWriter.WriteLine($"\t\t\t\t\t}},");
        }
        
        textWriter.Close();
    }

    public static void TerminateFile(GeneratedSettings settings)  
    {
        string fileName = $"{settings.Width}_{AlgorithmIndexToString(settings.algorithm)}_{settings.agentCount}";  
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{fileName}.json", true);

        textWriter.WriteLine($"----EOF---");

        textWriter.Close();
    }

    private static string AlgorithmIndexToString(int algorithmIndex) 
    {
        switch (algorithmIndex) 
        {
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
