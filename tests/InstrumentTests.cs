using GenshinMusicPlayer;
using System;
using System.Collections.Generic;
using WindowsInput.Native;

internal static class InstrumentTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void CheckKey(IInstrument instrument, int number, string name, VirtualKeyCode key)
    {
        NoteToPlay mapped = instrument.GetKeyCodeFromNote(60, new Note(0, number), null);
        Check(mapped.KeyboardPress == name && mapped.VirtualKeyCodePress == key,
            "Unexpected mapping for note " + number);
    }

    private static void Main()
    {
        for (int number = 0; number <= 127; number++)
        {
            Check(Note.GetNoteNumber(Note.GetNoteName(number)) == number,
                "Note name did not round trip: " + number);
        }
        Check(Note.GetNoteNumber("C#3") == 37, "Sharp note parsing failed.");

        Check(InstrumentCatalog.All.Count > 0, "The instrument list must not be empty.");
        var instrumentNames = new HashSet<string>();
        foreach (IInstrument listedInstrument in InstrumentCatalog.All)
        {
            Check(listedInstrument.Name == listedInstrument.GetType().Name,
                "The display name must follow the instrument class name.");
            Check(instrumentNames.Add(listedInstrument.Name),
                "The instrument list must not contain duplicate names.");
            Check(listedInstrument.SupportsLongPress == (listedInstrument is 晚风圆号),
                "The instrument's long-press capability is incorrect.");
        }

        IInstrument threeRow = new 风物之诗琴();
        Check(!threeRow.SupportsLongPress && new 晚风圆号().SupportsLongPress,
            "Only instruments that sustain notes should request a held key.");
        CheckKey(threeRow, 60, "Z", VirtualKeyCode.VK_Z);
        CheckKey(threeRow, 72, "A", VirtualKeyCode.VK_A);
        CheckKey(threeRow, 84, "Q", VirtualKeyCode.VK_Q);
        CheckKey(threeRow, 95, "U", VirtualKeyCode.VK_U);
        CheckKey(new 镜花之琴(), 60, "Z", VirtualKeyCode.VK_Z);
        CheckKey(new 老旧的诗琴(), 85, "W", VirtualKeyCode.VK_W);
        CheckKey(new 老旧的诗琴(), 94, "U", VirtualKeyCode.VK_U);
        CheckKey(new 跃律琴(), 84, "Q", VirtualKeyCode.VK_Q);

        foreach (IInstrument keyboard in new IInstrument[] { new 晚风圆号(), new 沃雅妮莎() })
        {
            CheckKey(keyboard, 60, "A", VirtualKeyCode.VK_A);
            CheckKey(keyboard, 71, "J", VirtualKeyCode.VK_J);
            CheckKey(keyboard, 72, "Q", VirtualKeyCode.VK_Q);
            CheckKey(keyboard, 83, "U", VirtualKeyCode.VK_U);
            Check(!keyboard.GetKeyCodeFromNote(60, new Note(0, 84), null).VirtualKeyCodePress.HasValue,
                "A two-row keyboard must not play a third row.");
            NoteToPlay raised = keyboard.GetKeyCodeFromNote(60, new Note(0, 61), true);
            Check(raised.KeyboardPress == "S" && raised.Modification == true,
                "Semitone handling must use the two-row key layout.");
            var rangeResult = keyboard.CheckNotes(60, new List<Note> { new Note(0, 60), new Note(0, 61), new Note(0, 85) });
            Check(rangeResult.MissedCount == 1 && rangeResult.OutOfRangeCount == 1,
                "The two-row range check is incorrect.");
        }

        foreach (IInstrument guitar in new IInstrument[] { new 悠可琴(), new 余音() })
        {
            CheckKey(guitar, 60, "Q", VirtualKeyCode.VK_Q);
            CheckKey(guitar, 71, "U", VirtualKeyCode.VK_U);
            CheckKey(guitar, 72, "Z", VirtualKeyCode.VK_Z);
            CheckKey(guitar, 84, "A", VirtualKeyCode.VK_A);
            CheckKey(guitar, 95, "J", VirtualKeyCode.VK_J);
            NoteToPlay raisedChord = guitar.GetKeyCodeFromNote(60, new Note(0, 61), true);
            Check(raisedChord.KeyboardPress == "W" && raisedChord.Modification == true,
                "Semitone handling must use the guitar chord row.");
            var guitarResult = guitar.CheckNotes(60, new List<Note> { new Note(0, 60), new Note(0, 72), new Note(0, 84), new Note(0, 97) });
            Check(guitarResult.MissedCount == 0 && guitarResult.OutOfRangeCount == 1,
                "The guitar range must include the low chord row and both melody rows.");
        }

        Console.WriteLine("PASS note names and two-/three-row instrument mappings");
    }
}
