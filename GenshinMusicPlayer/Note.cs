using System;
using System.Globalization;

namespace GenshinMusicPlayer
{
    public class Note : IComparable
    {
        private static string[] convertNoteNumberToName = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public static string GetNoteName(int noteNumber)
        {
            return convertNoteNumberToName[noteNumber % 12] + (noteNumber / 12).ToString();
        }

        public static int GetNoteNumber(string noteName)
        {
            if (noteName == null)
            {
                throw new ArgumentNullException(nameof(noteName));
            }

            int pitchLength = noteName.Length > 1 && noteName[1] == '#' ? 2 : 1;
            if (noteName.Length <= pitchLength)
            {
                throw new ArgumentException("Invalid note name.", nameof(noteName));
            }

            string pitch = noteName.Substring(0, pitchLength);
            int pitchNumber = Array.IndexOf(convertNoteNumberToName, pitch);
            int octave;
            if (pitchNumber < 0 || !int.TryParse(noteName.Substring(pitchLength), NumberStyles.None,
                CultureInfo.InvariantCulture, out octave) || octave > 10)
            {
                throw new ArgumentException("Invalid note name.", nameof(noteName));
            }

            int noteNumber = octave * 12 + pitchNumber;
            if (noteNumber > 127)
            {
                throw new ArgumentException("Note is outside the MIDI range.", nameof(noteName));
            }
            return noteNumber;
        }

        public static int[] GetNoteNumbers(params string[] noteNames)
        {
            if (noteNames == null)
            {
                throw new ArgumentNullException(nameof(noteNames));
            }

            int[] numbers = new int[noteNames.Length];
            for (int i = 0; i < noteNames.Length; i++)
            {
                numbers[i] = GetNoteNumber(noteNames[i]);
            }
            return numbers;
        }

        public double Time { get; private set; }
        public double EndTime { get; private set; }
        public int Number { get; private set; }
        public string Name { get { return GetNoteName(Number); } }

        int IComparable.CompareTo(object obj)
        {
            Note note = (Note)obj;
            var compareTime = Time.CompareTo(note.Time);
            return compareTime == 0 ? Number.CompareTo(note.Number) : compareTime;
        }

        public Note(Note note)
        {
            Time = note.Time;
            EndTime = note.EndTime;
            Number = note.Number;
        }

        public Note(double time, int number) : this(time, time, number) { }

        public Note(double time, double endTime, int number)
        {
            if (number < 0)
            {
                throw new Exception("Illegal note number");
            }
            if (endTime < time)
            {
                throw new ArgumentOutOfRangeException(nameof(endTime));
            }
            Time = time;
            EndTime = endTime;
            Number = number;
        }
    }
}
