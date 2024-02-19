using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FfmpegGuiApp
{
    public partial class MainForm : Form
    {
        private string? ffmpegPath;
        private string? inputFilePath;

        public MainForm()
        {
            InitializeComponent();
            InitializeComboBox();
            LoadFFmpegPath();
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        private void InitializeComboBox()
        {
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.Items.AddRange(new object[] { "MP4", "MOV", "OGV", "FLV", "AVI", "WMV", "MKV", "WEBM", "MPG", "MPEG", "M4V" });
            comboBox1.SelectedIndex = 0;
        }

        private void LoadFFmpegPath()
        {
            ffmpegPath = Properties.Settings.Default.FFmpegPath;
        }

        private void SaveFFmpegPath(string path)
        {
            Properties.Settings.Default.FFmpegPath = path;
            Properties.Settings.Default.Save();
        }

        private void button1_Click(object? sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video Files (*.mp4; *.mov; *.ogv; *.flv; *.avi; *.wmv; *.mkv; *.webm; *.mpg; *.mpeg; *.m4v)|*.mp4; *.mov; *.ogv; *.flv; *.avi; *.wmv; *.mkv; *.webm; *.mpg; *.mpeg; *.m4v";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                inputFilePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(inputFilePath);
                label2.Visible = true;
                label2.Text = fileName;
            }

            if (label2.Text.Length > 31)
            {
                label2.Text = label2.Text.Substring(0, 28) + "...";
            }
        }

        private async void button2_Click(object? sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            comboBox1.Enabled = false;
            fileToolStripMenuItem2.Enabled = false;
            editToolStripMenuItem.Enabled = false;

            try
            {
                if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
                {
                    MessageBox.Show("Please set the path to ffmpeg.exe first. Go to Edit > Set FFmpeg Tool to set it up.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (string.IsNullOrEmpty(inputFilePath) || !File.Exists(inputFilePath))
                {
                    MessageBox.Show("Please select a video file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string format = comboBox1.SelectedItem?.ToString().ToLower() ?? "";
                if (string.IsNullOrEmpty(format))
                {
                    MessageBox.Show("Please select an output format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath) ?? "", Path.GetFileNameWithoutExtension(inputFilePath) + "_output." + format);
                string arguments = $"-i \"{inputFilePath}\" \"{outputFilePath}\"";
                if (!File.Exists(outputFilePath))
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        ErrorDialog = false
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = processStartInfo;
                        process.EnableRaisingEvents = true;

                        process.OutputDataReceived += (s, evt) =>
                        {
                            if (!string.IsNullOrWhiteSpace(evt.Data))
                            {
                                string data = evt.Data;
                                int index = data.IndexOf("fps=", StringComparison.Ordinal);
                                if (index != -1)
                                {
                                    string fpsSubstring = data.Substring(index + 4, 2);
                                    int fps;
                                    if (int.TryParse(fpsSubstring, out fps))
                                    {
                                        int progress = (int)Math.Min((fps / 60.0) * 100, 100);
                                        UpdateProgressBar(progress);
                                    }
                                }
                            }
                        };
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        await Task.Run(() => process.WaitForExit());
                        if (File.Exists(outputFilePath))
                        {
                            MessageBox.Show("Conversion completed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Conversion failed. Output file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"Output already exists", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                button1.Enabled = true;
                button2.Enabled = true;
                comboBox1.Enabled = true;
                fileToolStripMenuItem2.Enabled = true;
                editToolStripMenuItem.Enabled = true;
            }
        }


        private void UpdateProgressBar(int progress)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action<int>(UpdateProgressBar), progress);
            }
            else
            {
                progressBar1.Value = progress;
            }
        }

        private void openToolStripMenuItem1_Click(object? sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void exitToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void setFFMPEGToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "ffmpeg.exe|ffmpeg.exe";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ffmpegPath = openFileDialog.FileName;
                SaveFFmpegPath(ffmpegPath);
            }
        }
        private void comboBox1_SelectedIndexChanged(object? sender, EventArgs e)
        {
        }
    }
}
