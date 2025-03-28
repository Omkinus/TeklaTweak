﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

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
            LoadSettings(); // Загрузка настроек при запуске
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
                    if (parts.Length == 4) // Проверяем, что строка содержит 4 части
                    {
                        var profileName = parts[0];
                        var profilePath = parts[1];
                        var shortcutFilesStr = parts[2].Split(',');
                        var ribbonFilesStr = parts[3].Split(',');

                        var shortcutFiles = new List<(string Name, string Path)>();
                        var ribbonFiles = new List<(string Name, string Path)>();

                        // Разбираем список файлов shortcuts
                        foreach (var file in shortcutFilesStr)
                        {
                            if (!string.IsNullOrEmpty(file))
                            {
                                var fileParts = file.Split(':');
                                if (fileParts.Length == 2)
                                    shortcutFiles.Add((fileParts[0], fileParts[1]));
                            }
                        }

                        // Разбираем список файлов ribbon
                        foreach (var file in ribbonFilesStr)
                        {
                            if (!string.IsNullOrEmpty(file))
                            {
                                var fileParts = file.Split(':');
                                if (fileParts.Length == 2)
                                    ribbonFiles.Add((fileParts[0], fileParts[1]));
                            }
                        }

                        // Создаем профиль
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

                // Преобразуем список файлов shortcuts в строку
                var shortcutFilesStr = string.Join(",", profile.ShortcutFiles.Select(f => $"{f.Name}:{f.Path}"));

                // Преобразуем список файлов ribbon в строку
                var ribbonFilesStr = string.Join(",", profile.RibbonFiles.Select(f => $"{f.Name}:{f.Path}"));

                // Формируем строку для записи в файл
                lines.Add($"{profileName}|{profile.Path}|{shortcutFilesStr}|{ribbonFilesStr}");
            }

            // Записываем все строки в файл profiles.txt
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

                // Имя файла будет всегда "KeyboardShortcuts.xml"
                string fixedFileName = "KeyboardShortcuts.xml";
                string destinationPath = Path.Combine(profile.Path, "shortcuts", fixedFileName);

                try
                {
                    // Копируем файл с новым именем
                    File.Copy(filePath, destinationPath, true);

                    // Создаем запись о файле
                    var fileEntry = (fixedFileName, destinationPath);

                    // Проверяем, существует ли уже такой файл в профиле
                    if (!profile.ShortcutFiles.Exists(f => f.Name == fixedFileName))
                    {
                        profile.ShortcutFiles.Add(fileEntry);
                        SaveProfiles();
                        UpdateProfileDetails();
                    }
                    else
                    {
                        MessageBox.Show($"A file with the name '{fixedFileName}' already exists and has been replaced.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
                Filter = "Ribbon Configuration Files (*.xml)|*.xml|All files (*.*)|*.*",
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

            // Проверяем, что выбраны пути для сохранения
            if (string.IsNullOrEmpty(shortcutSavePath) || string.IsNullOrEmpty(ribbonSavePath))
            {
                MessageBox.Show("Please select save paths for both shortcuts and ribbon configurations.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Импортируем файлы клавиатурных сочетаний
            if (profile.ShortcutFiles.Any())
            {
                foreach (var file in profile.ShortcutFiles)
                {
                    try
                    {
                        string destinationPath = Path.Combine(shortcutSavePath, Path.GetFileName(file.Path));
                        File.Copy(file.Path, destinationPath, true); // true означает замену файла, если он уже существует
                        MessageBox.Show($"Shortcut file imported: {destinationPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing shortcut file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // Импортируем файлы конфигурации ленты
            if (profile.RibbonFiles.Any())
            {
                foreach (var file in profile.RibbonFiles)
                {
                    try
                    {
                        string destinationPath = Path.Combine(ribbonSavePath, Path.GetFileName(file.Path));
                        File.Copy(file.Path, destinationPath, true); // true означает замену файла, если он уже существует
                        MessageBox.Show($"Ribbon configuration file imported: {destinationPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing ribbon file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // Если нет файлов для импорта
            if (!profile.ShortcutFiles.Any() && !profile.RibbonFiles.Any())
            {
                MessageBox.Show("No files to import for the selected profile.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
                SaveSettings(); // Сохраняем настройки
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
                SaveSettings(); // Сохраняем настройки
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

        // Метод сохранения настроек
        private void SaveSettings()
        {
            var settings = new List<string>
            {
                $"ShortcutSavePath={shortcutSavePath}",
                $"RibbonSavePath={ribbonSavePath}"
            };

            File.WriteAllLines(Path.Combine(appPath, "settings.txt"), settings);
        }

        // Метод загрузки настроек
        private void LoadSettings()
        {
            var settingsPath = Path.Combine(appPath, "settings.txt");
            if (File.Exists(settingsPath))
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        if (parts[0] == "ShortcutSavePath")
                        {
                            shortcutSavePath = parts[1];
                            shortcutPathTextBlock.Text = $"Selected Path: {shortcutSavePath}";
                        }
                        else if (parts[0] == "RibbonSavePath")
                        {
                            ribbonSavePath = parts[1];
                            ribbonPathTextBlock.Text = $"Selected Path: {ribbonSavePath}";
                        }
                    }
                }
            }
        }
    }
}