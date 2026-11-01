using System.Numerics;
using Raylib_cs;

namespace TuringFactory;

class Program {
    public static void Main(string[] args) {
        const float transitionDuration = 0.1f;
        const float stepInterval = 0.1f;
        string definition = args.Length > 0 ? string.Join(" ", args) : "1RB1RD_1LC0RC_1RA1LD_0RE0LB_0RZ1RC"; // Skelet 1 (default).

        TuringMachine machine = TuringMachine.Parse(definition);
        TransitionDiagramRenderer diagramRenderer = new(machine);

        char currentState = 'A';
        char? previousState = null;

        bool isPlaying = false;
        float stepTimer = 0f;

        Transition? activeTransition = null;
        float transitionProgress = 1f;

        Dictionary<int, char> tape = [];
        int tapeHead = 0;
        int previousTapeHead = 0;

        char? draggedNode = null;
        Vector2 dragOffset = Vector2.Zero;

        Font font = Raylib.LoadFontEx("resources/Sono-Regular.ttf", 72, null, 0);

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1280, 720, "Turing Factory");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose()) {
            Vector2 mousePosition = Raylib.GetMousePosition();
            Rectangle playButton = new(20, 630, 110, 45);

            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                draggedNode = diagramRenderer.FindNodeAtPosition(mousePosition, machine.States);
                if (draggedNode.HasValue) {
                    Vector2 nodePosition = draggedNode.Value == TuringMachine.HaltState ? diagramRenderer.HaltPosition : diagramRenderer.Positions[draggedNode.Value];
                    dragOffset = nodePosition - mousePosition;
                }
            }

            if (draggedNode.HasValue && Raylib.IsMouseButtonDown(MouseButton.Left)) {
                Vector2 newPosition = mousePosition + dragOffset;

                if (draggedNode.Value == TuringMachine.HaltState)
                    diagramRenderer.HaltPosition = newPosition;
                else
                    diagramRenderer.Positions[draggedNode.Value] = newPosition;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                draggedNode = null;

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, playButton)) {
                if (currentState == TuringMachine.HaltState) {
                    tape.Clear();
                    tapeHead = 0;
                    previousTapeHead = 0;
                    currentState = machine.StartState;
                    activeTransition = null;
                    previousState = null;
                }
                isPlaying = !isPlaying;
            }

            if (isPlaying && currentState != TuringMachine.HaltState) {
                stepTimer += Raylib.GetFrameTime();
                if (stepTimer >= stepInterval && transitionProgress >= 1f) {
                    stepTimer = 0f;
                    previousTapeHead = tapeHead;
                    StepMachine(machine, tape, ref tapeHead, ref currentState, ref activeTransition, ref previousState);
                    transitionProgress = 0f;
                }
            }

            if (transitionProgress < 1f)
                transitionProgress = MathF.Min(transitionProgress + Raylib.GetFrameTime() / transitionDuration, 1f);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            Raylib.DrawRectangleRec(playButton, Color.LightGray);
            Raylib.DrawRectangleLinesEx(playButton, 2f, Color.DarkGray);

            string buttonText = isPlaying ? "Pause" : "Play";
            Vector2 buttonSize = Raylib.MeasureTextEx(font, buttonText, 24, 1);
            Raylib.DrawTextEx(font, buttonText, new Vector2(
                playButton.X + playButton.Width / 2f - buttonSize.X / 2f,
                playButton.Y + playButton.Height / 2f - buttonSize.Y / 2f
            ), 24, 1, Color.Black);

            diagramRenderer.Draw(machine.States, currentState, previousState, activeTransition, transitionProgress, font);

            DrawTape(tape, tapeHead, previousTapeHead, transitionProgress, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            Raylib.EndDrawing();
        }
        Raylib.UnloadFont(font);
        Raylib.CloseWindow();
    }

    static void StepMachine(TuringMachine machine, Dictionary<int, char> tape, ref int tapeHead, ref char currentState, ref Transition? activeTransition, ref char? previousState) {
        if (currentState == TuringMachine.HaltState)
            return;
        char readValue = tape.TryGetValue(tapeHead, out char value) ? value : '0';
        if (!machine.TryGetTransition(currentState, readValue, out Transition? transition) || transition is null)
            return;

        previousState = currentState;
        activeTransition = transition;
        if (transition.WriteValue == '1')
            tape[tapeHead] = '1';
        else
            tape.Remove(tapeHead);
        tapeHead += transition.Direction == MoveDirection.Right ? 1 : -1;
        currentState = transition.NextState;
    }

    static void DrawTape(Dictionary<int, char> tape, int tapeHead, int previousTapeHead, float transitionProgress, int windowWidth, int windowHeight) {
        const int cellSize = 55;
        const int visibleCells = 15;
        float viewWidth = visibleCells * cellSize;
        float viewX = (windowWidth - viewWidth) / 2f;
        float viewY = windowHeight - 100;
        Rectangle tapeView = new(viewX, viewY, viewWidth, cellSize);
        float movementOffset = (tapeHead - previousTapeHead) * cellSize * (1f - transitionProgress);

        Raylib.DrawRectangleRec(tapeView, Color.RayWhite);
        Raylib.BeginScissorMode((int)tapeView.X, (int)tapeView.Y, (int)tapeView.Width, (int)tapeView.Height);

        int firstCell = tapeHead - visibleCells / 2 - 1;
        int lastCell = tapeHead + visibleCells / 2 + 1;
        for (int tapePosition = firstCell; tapePosition <= lastCell; tapePosition++) {
            float x = viewX + (tapePosition - tapeHead + visibleCells / 2) * cellSize + movementOffset;
            bool isFilled = tape.TryGetValue(tapePosition, out char value) && value == '1';
            Rectangle cell = new(x, viewY, cellSize, cellSize);

            Raylib.DrawRectangleRec(cell, isFilled ? Color.Black : Color.RayWhite);
            Raylib.DrawRectangleLinesEx(cell, tapePosition == tapeHead ? 5f : 2f, tapePosition == tapeHead ? Color.Gold : Color.DarkGray);
        }

        Raylib.EndScissorMode();
        Raylib.DrawRectangleLinesEx(tapeView, 2f, Color.DarkGray);
    }
}
