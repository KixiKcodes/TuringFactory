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

using System.Numerics;
using Raylib_cs;
using Transition = TuringFactory.TuringMachine.Transition;

namespace TuringFactory;

sealed class WorkspaceController(TransitionDiagramRenderer diagramRenderer) {
    readonly TransitionDiagramRenderer diagramRenderer = diagramRenderer;
    Camera2D camera = new() {
        Target = Vector2.Zero,
        Offset = Vector2.Zero,
        Rotation = 0f,
        Zoom = 1f
    };

    char? draggedNode;
    Vector2 dragOffset;
    bool isPanning;
    Vector2 previousMousePosition;
    public Camera2D Camera => camera;

    public void Update(Vector2 mousePosition, Rectangle workspace, IReadOnlyCollection<char> states) {
        bool mouseInWorkspace = Raylib.CheckCollisionPointRec(mousePosition, workspace);

        UpdatePanning(mousePosition, mouseInWorkspace);
        UpdateZoom(mousePosition, mouseInWorkspace);
        UpdateNodeDragging(mousePosition, mouseInWorkspace, states);
    }

    public void Draw(IReadOnlyCollection<char> states, char currentState, int stepCount, char? previousState, Transition? activeTransition, float transitionProgress, MachineEditorController editor, Vector2 mousePosition) {
        Raylib.BeginMode2D(camera);
        diagramRenderer.Draw(states, currentState, stepCount, previousState, activeTransition, transitionProgress, editor.SelectedState, editor.SelectedTransition);
        if (editor.TransitionSource is char source) {
            Vector2 worldMousePosition = Raylib.GetScreenToWorld2D(mousePosition, camera);
            diagramRenderer.DrawLinkPreview(source, worldMousePosition);
        }
        Raylib.EndMode2D();
    }

    void UpdatePanning(Vector2 mousePosition, bool mouseInWorkspace) {
        if (Raylib.IsMouseButtonPressed(MouseButton.Middle) && mouseInWorkspace) {
            isPanning = true;
            previousMousePosition = mousePosition;
        }

        if (isPanning && Raylib.IsMouseButtonDown(MouseButton.Middle)) {
            camera.Target -= (mousePosition - previousMousePosition) / camera.Zoom;
            previousMousePosition = mousePosition;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Middle))
            isPanning = false;
    }

    void UpdateZoom(Vector2 mousePosition, bool mouseInWorkspace) {
        float wheelMovement = Raylib.GetMouseWheelMove();
        if (!mouseInWorkspace || MathF.Abs(wheelMovement) <= 0f)
            return;

        Vector2 worldPositionBeforeZoom = Raylib.GetScreenToWorld2D(mousePosition, camera);
        camera.Zoom = Math.Clamp(camera.Zoom + wheelMovement * 0.1f, 0.25f, 3f);
        Vector2 worldPositionAfterZoom = Raylib.GetScreenToWorld2D(mousePosition, camera);
        camera.Target += worldPositionBeforeZoom - worldPositionAfterZoom;
    }

    void UpdateNodeDragging(Vector2 mousePosition, bool mouseInWorkspace, IReadOnlyCollection<char> states) {
        Vector2 workspaceMousePosition = Raylib.GetScreenToWorld2D(mousePosition, camera);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && mouseInWorkspace) {
            draggedNode = diagramRenderer.FindNodeAtPosition(workspaceMousePosition, states);
            if (draggedNode.HasValue) {
                Vector2 nodePosition = draggedNode.Value == TuringMachine.HaltState ? diagramRenderer.HaltPosition : diagramRenderer.Positions[draggedNode.Value];
                dragOffset = nodePosition - workspaceMousePosition;
            }
        }

        if (draggedNode.HasValue && Raylib.IsMouseButtonDown(MouseButton.Left)) {
            Vector2 newPosition = workspaceMousePosition + dragOffset;
            if (draggedNode.Value == TuringMachine.HaltState)
                diagramRenderer.HaltPosition = newPosition;
            else
                diagramRenderer.Positions[draggedNode.Value] = newPosition;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            draggedNode = null;
    }
}
