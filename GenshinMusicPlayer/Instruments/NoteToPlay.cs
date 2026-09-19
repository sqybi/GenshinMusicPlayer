using System.Text;
using WindowsInput.Native;

namespace GenshinMusicPlayer
{
    public class NoteToPlay : Note
    {
        // Modifcation:
        // Null means no modification; true means semitone higher than original note; false means semitone lower than original note.
        // If no key pressed, KeyboardPress will be "" and VirtualKeyCode will be null. Modification does not mean anything at this time.
        public bool? Modification { get; private set; }
        // KeyboardPress: the key name on keyboard pressed (without modifier).
        public string KeyboardPress { get; private set; }
        // VirtualKeyCodePress: the virtual key code corresponding to the key pressed (without modifier).
        public VirtualKeyCode? VirtualKeyCodePress { get; private set; }

        public NoteToPlay(Note note, bool? modification, string keyboardPress, VirtualKeyCode? virtualKeyCodePress) : base(note)
        {
            Modification = modification;
            KeyboardPress = keyboardPress;
            VirtualKeyCodePress = virtualKeyCodePress;
        }

        public NoteToPlay(double time, int number, bool? modification, string keyboardPress, VirtualKeyCode? virtualKeyCodePress) : base(time, number)
        {
            Modification = modification;
            KeyboardPress = keyboardPress;
            VirtualKeyCodePress = virtualKeyCodePress;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            if (!VirtualKeyCodePress.HasValue)
            {
                sb.Append("(");
            }
            sb.Append(Name);
            if (Modification.HasValue)
            {
                sb.Append(Modification.Value ? "↗" : "↘");
            }
            if (!VirtualKeyCodePress.HasValue)
            {
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}
