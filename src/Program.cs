/*
Turing Factory
Copyright (C) 2026 KixiK

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using MachineValidationResult = TuringFactory.TuringMachine.MachineValidationResult;

namespace TuringFactory;

class Program {
    public static void Main(string[] args) {
        // Debug mode flag check 
        bool debug = args.Length > 0 && string.Equals(args[0], "debug", StringComparison.OrdinalIgnoreCase);
        string[] machineArguments = debug ? args[1..] : args;
        if (debug) Console.WriteLine("== DEBUG MODE IS ENABLED! ==");
        Process process = Process.GetCurrentProcess();
        TimeSpan previousCpuTime = process.TotalProcessorTime;
        DateTime previousSampleTime = DateTime.UtcNow;

        // Machine class initialization and setting up other classes
        TuringMachine machine = machineArguments.Length > 0 ? TuringMachine.Parse(string.Join(" ", machineArguments)) : new TuringMachine();
        TransitionDiagramRenderer diagramRenderer = new(machine);
        WorkspaceController workspaceController = new(diagramRenderer);
        MachineEditorController editor = new(diagramRenderer, workspaceController);
        const SoundMode soundMode = SoundMode.Notes;
        UserInterface userInterface = new(soundMode);

        // Setup raylib window and audio and font
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1920, 1080, "Turing Factory");
        Raylib.SetWindowPosition(0, 0);
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(144);
        Raylib.InitAudioDevice();
        SoundManager soundManager = new(soundMode);
        TuringMachineRuntime runtime = new(machine, soundManager);
        Theme.LoadFont();

        // Main loop
        while (!Raylib.WindowShouldClose()) {
            if (debug)
                PrintDebugLog(process, previousCpuTime, previousSampleTime);
            float windowWidth = Raylib.GetScreenWidth();
            float windowHeight = Raylib.GetScreenHeight();
            Vector2 mousePosition = Raylib.GetMousePosition();
            UserInterface.Layout layout = userInterface.GetLayout(windowWidth, windowHeight);

            // Handle user interactions
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
            soundManager.Volume = userInterface.Volume;
            if (actions.SoundModeChanged)
                soundManager.SetMode(userInterface.SoundMode);
            MachineValidationResult validation = machine.Validate();
            runtime.Speed = userInterface.Speed;
            if (actions.Reset)
                runtime.Reset();
            if (modelChanged || definitionChanged)
                runtime.Reset();
            if (actions.TogglePlay && validation.IsValid)
                runtime.TogglePlay();

            runtime.Update(Raylib.GetFrameTime()); // update frame

            // Render visualizers and user interface
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

    // Debug logs shown in the upper left corner of the screen. These may or may not be useful.
    // I used them mostly for the FPS, which I noticed usually drops after around 2000 steps, but this can vary depending on the machine and tape history.
    private static void PrintDebugLog(Process process, TimeSpan previousCpuTime, DateTime previousSampleTime) {
        double elapsedMilliseconds = (DateTime.UtcNow - previousSampleTime).TotalMilliseconds;
        double cpuMilliseconds = (process.TotalProcessorTime - previousCpuTime).TotalMilliseconds;
        double cpuPercent = elapsedMilliseconds > 0 ? cpuMilliseconds / elapsedMilliseconds / Environment.ProcessorCount * 100 : 0;
        double memoryMegabytes = process.WorkingSet64 / 1024d / 1024d;
        string log = $"FPS: {Raylib.GetFPS()} | " + $"CPU: {cpuPercent:F1}% | " + $"Memory: {memoryMegabytes:F1} MB";

        Raylib.DrawTextEx(Theme.Font, log, new Vector2(0f, 0f), 24, 1, Theme.ElementLight);
    }
}
