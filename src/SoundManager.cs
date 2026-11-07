using Raylib_cs;

namespace TuringFactory.src;

public enum SoundMode {
    Drums,
    Notes
}

public sealed class SoundManager : IDisposable {
    static readonly string SoundsPath = Path.Combine(AppContext.BaseDirectory, "resources", "sounds");
    readonly Dictionary<char, Sound> stateSounds = [];
    readonly Dictionary<char, string> assignedNoteFiles = [];
    readonly List<string> availableNoteFiles = [.. noteFiles];
    SoundMode mode;
    float volume = 1f;

    public SoundMode Mode => mode;
    public float Volume {
        get => volume;
        set {
            volume = Math.Clamp(value, 0f, 1f);
            foreach (Sound sound in stateSounds.Values)
                Raylib.SetSoundVolume(sound, volume);
        }
    }

    static readonly string[] noteFiles = [
        "C3.wav", "D3.wav", "D#3.wav", "F3.wav", "G3.wav", "A#3.wav",
        "C4.wav", "D4.wav", "D#4.wav", "F4.wav", "G4.wav", "A#4.wav",
        "C5.wav", "D5.wav", "D#5.wav", "F5.wav", "G5.wav", "A#5.wav",
        "C6.wav", "D6.wav", "D#6.wav", "F6.wav", "G6.wav", "A#6.wav",
        "C7.wav"
    ];

    public SoundManager(SoundMode mode = SoundMode.Drums) {
        SetMode(mode);
    }

    public void PlayStateSound(char state) {
        if (stateSounds.TryGetValue(state, out Sound sound))
            Raylib.PlaySound(sound);
    }

    public void SetMode(SoundMode newMode) {
        if (mode == newMode && stateSounds.Count > 0)
            return;
        UnloadSounds();
        mode = newMode;
        assignedNoteFiles.Clear();
        availableNoteFiles.Clear();
        availableNoteFiles.AddRange(noteFiles);
        if (mode == SoundMode.Drums)
            LoadDrumSounds();
        else
            LoadStateSound(TuringMachine.HaltState, Path.Combine("notes", "finish.wav"));
    }

    public void Dispose() {
        UnloadSounds();
    }

    void UnloadSounds() {
        foreach (Sound sound in stateSounds.Values)
            Raylib.UnloadSound(sound);
        stateSounds.Clear();
    }

    public void RegisterStates(IEnumerable<char> states) {
        if (mode != SoundMode.Notes)
            return;

        HashSet<char> activeStates = [.. states.Where(state => state != TuringMachine.HaltState)];
        foreach (char state in assignedNoteFiles.Keys.Where(state => !activeStates.Contains(state)).ToList()) {
            Raylib.UnloadSound(stateSounds[state]);
            stateSounds.Remove(state);
            availableNoteFiles.Add(assignedNoteFiles[state]);
            assignedNoteFiles.Remove(state);
        }

        foreach (char state in activeStates.Order()) {
            if (assignedNoteFiles.ContainsKey(state) || availableNoteFiles.Count == 0)
                continue;
            int noteIndex = Random.Shared.Next(availableNoteFiles.Count);
            string noteFile = availableNoteFiles[noteIndex];
            availableNoteFiles.RemoveAt(noteIndex);
            assignedNoteFiles[state] = noteFile;
            LoadStateSound(state, Path.Combine("notes", noteFile));
        }
    }

    void LoadStateSound(char state, string fileName) {
        string path = Path.Combine(SoundsPath, fileName);
        if (File.Exists(path))
            stateSounds[state] = Raylib.LoadSound(path);
        if (stateSounds.TryGetValue(state, out Sound sound))
            Raylib.SetSoundVolume(sound, volume);
    }

    void LoadDrumSounds() {
        LoadStateSound('A', Path.Combine("drums", "kick.wav"));
        LoadStateSound('B', Path.Combine("drums", "snare.wav"));
        LoadStateSound('C', Path.Combine("drums", "closedHH.wav"));
        LoadStateSound('D', Path.Combine("drums", "pedalHH.wav"));
        LoadStateSound('E', Path.Combine("drums", "openHH.wav"));
    }

}
