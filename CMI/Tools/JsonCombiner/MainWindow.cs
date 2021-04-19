using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Tools.JsonCombiner
{
    public partial class MainWindow : Form
    {
        private readonly string notJsonFile = "File needs to be a JSON File.";
        private readonly string notValid = "Not a Valid Json File.";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnOpenFileSystemMaster_Click(object sender, EventArgs e)
        {
            try
            {
                if (FileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                SetOrValidate(TxtPathMaster, FileDialog.FileName);
            }
            catch (IOException)
            {
                LableOutput.Text = "Master " + notJsonFile;
                LableOutput.Visible = true;
            }
        }

        private void BtnOpenFileSystemSoruce_Click(object sender, EventArgs e)
        {
            try
            {
                if (FileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                SetOrValidate(TxtPathSource, FileDialog.FileName);
            }
            catch (IOException)
            {
                LableOutput.Text = "Source " + notJsonFile;
                LableOutput.Visible = true;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!IsValueValide("Keine Master File ausgewählt.", TxtPathMaster.Text))
            {
                return;
            }

            if (!IsValueValide("Keine Source File ausgewählt.", TxtPathSource.Text))
            {
                return;
            }

            if (!IsValueValide("Keine Sprache ausgewählt.", DropBoxLanguageSource.Text))
            {
                return;
            }

            if (stringToLng(DropBoxLanguageSource.Text) <= 0)
            {
                IsValueValide("Keine Sprache ausgewählt.");
                return;
            }

            JObject obj1;
            JObject obj2;

            var fileCombiner = new JSonFileCombiner();
            try
            {
                obj1 = fileCombiner.LoadJsonFileFromPath(TxtPathMaster.Text);
                obj2 = fileCombiner.LoadJsonFileFromPath(TxtPathSource.Text);
            }
            catch (Exception ex)
            {
                outputBox.Text = ex.Message;
                IsValueValide("Not a Valid Json File.");
                return;
            }

            LableOutput.Visible = false;
            try
            {
                outputBox.Text = fileCombiner.CombineJsons(obj1, obj2, getLanguageFromInt(stringToLng(DropBoxLanguageSource.Text)));
            }
            catch (Exception ex)
            {
                IsValueValide("Diese 2 JSON dateien können nicht miteinander gemerged werden. (" + ex.Message + ")");
                TxtPathMaster.Text = "";
                TxtPathSource.Text = "";
                Console.WriteLine(ex);
            }
        }

        private bool IsValueValide(string fehlermeldung, params string[] valuesToCheck)
        {
            if (valuesToCheck.Length == 0)
            {
                LableOutput.Visible = true;
                LableOutput.Text = fehlermeldung;
                return false;
            }

            if (valuesToCheck.Any(string.IsNullOrEmpty))
            {
                LableOutput.Visible = true;
                LableOutput.Text = fehlermeldung;
                return false;
            }

            return true;
        }

        private void SetOrValidate(Control boxToSet, string pathFile)
        {
            if (!FileDialog.FileName.EndsWith(".json"))
            {
                LableOutput.Text = notJsonFile;
                LableOutput.Visible = true;
                return;
            }

            try
            {
                JObject.Parse(File.ReadAllText(pathFile));
                boxToSet.Text = pathFile;
                LableOutput.Visible = false;
            }
            catch (JsonReaderException ex)
            {
                outputBox.Text = ex.Message;
                LableOutput.Text = notValid;
                LableOutput.Visible = true;
            }
        }


        private void MainWindow_Load(object sender, EventArgs e)
        {
            DropBoxLanguageSource.Items.Add(ConfigurationManager.AppSettings.Get("dropbox.empty"));
            DropBoxLanguageSource.Items.Add(ConfigurationManager.AppSettings.Get("dropbox.en"));
            DropBoxLanguageSource.Items.Add(ConfigurationManager.AppSettings.Get("dropbox.it"));
            DropBoxLanguageSource.Items.Add(ConfigurationManager.AppSettings.Get("dropbox.fr"));
            DropBoxLanguageSource.Text = (string) DropBoxLanguageSource.Items[0];
            saveFileDialog.Filter = "JSON File | .json";
        }

        private Language getLanguageFromInt(int i)
        {
            switch (i)
            {
                case 1:
                    return Language.En;
                case 2:
                    return Language.It;
                case 3:
                    return Language.Fr;
                default:
                    throw new Exception("id " + i + " not a Language");
            }
        }

        private int stringToLng(string s)
        {
            for (var i = 0; i < DropBoxLanguageSource.Items.Count; i++)
            {
                if (DropBoxLanguageSource.Items[i].ToString().Equals(s))
                {
                    return i;
                }
            }

            return -1;
        }

        private void BtnSaveAsFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                File.WriteAllText(saveFileDialog.FileName, outputBox.Text);
                outputLink.Text = "successfully saved.";
                outputLink.Visible = true;
            }
            catch (IOException)
            {
                LableOutput.Text = notJsonFile;
                LableOutput.Visible = true;
            }
        }

        private void BtnCopyAndSelect_Click(object sender, EventArgs e)
        {
            outputBox.SelectAll();
            outputBox.Focus();
            Clipboard.SetText(outputBox.Text);
        }
    }
}