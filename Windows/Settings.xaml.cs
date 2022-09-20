using System.Windows;
using System.Windows.Controls;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using System;
using System.Linq;
using DcimIngester.Rules;
using System.IO;
using System.Collections.Generic;

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
            TextBoxRules.Text = Properties.Settings.Default.Rules;

        }

        private void TextBoxDestination_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void TextBoxRules_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ButtonBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxDestination.Text = folderDialog.SelectedPath;
        }

        private void ButtonBrowseRules_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxRules.Text = fileDialog.FileName;
        }

        private void ValidateFields()
        {
            if ((TextBoxDestination.Text.Length > 0 &&
                TextBoxDestination.Text != Properties.Settings.Default.Destination) ||
                TextBoxRules.Text.Length > 0 &&
                TextBoxRules.Text != Properties.Settings.Default.Rules)
            {
                ButtonSave.IsEnabled = true;
            }
            else ButtonSave.IsEnabled = false;
        }


        private void ButtonReloadFileTree_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Rules != ""){
                var parser = new Parser();
                var tree = parser.Parse(File.ReadAllText(Properties.Settings.Default.Rules));
                var evaluator = new FileTreeEvaluator();
                List<String> paths = (List<String>)evaluator.Evaluate(tree.Block);

                PopulateTreeView(FileTreeView, paths, '/');
            }
        }

        private void PopulateTreeView(TreeView treeView, IEnumerable<string> paths, char pathSeparator) {
            List<MyTreeViewItem> sourceCollection = new List<MyTreeViewItem>();
            foreach (string path in paths) {
                string[] fileItems = path.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (fileItems.Any()) {

                MyTreeViewItem root = sourceCollection.FirstOrDefault(x=>x.Name.Equals(fileItems[0]) && x.Level.Equals(1));
                if (root == null) {
                    root = new MyTreeViewItem()
                    {
                    Level = 1,
                    Name = fileItems[0],
                    SubItems = new List<MyTreeViewItem>()
                    };
                    sourceCollection.Add(root);
                }

                if (fileItems.Length > 1) {

                    MyTreeViewItem parentItem = root;
                    int level = 2;
                    for (int i = 1; i < fileItems.Length; ++i) {

                    MyTreeViewItem subItem = parentItem.SubItems.FirstOrDefault(x => x.Name.Equals(fileItems[i]) && x.Level.Equals(level));
                    if (subItem == null) {
                        subItem = new MyTreeViewItem()
                        {
                        Name = fileItems[i],
                        Level = level,
                        SubItems = new List<MyTreeViewItem>()
                        };
                        parentItem.SubItems.Add(subItem);
                    }

                    parentItem = subItem;
                    level++;
                    }
                }
                }
            }

            treeView.ItemsSource = sourceCollection;
            }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Destination = TextBoxDestination.Text;
            Properties.Settings.Default.Rules = TextBoxRules.Text;

            Properties.Settings.Default.Save();
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
    
    public class MyTreeViewItem
    {
        public int Level {
            get;
            set;
        }

        public string Name {
            get;
            set;
        }

        public List<MyTreeViewItem> SubItems {
            get;
            set;
        }
    }
}
