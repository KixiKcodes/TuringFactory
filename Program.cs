using System.Numerics;
using Raylib_cs;

namespace TuringFactory;

class Program {
    public static void Main(string[] args) {
        const float slowestStepDuration = 0.4f;
        const float fastestStepDuration = 0.01f;
        float speed = 0.5f;
        string definition = args.Length > 0 ? string.Join(" ", args) : "1RB1RD_1LC0RC_1RA1LD_0RE0LB_0RZ1RC"; // Skelet 1 (default).

        TuringMachine machine = TuringMachine.Parse(definition);
        TransitionDiagramRenderer diagramRenderer = new(machine);

        char currentState = 'A';
        char? previousState = null;
        bool isPlaying = false;
        float stepTimer = 0f;
        int stepCount = 0;

        Transition? activeTransition = null;
        float transitionProgress = 1f;

        Dictionary<int, char> tape = [];
        Dictionary<int, char> tapeFillStates = [];
        int tapeHead = 0;
        int previousTapeHead = 0;
        List<TapeHistoryRenderer.TapeHistorySnapshot> tapeHistory = [new(new Dictionary<int, char>(tape), new Dictionary<int, char>(tapeFillStates), tapeHead)];

        char? draggedNode = null;
        Vector2 dragOffset = Vector2.Zero;
        bool draggingSlider = false;

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1920, 1080, "Turing Factory");
        Raylib.SetTargetFPS(120);
        Font font = Raylib.LoadFontEx("resources/Sono-Regular.ttf", 72, null, 0);

