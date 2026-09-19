namespace GenshinMusicPlayer
{
    public class BaseTwoRowsKeyboardInstrument : BaseInstrument, IInstrument
    {
        protected override bool IsTwoRow { get; } = true;

        protected override int[] NoteNumbers { get; } = Note.GetNoteNumbers(
            "C3", "D3", "E3", "F3", "G3", "A3", "B3",
            "C4", "D4", "E4", "F4", "G4", "A4", "B4");
    }
}
