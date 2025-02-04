using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf; // Для VistaFolderBrowserDialog

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly string appPath = System.AppDomain.CurrentDomain.BaseDirectory;
        private readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile>();
        private string currentProfileName = string.Empty;

        private string shortcutSavePath = string.Empty;
        private string ribbonSavePath = string.Empty;
        private readonly List<(string Name, string Path)> shortcutFiles = new List<(string Name, string Path)>();
        private readonly List<(string Name, string Path)> ribbonFiles = new List<(string Name, string Path)>();
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
            public string Name { get; set; }
            public string Path { get; set; } = string.Empty; // Путь к папке профиля
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
                        var profilePath = parts[1];
                        var shortcutFilesStr = parts[2].Split(',');
                        var ribbonFilesStr = parts[3].Split(',');

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
                            Name = profileName,
                            Path = profilePath,
                            ShortcutFiles = shortcutFiles,
                            RibbonFiles = ribbonFiles
                        };
                    }
                }
            }
        }

        // Метод обновления ComboBox профилей
        private void UpdateComboBoxProfiles()
        {
            comboBoxProfiles.ItemsSource = null;
            comboBoxProfiles.ItemsSource = profiles.Keys.ToList();
            if (profiles.Count > 0)
            {
                comboBoxProfiles.SelectedItem = profiles.Keys.First();
                currentProfileName = comboBoxProfiles.SelectedItem.ToString();
                UpdateProfileDetails();
            }
        }

        // Метод обновления деталей выбранного профиля
        private void UpdateProfileDetails()
        {
            if (string.IsNullOrEmpty(currentProfileName)) return;

            var profile = profiles[currentProfileName];

            checkboxShortcutLoaded.IsChecked = profile.ShortcutFiles.Any();
            checkboxRibbonLoaded.IsChecked = profile.RibbonFiles.Any();
        }

        // Метод создания нового профиля
        private void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileName = textBoxNewProfileName.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (profiles.ContainsKey(profileName))
            {
                MessageBox.Show($"A profile with the name '{profileName}' already exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем папку для профиля
            var profilePath = Path.Combine(appPath, "Profiles", profileName);
            Directory.CreateDirectory(profilePath);

            // Создаем подпапки для shortcuts и ribbon
            Directory.CreateDirectory(Path.Combine(profilePath, "shortcuts"));
            Directory.CreateDirectory(Path.Combine(profilePath, "ribbon"));

            profiles[profileName] = new Profile
            {
                Name = profileName,
                Path = profilePath
            };

            SaveProfiles();
            UpdateComboBoxProfiles();
            comboBoxProfiles.SelectedItem = profileName;
            currentProfileName = profileName;
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

                lines.Add($"{profileName}|{profile.Path}|{shortcutFilesStr}|{ribbonFilesStr}");
            }

            File.WriteAllLines(Path.Combine(appPath, "profiles.txt"), lines);
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
                string filePath = openFileDialog.FileName;
                var profile = profiles[currentProfileName];

                string fileName = Path.GetFileName(filePath);
                string destinationPath = Path.Combine(profile.Path, "shortcuts", fileName);

                try
                {
                    File.Copy(filePath, destinationPath, true);
                    var fileEntry = (fileName, destinationPath);

                    if (!profile.ShortcutFiles.Exists(f => f.Name == fileName))
                    {
                        profile.ShortcutFiles.Add(fileEntry);
                        SaveProfiles();
                        UpdateProfileDetails();
                    }
                    else
                    {
                        MessageBox.Show($"A file with the name '{fileName}' already exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading shortcut file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                string filePath = openFileDialog.FileName;
                var profile = profiles[currentProfileName];

                string fileName = Path.GetFileName(filePath);
                string destinationPath = Path.Combine(profile.Path, "ribbon", fileName);

                try
                {
                    File.Copy(filePath, destinationPath, true);
                    var fileEntry = (fileName, destinationPath);

                    if (!profile.RibbonFiles.Exists(f => f.Name == fileName))
                    {
                        profile.RibbonFiles.Add(fileEntry);
                        SaveProfiles();
                        UpdateProfileDetails();
                    }
                    else
                    {
                        MessageBox.Show($"A file with the name '{fileName}' already exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading ribbon file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

            if (profile.RibbonFiles.Any())
            {
                foreach (var file in profile.RibbonFiles)
                {
                    MessageBox.Show($"Importing ribbon configuration file: {file.Path}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Обработчик события изменения выбора в ComboBox профилей
        private void ComboBoxProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxProfiles.SelectedItem != null)
            {
                currentProfileName = comboBoxProfiles.SelectedItem.ToString();
                UpdateProfileDetails();
            }
        }

        // Метод выбора пути сохранения для клавиатурных сочетаний
        private void SelectShortcutPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog(); // Диалог выбора папки
            if (dialog.ShowDialog() == true) // Если пользователь выбрал папку
            {
                shortcutSavePath = dialog.SelectedPath; // Сохраняем выбранный путь
                shortcutPathTextBlock.Text = $"Selected Path: {shortcutSavePath}"; // Обновляем текст в интерфейсе
            }
        }

        // Метод выбора пути сохранения для конфигурации ленты
        private void SelectRibbonPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog(); // Диалог выбора папки
            if (dialog.ShowDialog() == true) // Если пользователь выбрал папку
            {
                ribbonSavePath = dialog.SelectedPath; // Сохраняем выбранный путь
                ribbonPathTextBlock.Text = $"Selected Path: {ribbonSavePath}"; // Обновляем текст в интерфейсе
            }
        }

        // Метод закрытия программы
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрываем текущее окно
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxProfiles.SelectedItem == null)
            {
                MessageBox.Show("Please select a profile to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var profileName = comboBoxProfiles.SelectedItem.ToString();

            // Подтверждение удаления
            var result = MessageBox.Show($"Are you sure you want to delete the profile '{profileName}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Удаление записи профиля из словаря
            if (profiles.ContainsKey(profileName))
            {
                profiles.Remove(profileName);

                // Удаление папки профиля
                var profilePath = Path.Combine(appPath, "Profiles", profileName);
                if (Directory.Exists(profilePath))
                {
                    Directory.Delete(profilePath, true); // Рекурсивное удаление папки
                }

                // Сохранение обновленных профилей
                SaveProfiles();
                UpdateComboBoxProfiles();

                MessageBox.Show($"Profile '{profileName}' has been deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Profile '{profileName}' does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}