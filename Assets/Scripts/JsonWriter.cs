using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JsonWriter
{
    private string _fileName;

    public JsonWriter(GeneratedSettings settings, int discoverableCells) 
    {
        _fileName = $"{AlgorithmIndexToString(settings.algorithm)}_{settings.Width}x{settings.Height}x{settings.Depth}_{settings.agentCount}";

        InitFile(settings);
        InitTest(discoverableCells, settings.seed, true);
    }

    private void InitFile(GeneratedSettings settings) 
    {
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{_fileName}.json", false);

        string header = $"{{\n\t\"scenario\" : \n\t{{\n\t\t\"algorithm\" : \"{AlgorithmIndexToString(settings.algorithm)}\",\n"
                      + $"\t\t\"mapX\" : {settings.Width},\n"
                      + $"\t\t\"mapY\" : {settings.Height},\n"
                      + $"\t\t\"mapZ\" : {settings.Depth},\n"
                      + $"\t\t\"duration\" : {settings.duration},\n"
                      + $"\t\t\"agents\" : {settings.agentCount},";

        textWriter.WriteLine(header); 
        textWriter.Close();
    }

    public void InitTest(int discoverableCells, int seed, bool initialTest = false) 
    {
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{_fileName}.json", true);

        if (!initialTest) 
        {
            textWriter.Write($",\t\t\t\n\t\t\t{{\n\t\t\t\t\"seed\" : {seed},\n\t\t\t\t\"freeCells\" : {discoverableCells},\n\t\t\t\t\"data\" : \n\t\t\t\t[");
        }
        else 
        {
            textWriter.WriteLine($"\t\t\"simulations\": \n\t\t[\n\t\t\t{{\n\t\t\t\t\"seed\" : {seed},\n\t\t\t\t\"freeCells\" : {discoverableCells},\n\t\t\t\t\"data\" : \n\t\t\t\t[");
        }


        textWriter.Close();
    }

    public void AddData(int time, float progress) 
    {
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{_fileName}.json", true);

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

    public void EndFile() 
    {
        TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{_fileName}.json", true);

        textWriter.Write($"\n\t\t]\n\t}}\n}}");

        textWriter.Close();
    }

    /*

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

    */

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
