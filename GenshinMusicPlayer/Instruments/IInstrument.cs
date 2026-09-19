using System.Collections.Generic;

namespace GenshinMusicPlayer
{
    public interface IInstrument
    {
        InstrumentCheckNotesResult CheckNotes(int baseNoteNumber, List<Note> notes);
        NoteToPlay GetKeyCodeFromNote(int baseNoteNumber, Note note, bool? isHigherFirst);
    }
}
