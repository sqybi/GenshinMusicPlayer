using Microsoft.Win32;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace GenshinMusicPlayer
{
    #region Instruments

    public class InstrumentCheckNotesResult
    {
        public int MissedCount { get; set; }
        public int OutOfRangeCount { get; set; }
    }

    public interface IInstrument
    {
        InstrumentCheckNotesResult CheckNotes(int baseNoteNumber, List<Note> notes);
        VirtualKeyCode? GetKeyCodeFromNote(int baseNoteNumber, Note note, bool isHigherFirst);
    }

    public class BaseInstrument : IInstrument
    {
        protected readonly VirtualKeyCode[] keyCodes = {
            VirtualKeyCode.VK_Z, VirtualKeyCode.VK_X, VirtualKeyCode.VK_C, VirtualKeyCode.VK_V, VirtualKeyCode.VK_B, VirtualKeyCode.VK_N, VirtualKeyCode.VK_M,
            VirtualKeyCode.VK_A, VirtualKeyCode.VK_S, VirtualKeyCode.VK_D, VirtualKeyCode.VK_F, VirtualKeyCode.VK_G, VirtualKeyCode.VK_H, VirtualKeyCode.VK_J,
            VirtualKeyCode.VK_Q, VirtualKeyCode.VK_W, VirtualKeyCode.VK_E, VirtualKeyCode.VK_R, VirtualKeyCode.VK_T, VirtualKeyCode.VK_Y, VirtualKeyCode.VK_U
        };

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

        protected InstrumentCheckNotesResult CheckNotesInternal(int baseNoteNumber, List<Note> notes, int[] noteNumbers)
        {
            int minNoteNumber = noteNumbers[0] + baseNoteNumber - 1;
            int maxNoteNumber = noteNumbers[noteNumbers.Length - 1] + baseNoteNumber + 1;
            HashSet<int> availableNotes = new HashSet<int>();
            foreach (var noteNumber in noteNumbers)
            {
                availableNotes.Add(noteNumber + baseNoteNumber);
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

        protected VirtualKeyCode? GetKeyCodeFromNoteInternal(int baseNoteNumber, Note note, bool isHigherFirst, int[] noteNumbers)
        {
            int noteNumber = note.Number - baseNoteNumber;

            int pos = BinarySearch(noteNumbers, noteNumber);
            if (pos != -1)
            {
                return keyCodes[pos];
            }

            if (isHigherFirst)
            {
                noteNumber++;
            }
            else
            {
                noteNumber--;
            }
            pos = BinarySearch(noteNumbers, noteNumber);
            if (pos != -1)
            {
                return keyCodes[pos];
            }

            if (isHigherFirst)
            {
                noteNumber -= 2;
            }
            else
            {
                noteNumber += 2;
            }
            pos = BinarySearch(noteNumbers, noteNumber);
            if (pos != -1)
            {
                return keyCodes[pos];
            }

            return null;
        }

        InstrumentCheckNotesResult IInstrument.CheckNotes(int baseNoteNumber, List<Note> notes)
        {
            throw new NotImplementedException();
        }

        VirtualKeyCode? IInstrument.GetKeyCodeFromNote(int baseNoteNumber, Note note, bool isHigherFirst)
        {
            throw new NotImplementedException();
        }
    }

    public class 风物之诗琴 : BaseInstrument, IInstrument
    {
        protected readonly int[] noteNumbers = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24, 26, 28, 29, 31, 33, 35 };

        public InstrumentCheckNotesResult CheckNotes(int baseNoteNumber, List<Note> notes)
        {
            return CheckNotesInternal(baseNoteNumber, notes, noteNumbers);
        }

        public VirtualKeyCode? GetKeyCodeFromNote(int baseNoteNumber, Note note, bool isHigherFirst)
        {
            return GetKeyCodeFromNoteInternal(baseNoteNumber, note, isHigherFirst, noteNumbers);
        }
    }

    public class 镜花之琴 : 风物之诗琴, IInstrument { }

    public class 老旧的诗琴 : BaseInstrument, IInstrument
    {
        protected readonly int[] noteNumbers = { 0, 2, 3, 5, 7, 9, 10, 12, 14, 15, 17, 19, 21, 22, 24, 25, 27, 29, 31, 32, 34 };

        public InstrumentCheckNotesResult CheckNotes(int baseNoteNumber, List<Note> notes)
        {
            return CheckNotesInternal(baseNoteNumber, notes, noteNumbers);
        }

        public VirtualKeyCode? GetKeyCodeFromNote(int baseNoteNumber, Note note, bool isHigherFirst)
        {
            return GetKeyCodeFromNoteInternal(baseNoteNumber, note, isHigherFirst, noteNumbers);
        }
    }

    #endregion

    public class MidiFileProperty : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }

    public class Note : IComparable
    {
        private static string[] convertNoteNumberToName = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public static string GetNoteName(int noteNumber)
        {
            return convertNoteNumberToName[noteNumber % 12] + (noteNumber / 12).ToString();
        }

        public long Time { get; private set; }
        public int Number { get; private set; }
        public string Name {
            get
            {
                return GetNoteName(Number);
            }
        }

        public Note(long time, int number)
        {
            if (number < 0)
            {
                throw new Exception("Illegal note number");
            }
            Time = time;
            Number = number;
        }

        int IComparable.CompareTo(object obj)
        {
            Note note = (Note)obj;
            var compareTime = Time.CompareTo(note.Time);
            if (compareTime == 0)
            {
                return Number.CompareTo(note.Number);
            }
            else
            {
                return compareTime;
            }
        }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly InputSimulator sim = new InputSimulator();

        private string midiFilePath = "";
        private MidiFile midiFile = null;
        private long maxNoteOffTime = 0;
        private BindingList<MidiFileProperty> midiFileProperties = new BindingList<MidiFileProperty>();
        private List<Note> notes = null;
        private IInstrument instrument = null;

        private int tone;
        private bool isHigherFirst;
        private long noteMergingTime;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        private void LoadMidiFileInfo(MidiFile file)
        {
            Note minNote = null;
            Note maxNote = null;
            maxNoteOffTime = 0;

            // Load File
            notes = new List<Note>();
            for (int track = 0; track < file.Tracks; track++)
            {
                foreach (var midiEvent in file.Events[track].OfType<NoteOnEvent>())
                {
                    if (MidiEvent.IsNoteOn(midiEvent))
                    {
                        var currentNote = new Note(midiEvent.AbsoluteTime, midiEvent.NoteNumber);
                        notes.Add(currentNote);
                        if (minNote == null || currentNote.Number < minNote.Number) minNote = currentNote;
                        if (maxNote == null || currentNote.Number > maxNote.Number) maxNote = currentNote;
                        if (midiEvent.OffEvent.AbsoluteTime > maxNoteOffTime) maxNoteOffTime = midiEvent.OffEvent.AbsoluteTime;
                    }
                }
            }
            notes.Sort();

            // Read file properties
            midiFileProperties.Clear();
            midiFileProperties.Add(new MidiFileProperty() { Name = "总音符数量", Value = notes.Count.ToString() });
            midiFileProperties.Add(new MidiFileProperty() { Name = "最低音符", Value = minNote.Name });
            midiFileProperties.Add(new MidiFileProperty() { Name = "最高音符", Value = maxNote.Name });
            midiFileProperties.Add(new MidiFileProperty() { Name = "文件时长", Value = string.Format("{0:F3} 秒", maxNoteOffTime / 1000.0) });
            ListViewFileProperties.ItemsSource = midiFileProperties;
        }

        private void LoadMidiFile(string fileName)
        {
            if (fileName != "")
            {
                midiFile = new MidiFile(fileName);
                LoadMidiFileInfo(midiFile);
                midiFilePath = fileName;
                TextBoxCurrentFileName.Text = midiFilePath;
            }
            else
            {
                TextBoxCurrentFileName.Text = "";
                midiFilePath = "";
                midiFile = null;
                notes = null;
            }
        }

        private void UpdateComboBoxTone()
        {
            if (ComboBoxTone == null)
            {
                return;
            }
            ComboBoxTone.Items.Clear();
            if (notes != null && instrument != null)
            {
                int selectedIndex = -1;
                InstrumentCheckNotesResult bestResult = null;
                for (int i = 0; i <= 127; ++i)
                {
                    var result = instrument.CheckNotes(i, notes);
                    ComboBoxTone.Items.Add(String.Format("低音 do = {0} | {1} 个音符在范围外 | {2} 个音符和琴上相差半音", Note.GetNoteName((int)i), result.OutOfRangeCount, result.MissedCount));
                    if (bestResult == null 
                        || result.OutOfRangeCount < bestResult.OutOfRangeCount 
                        || (result.OutOfRangeCount == bestResult.OutOfRangeCount 
                            && result.MissedCount < bestResult.MissedCount))
                    {
                        bestResult = result;
                        selectedIndex = i;
                    }
                }
                ComboBoxTone.SelectedIndex = selectedIndex;
            }
            else
            {
                ComboBoxTone.SelectedIndex = -1;
            }
        }

        private void PlayMusic()
        {
            // Switch to Genshin window and wait...
            Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                TextBoxCurrentNote.Text = "3 秒后开始……";
            });
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName == "YuanShen")
                {
                    SwitchToThisWindow(process.MainWindowHandle, true);
                    break;
                }
            }
            Thread.Sleep(1000);
            Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                TextBoxCurrentNote.Text = "2 秒后开始……";
            });
            Thread.Sleep(1000);
            Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                TextBoxCurrentNote.Text = "1 秒后开始……";
            });
            Thread.Sleep(1000);
            Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                TextBoxCurrentNote.Text = "";
            });

            var startTime = DateTime.Now;

            int noteIdx = 0;
            while (noteIdx < notes.Count)
            {
                var nextTimeToBePlayed = startTime + TimeSpan.FromMilliseconds(notes[noteIdx].Time);

                // Merge notes
                int nextNoteIdx = noteIdx + 1;
                while (nextNoteIdx < notes.Count && notes[nextNoteIdx].Time - notes[noteIdx].Time <= noteMergingTime)
                {
                    nextNoteIdx++;
                }

                // Generate keys
                List<Note> notesToPlay = new List<Note>();
                List<VirtualKeyCode> keysToPress = new List<VirtualKeyCode>();
                for (int i = noteIdx; i < nextNoteIdx; ++i)
                {
                    var keyCode = instrument.GetKeyCodeFromNote(tone, notes[i], isHigherFirst);
                    if (keyCode.HasValue)
                    {
                        notesToPlay.Add(notes[i]);
                        keysToPress.Add(keyCode.Value);
                    }
                }

                // Wait and press
                var timeToSleep = nextTimeToBePlayed - DateTime.Now;
                if (timeToSleep > TimeSpan.FromSeconds(0))
                {
                    Thread.Sleep(timeToSleep);
                }
                if (keysToPress.Count > 0)
                {
                    sim.Keyboard.KeyPress(keysToPress.ToArray());
                }

                // Update UI
                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    ProgressBarPlay.Value = (nextTimeToBePlayed - startTime).TotalMilliseconds / maxNoteOffTime * 100;
                    TextBoxCurrentNote.Text = notesToPlay.Aggregate("", (text, note) => text + " " + note.Name).Substring(1);
                    WrapPanelHistoryNotes.Children.Add(new TextBox() { Text = TextBoxCurrentNote.Text, Margin = new Thickness(2, 2, 2, 2) });
                });

                noteIdx = nextNoteIdx;
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                ProgressBarPlay.Value = 100;
                ButtonLoadMidiFile.IsEnabled = true;
                ButtonStart.IsEnabled = true;
            });
        }

        private void ButtonLoadMidiFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openMidiFileDialog = new OpenFileDialog();
            openMidiFileDialog.Filter = "MIDI 文件 (*.mid;*.midi)|*.mid;*.midi";
            if (openMidiFileDialog.ShowDialog() == true)
            {
                LoadMidiFile(openMidiFileDialog.FileName);
            }
            UpdateComboBoxTone();
        }

        private void ComboBoxInstrument_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var instrumentSelection = ComboBoxInstrument.SelectedIndex;
            switch (instrumentSelection)
            {
                case 0:
                    instrument = new 风物之诗琴();
                    break;
                case 1:
                    instrument = new 镜花之琴();
                    break;
                case 2:
                    instrument = new 老旧的诗琴();
                    break;
                default:
                    instrument = null;
                    break;
            }
            UpdateComboBoxTone();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            ButtonStart.IsEnabled = false;
            if (notes == null)
            {
                MessageBox.Show("请先加载 MIDI 文件！");
                ButtonStart.IsEnabled = true;
                return;
            }
            if (ComboBoxInstrument.SelectedIndex == -1)
            {
                MessageBox.Show("请选择乐器！");
                ButtonStart.IsEnabled = true;
                return;
            }
            if (ComboBoxTone.SelectedIndex == -1)
            {
                MessageBox.Show("请选择调性！");
                ButtonStart.IsEnabled = true;
                return;
            }
            if (RadioButtonNoteSemitoneNotAllowed.IsChecked == true)
            {
                var result = instrument.CheckNotes(ComboBoxTone.SelectedIndex, notes);
                if (result.MissedCount > 0)
                {
                    MessageBox.Show("有不允许的半音音符存在！");
                    ButtonStart.IsEnabled = true;
                    return;
                }
            }
            if (RadioButtonNoteOutOfRangeNotAllowed.IsChecked == true)
            {
                var result = instrument.CheckNotes(ComboBoxTone.SelectedIndex, notes);
                if (result.OutOfRangeCount > 0)
                {
                    MessageBox.Show("有不允许的乐器范围外音符存在！");
                    ButtonStart.IsEnabled = true;
                    return;
                }
            }
            if (!ulong.TryParse(TextBoxNoteMerging.Text, out ulong parseResult))
            {
                MessageBox.Show("音符合并演奏选项不是合法的正整数！");
                ButtonStart.IsEnabled = true;
                return;
            }
            
            ButtonLoadMidiFile.IsEnabled = false;
            WrapPanelHistoryNotes.Children.Clear();
            ProgressBarPlay.Value = 0;

            tone = ComboBoxTone.SelectedIndex;
            isHigherFirst = RadioButtonNoteSemitoneHigher.IsChecked == true;
            noteMergingTime = (long)parseResult;

            Thread thread = new Thread(PlayMusic);
            thread.Start();
        }

        public MainWindow()
        {
            InitializeComponent();

            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("请以管理员权限运行程序！程序即将退出……");
                Application.Current.Shutdown();
            }

            ListViewFileProperties.ItemsSource = midiFileProperties;
        }
    }
}
