using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GenshinMusicPlayer
{
    internal sealed class MidiFileAnalysis
    {
        internal List<Note> Notes { get; private set; }
        internal Note MinNote { get; private set; }
        internal Note MaxNote { get; private set; }
        internal double LastNoteOffTime { get; private set; }
        internal bool HasTempo { get; private set; }
        internal bool HasMultipleTempos { get; private set; }
        internal double FirstTempo { get; private set; }

        internal static MidiFileAnalysis Analyze(MidiFile file)
        {
            if (file.DeltaTicksPerQuarterNote <= 0)
            {
                throw new InvalidDataException("MIDI 文件的每拍时钟数无效。");
            }

            var result = new MidiFileAnalysis { Notes = new List<Note>() };
            double quarterNoteTime = 0.5;
            for (int track = 0; track < file.Tracks; track++)
            {
                foreach (var midiEvent in file.Events[track].OfType<TempoEvent>())
                {
                    if (midiEvent.Tempo <= 0 || double.IsNaN(midiEvent.Tempo) || double.IsInfinity(midiEvent.Tempo))
                    {
                        throw new InvalidDataException("MIDI 文件中的速度标识无效。");
                    }
                    if (!result.HasTempo)
                    {
                        result.HasTempo = true;
                        result.FirstTempo = midiEvent.Tempo;
                        quarterNoteTime = 60.0 / midiEvent.Tempo;
                    }
                    else
                    {
                        result.HasMultipleTempos = true;
                    }
                }
            }

            for (int track = 0; track < file.Tracks; track++)
            {
                foreach (var midiEvent in file.Events[track].OfType<NoteOnEvent>())
                {
                    if (!MidiEvent.IsNoteOn(midiEvent)) continue;
                    if (midiEvent.OffEvent == null)
                    {
                        throw new InvalidDataException("MIDI 文件中的音符缺少结束事件。");
                    }

                    double startTime = (double)midiEvent.AbsoluteTime / file.DeltaTicksPerQuarterNote * quarterNoteTime * 1000;
                    double stopTime = (double)midiEvent.OffEvent.AbsoluteTime / file.DeltaTicksPerQuarterNote * quarterNoteTime * 1000;
                    if (stopTime < startTime)
                    {
                        throw new InvalidDataException("MIDI 文件中的音符结束时间早于开始时间。");
                    }
                    var note = new Note(startTime, stopTime, midiEvent.NoteNumber);
                    result.Notes.Add(note);
                    if (result.MinNote == null || note.Number < result.MinNote.Number) result.MinNote = note;
                    if (result.MaxNote == null || note.Number > result.MaxNote.Number) result.MaxNote = note;
                    if (stopTime > result.LastNoteOffTime) result.LastNoteOffTime = stopTime;
                }
            }

            if (result.Notes.Count == 0)
            {
                throw new InvalidDataException("MIDI 文件中没有可演奏的音符。");
            }
            result.Notes.Sort();
            return result;
        }
    }
}
