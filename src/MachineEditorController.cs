using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

sealed class MachineEditorController(TransitionDiagramRenderer diagramRenderer, WorkspaceController workspaceController) {
    readonly TransitionDiagramRenderer diagramRenderer = diagramRenderer;
    readonly WorkspaceController workspaceController = workspaceController;
    char? selectedState;
    (char State, char ReadSymbol)? selectedTransition;
    char? transitionSource;
    (char State, char ReadSymbol)? draggedSelfLoop;
    Vector2 selfLoopDragStart;

    public char? SelectedState => selectedState;
    public (char State, char ReadSymbol)? SelectedTransition => selectedTransition;
    public char? TransitionSource => transitionSource;

    public bool Update(Vector2 mousePosition, UserInterface.Layout layout, TuringMachine machine) {
        Rectangle workspace = layout.Workspace;
        if (!Raylib.CheckCollisionPointRec(mousePosition, workspace))
            return false;
        bool onFixedControl = Raylib.CheckCollisionPointRec(mousePosition, layout.PlayButton) ||
            Raylib.CheckCollisionPointRec(mousePosition, layout.ResetButton) ||
            Raylib.CheckCollisionPointRec(mousePosition, layout.SpeedSlider) ||
            mousePosition.Y < 70f;
        if (onFixedControl) {
            if (Raylib.IsMouseButtonReleased(MouseButton.Right))
                transitionSource = null;
            return false;
        }

        Vector2 worldPosition = Raylib.GetScreenToWorld2D(mousePosition, workspaceController.Camera);
        bool changed = false;

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && selectedTransition.HasValue) {
            (char State, char ReadSymbol)? loop = diagramRenderer.FindSelfLoopAtPosition(worldPosition);
            if (loop.HasValue && selectedTransition == loop) {
                draggedSelfLoop = loop;
                selfLoopDragStart = worldPosition;
            }
        }
        if (draggedSelfLoop.HasValue && Raylib.IsMouseButtonDown(MouseButton.Left)) {
            if (Vector2.DistanceSquared(worldPosition, selfLoopDragStart) > 9f)
                diagramRenderer.RotateSelfLoop(draggedSelfLoop.Value, worldPosition);
            return false;
        }
        if (draggedSelfLoop.HasValue && Raylib.IsMouseButtonReleased(MouseButton.Left)) {
            draggedSelfLoop = null;
            return false;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Right)) {
            transitionSource = diagramRenderer.FindNodeAtPosition(worldPosition, machine.States);
            if (transitionSource == TuringMachine.HaltState)
                transitionSource = null;
        }

        if (transitionSource.HasValue && Raylib.IsMouseButtonReleased(MouseButton.Right)) {
            char? target = diagramRenderer.FindNodeAtPosition(worldPosition, machine.States);
            if (target.HasValue) {
                char readSymbol = machine.TryGetTransition(transitionSource.Value, '0', out _) ? '1' : '0';

                machine.SetTransition(transitionSource.Value, readSymbol, new Transition(readSymbol, '0', MoveDirection.Right, target.Value));
                selectedTransition = (transitionSource.Value, readSymbol);
                selectedState = null;
                diagramRenderer.Refresh();
                changed = true;
            }
            transitionSource = null;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
            char? state = diagramRenderer.FindNodeAtPosition(worldPosition, machine.States);
            TransitionDiagramRenderer.TransitionHit? control = diagramRenderer.FindTransitionControlAtPosition(worldPosition);
            bool doubleClick = Raylib.GetTime() - lastClickTime < 0.3;
            lastClickTime = Raylib.GetTime();
            if (state.HasValue) {
                selectedState = state;
                selectedTransition = null;
            } else if (control.HasValue) {
                selectedState = null;
                selectedTransition = (control.Value.State, control.Value.ReadSymbol);
                changed |= ToggleTransition(machine, control.Value);
            } else {
                (char State, char ReadSymbol)? transition = diagramRenderer.FindTransitionAtPosition(worldPosition);
                selectedState = null;
                selectedTransition = transition;
                if (!transition.HasValue && doubleClick)
                    changed = AddStateAtPosition(machine, worldPosition);
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Delete) || Raylib.IsKeyPressed(KeyboardKey.Backspace)) {
            if (selectedTransition.HasValue) {
                machine.RemoveTransition(selectedTransition.Value.State, selectedTransition.Value.ReadSymbol);
                selectedTransition = null;
                diagramRenderer.Refresh();
                changed = true;
            } else if (selectedState.HasValue && machine.RemoveState(selectedState.Value)) {
                selectedState = null;
                diagramRenderer.RemapStatePositions(machine.LastStateRenames);
                diagramRenderer.Refresh();
                changed = true;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) {
            transitionSource = null;
            selectedState = null;
            selectedTransition = null;
        }

        return changed;
    }

    bool AddStateAtPosition(TuringMachine machine, Vector2 position) {
        char state = NextState(machine);
        if (state == TuringMachine.HaltState)
            return false;
        machine.AddState(state);
        diagramRenderer.Refresh();
        diagramRenderer.SetStatePosition(state, position);
        selectedState = state;
        return true;
    }

    bool ToggleTransition(TuringMachine machine, TransitionDiagramRenderer.TransitionHit hit) {
        if (!machine.TryGetTransition(hit.State, hit.ReadSymbol, out Transition? transition) || transition is null)
            return false;

        Transition updated = hit.Control switch {
            TransitionDiagramRenderer.TransitionControl.Read => transition with { ReadValue = transition.ReadValue == '0' ? '1' : '0' },
            TransitionDiagramRenderer.TransitionControl.Write => transition with { WriteValue = transition.WriteValue == '0' ? '1' : '0' },
            TransitionDiagramRenderer.TransitionControl.Direction => transition with {
                Direction = transition.Direction == MoveDirection.Left ? MoveDirection.Right : MoveDirection.Left
            },
            _ => transition
        };

        if (hit.Control == TransitionDiagramRenderer.TransitionControl.Read) {
            if (machine.TryGetTransition(hit.State, updated.ReadValue, out _)) {
                if (!machine.SwapTransitions(hit.State))
                    return false;
            } else {
                machine.RemoveTransition(hit.State, hit.ReadSymbol);
                machine.SetTransition(hit.State, updated.ReadValue, updated);
            }
            selectedTransition = (hit.State, updated.ReadValue);
        } else {
            machine.SetTransition(hit.State, hit.ReadSymbol, updated);
        }
        diagramRenderer.Refresh();
        return true;
    }

    double lastClickTime = -1d;

    static char NextState(TuringMachine machine) {
        for (char state = 'A'; state < TuringMachine.HaltState; state++)
            if (!machine.States.Contains(state))
                return state;
        return TuringMachine.HaltState;
    }
}
