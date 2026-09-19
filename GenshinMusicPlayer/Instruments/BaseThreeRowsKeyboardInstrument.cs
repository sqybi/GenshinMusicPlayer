namespace GenshinMusicPlayer
{
    public class BaseThreeRowsKeyboardInstrument : BaseInstrument, IInstrument
    {
        protected override bool IsTwoRow { get; } = false;

        protected override int[] NoteNumbers { get; } = Note.GetNoteNumbers(
            "C3", "D3", "E3", "F3", "G3", "A3", "B3",
            "C4", "D4", "E4", "F4", "G4", "A4", "B4",
            "C5", "D5", "E5", "F5", "G5", "A5", "B5");
    }
}
