using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DesktopManager;

public partial class WindowManager
{
        /// <summary>
        /// Pastes text into the specified window using the clipboard.
        /// </summary>
        /// <param name="windowInfo">The target window.</param>
        /// <param name="text">Text to paste.</param>
        public void PasteText(WindowInfo windowInfo, string text) {
            WindowInputService.PasteText(windowInfo, text);
        }

        /// <summary>
        /// Pastes text into the specified window using the clipboard and options.
        /// </summary>
        /// <param name="windowInfo">The target window.</param>
        /// <param name="text">Text to paste.</param>
        /// <param name="options">Input options.</param>
        public void PasteText(WindowInfo windowInfo, string text, WindowInputOptions options) {
            WindowInputService.PasteText(windowInfo, text, options);
        }

        /// <summary>
        /// Types text into the specified window by simulating keyboard input.
        /// </summary>
        /// <param name="windowInfo">The target window.</param>
        /// <param name="text">Text to type.</param>
        /// <param name="delay">Delay in milliseconds between characters.</param>
        public void TypeText(WindowInfo windowInfo, string text, int delay = 0) {
            WindowInputService.TypeText(windowInfo, text, delay);
        }

        /// <summary>
        /// Types text into the specified window using input options.
        /// </summary>
        /// <param name="windowInfo">The target window.</param>
        /// <param name="text">Text to type.</param>
        /// <param name="options">Input options.</param>
        public void TypeText(WindowInfo windowInfo, string text, WindowInputOptions options) {
            WindowInputService.TypeText(windowInfo, text, options);
        }

        /// <summary>
        /// Sends keys to the specified window.
        /// </summary>
        /// <param name="windowInfo">The target window.</param>
        /// <param name="keys">Keys to send.</param>
        public void SendKeys(WindowInfo windowInfo, params VirtualKey[] keys) {
            WindowInputService.SendKeys(windowInfo, keys);
        }

        /// <summary>
        /// Sends keys to the specified window using input options.
        /// </summary>
        /// <param name="windowInfo">The target window.</param>
        /// <param name="keys">Keys to send.</param>
        /// <param name="options">Input options.</param>
        public void SendKeys(WindowInfo windowInfo, IReadOnlyList<VirtualKey> keys, WindowInputOptions options) {
            WindowInputService.SendKeys(windowInfo, keys, options);
        }

        /// <summary>
        /// Saves the current window layout to a JSON file.
        /// </summary>
        /// <param name="path">Destination path for the layout.</param>
        /// <exception cref="System.IO.IOException">Thrown when writing to the file fails.</exception>
        public void SaveLayout(string path) {
            var layout = new WindowLayout {
                Windows = GetWindows().Select(GetWindowPosition).ToList()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(layout,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var fullPath = System.IO.Path.GetFullPath(path);
            var directory = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) {
                System.IO.Directory.CreateDirectory(directory);
            }
            AtomicFileWriter.WriteAllText(fullPath, json);
        }

        /// <summary>
        /// Loads a window layout from a JSON file and applies it.
        /// </summary>
        /// <param name="path">Path to the layout file.</param>
        /// <param name="validate">Validate layout before applying.</param>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the layout file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the file is not a valid layout.</exception>
        public void LoadLayout(string path, bool validate = false) {
            if (!System.IO.File.Exists(path)) {
                throw new System.IO.FileNotFoundException("Layout file not found", path);
            }

            var json = System.IO.File.ReadAllText(path);
            WindowLayout? layout;
            try {
                layout = System.Text.Json.JsonSerializer.Deserialize<WindowLayout>(json);
            } catch (System.Text.Json.JsonException ex) {
                throw new InvalidOperationException($"Invalid layout file: {ex.Message}", ex);
            }
            if (layout == null) {
                return;
            }

            if (validate) {
                ValidateLayout(layout);
            }

            IReadOnlyList<WindowLayoutApplyResult> results = ApplyLayout(layout);
            WindowLayoutApplyResult? failed = results.FirstOrDefault(result => result.Error != null);
            if (failed != null) {
                throw new InvalidOperationException(failed.Error);
            }
        }
        private static void ValidateLayout(WindowLayout layout) {
            if (layout.Windows == null) {
                throw new InvalidDataException("Layout does not contain any windows.");
            }

            foreach (var window in layout.Windows) {
                if (string.IsNullOrWhiteSpace(window.Title)) {
                    throw new InvalidDataException("Window title is required.");
                }

                if (window.ProcessId == 0) {
                    throw new InvalidDataException($"Window '{window.Title}' has invalid process id.");
                }
            }
        }
}
