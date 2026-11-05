using System.Diagnostics;
using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

class Program {
    public static void Main(string[] args) {
        bool debug = args.Length > 0 && string.Equals(args[0], "debug", StringComparison.OrdinalIgnoreCase);
        string[] machineArguments = debug ? args[1..] : args;
        if (debug) Console.WriteLine("== DEBUG MODE IS ENABLED! ==");
        Process process = Process.GetCurrentProcess();
        TimeSpan previousCpuTime = process.TotalProcessorTime;
        DateTime previousSampleTime = DateTime.UtcNow;

        TuringMachine machine = machineArguments.Length > 0 ? TuringMachine.Parse(string.Join(" ", machineArguments)) : new TuringMachine();
        TuringMachineRuntime runtime = new(machine);
        TransitionDiagramRenderer diagramRenderer = new(machine);
        WorkspaceController workspaceController = new(diagramRenderer);
        MachineEditorController editor = new(diagramRenderer, workspaceController);
        UserInterface userInterface = new();

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1920, 1080, "Turing Factory");
        Raylib.SetWindowPosition(0, 0);
        if (!Raylib.IsWindowReady())
            throw new InvalidOperationException("Raylib failed to initialize the window.");
        Raylib.SetTargetFPS(144);
        Theme.LoadFont();

        while (!Raylib.WindowShouldClose()) {
            if (debug)
                PrintDebugLog(process, previousCpuTime, previousSampleTime);
            float windowWidth = Raylib.GetScreenWidth();
            float windowHeight = Raylib.GetScreenHeight();
            Vector2 mousePosition = Raylib.GetMousePosition();
            UserInterface.Layout layout = userInterface.GetLayout(windowWidth, windowHeight);

            workspaceController.Update(mousePosition, layout.Workspace, machine.States);
            bool modelChanged = editor.Update(mousePosition, layout, machine);
            MachineValidationResult validation = machine.Validate();
            UserInterface.Actions actions = userInterface.Update(mousePosition, layout);
            runtime.Speed = userInterface.Speed;
            if (actions.Reset)
                runtime.Reset();
            if (modelChanged)
                runtime.Reset();
            if (actions.TogglePlay && validation.IsValid)
                runtime.TogglePlay();

            runtime.Update(Raylib.GetFrameTime());

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Theme.Background);
            workspaceController.Draw(machine.States, runtime.CurrentState, runtime.StepCount, runtime.PreviousState, runtime.ActiveTransition, runtime.TransitionProgress, editor, mousePosition);
            TapeRenderer.Draw(runtime.Tape, runtime.TapeHead, runtime.PreviousTapeHead, runtime.TransitionProgress, (int)windowWidth, (int)windowHeight);
            TapeHistoryRenderer.Draw(runtime.TapeHistory, machine.States, (int)windowWidth, (int)windowHeight);
            userInterface.Draw(machine.ToDefinitionString(), validation, runtime, layout);
            Raylib.EndDrawing();
        }

        Theme.UnloadFont();
        Raylib.CloseWindow();
    }

    private static void PrintDebugLog(Process process, TimeSpan previousCpuTime, DateTime previousSampleTime) {
        double elapsedMilliseconds = (DateTime.UtcNow - previousSampleTime).TotalMilliseconds;
        double cpuMilliseconds = (process.TotalProcessorTime - previousCpuTime).TotalMilliseconds;
        double cpuPercent = elapsedMilliseconds > 0 ? cpuMilliseconds / elapsedMilliseconds / Environment.ProcessorCount * 100 : 0;
        double memoryMegabytes = process.WorkingSet64 / 1024d / 1024d;

        Console.WriteLine($"FPS: {Raylib.GetFPS()} | " + $"CPU: {cpuPercent:F1}% | " + $"Memory: {memoryMegabytes:F1} MB");
    }
}
