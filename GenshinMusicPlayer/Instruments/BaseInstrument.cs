using System;
using System.Collections.Generic;
using WindowsInput.Native;

namespace GenshinMusicPlayer
{
    public abstract class BaseInstrument : IInstrument
    {
        private static readonly VirtualKeyCode[] threeRowKeyCodes = {
            VirtualKeyCode.VK_Z, VirtualKeyCode.VK_X, VirtualKeyCode.VK_C, VirtualKeyCode.VK_V, VirtualKeyCode.VK_B, VirtualKeyCode.VK_N, VirtualKeyCode.VK_M,
            VirtualKeyCode.VK_A, VirtualKeyCode.VK_S, VirtualKeyCode.VK_D, VirtualKeyCode.VK_F, VirtualKeyCode.VK_G, VirtualKeyCode.VK_H, VirtualKeyCode.VK_J,
            VirtualKeyCode.VK_Q, VirtualKeyCode.VK_W, VirtualKeyCode.VK_E, VirtualKeyCode.VK_R, VirtualKeyCode.VK_T, VirtualKeyCode.VK_Y, VirtualKeyCode.VK_U
        };
        private static readonly string[] threeRowKeyNames = {
            "Z", "X", "C", "V", "B", "N", "M",
            "A", "S", "D", "F", "G", "H", "J",
            "Q", "W", "E", "R", "T", "Y", "U",
        };
        private static readonly VirtualKeyCode[] twoRowKeyCodes = {
            VirtualKeyCode.VK_A, VirtualKeyCode.VK_S, VirtualKeyCode.VK_D, VirtualKeyCode.VK_F, VirtualKeyCode.VK_G, VirtualKeyCode.VK_H, VirtualKeyCode.VK_J,
            VirtualKeyCode.VK_Q, VirtualKeyCode.VK_W, VirtualKeyCode.VK_E, VirtualKeyCode.VK_R, VirtualKeyCode.VK_T, VirtualKeyCode.VK_Y, VirtualKeyCode.VK_U
        };
        private static readonly string[] twoRowKeyNames = {
            "A", "S", "D", "F", "G", "H", "J",
            "Q", "W", "E", "R", "T", "Y", "U",
        };

        // Note numbers are absolute pitches used to describe the layout. The first pitch
        // corresponds to the lowest note selected in the UI.
        protected abstract int[] NoteNumbers { get; }
        protected virtual bool IsTwoRow => false;

        private int[] GetValidatedNoteNumbers()
        {
            int[] numbers = NoteNumbers;
            int keyCount = IsTwoRow ? twoRowKeyCodes.Length : threeRowKeyCodes.Length;
            if (numbers == null || numbers.Length != keyCount)
            {
                throw new InvalidOperationException("The number of notes must match the instrument's keys.");
            }
            for (int i = 1; i < numbers.Length; i++)
            {
                if (numbers[i] <= numbers[i - 1])
                {
                    throw new InvalidOperationException("Instrument notes must be in ascending order.");
                }
            }
            return numbers;
        }

        protected int BinarySearch(int[] data, int value)
        {
            int left = 0;
            int right = data.Length;
            // Loop: find in [left, right)
            while (left < right)
            {
                int mid = (left + right) / 2;
                if (value == data[mid])
                {
                    return mid;
                }
                else if (value > data[mid])
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid;
                }
            }
            return -1;
        }
        
        InstrumentCheckNotesResult IInstrument.CheckNotes(int baseNoteNumber, List<Note> notes)
        {
            int[] noteNumbers = GetValidatedNoteNumbers();
            int lowestNoteNumber = noteNumbers[0];
            int minNoteNumber = baseNoteNumber - 1;
            int maxNoteNumber = noteNumbers[noteNumbers.Length - 1] - lowestNoteNumber + baseNoteNumber + 1;
            HashSet<int> availableNotes = new HashSet<int>();
            foreach (var noteNumber in noteNumbers)
            {
                availableNotes.Add(noteNumber - lowestNoteNumber + baseNoteNumber);
            }

            InstrumentCheckNotesResult result = new InstrumentCheckNotesResult();
            foreach (var note in notes)
            {
                if (note.Number < minNoteNumber || note.Number > maxNoteNumber)
                {
                    result.OutOfRangeCount++;
                }
                else if (!availableNotes.Contains(note.Number))
                {
                    result.MissedCount++;
                }
            }
            return result;
        }

        NoteToPlay IInstrument.GetKeyCodeFromNote(int baseNoteNumber, Note note, bool? isHigherFirst)
        {
            int[] noteNumbers = GetValidatedNoteNumbers();
            VirtualKeyCode[] keyCodes = IsTwoRow ? twoRowKeyCodes : threeRowKeyCodes;
            string[] keyNames = IsTwoRow ? twoRowKeyNames : threeRowKeyNames;
            int noteNumber = note.Number - baseNoteNumber + noteNumbers[0];

            int pos = BinarySearch(noteNumbers, noteNumber);
            if (pos != -1)
            {
                return new NoteToPlay(note, null, keyNames[pos], keyCodes[pos]);
            }

            if (isHigherFirst.HasValue)
            {
                bool modifier;
                if (isHigherFirst.Value)
                {
                    noteNumber++;
                    modifier = true;
                }
                else
                {
                    noteNumber--;
                    modifier = false;
                }
                pos = BinarySearch(noteNumbers, noteNumber);
                if (pos != -1)
                {
                    return new NoteToPlay(note, modifier, keyNames[pos], keyCodes[pos]);
                }

                if (isHigherFirst.Value)
                {
                    noteNumber -= 2;
                    modifier = false;
                }
                else
                {
                    noteNumber += 2;
                    modifier = true;
                }
                pos = BinarySearch(noteNumbers, noteNumber);
                if (pos != -1)
                {
                    return new NoteToPlay(note, modifier, keyNames[pos], keyCodes[pos]);
                }
            }

            return new NoteToPlay(note, null, "", null);
        }
    }
}
