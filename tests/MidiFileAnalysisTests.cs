using GenshinMusicPlayer;
using NAudio.Midi;
using System;
using System.IO;

internal static class MidiFileAnalysisTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static byte[] Midi(byte[] track)
    {
        var header = new byte[] {
            0x4d, 0x54, 0x68, 0x64, 0, 0, 0, 6, 0, 0, 0, 1, 1, 0xe0,
            0x4d, 0x54, 0x72, 0x6b, 0, 0, 0, (byte)track.Length
        };
        var file = new byte[header.Length + track.Length];
        Array.Copy(header, file, header.Length);
        Array.Copy(track, 0, file, header.Length, track.Length);
        return file;
    }

    private static MidiFile Read(byte[] bytes, bool strictChecking = true)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, bytes);
            return new MidiFile(path, strictChecking);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void ExpectInvalidData(Action action, string expectedMessage)
    {
        try { action(); }
        catch (InvalidDataException ex)
        {
            Check(ex.Message.Contains(expectedMessage), "Wrong error: " + ex.Message);
            return;
        }
        throw new Exception("Expected InvalidDataException: " + expectedMessage);
    }

    private static int Main()
    {
        try
        {
            var empty = Read(Midi(new byte[] { 0, 0xff, 0x2f, 0 }));
            ExpectInvalidData(() => MidiFileAnalysis.Analyze(empty), "没有可演奏的音符");
            Console.WriteLine("PASS empty MIDI");

            var silent = Read(Midi(new byte[] { 0, 0x90, 0x3c, 0, 0, 0xff, 0x2f, 0 }), false);
            ExpectInvalidData(() => MidiFileAnalysis.Analyze(silent), "没有可演奏的音符");
            Console.WriteLine("PASS MIDI without playable notes");

            var missingOff = Read(Midi(new byte[] { 0, 0x90, 0x3c, 0x40, 0, 0xff, 0x2f, 0 }), false);
            ExpectInvalidData(() => MidiFileAnalysis.Analyze(missingOff), "缺少结束事件");
            Console.WriteLine("PASS missing note-off");

            var normal = Read(Midi(new byte[] { 0, 0x90, 0x3c, 0x40, 0x83, 0x60, 0x80, 0x3c, 0, 0, 0xff, 0x2f, 0 }));
            var result = MidiFileAnalysis.Analyze(normal);
            Check(result.Notes.Count == 1 && result.MinNote.Name == "C5" && result.MaxNote.Name == "C5", "Wrong note analysis.");
            Check(Math.Abs(result.LastNoteOffTime - 500) < 0.001, "Wrong duration.");
            Console.WriteLine("PASS valid MIDI");

            var instant = Read(Midi(new byte[] { 0, 0x90, 0x3c, 0x40, 0, 0x80, 0x3c, 0, 0, 0xff, 0x2f, 0 }));
            Check(MidiFileAnalysis.Analyze(instant).LastNoteOffTime == 0, "Zero-duration note should load.");
            Console.WriteLine("PASS zero-duration note");

            try { Read(new byte[] { 1, 2, 3 }); }
            catch (Exception) { Console.WriteLine("PASS malformed MIDI rejected"); return 0; }
            throw new Exception("Malformed MIDI was accepted.");
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
