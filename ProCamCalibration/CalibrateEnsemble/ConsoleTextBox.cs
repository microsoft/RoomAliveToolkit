using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace RoomAliveToolkit
{
    public partial class ConsoleTextBox : UserControl
    {
        ConsoleRedirection consoleRedirection;
        public ConsoleTextBox()
        {
            InitializeComponent();
            consoleRedirection = new ConsoleRedirection(richTextBox1);
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.AutoFlush = true;
            Console.SetOut(consoleRedirection);
        }
    }
    public class ConsoleRedirection : TextWriter
    {
        RichTextBox textBox;
        public ConsoleRedirection(RichTextBox textBox)
        {
            this.textBox = textBox;
        }
        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }

        const int maxLines = 1000;
        public override void Write(string value)
        {
            MethodInvoker invoker = delegate
            {
                textBox.AppendText(value);
                var lines = textBox.Lines;
                var nLines = lines.Length;
                if (nLines > (maxLines + 100))
                {
                    var newLines = new string[maxLines];
                    for (int i = 0; i < maxLines; i++)
                        newLines[i] = lines[nLines - maxLines + i];
                    textBox.Lines = newLines;
                }
                textBox.SelectionStart = textBox.Text.Length;
                textBox.ScrollToCaret();
            };
            if (textBox.Created)
                textBox.BeginInvoke(invoker);
        }
        public override void WriteLine(string x)
        {
            Write(x + Environment.NewLine);
        }
        public override void Write(bool value) { this.Write(value.ToString()); }
        public override void Write(char value) { this.Write(value.ToString()); }
        public override void Write(char[] buffer) { this.Write(new string(buffer)); }
        public override void Write(char[] buffer, int index, int count) { this.Write(new string(buffer, index, count)); }
        public override void Write(decimal value) { this.Write(value.ToString()); }
        public override void Write(double value) { this.Write(value.ToString()); }
        public override void Write(float value) { this.Write(value.ToString()); }
        public override void Write(int value) { this.Write(value.ToString()); }
        public override void Write(long value) { this.Write(value.ToString()); }
        public override void Write(string format, object arg0) { this.WriteLine(string.Format(format, arg0)); }
        public override void Write(string format, object arg0, object arg1) { this.WriteLine(string.Format(format, arg0, arg1)); }
        public override void Write(string format, object arg0, object arg1, object arg2) { this.WriteLine(string.Format(format, arg0, arg1, arg2)); }
        public override void Write(string format, params object[] arg) { this.WriteLine(string.Format(format, arg)); }
        public override void Write(uint value) { this.WriteLine(value.ToString()); }
        public override void Write(ulong value) { this.WriteLine(value.ToString()); }
        public override void Write(object value) { this.WriteLine(value.ToString()); }
        public override void WriteLine() { this.Write(Environment.NewLine); }
        public override void WriteLine(bool value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(char value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(char[] buffer) { this.WriteLine(new string(buffer)); }
        public override void WriteLine(char[] buffer, int index, int count) { this.WriteLine(new string(buffer, index, count)); }
        public override void WriteLine(decimal value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(double value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(float value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(int value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(long value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(string format, object arg0) { this.WriteLine(string.Format(format, arg0)); }
        public override void WriteLine(string format, object arg0, object arg1) { this.WriteLine(string.Format(format, arg0, arg1)); }
        public override void WriteLine(string format, object arg0, object arg1, object arg2) { this.WriteLine(string.Format(format, arg0, arg1, arg2)); }
        public override void WriteLine(string format, params object[] arg) { this.WriteLine(string.Format(format, arg)); }
        public override void WriteLine(uint value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(ulong value) { this.WriteLine(value.ToString()); }
        public override void WriteLine(object value) { this.WriteLine(value.ToString()); }
    }
}
