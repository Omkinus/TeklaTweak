using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace teklatweak
{
    public partial class MainWindow : Window
    {
        private readonly string appPath = System.AppDomain.CurrentDomain.BaseDirectory;
        private readonly List<string> shortcutFiles = new List<string>();
        private readonly List<string> ribbonFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            // Загрузка ранее сохраненных файлов при запуске
            LoadSavedFiles();
            UpdateComboBoxes();
        }

        private void LoadSavedFiles()
        {
            if (File.Exists(Path.Combine(appPath, "shortcut_files.txt")))
            {
                shortcutFiles.AddRange(File.ReadAllLines(Path.Combine(appPath, "shortcut_files.txt")));
            }

            if (File.Exists(Path.Combine(appPath, "ribbon_files.txt")))
            {
                ribbonFiles.AddRange(File.ReadAllLines(Path.Combine(appPath, "ribbon_files.txt")));
            }
        }

        private void LoadKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Keyboard Shortcut Files (*.xml)|*.xml|All files (*.*)|*.*",
                Title = "Select Keyboard Shortcuts File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                CopyFileToAppDirectory(filePath, "keyboard_shortcuts", shortcutFiles, "shortcut_files.txt");
                UpdateComboBoxes();
            }
        }

        private void LoadRibbon_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Ribbon Configuration Files (*.config)|*.config|All files (*.*)|*.*",
                Title = "Select Ribbon Configuration File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                CopyFileToAppDirectory(filePath, "ribbon_configurations", ribbonFiles, "ribbon_files.txt");
                UpdateComboBoxes();
            }
        }

        private void CopyFileToAppDirectory(string sourcePath, string folderName, List<string> fileList, string saveFileName)
        {
            string fileName = Path.GetFileName(sourcePath);
            string destinationPath = Path.Combine(appPath, folderName, fileName);

            // Создаем папку если её нет
            Directory.CreateDirectory(Path.Combine(appPath, folderName));

            // Копируем файл в директорию программы
            File.Copy(sourcePath, destinationPath, true);

            // Сохраняем путь относительно программы
            string relativePath = Path.Combine(folderName, fileName);
            if (!fileList.Contains(relativePath))
            {
                fileList.Add(relativePath);
            }

            SaveLoadedFiles(fileList, saveFileName);
        }

        private void SaveLoadedFiles(List<string> fileList, string saveFileName)
        {
            File.WriteAllLines(Path.Combine(appPath, saveFileName), fileList);
        }

        private void UpdateComboBoxes()
        {
            comboBoxShortcuts.ItemsSource = null;
            comboBoxShortcuts.ItemsSource = shortcutFiles;

            comboBoxRibbon.ItemsSource = null;
            comboBoxRibbon.ItemsSource = ribbonFiles;
        }

        private void RemoveShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxShortcuts.SelectedItem != null)
            {
                string selectedFile = comboBoxShortcuts.SelectedItem.ToString();
                RemoveFile(selectedFile, shortcutFiles, "shortcut_files.txt", "keyboard_shortcuts");
                UpdateComboBoxes();
            }
        }

        private void RemoveRibbon_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxRibbon.SelectedItem != null)
            {
                string selectedFile = comboBoxRibbon.SelectedItem.ToString();
                RemoveFile(selectedFile, ribbonFiles, "ribbon_files.txt", "ribbon_configurations");
                UpdateComboBoxes();
            }
        }

        private void RemoveFile(string filePath, List<string> fileList, string saveFileName, string folderName)
        {
            // Удаляем файл из списка
            fileList.Remove(filePath);

            // Удаляем файл из папки программы
            string fullPath = Path.Combine(appPath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Сохраняем обновленный список
            SaveLoadedFiles(fileList, saveFileName);
        }

        private void ComboBoxShortcuts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику для выбора файла
        }

        private void ComboBoxRibbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику для выбора файла
        }
    }
}