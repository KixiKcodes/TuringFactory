namespace TuringFactory.src;

sealed class TuringMachineRuntime {
    const float slowestStepDuration = 1.0f;
    const float fastestStepDuration = 0.01f;

    readonly TuringMachine machine;
    readonly SoundManager soundManager;
    readonly Dictionary<int, char> tape = [];
    readonly Dictionary<int, char> tapeFillStates = [];
    readonly List<TapeHistoryRenderer.TapeHistorySnapshot> tapeHistory = [];

    float stepTimer;
    float speed = 0.5f;

    public TuringMachine Machine => machine;
    public IReadOnlyDictionary<int, char> Tape => tape;
    public IReadOnlyList<TapeHistoryRenderer.TapeHistorySnapshot> TapeHistory => tapeHistory;
    public int TapeHead { get; private set; }
    public int PreviousTapeHead { get; private set; }
    public char CurrentState { get; private set; }
    public char? PreviousState { get; private set; }
    public Transition? ActiveTransition { get; private set; }
    public float TransitionProgress { get; private set; } = 1f;
    public int StepCount { get; private set; }
    public bool IsPlaying { get; private set; }
    public float Speed { get => speed; set => speed = Math.Clamp(value, 0f, 1f); }

    public TuringMachineRuntime(TuringMachine machine, SoundManager soundManager) {
        this.machine = machine;
        this.soundManager = soundManager;
        Reset();
    }

    public void Update(float deltaTime) {
        float stepDuration = GetStepDuration();

        if (IsPlaying && CurrentState != TuringMachine.HaltState) {
            stepTimer += deltaTime;
            if (stepTimer >= stepDuration && TransitionProgress >= 1f) {
                stepTimer = 0f;
                PreviousTapeHead = TapeHead;
                StepMachine();
                StepCount++;
                AddHistorySnapshot();
                TransitionProgress = 0f;
            }
        }

        float previousTransitionProgress = TransitionProgress;
        if (TransitionProgress < 1f) {
            TransitionProgress = MathF.Min(TransitionProgress + deltaTime / stepDuration, 1f);
            if (previousTransitionProgress < 1f && TransitionProgress >= 1f)
                soundManager.PlayStateSound(CurrentState);
        }
    }

    public void TogglePlay() {
        if (CurrentState == TuringMachine.HaltState)
            Reset();
        IsPlaying = !IsPlaying;
    }

    public void Reset() {
        tape.Clear();
        tapeFillStates.Clear();
        tapeHistory.Clear();
        TapeHead = 0;
        PreviousTapeHead = 0;
        CurrentState = machine.StartState;
        PreviousState = null;
        ActiveTransition = null;
        TransitionProgress = 1f;
        StepCount = 0;
        stepTimer = 0f;
        IsPlaying = false;
        AddHistorySnapshot();
    }

    float GetStepDuration() {
        float minSpeed = 1f / slowestStepDuration;
        float maxSpeed = 1f / fastestStepDuration;
        float stepsPerSecond = minSpeed * MathF.Pow(maxSpeed / minSpeed, Speed);

        return 1f / stepsPerSecond;
    }

    void AddHistorySnapshot() {
        tapeHistory.Add(new(new Dictionary<int, char>(tape), new Dictionary<int, char>(tapeFillStates), TapeHead));
    }

    void StepMachine() {
        if (CurrentState == TuringMachine.HaltState)
            return;

        char readValue = tape.TryGetValue(TapeHead, out char value) ? value : '0';
        if (!machine.TryGetTransition(CurrentState, readValue, out Transition? transition) || transition is null)
            return;

        PreviousState = CurrentState;
        ActiveTransition = transition;
        if (transition.WriteValue == '1') {
            tape[TapeHead] = '1';
            tapeFillStates[TapeHead] = CurrentState;
        } else {
            tape[TapeHead] = '0';
            tapeFillStates[TapeHead] = CurrentState;
        }

        TapeHead += transition.Direction == MoveDirection.Right ? 1 : -1;
        CurrentState = transition.NextState;
    }
}
