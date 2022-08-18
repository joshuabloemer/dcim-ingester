using System.Windows;
using System.Windows.Controls;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using System;
using System.Linq;

namespace DcimIngester.Windows
{
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxDestination.Text = Properties.Settings.Default.Destination;
            ComboBoxSubfolders.SelectedIndex = Properties.Settings.Default.Subfolders;
            // Initialize first Rule
            Rule Rule0 = new Rule(0);
            // Generate_Ui(Rule0);

        }

        private void TextBoxDestination_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ButtonBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxDestination.Text = folderDialog.SelectedPath;
        }

        private void ComboBoxSubfolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ValidateFields()
        {
            if ((TextBoxDestination.Text.Length > 0 &&
                TextBoxDestination.Text != Properties.Settings.Default.Destination) ||
                ComboBoxSubfolders.SelectedIndex != Properties.Settings.Default.Subfolders)
            {
                ButtonSave.IsEnabled = true;
            }
            else ButtonSave.IsEnabled = false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Destination = TextBoxDestination.Text;
            Properties.Settings.Default.Subfolders = ComboBoxSubfolders.SelectedIndex;
            Properties.Settings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Generate_Ui(Rule initial_rule)
        {
            Console.WriteLine("generating");

        }

        // private void Rule1_TextChanged(object sender, TextChangedEventArgs e)
        // {
        //     TextBox textBox = sender as TextBox ?? throw new Exception("TextChanged was called without a TextBox, this should not happen");
        //     Console.WriteLine(textBox.Text);
        //     Console.WriteLine(textBox.Text.Length);
        //     Console.WriteLine(textBox.Text.GetType());
        //     if (textBox.Text.Length > 0) 
        //     {
        //         // make sure all empty fields are present
        //         // create if not
        //         string indentlevel = textBox.Name.Substring(4);
        //         string[] levels = indentlevel.Split("_");
        //         // Console.WriteLine(indentlevel);
        //         // // Console.WriteLine(levels[0]);
        //         // // Console.WriteLine(levels[1]);
        //         // var under = this.FindName("Rule" + String.Join("_",levels[0..^2]) +"_"+ levels[^2] +"_"+ (Int32.Parse(levels[^1])+1));
        //         // var indent = this.FindName(textBox.Name+"1_1");
        //         // var next = this.FindName("Rule" + (Int32.Parse(levels[^1])+1) +"_"+ 1);
        //         // //levels[^2] +"_"+ (Int32.Parse(levels[^1])+1))

        //         // Console.WriteLine("Rule" + String.Join("_",levels[0..^1]) +"_"+ (Int32.Parse(levels[^1])+1));
        //         // Console.WriteLine(textBox.Name+"_1_1");
        //         // // TODO: fix issue where extra underscore is placed before  
        //         // Console.WriteLine("Rule" + String.Join("_",new string[]{String.Join("_",levels[0..^2]),(Int32.Parse(levels[^2])+1) +"_1"}.Where(s => !string.IsNullOrEmpty(s))));


        //         // // create textbox
        //         // if (under == null)
        //         // {
        //         //     Console.WriteLine("null");
        //         // }
        //         // Console.WriteLine(under);

        //         // Console.WriteLine(textBox.Name.Substring(4));

        //     }
        // }
    }
}
