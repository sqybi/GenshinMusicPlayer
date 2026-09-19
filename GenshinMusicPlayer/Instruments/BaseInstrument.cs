using System;
using System.Collections.Generic;
using WindowsInput.Native;

namespace GenshinMusicPlayer
{
    public abstract class BaseInstrument : IInstrument
    {
        public virtual string Name => GetType().Name;
        public virtual bool SupportsLongPress => false;

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

        // Note numbers follow keyboard order. The lowest pitch corresponds to the
        // lowest note selected in the UI; rows need not be in pitch order.
        protected abstract int[] NoteNumbers { get; }
        protected abstract bool IsTwoRow { get; }

        private int[] GetValidatedNoteNumbers()
        {
            int[] numbers = NoteNumbers;
            int keyCount = IsTwoRow ? twoRowKeyCodes.Length : threeRowKeyCodes.Length;
            if (numbers == null || numbers.Length != keyCount)
            {
                throw new InvalidOperationException("The number of notes must match the instrument's keys.");
            }
            var uniqueNumbers = new HashSet<int>();
            foreach (int number in numbers)
            {
                if (!uniqueNumbers.Add(number))
                {
                    throw new InvalidOperationException("Instrument notes must be unique.");
                }
            }
            return numbers;
        }

        private static int FindNotePosition(int[] data, int value)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (value == data[i])
                {
                    return i;
                }
            }
            return -1;
        }
        
        InstrumentCheckNotesResult IInstrument.CheckNotes(int baseNoteNumber, List<Note> notes)
        {
            int[] noteNumbers = GetValidatedNoteNumbers();
            int lowestNoteNumber = int.MaxValue;
            int highestNoteNumber = int.MinValue;
            foreach (int number in noteNumbers)
            {
                lowestNoteNumber = Math.Min(lowestNoteNumber, number);
                highestNoteNumber = Math.Max(highestNoteNumber, number);
            }
            int minNoteNumber = baseNoteNumber - 1;
            int maxNoteNumber = highestNoteNumber - lowestNoteNumber + baseNoteNumber + 1;
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
            int lowestNoteNumber = int.MaxValue;
            foreach (int number in noteNumbers)
            {
                lowestNoteNumber = Math.Min(lowestNoteNumber, number);
            }
            int noteNumber = note.Number - baseNoteNumber + lowestNoteNumber;

            int pos = FindNotePosition(noteNumbers, noteNumber);
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
                pos = FindNotePosition(noteNumbers, noteNumber);
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
                pos = FindNotePosition(noteNumbers, noteNumber);
                if (pos != -1)
                {
                    return new NoteToPlay(note, modifier, keyNames[pos], keyCodes[pos]);
                }
            }

            return new NoteToPlay(note, null, "", null);
        }
    }
}
