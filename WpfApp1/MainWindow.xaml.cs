using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf; // Для VistaFolderBrowserDialog
using System.Text;
using System.Linq;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly string appPath = System.AppDomain.CurrentDomain.BaseDirectory;
        private readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile>(); // Хранилище профилей
        private string currentProfileName = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            LoadProfiles(); // Загрузка профилей при запуске
            UpdateComboBoxProfiles(); // Обновление комбобокса профилей
            this.MouseDown += delegate { DragMove(); };
        }

        // Класс для хранения информации о профиле
        private class Profile
        {
            public string ShortcutSavePath { get; set; } = string.Empty;
            public string RibbonSavePath { get; set; } = string.Empty;
            public List<(string Name, string Path)> ShortcutFiles { get; set; } = new List<(string Name, string Path)>();
            public List<(string Name, string Path)> RibbonFiles { get; set; } = new List<(string Name, string Path)>();
        }

        // Метод загрузки профилей из файла
        private void LoadProfiles()
        {
            if (File.Exists(Path.Combine(appPath, "profiles.txt")))
            {
                var lines = File.ReadAllLines(Path.Combine(appPath, "profiles.txt"));
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 5)
                    {
                        var profileName = parts[0];
                        var shortcutPath = parts[1];
                        var ribbonPath = parts[2];
                        var shortcutFilesStr = parts[3].Split(',');
                        var ribbonFilesStr = parts[4].Split(',');

                        var shortcutFiles = new List<(string Name, string Path)>();
                        var ribbonFiles = new List<(string Name, string Path)>();

                        foreach (var file in shortcutFilesStr)
                        {
                            if (!string.IsNullOrEmpty(file))
                            {
                                var fileParts = file.Split(':');
                                if (fileParts.Length == 2)
                                    shortcutFiles.Add((fileParts[0], fileParts[1]));
                            }
                        }

                        foreach (var file in ribbonFilesStr)
                        {
                            if (!string.IsNullOrEmpty(file))
                            {
                                var fileParts = file.Split(':');
                                if (fileParts.Length == 2)
                                    ribbonFiles.Add((fileParts[0], fileParts[1]));
                            }
                        }

                        profiles[profileName] = new Profile
                        {
                            ShortcutSavePath = shortcutPath,
                            RibbonSavePath = ribbonPath,
                            ShortcutFiles = shortcutFiles,
                            RibbonFiles = ribbonFiles
                        };
                    }
                }
            }
        }

        // Метод сохранения профилей в файл
        private void SaveProfiles()
        {
            var lines = new List<string>();
            foreach (var kvp in profiles)
            {
                var profileName = kvp.Key;
                var profile = kvp.Value;

                var shortcutFilesStr = string.Join(",", profile.ShortcutFiles.Select(f => $"{f.Name}:{f.Path}"));
                var ribbonFilesStr = string.Join(",", profile.RibbonFiles.Select(f => $"{f.Name}:{f.Path}"));

                lines.Add($"{profileName}|{profile.ShortcutSavePath}|{profile.RibbonSavePath}|{shortcutFilesStr}|{ribbonFilesStr}");
            }

            File.WriteAllLines(Path.Combine(appPath, "profiles.txt"), lines);
        }

        // Метод обновления комбобокса профилей
        private void UpdateComboBoxProfiles()
        {
            comboBoxProfiles.ItemsSource = null;
            comboBoxProfiles.ItemsSource = profiles.Keys.ToList();
            if (profiles.Count > 0)
            {
                comboBoxProfiles.SelectedItem = profiles.Keys.First();
                currentProfileName = comboBoxProfiles.SelectedItem.ToString();
            }
        }

        // Метод создания нового профиля
        private void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Enter a name for the new profile:", "New Profile");
            if (dialog.ShowDialog() == true)
            {
                var profileName = dialog.Result;
                if (string.IsNullOrEmpty(profileName))
                {
                    MessageBox.Show("Profile name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (profiles.ContainsKey(profileName))
                {
                    MessageBox.Show($"A profile with the name '{profileName}' already exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                profiles[profileName] = new Profile();
                SaveProfiles();
                UpdateComboBoxProfiles();
                comboBoxProfiles.SelectedItem = profileName;
                currentProfileName = profileName;
            }
        }

        // Метод выбора пути сохранения для клавиатурных сочетаний
        private void SelectShortcutPath_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentProfileName))
            {
                MessageBox.Show("Please select or create a profile first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                profiles[currentProfileName].ShortcutSavePath = dialog.SelectedPath;
                SaveProfiles();
                UpdatePathsTextBlock();
            }
        }

        // Метод выбора пути сохранения для конфигурации ленты
        private void SelectRibbonPath_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentProfileName))
            {
                MessageBox.Show("Please select or create a profile first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                profiles[currentProfileName].RibbonSavePath = dialog.SelectedPath;
                SaveProfiles();
                UpdatePathsTextBlock();
            }
        }

        // Метод обновления текстового блока с путями
        private void UpdatePathsTextBlock()
        {
            if (string.IsNullOrEmpty(currentProfileName)) return;

            var profile = profiles[currentProfileName];
            shortcutPathTextBlock.Text = $"Shortcut Path: {profile.ShortcutSavePath}";
            ribbonPathTextBlock.Text = $"Ribbon Path: {profile.RibbonSavePath}";
        }

        // Метод загрузки файла клавиатурных сочетаний
        private void LoadKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentProfileName))
            {
                MessageBox.Show("Please select or create a profile first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Keyboard Shortcut Files (*.xml)|*.xml|All files (*.*)|*.*",
                Title = "Select Keyboard Shortcuts File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                var profile = profiles[currentProfileName];

                if (string.IsNullOrEmpty(profile.ShortcutSavePath))
                {
                    MessageBox.Show("Please select a save path for shortcuts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fileName = Path.GetFileName(filePath);
                var customName = RequestFileName("Enter a name for the shortcut file:");

                if (string.IsNullOrEmpty(customName))
                {
                    MessageBox.Show("File name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var destinationPath = Path.Combine(profile.ShortcutSavePath, fileName);
                File.Copy(filePath, destinationPath, true);

                var fileEntry = (customName, Path.Combine(profile.ShortcutSavePath, fileName));
                if (!profile.ShortcutFiles.Exists(f => f.Name == customName))
                {
                    profile.ShortcutFiles.Add(fileEntry);
                    SaveProfiles();
                }
                else
                {
                    MessageBox.Show($"A file with the name '{customName}' already exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Метод загрузки файла конфигурации ленты
        private void LoadRibbon_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentProfileName))
            {
                MessageBox.Show("Please select or create a profile first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Ribbon Configuration Files (*.config)|*.config|All files (*.*)|*.*",
                Title = "Select Ribbon Configuration File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                var profile = profiles[currentProfileName];

                if (string.IsNullOrEmpty(profile.RibbonSavePath))
                {
                    MessageBox.Show("Please select a save path for ribbon configurations first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fileName = Path.GetFileName(filePath);
                var customName = RequestFileName("Enter a name for the ribbon configuration file:");

                if (string.IsNullOrEmpty(customName))
                {
                    MessageBox.Show("File name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var destinationPath = Path.Combine(profile.RibbonSavePath, fileName);
                File.Copy(filePath, destinationPath, true);

                var fileEntry = (customName, Path.Combine(profile.RibbonSavePath, fileName));
                if (!profile.RibbonFiles.Exists(f => f.Name == customName))
                {
                    profile.RibbonFiles.Add(fileEntry);
                    SaveProfiles();
                }
                else
                {
                    MessageBox.Show($"A file with the name '{customName}' already exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Метод запроса имени файла у пользователя
        private string RequestFileName(string message)
        {
            var dialog = new InputDialog(message, "File Name");
            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }
            return null;
        }

        // Метод импорта файлов
        private void ImportFiles_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentProfileName))
            {
                MessageBox.Show("Please select or create a profile first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var profile = profiles[currentProfileName];

            if (profile.ShortcutFiles.Any())
            {
                foreach (var file in profile.ShortcutFiles)
                {
                    MessageBox.Show($"Importing shortcut file: {file.Path}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("No shortcut files to import.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (profile.RibbonFiles.Any())
            {
                foreach (var file in profile.RibbonFiles)
                {
                    MessageBox.Show($"Importing ribbon configuration file: {file.Path}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("No ribbon configuration files to import.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Обработчик события изменения выбора в ComboBox профилей
        private void ComboBoxProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxProfiles.SelectedItem != null)
            {
                currentProfileName = comboBoxProfiles.SelectedItem.ToString();
                UpdatePathsTextBlock();
            }
        }
    }

    // Простой диалог для ввода текста
    public class InputDialog : Window
    {
        private readonly TextBox _inputTextBox;

        public InputDialog(string message, string title)
        {
            Title = title;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(new TextBlock { Text = message });

            _inputTextBox = new TextBox { Margin = new Thickness(0, 5, 0, 0) };
            stackPanel.Children.Add(_inputTextBox);

            var okButton = new Button
            {
                Content = "OK",
                IsDefault = true,
                Margin = new Thickness(0, 10, 0, 0)
            };
            okButton.Click += (s, e) => DialogResult = true;

            stackPanel.Children.Add(okButton);
            Content = stackPanel;
        }

        public string Result => _inputTextBox.Text;
    }
}