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
        TransitionDiagramRenderer diagramRenderer = new(machine);
        WorkspaceController workspaceController = new(diagramRenderer);
        MachineEditorController editor = new(diagramRenderer, workspaceController);
        const SoundMode soundMode = SoundMode.Notes;
        UserInterface userInterface = new(soundMode);

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1920, 1080, "Turing Factory");
        Raylib.SetWindowPosition(0, 0);
        if (!Raylib.IsWindowReady())
            throw new InvalidOperationException("Raylib failed to initialize the window.");
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(144);
        Raylib.InitAudioDevice();
        SoundManager soundManager = new(soundMode);
        soundManager.RegisterStates(machine.States);
        TuringMachineRuntime runtime = new(machine, soundManager);
        Theme.LoadFont();

        while (!Raylib.WindowShouldClose()) {
            if (debug)
                PrintDebugLog(process, previousCpuTime, previousSampleTime);
            float windowWidth = Raylib.GetScreenWidth();
            float windowHeight = Raylib.GetScreenHeight();
            Vector2 mousePosition = Raylib.GetMousePosition();
            UserInterface.Layout layout = userInterface.GetLayout(windowWidth, windowHeight);

            string definition = machine.ToDefinitionString();
            UserInterface.Actions actions = userInterface.Update(mousePosition, layout, definition);
            bool modelChanged = false;
            if (!userInterface.IsEditingDefinition) {
                workspaceController.Update(mousePosition, layout.Workspace, machine.States);
                modelChanged = editor.Update(mousePosition, layout, machine);
            }
            bool definitionChanged = false;
            if (actions.DefinitionSubmission is string submittedDefinition) {
                try {
                    machine.ReplaceWith(TuringMachine.Parse(submittedDefinition));
                    diagramRenderer.Refresh();
                    definitionChanged = true;
                } catch (Exception error) when (error is ArgumentException or FormatException) {
                    userInterface.RejectDefinition(error.Message);
                }
            }
            soundManager.RegisterStates(machine.States);
            soundManager.Volume = userInterface.Volume;
            if (actions.SoundModeChanged) {
                soundManager.SetMode(userInterface.SoundMode);
                soundManager.RegisterStates(machine.States);
            }
            MachineValidationResult validation = machine.Validate();
            runtime.Speed = userInterface.Speed;
            if (actions.Reset)
                runtime.Reset();
            if (modelChanged || definitionChanged)
                runtime.Reset();
            if (actions.TogglePlay && validation.IsValid)
                runtime.TogglePlay();

            runtime.Update(Raylib.GetFrameTime());

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Theme.Background);
            workspaceController.Draw(machine.States, runtime.CurrentState, runtime.StepCount, runtime.PreviousState, runtime.ActiveTransition, runtime.TransitionProgress, editor, mousePosition);
            TapeRenderer.Draw(runtime.Tape, runtime.TapeHead, runtime.PreviousTapeHead, runtime.TransitionProgress, layout);
            TapeHistoryRenderer.Draw(runtime.TapeHistory, machine.States, layout);
            userInterface.Draw(machine.ToDefinitionString(), validation, runtime, layout);
            Raylib.EndDrawing();
        }

        Theme.UnloadFont();
        soundManager.Dispose();
        Raylib.CloseWindow();
        Raylib.CloseAudioDevice();
    }

    private static void PrintDebugLog(Process process, TimeSpan previousCpuTime, DateTime previousSampleTime) {
        double elapsedMilliseconds = (DateTime.UtcNow - previousSampleTime).TotalMilliseconds;
        double cpuMilliseconds = (process.TotalProcessorTime - previousCpuTime).TotalMilliseconds;
        double cpuPercent = elapsedMilliseconds > 0 ? cpuMilliseconds / elapsedMilliseconds / Environment.ProcessorCount * 100 : 0;
        double memoryMegabytes = process.WorkingSet64 / 1024d / 1024d;
        string log = $"FPS: {Raylib.GetFPS()} | " + $"CPU: {cpuPercent:F1}% | " + $"Memory: {memoryMegabytes:F1} MB";

        Raylib.DrawTextEx(Theme.Font, log, new Vector2(0f, 0f), 24, 1, Theme.ElementLight);
    }
}
