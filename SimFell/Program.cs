using System.Collections.Concurrent;
using System.Diagnostics;
using SimFell.Engine.Base;
using SimFell.Reporting;
using SimFell.Logging;
using SimFell.SimmyRewrite;
using Spectre.Console;

namespace SimFell;

public class Program
{
    SimFellConfig config = new SimFellConfig();

    private void Setup()
    {
        // TODO: Load in the Constants File/Get Data from API.
    }

    private void LoadConfiguration(string configPath)
    {
        config = SimFellConfig.LoadFromFile(configPath);
    }

    private void RunSim()
    {
        if (config.SimType == SimFellConfig.SimulationType.Debug)
        {
            ConsoleLogger.SetLevel(SimulationLogLevel.All);
            SimRandom.EnableDeterminism(false);
            config.RunCount = 1;
        }
        else
        {
            ConsoleLogger.SetLevel(SimulationLogLevel.Minimal);
        }

        List<SimLoop> simLoops = new List<SimLoop>();


        var tasks = new Task[config.RunCount];
        var exceptions = new List<Exception>();
        var exceptionLock = new object();
        var stopwatch = Stopwatch.StartNew();

        //Run all
        for (int i = 0; i < config.RunCount; i++)
        {
            int threadId = i;

            SimLoop loop = new SimLoop();
            simLoops.Add(loop);

            // If we are in Debug mode, set the Console Logger Simloop for better printing.
            if (config.SimType == SimFellConfig.SimulationType.Debug) ConsoleLogger.simLoop = loop;
            else ConsoleLogger.simLoop = null;

            Unit player = config.GetHero();

            List<Unit> freshEnemies = new List<Unit>();
            for (int e = 0; e < config.Enemies; e++)
            {
                freshEnemies.Add(new Unit("Goblin: #" + e, true));
            }

            tasks[i] = Task.Run(() =>
            {
                try
                {
                    loop.Start(
                        player,
                        freshEnemies,
                        config.Duration);
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(new Exception($"Thread {threadId}: {ex.Message}", ex));
                    }
                }
            });
        }

        // Wait for all the tasks to finish before we do calculations.
        try
        {
            Task.WaitAll(tasks);
        }
        catch (AggregateException ae)
        {
            lock (exceptionLock)
            {
                exceptions.AddRange(ae.InnerExceptions);
            }
        }

        stopwatch.Stop();

        Console.WriteLine("Duration: " + stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"));
        Console.WriteLine("Itterations: " + config.RunCount.ToString("N0"));
        var results = new ResultsReporter(simLoops, config);
        results.Display();
    }

    public void ApplicationLoop()
    {
        bool exit = false;
        string directory = Directory.GetCurrentDirectory();

        while (!exit)
        {
            var fileMap = GetSimfellFiles(directory);
            var files = fileMap.Values.ToList();

            files.Add("Refresh File List");
            files.Add("Exit");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green].simfell[/] file")
                    .PageSize(10)
                    .AddChoices(files)
            );

            if (choice == "Exit")
            {
                exit = true;
            }
            else if (choice == "Refresh File List")
            {
                continue;
            }
            else
            {
                var selectedPath = fileMap
                    .FirstOrDefault(kv => kv.Value.Equals(choice, StringComparison.OrdinalIgnoreCase)).Key;
                LoadConfiguration(selectedPath);
                RunSim();
            }
        }
    }

    private Dictionary<string, string> GetSimfellFiles(string directory)
    {
        string configsPath = Path.Combine(directory, "Configs");
        var fileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(configsPath))
            return fileMap;

        foreach (var fullPath in Directory.GetFiles(configsPath, "*.simfell", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(fullPath);
            fileMap[fullPath] = fileName;
        }

        return fileMap;
    }

    public void CommandLineRun()
    {
        // TODO: If the command line has a configuration file. Handle it that way.
        // Do some shit I don't know.
        Console.WriteLine("This feature is not implemented yet.");
    }

    static void Main(string[] args)
    {
        var Program = new Program();
        Program.Setup(); //Setup the Constants.
        // TODO: Check to see if application is in Command Line Argument Mode If It is. Run That.
        // CommandLineRun();
        // Other wise, run the application loop.
        Program.ApplicationLoop();
    }
}