namespace TuringFactory;

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
    public IReadOnlyDictionary<char, char> LastStateRenames { get; private set; } = new Dictionary<char, char>();

    public TuringMachine() {
        AddState(StartState);
        SetTransition(StartState, '0', new Transition('0', '0', MoveDirection.Right, HaltState));
        SetTransition(StartState, '1', new Transition('1', '0', MoveDirection.Right, HaltState));
    }

    public static TuringMachine Parse(string input) {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Machine definition cannot be empty.", nameof(input));
        
        string[] stateBlocks = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        TuringMachine machine = new(false);
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

    private TuringMachine(bool createDefaultState) { }

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
        SetTransition(state, readSymbol, new Transition(readSymbol, writeSymbol, direction, nextState));
    }

    public void SetTransition(char state, char readSymbol, Transition transition) {
        if (readSymbol is not ('0' or '1'))
            throw new ArgumentException("Read symbol must be 0 or 1.", nameof(readSymbol));
        if (transition.WriteValue is not ('0' or '1'))
            throw new ArgumentException("Write symbol must be 0 or 1.", nameof(transition));
        if (transition.NextState != HaltState && !char.IsUpper(transition.NextState))
            throw new ArgumentException($"Invalid next state '{transition.NextState}'.", nameof(transition));
        AddState(state);
        _transitions[state][readSymbol] = transition with { ReadValue = readSymbol };
    }

    public void RemoveTransition(char state, char readSymbol) {
        if (_transitions.TryGetValue(state, out var stateTransitions))
            stateTransitions.Remove(readSymbol);
    }

    public bool RemoveState(char state) {
        if (state == StartState || state == HaltState || !_transitions.ContainsKey(state))
            return false;

        Dictionary<char, char> renames = [];
        List<char> remainingStates = _transitions.Keys.Where(existing => existing != state).Order().ToList();
        for (int i = 0; i < remainingStates.Count; i++) {
            char oldState = remainingStates[i];
            char newState = (char)('A' + i);
            if (oldState != newState)
                renames[oldState] = newState;
        }

        Dictionary<char, Dictionary<char, Transition>> renamedTransitions = [];
        foreach (char oldState in remainingStates) {
            char newState = renames.GetValueOrDefault(oldState, oldState);
            Dictionary<char, Transition> transitions = [];
            foreach (var pair in _transitions[oldState]) {
                if (pair.Value.NextState == state)
                    continue;
                char nextState = renames.GetValueOrDefault(pair.Value.NextState, pair.Value.NextState);
                transitions[pair.Key] = pair.Value with { NextState = nextState };
            }
            renamedTransitions[newState] = transitions;
        }

        _transitions.Clear();
        foreach (var pair in renamedTransitions)
            _transitions[pair.Key] = pair.Value;
        LastStateRenames = renames;
        return true;
    }

    public bool SwapTransitions(char state) {
        if (!_transitions.TryGetValue(state, out var transitions) ||
            !transitions.TryGetValue('0', out Transition? zero) ||
            !transitions.TryGetValue('1', out Transition? one))
            return false;

        transitions['0'] = one with { ReadValue = '0' };
        transitions['1'] = zero with { ReadValue = '1' };
        return true;
    }

    public MachineValidationResult Validate() {
        List<string> errors = [];
        if (!_transitions.ContainsKey(StartState))
            errors.Add("Start state A is missing.");

        foreach (var state in _transitions.Keys.Order())
            foreach (char readSymbol in new[] { '0', '1' })
                if (!_transitions[state].ContainsKey(readSymbol))
                    errors.Add($"State {state} is missing a transition for {readSymbol}.");

        foreach (var stateTransitions in _transitions.Values)
            foreach (Transition transition in stateTransitions.Values)
                if (transition.NextState != HaltState && !_transitions.ContainsKey(transition.NextState))
                    errors.Add($"Transition to missing state {transition.NextState}.");

        return new MachineValidationResult(errors.Count == 0, errors);
    }

    public string ToDefinitionString() {
        return string.Join("_", _transitions.Keys.Order().Select(state =>
            string.Concat(EncodeTransition(state, '0'), EncodeTransition(state, '1'))));
    }

    string EncodeTransition(char state, char readSymbol) {
        if (!TryGetTransition(state, readSymbol, out Transition? transition) || transition is null)
            return "???";
        return $"{transition.WriteValue}{(transition.Direction == MoveDirection.Left ? 'L' : 'R')}{transition.NextState}";
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

public sealed record MachineValidationResult(bool IsValid, IReadOnlyList<string> Errors);
