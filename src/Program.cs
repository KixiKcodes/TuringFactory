using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

class Program {
    public static void Main(string[] args) {
        bool debug = args.Length > 0 && string.Equals(args[0], "debug", StringComparison.OrdinalIgnoreCase);
        string[] machineArguments = debug ? args[1..] : args;

        TuringMachine machine = machineArguments.Length > 0 ? TuringMachine.Parse(string.Join(" ", machineArguments)) : new TuringMachine();
        TuringMachineRuntime runtime = new(machine);
        TransitionDiagramRenderer diagramRenderer = new(machine);
        WorkspaceController workspaceController = new(diagramRenderer);
        MachineEditorController editor = new(diagramRenderer, workspaceController);
        UserInterface userInterface = new();

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1920, 1080, "Turing Factory");
        if (!Raylib.IsWindowReady())
            throw new InvalidOperationException("Raylib failed to initialize the window.");
        Raylib.SetTargetFPS(144);
        Theme.LoadFont();
        if (debug) Console.WriteLine("Init Finished");

        while (!Raylib.WindowShouldClose()) {
            if (debug) Console.WriteLine("Frame Start");
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

            if (debug) Console.WriteLine("Starting Frame Renders");
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Theme.Background);
            if (debug) Console.WriteLine("Starting Workspace Render");
            workspaceController.Draw(machine.States, runtime.CurrentState, runtime.StepCount, runtime.PreviousState, runtime.ActiveTransition, runtime.TransitionProgress, editor, mousePosition);
            if (debug) Console.WriteLine("Starting Tape Render");
            TapeRenderer.Draw(runtime.Tape, runtime.TapeHead, runtime.PreviousTapeHead, runtime.TransitionProgress, (int)windowWidth, (int)windowHeight);
            if (debug) Console.WriteLine("Starting History Render");
            TapeHistoryRenderer.Draw(runtime.TapeHistory, machine.States, (int)windowWidth, (int)windowHeight);
            if (debug) Console.WriteLine("Starting UI Render");
            userInterface.Draw(machine.ToDefinitionString(), validation, runtime, layout);
            Raylib.EndDrawing();
            if (debug) Console.WriteLine("Frame End");
        }

        Theme.UnloadFont();
        Raylib.CloseWindow();
    }
}
