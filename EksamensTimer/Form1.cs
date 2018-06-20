using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace EksamensTimer
{
    public partial class Form1 : Form
    {
        private const int Minutes = 12;
        private const int Seconds = 0;
        private static readonly TimeSpan DefaultFullTime = new TimeSpan(0, Minutes, Seconds);


        private readonly System.Media.SoundPlayer _soundPlayer = new System.Media.SoundPlayer(@"notification.wav");
        private bool _unsaved = false;
        private DateTime _dt = DateTime.Now;
        private TimeSpan _fullTime = DefaultFullTime;
        private int _enterSequence = 0;
        private const int TimeEnter = 1;
        private bool _alarmed = false;
        private const bool Save = true;
        private TimeSpan Elapsed => DateTime.Now - _dt;
        private bool _playing = false;

        private Color _lastColor;

        public Form1()
        {
            InitializeComponent();
            ResetTime();
            label1.Text = "CTRL+ [G: Green                            " 
                        + "R: Red                              "
                        + "B: Black                            "
                        + "S: Silence                          "
                        + "Q: Reset time                       "
                        + "T: Timestamp]";
            _lastColor = textBox1.SelectionColor;
            textBox1.SelectionChanged += (sender, args) =>
            {

                if(!_inSelectionHandler && (_inSelectionHandler=true))
                    try
                    {
                        if (textBox1.SelectionLength == 0)
                        {
                            label2.Text = GetColor(textBox1.SelectionStart).ToString();
                        }
                    }
                    finally
                    {
                        _inSelectionHandler = false;
                    }
            };
        }

        private bool _inSelectionHandler = false;

        private Color GetColor(int position)
        {
            textBox1.SuspendLayout();
            int ostart = textBox1.SelectionStart;
            int olen = textBox1.SelectionLength;
            textBox1.SelectionStart = position;
            textBox1.SelectionLength = 0;
            var color = textBox1.SelectionColor;
            textBox1.SelectionStart = ostart;
            textBox1.SelectionLength = olen;
            textBox1.ResumeLayout();
            return color;
        }

        string GetTimeString(TimeSpan time)
        {
            var remaining = _fullTime - time;
            int ceilSecond = 0;
            if (time.Milliseconds > 0)
                ceilSecond = 1;
            return $"{time.Minutes:D2}:{(time.Seconds+ceilSecond):D2} ({remaining.Minutes:D2}:{remaining.Seconds:D2})";
        }


        void SetPlay(bool state)
        {
            try
            {
                if (state)
                    _soundPlayer.PlayLooping();
                else
                    _soundPlayer.Stop();
                _playing = state;
            }
            catch
            {
                try
                {
                    _soundPlayer.Stop();
                }
                catch { }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var time = Elapsed;
            Text = GetTimeString(time);

            if(time > _fullTime && ! _alarmed && (_alarmed=true)) 
            {
                SetPlay(true);
            }

            if (textBox1.Lines.Length > 2)
            {
                SaveFile();
            }
        }

        private void SaveFile()
        {
            if (_unsaved && Save)
            {
                var date = $"{_dt.Year:D2}-{_dt.Month:D2}-{_dt.Day:D2} {_dt.Hour:D2}-{_dt.Minute:D2}-{_dt.Second:D2}";
                try
                {
                    var name = $"{date} {textBox1.Lines[0]}";
                    SaveFile(name);
                }
                catch
                {
                    SaveFile(date);
                }
            }
        }


        private void SaveFile(string name)
        {
            var textfile = $"{name}.txt";
            var rtfFile = $"{name}.rtf";
            File.WriteAllText(textfile, textBox1.Text);
            File.WriteAllText(rtfFile, textBox1.Rtf);
            _unsaved = false;
       }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            _unsaved = true;
            //if(textBox1.Text.Length < 8)
            //{
            //    ResetTime();
            //    timer1.Enabled = false;
            //} else 
            if (timer1.Enabled == false)
            {
                ResetTime();
                timer1.Enabled = true;
            }
        }

        private void ResetTime()
        {
            _dt = DateTime.Now;
            _fullTime = DefaultFullTime;
            Text = GetTimeString(_dt - _dt);
            _alarmed = false;
            SetPlay(false);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var handled = false;
            switch (keyData)
            {
                    
                case Keys.Control | Keys.G:
                    SetSelectedColor(Color.Green);
                    handled = true;
                    break;
                case Keys.Alt | Keys.G:
                    SetLineColor(Color.Green);
                    handled = true;
                    break;
                case Keys.Control | Keys.R:
                    SetSelectedColor(Color.Red);
                    handled = true;
                    break;
                case Keys.Alt | Keys.R:
                    SetLineColor(Color.Red);
                    handled = true;
                    break;
                case Keys.Control | Keys.B:
                    SetSelectedColor(Color.Black);
                    handled = true;
                    break;
                case Keys.Alt | Keys.B:
                    SetLineColor(Color.Black);
                    handled = true;
                    break;
                case Keys.Control | Keys.S:
                    SetPlay(false);
                    handled = true;
                    break;
                case Keys.Control | Keys.Q:
                    ResetTime();
                    handled = true;
                    break;
                case Keys.Enter | Keys.Control:
                    textBox1.SuspendLayout();
                    int oldpos = textBox1.SelectionStart;
                    int oldlen = textBox1.SelectionLength;
                    int firstcharindex = textBox1.GetFirstCharIndexOfCurrentLine();
                    textBox1.SelectionLength = 0;
                    textBox1.SelectionStart = firstcharindex;
                    var added = GetTimeString(Elapsed) + ": ";
                    textBox1.SelectedText = added;
                    textBox1.SelectionLength = oldlen;
                    textBox1.SelectionStart = oldpos + added.Length; ;
                    handled = true;
                    textBox1.ResumeLayout();
                    break;
                case Keys.Enter:
                    var val = base.ProcessCmdKey(ref msg, keyData);
                    _enterSequence++;
                    if (_enterSequence>TimeEnter)
                    {
                        textBox1.SelectedText = "\n";
                        textBox1.SelectedText = GetTimeString(Elapsed)+":  ";
                        _enterSequence = 0;
                        return true;
                    }
                    return val;
                case Keys.Control | Keys.L:
                    MessageBox.Show(textBox1.SelectionColor.ToString());
                    return true;
                case Keys.Control | Keys.T:
                    //ResetTime();
                    var now = DateTime.Now;
                    var clockString = $"{now.Hour:D2}:{now.Minute:D2}:{now.Second:D2}";
                        
                    textBox1.SelectedText = clockString + " - " + GetTimeString(Elapsed);
                    handled = true;
                    break;
            }

            _enterSequence = 0;
            return handled || base.ProcessCmdKey(ref msg, keyData);
        }



        private void SetSelectedColor(Color color)
        {
            textBox1.SelectionColor = color;
        }

        private void SetLineColor(Color color)
        {
            void SelectCurrentLine()
            {
                int firstcharindex = textBox1.GetFirstCharIndexOfCurrentLine();
                int currentline = textBox1.GetLineFromCharIndex(firstcharindex);
                int length;
                if (textBox1.Lines.Length > 0)
                {
                    string currentlinetext = textBox1.Lines[currentline];
                    length = currentlinetext.Length;
                }
                else length = 0;
                textBox1.Select(firstcharindex, length);
            }

            textBox1.SuspendLayout();
            Color currentColor = textBox1.SelectionColor;
            var pos = textBox1.SelectionStart;
            SelectCurrentLine();
            SetSelectedColor(color);
            textBox1.DeselectAll();
            textBox1.SelectionStart = pos;
            textBox1.SelectionLength = 0;
            textBox1.SelectionColor = currentColor;
            textBox1.ResumeLayout();
        }
    }
}
