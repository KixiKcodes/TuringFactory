using Raylib_cs;

namespace TuringFactory;

public enum SoundMode {
    Drums,
    Notes
}

public sealed class SoundManager : IDisposable {
    static readonly string SoundsPath = Path.Combine(AppContext.BaseDirectory, "resources", "sounds");
    readonly Dictionary<char, Sound> stateSounds = [];
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
        if (mode == SoundMode.Drums)
            LoadDrumSounds();
        else
            LoadNoteSounds();
    }

    public void Dispose() {
        UnloadSounds();
    }

    void UnloadSounds() {
        foreach (Sound sound in stateSounds.Values)
            Raylib.UnloadSound(sound);
        stateSounds.Clear();
    }

    void LoadStateSound(char state, string fileName) {
        string path = Path.Combine(SoundsPath, fileName);
        if (File.Exists(path))
            stateSounds[state] = Raylib.LoadSound(path);
        if (stateSounds.TryGetValue(state, out Sound sound))
            Raylib.SetSoundVolume(sound, volume);
    }

    // TODO: Later on I plan on implementing a way to assign the sound you want to each state, for full customizability.

    void LoadNoteSounds() {
        LoadStateSound('A', Path.Combine("notes", "A#3.wav"));
        LoadStateSound('B', Path.Combine("notes", "A#4.wav"));
        LoadStateSound('C', Path.Combine("notes", "A#5.wav"));
        LoadStateSound('D', Path.Combine("notes", "A#6.wav"));
        LoadStateSound('E', Path.Combine("notes", "C3.wav"));
        LoadStateSound('F', Path.Combine("notes", "C4.wav"));
        LoadStateSound('G', Path.Combine("notes", "C5.wav"));
        LoadStateSound('H', Path.Combine("notes", "C6.wav"));
        LoadStateSound('I', Path.Combine("notes", "C7.wav"));
        LoadStateSound('J', Path.Combine("notes", "D#3.wav"));
        LoadStateSound('K', Path.Combine("notes", "D#4.wav"));
        LoadStateSound('L', Path.Combine("notes", "D#5.wav"));
        LoadStateSound('M', Path.Combine("notes", "D#6.wav"));
        LoadStateSound('N', Path.Combine("notes", "D3.wav"));
        LoadStateSound('O', Path.Combine("notes", "D4.wav"));
        LoadStateSound('P', Path.Combine("notes", "D5.wav"));
        LoadStateSound('Q', Path.Combine("notes", "D6.wav"));
        LoadStateSound('R', Path.Combine("notes", "F3.wav"));
        LoadStateSound('S', Path.Combine("notes", "F4.wav"));
        LoadStateSound('T', Path.Combine("notes", "F5.wav"));
        LoadStateSound('U', Path.Combine("notes", "F6.wav"));
        LoadStateSound('V', Path.Combine("notes", "G3.wav"));
        LoadStateSound('W', Path.Combine("notes", "G4.wav"));
        LoadStateSound('X', Path.Combine("notes", "G5.wav"));
        LoadStateSound('Y', Path.Combine("notes", "G6.wav"));
        LoadStateSound(TuringMachine.HaltState, Path.Combine("notes", "finish.wav"));
    }

    void LoadDrumSounds() {
        LoadStateSound('A', Path.Combine("drums", "kickacoustic.wav"));
        LoadStateSound('B', Path.Combine("drums", "snareacoustic.wav"));
        LoadStateSound('C', Path.Combine("drums", "closedHH1.wav"));
        LoadStateSound('D', Path.Combine("drums", "snarerim1.wav"));
        LoadStateSound('E', Path.Combine("drums", "openHHshort1.wav"));
        LoadStateSound('F', Path.Combine("drums", "openHHlong1.wav"));
        LoadStateSound('G', Path.Combine("drums", "cymbalride.wav"));
        LoadStateSound('H', Path.Combine("drums", "closedHH2.wav"));
        LoadStateSound('I', Path.Combine("drums", "openHHshort2.wav"));
        LoadStateSound('J', Path.Combine("drums", "openHHlong2.wav"));
        LoadStateSound('K', Path.Combine("drums", "snarerim2.wav"));
        LoadStateSound('L', Path.Combine("drums", "snarethin.wav"));
        LoadStateSound('M', Path.Combine("drums", "closedHH3.wav"));
        LoadStateSound('N', Path.Combine("drums", "openHHshort3.wav"));
        LoadStateSound('O', Path.Combine("drums", "openHHlong3.wav"));
        LoadStateSound('P', Path.Combine("drums", "snarerim3.wav"));
        LoadStateSound('Q', Path.Combine("drums", "cymbalridebell.wav"));
        LoadStateSound('R', Path.Combine("drums", "kickelectric.wav"));
        LoadStateSound('S', Path.Combine("drums", "snareelectric.wav"));
        LoadStateSound('T', Path.Combine("drums", "cowbell.wav"));
        LoadStateSound('U', Path.Combine("drums", "cymbalsplash.wav"));
        LoadStateSound('V', Path.Combine("drums", "tomhigh.wav"));
        LoadStateSound('W', Path.Combine("drums", "tommid.wav"));
        LoadStateSound('X', Path.Combine("drums", "tomlow.wav"));
        LoadStateSound('Y', Path.Combine("drums", "cymbalcrash.wav"));
        LoadStateSound(TuringMachine.HaltState, Path.Combine("drums", "finish.wav"));
    }
}
