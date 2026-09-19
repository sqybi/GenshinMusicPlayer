namespace GenshinMusicPlayer
{
    public class 老旧的诗琴 : BaseThreeRowsKeyboardInstrument, IInstrument
    {
        protected override int[] NoteNumbers { get; } = Note.GetNoteNumbers(
            "C3", "D3", "D#3", "F3", "G3", "A3", "A#3",
            "C4", "D4", "D#4", "F4", "G4", "A4", "A#4",
            "C5", "C#5", "D#5", "F5", "G5", "G#5", "A#5");
    }
}