        while (!Raylib.WindowShouldClose()) {
            const float uiPadding = 20f;
            const float buttonHeight = 45f;
            const float buttonWidth = 110f;
            const float buttonGap = 10f;
            const float sliderWidth = 300f;
            const float sliderHeight = 8f;

            Vector2 mousePosition = Raylib.GetMousePosition();
            float windowWidth = Raylib.GetScreenWidth();
            float windowHeight = Raylib.GetScreenHeight();
            float buttonY = windowHeight - uiPadding - buttonHeight;
            Rectangle playButton = new(uiPadding, buttonY, buttonWidth, buttonHeight);
            Rectangle resetButton = new(playButton.X + playButton.Width + buttonGap, buttonY, buttonWidth, buttonHeight);
            Rectangle speedSlider = new(uiPadding, buttonY - 25f, sliderWidth, sliderHeight);
            float stepDuration = slowestStepDuration - speed * (slowestStepDuration - fastestStepDuration);

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

            float sliderHandleX = speedSlider.X + speed * speedSlider.Width;
            Vector2 sliderHandlePosition = new(sliderHandleX, speedSlider.Y + speedSlider.Height / 2f);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
                Vector2.Distance(mousePosition, sliderHandlePosition) <= 12f) {
                draggingSlider = true;
            }
            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                draggingSlider = false;
            if (draggingSlider)
                speed = Math.Clamp((mousePosition.X - speedSlider.X) / speedSlider.Width, 0f, 1f);

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, playButton)) {
                if (currentState == TuringMachine.HaltState) {
                    tape.Clear();
                    tapeFillStates.Clear();
                    tapeHead = 0;
                    previousTapeHead = 0;
                    tapeHistory.Clear();
                    tapeHistory.Add(new(new Dictionary<int, char>(tape), new Dictionary<int, char>(tapeFillStates), tapeHead));
                    currentState = machine.StartState;
                    activeTransition = null;
                    previousState = null;
                }
                isPlaying = !isPlaying;
            }

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
                Raylib.CheckCollisionPointRec(mousePosition, resetButton)) {
                tape.Clear();
                tapeFillStates.Clear();
                tapeHead = 0;
                previousTapeHead = 0;
                tapeHistory.Clear();
                tapeHistory.Add(new(new Dictionary<int, char>(tape), new Dictionary<int, char>(tapeFillStates), tapeHead));
                currentState = machine.StartState;
                previousState = null;
                activeTransition = null;
                transitionProgress = 1f;
                stepTimer = 0f;
                stepCount = 0;
                isPlaying = false;
            }

            if (isPlaying && currentState != TuringMachine.HaltState) {
                stepTimer += Raylib.GetFrameTime();
                if (stepTimer >= stepDuration && transitionProgress >= 1f) {
                    stepTimer = 0f;
                    previousTapeHead = tapeHead;
                    StepMachine(machine, tape, tapeFillStates, ref tapeHead, ref currentState, ref activeTransition, ref previousState);
                    stepCount++;
                    tapeHistory.Add(new(new Dictionary<int, char>(tape), new Dictionary<int, char>(tapeFillStates), tapeHead));
                    transitionProgress = 0f;
                }
            }

            if (transitionProgress < 1f)
                transitionProgress = MathF.Min(transitionProgress + Raylib.GetFrameTime() / stepDuration, 1f);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            Vector2 titleSize = Raylib.MeasureTextEx(font, definition, 28, 1);
            Raylib.DrawTextEx(font, definition, new Vector2(windowWidth / 2f - titleSize.X / 2f, uiPadding), 28, 1, Color.Black);

            string stepText = $"Steps: {stepCount}";
            Raylib.DrawTextEx(font, stepText, new Vector2(uiPadding, uiPadding), 24, 1, Color.Black);

            Raylib.DrawRectangleRec(playButton, Color.LightGray);
            Raylib.DrawRectangleLinesEx(playButton, 2f, Color.DarkGray);
            Raylib.DrawRectangleRec(resetButton, Color.LightGray);
            Raylib.DrawRectangleLinesEx(resetButton, 2f, Color.DarkGray);

            string buttonText = isPlaying ? "Pause" : "Play";
            Vector2 buttonSize = Raylib.MeasureTextEx(font, buttonText, 24, 1);
            Vector2 buttonPosition = new(playButton.X + playButton.Width / 2f - buttonSize.X / 2f, playButton.Y + playButton.Height / 2f - buttonSize.Y / 2f);
            Raylib.DrawTextEx(font, buttonText, buttonPosition, 24, 1, Color.Black);

            Vector2 resetTextSize = Raylib.MeasureTextEx(font, "Reset", 24, 1);
            Raylib.DrawTextEx(font,"Reset", new Vector2(resetButton.X + resetButton.Width / 2f - resetTextSize.X / 2f, resetButton.Y + resetButton.Height / 2f - resetTextSize.Y / 2f), 24, 1, Color.Black);

            Raylib.DrawRectangleRec(speedSlider, Color.LightGray);
            Raylib.DrawRectangleLinesEx(speedSlider, 2f, Color.DarkGray);
            float sliderX = speedSlider.X + speed * speedSlider.Width;
            Raylib.DrawCircle((int)sliderX, (int)(speedSlider.Y + speedSlider.Height / 2f), 9, Color.DarkGray);

            diagramRenderer.Draw(machine.States, currentState, previousState, activeTransition, transitionProgress, font);
            DrawTape(tape, tapeHead, previousTapeHead, transitionProgress, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            TapeHistoryRenderer.Draw(tapeHistory, machine.States, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

            Raylib.EndDrawing();
        }
        Raylib.UnloadFont(font);
        Raylib.CloseWindow();
    }

    static void StepMachine(TuringMachine machine, Dictionary<int, char> tape, Dictionary<int, char> tapeFillStates, ref int tapeHead, ref char currentState, ref Transition? activeTransition, ref char? previousState) {
        if (currentState == TuringMachine.HaltState)
            return;
        char readValue = tape.TryGetValue(tapeHead, out char value) ? value : '0';
        if (!machine.TryGetTransition(currentState, readValue, out Transition? transition) || transition is null)
            return;

        previousState = currentState;
        activeTransition = transition;
        if (transition.WriteValue == '1') {
            tape[tapeHead] = '1';
            tapeFillStates[tapeHead] = currentState;
        } else {
            tape.Remove(tapeHead);
            tapeFillStates.Remove(tapeHead);
        }
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
