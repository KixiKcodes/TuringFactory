namespace BusyBeaver;

public enum MoveDirection {
    Left,
    Right
}

public sealed record Transition(
    char ReadValue,
    char WriteValue,
    MoveDirection Direction,
    char NextState
);

public sealed class TuringMachine {
    public const char HaltState = 'Z';
    public char StartState { get; } = 'A';

    private readonly Dictionary<char, Dictionary<char, Transition>> _transitions = [];
    public IReadOnlyCollection<char> States => _transitions.Keys;
    public IReadOnlyDictionary<char, Dictionary<char, Transition>> Transitions => _transitions;

    private TuringMachine() { }

    public static TuringMachine Parse(string input) {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Machine definition cannot be empty.", nameof(input));
        
        string[] stateBlocks = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        TuringMachine machine = new();
        for (int i = 0; i < stateBlocks.Length; i++) {
            char state = (char)('A' + i);
            machine.AddState(state);
            string block = stateBlocks[i];
            if (block.Length != 6)
                throw new FormatException($"State {state} must contain exactly two transitions.");
            machine.AddTransition(state, '0', block[..3]);
            machine.AddTransition(state, '1', block.Substring(3, 3));
        }

        return machine;
    }

    public void AddState(char state) {
        if (!_transitions.ContainsKey(state))
            _transitions[state] = [];
    }

    public void AddTransition(char state, char readSymbol, string encodedTransition) {
        if (encodedTransition.Length != 3)
            throw new FormatException($"Transition '{encodedTransition}' must be 3 characters.");
        AddState(state);

        var writeSymbol = encodedTransition[0];
        var direction = encodedTransition[1] switch {
            'L' => MoveDirection.Left,
            'R' => MoveDirection.Right,
            _ => throw new FormatException($"Invalid direction '{encodedTransition[1]}' in transition '{encodedTransition}'.")
        };

        var nextState = encodedTransition[2];
        if (nextState != HaltState && !char.IsUpper(nextState))
            throw new FormatException($"Invalid next state '{nextState}'.");
        _transitions[state][readSymbol] = new Transition(readSymbol, writeSymbol, direction, nextState);
    }

    public bool TryGetTransition(char state, char readSymbol, out Transition? transition) {
        if (_transitions.TryGetValue(state, out var stateTransitions) && stateTransitions.TryGetValue(readSymbol, out var t)) {
            transition = t;
            return true;
        }
        transition = null;
        return false;
    }
}
