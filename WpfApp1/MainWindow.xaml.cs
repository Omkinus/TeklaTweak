using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf; // Для VistaFolderBrowserDialog

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly string appPath = System.AppDomain.CurrentDomain.BaseDirectory;
        private readonly List<string> shortcutFiles = new List<string>();
        private readonly List<string> ribbonFiles = new List<string>();

        // Переменные для хранения выбранных путей
        private string shortcutSavePath = string.Empty;
        private string ribbonSavePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            // Загрузка ранее сохраненных файлов при запуске
            LoadSavedFiles();
            UpdateComboBoxes();

            this.MouseDown += delegate { DragMove(); };
        }

        // Метод загрузки ранее сохраненных файлов
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

        // Метод загрузки файла клавиатурных сочетаний
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

                if (!string.IsNullOrEmpty(shortcutSavePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destinationPath = Path.Combine(shortcutSavePath, fileName);

                    // Копируем файл в выбранную папку
                    File.Copy(filePath, destinationPath, true);

                    // Сохраняем путь относительно программы
                    string relativePath = Path.Combine("keyboard_shortcuts", fileName);
                    if (!shortcutFiles.Contains(relativePath))
                    {
                        shortcutFiles.Add(relativePath);
                    }

                    SaveLoadedFiles(shortcutFiles, "shortcut_files.txt");
                    UpdateComboBoxes();
                }
                else
                {
                    MessageBox.Show("Please select a save path for shortcuts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Метод загрузки файла конфигурации ленты
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

                if (!string.IsNullOrEmpty(ribbonSavePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destinationPath = Path.Combine(ribbonSavePath, fileName);

                    // Копируем файл в выбранную папку
                    File.Copy(filePath, destinationPath, true);

                    // Сохраняем путь относительно программы
                    string relativePath = Path.Combine("ribbon_configurations", fileName);
                    if (!ribbonFiles.Contains(relativePath))
                    {
                        ribbonFiles.Add(relativePath);
                    }

                    SaveLoadedFiles(ribbonFiles, "ribbon_files.txt");
                    UpdateComboBoxes();
                }
                else
                {
                    MessageBox.Show("Please select a save path for ribbon configurations first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Метод сохранения списка файлов
        private void SaveLoadedFiles(List<string> fileList, string saveFileName)
        {
            File.WriteAllLines(Path.Combine(appPath, saveFileName), fileList);
        }

        // Метод обновления ComboBox
        private void UpdateComboBoxes()
        {
            comboBoxShortcuts.ItemsSource = null;
            comboBoxShortcuts.ItemsSource = shortcutFiles;

            comboBoxRibbon.ItemsSource = null;
            comboBoxRibbon.ItemsSource = ribbonFiles;
        }

        // Метод удаления выбранного файла из списка клавиатурных сочетаний
        private void RemoveShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxShortcuts.SelectedItem != null)
            {
                string selectedFile = comboBoxShortcuts.SelectedItem.ToString();
                RemoveFile(selectedFile, shortcutFiles, "shortcut_files.txt", "keyboard_shortcuts");
                UpdateComboBoxes();
            }
        }

        // Метод удаления выбранного файла из списка конфигураций ленты
        private void RemoveRibbon_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxRibbon.SelectedItem != null)
            {
                string selectedFile = comboBoxRibbon.SelectedItem.ToString();
                RemoveFile(selectedFile, ribbonFiles, "ribbon_files.txt", "ribbon_configurations");
                UpdateComboBoxes();
            }
        }

        // Метод удаления файла
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

        // Метод выбора пути сохранения для клавиатурных сочетаний
        private void SelectShortcutPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                shortcutSavePath = dialog.SelectedPath;
                shortcutPathTextBlock.Text = $"Selected Path: {shortcutSavePath}";
            }
        }

        // Метод выбора пути сохранения для конфигурации ленты
        private void SelectRibbonPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                ribbonSavePath = dialog.SelectedPath;
                ribbonPathTextBlock.Text = $"Selected Path: {ribbonSavePath}";
            }
        }

        // Обработчики событий изменения выбора в ComboBox
        private void ComboBoxShortcuts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику для выбора файла
        }

        private void ComboBoxRibbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику для выбора файла
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ImportShortcuts_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("Importing shortcuts...");
        }

        private void ImportRibbon_Click(object sender, RoutedEventArgs e)
        {
            Tekla.Structures.Model.Model model = new Tekla.Structures.Model.Model();

            model.CommitChanges();
            //builder.InvokeCommand("CommandRepository", "KeyboardShortcut.Customize");
            //builder.wpf.View("KeyboardShortcut.CustomizePage").Find("AID_KS_ImportButton").As.Button.Invoke();
            //builder.wpf.View("KeyboardShortcut.CustomizePage").Find("AID_KS_CloseButton").As.Button.Invoke();


        }
    }
}