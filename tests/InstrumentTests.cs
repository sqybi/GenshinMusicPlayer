using GenshinMusicPlayer;
using System;
using System.Collections.Generic;
using WindowsInput.Native;

internal static class InstrumentTests
{
    private sealed class TwoRowInstrument : BaseInstrument
    {
        protected override bool IsTwoRow => true;
        protected override int[] NoteNumbers { get; } = Note.GetNoteNumbers(
            "C3", "D3", "E3", "F3", "G3", "A3", "B3",
            "C4", "D4", "E4", "F4", "G4", "A4", "B4");
    }

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

        IInstrument threeRow = new 风物之诗琴();
        CheckKey(threeRow, 60, "Z", VirtualKeyCode.VK_Z);
        CheckKey(threeRow, 72, "A", VirtualKeyCode.VK_A);
        CheckKey(threeRow, 84, "Q", VirtualKeyCode.VK_Q);
        CheckKey(threeRow, 95, "U", VirtualKeyCode.VK_U);
        CheckKey(new 镜花之琴(), 60, "Z", VirtualKeyCode.VK_Z);
        CheckKey(new 老旧的诗琴(), 85, "W", VirtualKeyCode.VK_W);
        CheckKey(new 老旧的诗琴(), 94, "U", VirtualKeyCode.VK_U);

        IInstrument twoRow = new TwoRowInstrument();
        CheckKey(twoRow, 60, "A", VirtualKeyCode.VK_A);
        CheckKey(twoRow, 71, "J", VirtualKeyCode.VK_J);
        CheckKey(twoRow, 72, "Q", VirtualKeyCode.VK_Q);
        CheckKey(twoRow, 83, "U", VirtualKeyCode.VK_U);
        Check(!twoRow.GetKeyCodeFromNote(60, new Note(0, 84), null).VirtualKeyCodePress.HasValue,
            "The two-row instrument must not play a third row.");
        NoteToPlay raised = twoRow.GetKeyCodeFromNote(60, new Note(0, 61), true);
        Check(raised.KeyboardPress == "S" && raised.Modification == true,
            "Semitone handling must use the two-row key layout.");

        var result = twoRow.CheckNotes(60, new List<Note> { new Note(0, 60), new Note(0, 61), new Note(0, 85) });
        Check(result.MissedCount == 1 && result.OutOfRangeCount == 1,
            "The two-row range check is incorrect.");
        Console.WriteLine("PASS note names and two-/three-row instrument mappings");
    }
}
