namespace DesktopManager;

public partial class WindowManager {
        /// <summary>
        /// Moves the mouse cursor to the specified screen coordinates.
        /// </summary>
        /// <param name="x">X coordinate in pixels.</param>
        /// <param name="y">Y coordinate in pixels.</param>
        public void MoveMouse(int x, int y) {
            MouseInputService.MoveCursor(x, y);
        }

        /// <summary>
        /// Performs a mouse click using the specified button.
        /// </summary>
        /// <param name="button">Button to click.</param>
        public void ClickMouse(MouseButton button) {
            MouseInputService.Click(button);
        }

        /// <summary>
        /// Scrolls the mouse wheel vertically.
        /// </summary>
        /// <param name="delta">Scroll amount.</param>
        public void ScrollMouse(int delta) {
            MouseInputService.Scroll(delta);
        }

        /// <summary>
        /// Drags the mouse between two points.
        /// </summary>
        /// <param name="button">Button to hold during the drag.</param>
        /// <param name="startX">Starting X coordinate.</param>
        /// <param name="startY">Starting Y coordinate.</param>
        /// <param name="endX">Ending X coordinate.</param>
        /// <param name="endY">Ending Y coordinate.</param>
        /// <param name="stepDelay">Delay in milliseconds between steps.</param>
        public void DragMouse(MouseButton button, int startX, int startY, int endX, int endY, int stepDelay) {
            MouseInputService.MouseDrag(button, startX, startY, endX, endY, stepDelay);
        }
}
