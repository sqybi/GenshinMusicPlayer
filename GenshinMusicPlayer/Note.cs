using System;

namespace GenshinMusicPlayer
{
    public class Note : IComparable
    {
        private static string[] convertNoteNumberToName = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public static string GetNoteName(int noteNumber)
        {
            return convertNoteNumberToName[noteNumber % 12] + (noteNumber / 12).ToString();
        }

        public double Time { get; private set; }
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
            Number = note.Number;
        }

        public Note(double time, int number)
        {
            if (number < 0)
            {
                throw new Exception("Illegal note number");
            }
            Time = time;
            Number = number;
        }
    }
}
