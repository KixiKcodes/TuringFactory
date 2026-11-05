using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

class Program {
    public static void Main(string[] args) {
        TuringMachine machine = args.Length > 0 ? TuringMachine.Parse(string.Join(" ", args)) : new TuringMachine();
        TuringMachineRuntime runtime = new(machine);
        TransitionDiagramRenderer diagramRenderer = new(machine);
        WorkspaceController workspaceController = new(diagramRenderer);
        MachineEditorController editor = new(diagramRenderer, workspaceController);
        UserInterface userInterface = new();

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1920, 1080, "Turing Factory");
        Raylib.SetTargetFPS(144);
        Theme.LoadFont();

        while (!Raylib.WindowShouldClose()) {
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
}
