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

            var current = GetWindows();
            foreach (var target in layout.Windows) {
                // Try to find window with exact match first
                var window = current.FirstOrDefault(w => w.ProcessId == target.ProcessId && w.Title == target.Title);
                
                // If not found, try fallback matching by title only (in case ProcessId changed)
                if (window == null) {
                    window = current.FirstOrDefault(w => w.Title == target.Title);
                }
                
                // If still not found, try by process ID only (in case title changed slightly)
                if (window == null) {
                    window = current.FirstOrDefault(w => w.ProcessId == target.ProcessId);
                }
                
                if (window != null) {
                    // Debug info for layout restoration
                    System.Diagnostics.Debug.WriteLine($"Restoring window: Handle={window.Handle:X8}, PID={window.ProcessId}, Title='{window.Title}'");
                    System.Diagnostics.Debug.WriteLine($"Target position: Left={target.Left}, Top={target.Top}, Width={target.Width}, Height={target.Height}");
                    
                    // restore window to allow repositioning
                    RestoreWindow(window);
                    
                    // Verify window can be repositioned before trying
                    var beforePosition = GetWindowPosition(window);
                    System.Diagnostics.Debug.WriteLine($"Before SetWindowPosition: Left={beforePosition.Left}, Top={beforePosition.Top}");
                    
                    SetWindowPosition(window, target.Left, target.Top, target.Width, target.Height);
                    
                    // Verify the position was actually set
                    var afterPosition = GetWindowPosition(window);
                    System.Diagnostics.Debug.WriteLine($"After SetWindowPosition: Left={afterPosition.Left}, Top={afterPosition.Top}");
                    
                    if (afterPosition.Left != target.Left || afterPosition.Top != target.Top) {
                        System.Diagnostics.Debug.WriteLine($"WARNING: Position setting failed! Expected ({target.Left}, {target.Top}), got ({afterPosition.Left}, {afterPosition.Top})");
                    }
                    if (target.State.HasValue) {
                        switch (target.State.Value) {
                            case WindowState.Minimize:
                                MinimizeWindow(window);
                                break;
                            case WindowState.Maximize:
                                MaximizeWindow(window);
                                break;
                            case WindowState.Normal:
                                // already restored above
                                break;
                            case WindowState.Close:
                                CloseWindow(window);
                                break;
                        }
                        window.State = target.State;
                    }
                }
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
