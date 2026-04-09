using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DesktopManager.Cli;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Live automation certification tests for the DesktopManager classic-control harness.
/// </summary>
public class DesktopManagerAutomationBasicsTests {
    private const int StatusTimeoutMilliseconds = 5000;

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the DesktopManager test app publishes the classic-control snapshot that basic automation depends on.
    /// </summary>
    public void DesktopManagerTestAppSession_ReadStatus_ExposesBasicControlsSnapshot() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-status");

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.EditorHandle != 0 && candidate.ApplyButtonBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The DesktopManager test app did not publish the basic-controls status snapshot in time.");

        Assert.IsTrue(status.AutomationCheckBoxChecked);
        Assert.AreEqual("Alpha", status.SelectedOption);
        Assert.AreEqual("Basic controls ready.", status.BasicActionStatus);
        Assert.IsTrue(status.EditorBounds.Width > 0);
        Assert.IsTrue(status.AutomationCheckBoxBounds.Width > 0);
        Assert.IsTrue(status.OptionsComboBoxBounds.Width > 0);
        Assert.IsTrue(status.ApplyButtonBounds.Width > 0);
        Assert.IsTrue(status.OptionsComboBoxHandle != 0);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures direct control clicks can invoke the harness Apply button without relying on coordinates.
    /// </summary>
    public void DesktopAutomationService_ClickControls_InvokesTestAppApplyButton() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-apply");
        DesktopAutomationService automation = new();

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.ApplyButtonHandle != 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not expose the Apply button handle in time.");
        WindowControlQueryOptions applyButtonQuery = new() {
            Handle = new IntPtr(readyStatus.ApplyButtonHandle)
        };

        IReadOnlyList<WindowControlTargetInfo> clickedControls = automation.ClickControls(
            CreateCurrentWindowQuery(session),
            applyButtonQuery,
            MouseButton.Left,
            allWindows: false,
            allControls: false);

        Assert.AreEqual(1, clickedControls.Count);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => string.Equals(candidate.BasicActionStatus, "Applied option 'Alpha' with checkbox enabled.", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not report the Apply button result.");
        Assert.AreEqual("Applied option 'Alpha' with checkbox enabled.", status.BasicActionStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures combo-box selection changes round-trip through the live test app without coordinates.
    /// </summary>
    public void DesktopAutomationService_SetControlSelectedValue_UpdatesTestAppComboBox() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-combo");
        DesktopAutomationService automation = new();

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.OptionsComboBoxHandle != 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not expose the combo-box handle in time.");

        IntPtr currentWindowHandle = CreateCurrentWindowQuery(session).Handle ?? session.WindowHandle;
        automation.SetControlSelectedValue(currentWindowHandle, new IntPtr(readyStatus.OptionsComboBoxHandle), "Beta");

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => string.Equals(candidate.SelectedOption, "Beta", StringComparison.Ordinal) &&
                string.Equals(candidate.BasicActionStatus, "Selected option: Beta", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the updated combo-box selection.");
        Assert.AreEqual("Beta", status.SelectedOption);
        Assert.AreEqual("Selected option: Beta", status.BasicActionStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures direct text mutation updates the harness editor and can be verified through the app-published status snapshot.
    /// </summary>
    public void DesktopAutomationService_SetControlText_UpdatesTestAppEditorText() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-text");
        DesktopAutomationService automation = new();

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.EditorHandle != 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not expose the editor handle in time.");

        string expectedText = "automation-basics-" + Guid.NewGuid().ToString("N");
        IReadOnlyList<WindowControlTargetInfo> updatedControls = automation.SetControlText(
            CreateCurrentWindowQuery(session),
            new WindowControlQueryOptions {
                Handle = new IntPtr(readyStatus.EditorHandle)
            },
            expectedText,
            allWindows: false,
            allControls: false);

        Assert.AreEqual(1, updatedControls.Count);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => string.Equals(candidate.EditorText, expectedText, StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the updated editor text.");
        Assert.AreEqual(expectedText, status.EditorText);

    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures DesktopManager can wait for a real client-area pixel change after a generic text mutation.
    /// </summary>
    public void DesktopAutomationService_WaitForWindowVisualChange_ObservesClientAreaMutation() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-visual-change");

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.EditorHandle != 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not expose the editor handle in time.");

        string expectedText = "visual-change-" + Guid.NewGuid().ToString("N");
        Thread mutationThread = new(() => {
            Thread.Sleep(400);
            DesktopAutomationService mutationAutomation = new();
            mutationAutomation.SetControlText(
                CreateCurrentWindowQuery(session),
                new WindowControlQueryOptions {
                    Handle = new IntPtr(readyStatus.EditorHandle)
                },
                expectedText,
                allWindows: false,
                allControls: false);
        });

        mutationThread.Start();
        DesktopWindowVisualChangeObservation observation = new DesktopAutomationService().WaitForWindowVisualChange(
            CreateCurrentWindowQuery(session),
            targetName: null,
            clientArea: true,
            timeoutMilliseconds: StatusTimeoutMilliseconds,
            intervalMilliseconds: 100,
            minimumChangedRatio: 0.002,
            differenceThreshold: 16);
        mutationThread.Join();

        Assert.AreEqual(session.WindowHandle, observation.Window.Handle);
        Assert.IsTrue(observation.ClientArea);
        Assert.IsNull(observation.TargetName);
        Assert.IsTrue(observation.Metrics.ChangedSampleCount > 0);
        Assert.IsTrue(observation.Metrics.ChangedSampleRatio >= 0.002d);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => string.Equals(candidate.EditorText, expectedText, StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the updated editor text after the visual-change observation.");
        Assert.AreEqual(expectedText, status.EditorText);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures public window click results can include visual-change verification metrics.
    /// </summary>
    public void DesktopOperations_ClickWindowPoint_ReturnsVisualChangeObservation() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-click-visual");

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.ApplyButtonBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the Apply button bounds in time.");

        DesktopWindowGeometry geometry = new DesktopAutomationService().GetWindowGeometry(session.WindowHandle);
        int relativeX = readyStatus.ApplyButtonBounds.Left - geometry.ClientLeft + (readyStatus.ApplyButtonBounds.Width / 2);
        int relativeY = readyStatus.ApplyButtonBounds.Top - geometry.ClientTop + (readyStatus.ApplyButtonBounds.Height / 2);

        WindowChangeResult result = DesktopOperations.ClickWindowPoint(
            new WindowSelectionCriteria {
                Handle = $"0x{session.WindowHandle.ToInt64():X}",
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            relativeX,
            relativeY,
            xRatio: null,
            yRatio: null,
            button: "left",
            activate: false,
            clientArea: true,
            artifactOptions: new MutationArtifactOptions {
                WaitForVisualChange = true,
                VisualClientArea = true,
                VisualTimeoutMilliseconds = StatusTimeoutMilliseconds,
                VisualIntervalMilliseconds = 100,
                VisualMinimumChangedRatio = 0.002,
                VisualDifferenceThreshold = 16
            });

        Assert.AreEqual(1, result.Count);
        Assert.IsNotNull(result.VisualChange, "The click result should include a visual-change verification block.");
        Assert.IsTrue(result.VisualChange!.ChangedSampleCount > 0);
        Assert.IsTrue(result.VisualChange.ClientArea);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => string.Equals(candidate.BasicActionStatus, "Applied option 'Alpha' with checkbox enabled.", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the Apply button outcome after the click.");
        Assert.AreEqual("Applied option 'Alpha' with checkbox enabled.", status.BasicActionStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures a reusable visual baseline can be saved from the live harness and immediately asserted.
    /// </summary>
    public void DesktopAutomationService_SaveAndAssertVisualBaseline_MatchesCurrentClientArea() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-visual-baseline");

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.EditorHandle != 0 && candidate.EditorBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not expose stable client-area content in time.");

        string baselineName = "DesktopManagerAutomationBasicsTests-" + Guid.NewGuid().ToString("N");
        string metadataPath = DesktopStateStore.GetVisualBaselinePath(baselineName);
        string imagePath = DesktopStateStore.GetVisualBaselineImagePath(baselineName);
        DesktopAutomationService automation = new();

        try {
            DesktopVisualBaselineDefinition baseline = automation.SaveVisualBaseline(
                baselineName,
                CreateCurrentWindowQuery(session),
                targetName: null,
                clientArea: true,
                description: "Immediate live harness baseline");

            Assert.IsTrue(baseline.ClientArea);
            Assert.IsNull(baseline.TargetName);
            Assert.IsTrue(File.Exists(metadataPath));
            Assert.IsTrue(File.Exists(imagePath));
            Assert.IsTrue(baseline.Width > 0);
            Assert.IsTrue(baseline.Height > 0);

            DesktopVisualBaselineAssertionResult assertion = automation.AssertVisualBaseline(
                baselineName,
                CreateCurrentWindowQuery(session),
                targetName: null,
                clientArea: true,
                maxChangedRatio: 0.02,
                differenceThreshold: 16);

            Assert.IsTrue(assertion.Matched, "The saved client-area baseline should match immediately on the live harness.");
            Assert.AreEqual(session.WindowHandle, assertion.Window.Handle);
            Assert.IsTrue(assertion.ClientArea);
            Assert.IsNull(assertion.TargetName);
            Assert.IsFalse(assertion.Metrics.SizeChanged);
            Assert.IsTrue(assertion.Metrics.SampleCount > 0);
            Assert.IsTrue(assertion.Metrics.ChangedSampleRatio <= 0.02d);
        } finally {
            if (File.Exists(metadataPath)) {
                File.Delete(metadataPath);
            }

            if (File.Exists(imagePath)) {
                File.Delete(imagePath);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures a saved visual baseline can be resolved back to the same live control region inside the current client area.
    /// </summary>
    public void DesktopAutomationService_ResolveVisualBaseline_FindsSavedTargetRegion() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-visual-resolve");

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.ApplyButtonBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the Apply button bounds in time.");

        DesktopAutomationService automation = new();
        DesktopWindowGeometry geometry = automation.GetWindowGeometry(session.WindowHandle);
        string targetName = "DesktopManagerAutomationBasicsTests-Target-" + Guid.NewGuid().ToString("N");
        string baselineName = "DesktopManagerAutomationBasicsTests-Resolve-" + Guid.NewGuid().ToString("N");
        string targetPath = DesktopStateStore.GetTargetPath(targetName);
        string metadataPath = DesktopStateStore.GetVisualBaselinePath(baselineName);
        string imagePath = DesktopStateStore.GetVisualBaselineImagePath(baselineName);
        int expectedRelativeX = readyStatus.ApplyButtonBounds.Left - geometry.ClientLeft;
        int expectedRelativeY = readyStatus.ApplyButtonBounds.Top - geometry.ClientTop;

        try {
            automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
                XRatio = expectedRelativeX / (double)geometry.ClientWidth,
                YRatio = expectedRelativeY / (double)geometry.ClientHeight,
                WidthRatio = readyStatus.ApplyButtonBounds.Width / (double)geometry.ClientWidth,
                HeightRatio = readyStatus.ApplyButtonBounds.Height / (double)geometry.ClientHeight,
                ClientArea = true,
                Description = "Apply button visual anchor"
            });
            automation.SaveVisualBaseline(
                baselineName,
                CreateCurrentWindowQuery(session),
                targetName,
                clientArea: false,
                description: "Apply button baseline");

            DesktopVisualBaselineResolveResult resolution = automation.ResolveVisualBaseline(
                baselineName,
                CreateCurrentWindowQuery(session),
                clientArea: true,
                maxAverageDifference: 10.0,
                differenceThreshold: 18,
                scanStep: 6);

            Assert.IsTrue(resolution.Matched, "The live client area should still contain the saved Apply button region.");
            Assert.IsTrue(Math.Abs(resolution.RelativeX - expectedRelativeX) <= 6, "Resolved X should stay near the published Apply button bounds.");
            Assert.IsTrue(Math.Abs(resolution.RelativeY - expectedRelativeY) <= 6, "Resolved Y should stay near the published Apply button bounds.");
            Assert.AreEqual(readyStatus.ApplyButtonBounds.Width, resolution.Width);
            Assert.AreEqual(readyStatus.ApplyButtonBounds.Height, resolution.Height);
            Assert.AreEqual(geometry.ClientLeft + resolution.RelativeX, resolution.ScreenX);
            Assert.AreEqual(geometry.ClientTop + resolution.RelativeY, resolution.ScreenY);
        } finally {
            if (File.Exists(targetPath)) {
                File.Delete(targetPath);
            }

            if (File.Exists(metadataPath)) {
                File.Delete(metadataPath);
            }

            if (File.Exists(imagePath)) {
                File.Delete(imagePath);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the public click flow can reuse a saved visual baseline as an anchor instead of raw coordinates.
    /// </summary>
    public void DesktopOperations_ClickWindowVisualBaseline_InvokesApplyButton() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-click-visual-baseline");

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.ApplyButtonBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the Apply button bounds in time.");

        DesktopAutomationService automation = new();
        DesktopWindowGeometry geometry = automation.GetWindowGeometry(session.WindowHandle);
        string targetName = "DesktopManagerAutomationBasicsTests-ClickTarget-" + Guid.NewGuid().ToString("N");
        string baselineName = "DesktopManagerAutomationBasicsTests-ClickBaseline-" + Guid.NewGuid().ToString("N");
        string targetPath = DesktopStateStore.GetTargetPath(targetName);
        string metadataPath = DesktopStateStore.GetVisualBaselinePath(baselineName);
        string imagePath = DesktopStateStore.GetVisualBaselineImagePath(baselineName);
        int relativeX = readyStatus.ApplyButtonBounds.Left - geometry.ClientLeft;
        int relativeY = readyStatus.ApplyButtonBounds.Top - geometry.ClientTop;

        try {
            automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
                XRatio = relativeX / (double)geometry.ClientWidth,
                YRatio = relativeY / (double)geometry.ClientHeight,
                WidthRatio = readyStatus.ApplyButtonBounds.Width / (double)geometry.ClientWidth,
                HeightRatio = readyStatus.ApplyButtonBounds.Height / (double)geometry.ClientHeight,
                ClientArea = true,
                Description = "Apply button visual click anchor"
            });
            automation.SaveVisualBaseline(
                baselineName,
                CreateCurrentWindowQuery(session),
                targetName,
                clientArea: false,
                description: "Apply button click baseline");

            WindowChangeResult result = DesktopOperations.ClickWindowVisualBaseline(
                new WindowSelectionCriteria {
                    Handle = $"0x{session.WindowHandle.ToInt64():X}",
                    IncludeHidden = false,
                    IncludeCloaked = false,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                },
                baselineName,
                "left",
                activate: false,
                clientArea: true,
                maxAverageDifference: 10.0,
                differenceThreshold: 18,
                scanStep: 6,
                artifactOptions: null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("visual-baseline", result.TargetKind);
            Assert.AreEqual(baselineName, result.TargetName);
            Assert.AreEqual(1, result.ResolvedTargets.Count);
            Assert.AreEqual("target", result.ResolvedTargets[0].Role);
            Assert.AreEqual("visual-baseline", result.ResolvedTargets[0].Kind);
            Assert.AreEqual(baselineName, result.ResolvedTargets[0].Name);
            Assert.IsTrue(result.ResolvedTargets[0].Width > 0);
            Assert.IsTrue(result.ResolvedTargets[0].Height > 0);

            DesktopManagerTestAppStatus status = session.WaitForStatus(
                candidate => string.Equals(candidate.BasicActionStatus, "Applied option 'Alpha' with checkbox enabled.", StringComparison.Ordinal),
                StatusTimeoutMilliseconds,
                "The automation basics harness did not report the Apply button result after the visual-baseline click.");
            Assert.AreEqual("Applied option 'Alpha' with checkbox enabled.", status.BasicActionStatus);
        } finally {
            if (File.Exists(targetPath)) {
                File.Delete(targetPath);
            }

            if (File.Exists(metadataPath)) {
                File.Delete(metadataPath);
            }

            if (File.Exists(imagePath)) {
                File.Delete(imagePath);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the public click flow can resolve visible text through OCR and invoke the matching button.
    /// </summary>
    public void DesktopOperations_ClickWindowText_InvokesApplyButton() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-click-ocr-text");

        session.WaitForStatus(
            candidate => candidate.ApplyButtonBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the Apply button bounds in time.");

        WindowChangeResult result = DesktopOperations.ClickWindowText(
            new WindowSelectionCriteria {
                Handle = $"0x{session.WindowHandle.ToInt64():X}",
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            "Apply",
            "left",
            activate: false,
            contains: false,
            targetName: null,
            clientArea: true,
            languageTag: "en-US",
            artifactOptions: null);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ocr-text", result.TargetKind);
        Assert.AreEqual("Apply", result.TargetName);
        Assert.AreEqual(1, result.ResolvedTargets.Count);
        Assert.AreEqual("target", result.ResolvedTargets[0].Role);
        Assert.AreEqual("ocr-text", result.ResolvedTargets[0].Kind);
        Assert.AreEqual("Apply", result.ResolvedTargets[0].Name);
        Assert.IsTrue(result.ResolvedTargets[0].Width > 0);
        Assert.IsTrue(result.ResolvedTargets[0].Height > 0);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => string.Equals(candidate.BasicActionStatus, "Applied option 'Alpha' with checkbox enabled.", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not report the Apply button result after the OCR click.");
        Assert.AreEqual("Applied option 'Alpha' with checkbox enabled.", status.BasicActionStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures OCR-driven clicks can return built-in visual-change verification metrics in the same result.
    /// </summary>
    public void DesktopOperations_ClickWindowText_ReturnsVisualChangeObservation() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-click-ocr-text-visual");

        session.WaitForStatus(
            candidate => candidate.ApplyButtonBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the Apply button bounds in time.");

        WindowChangeResult result = DesktopOperations.ClickWindowText(
            new WindowSelectionCriteria {
                Handle = $"0x{session.WindowHandle.ToInt64():X}",
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            "Apply",
            "left",
            activate: false,
            contains: false,
            targetName: null,
            clientArea: true,
            languageTag: "en-US",
            artifactOptions: new MutationArtifactOptions {
                WaitForVisualChange = true,
                VisualClientArea = true,
                VisualTimeoutMilliseconds = StatusTimeoutMilliseconds,
                VisualIntervalMilliseconds = 100,
                VisualMinimumChangedRatio = 0.002,
                VisualDifferenceThreshold = 16
            });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ocr-text", result.TargetKind);
        Assert.IsNotNull(result.VisualChange, "The OCR click result should include a visual-change verification block.");
        Assert.IsTrue(result.VisualChange!.ChangedSampleCount > 0);
        Assert.IsTrue(result.VisualChange.ClientArea);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => string.Equals(candidate.BasicActionStatus, "Applied option 'Alpha' with checkbox enabled.", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the Apply button outcome after the OCR click.");
        Assert.AreEqual("Applied option 'Alpha' with checkbox enabled.", status.BasicActionStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the public drag flow can reuse saved visual baselines as start and end anchors.
    /// </summary>
    public void DesktopOperations_DragWindowVisualBaselines_CompletesDropFlow() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-drag-visual-baseline");

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.DragSourceBounds.Width > 0 && candidate.DropTargetBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the drag/drop bounds in time.");

        DesktopAutomationService automation = new();
        DesktopWindowGeometry geometry = automation.GetWindowGeometry(session.WindowHandle);
        string startTargetName = "DesktopManagerAutomationBasicsTests-DragStartTarget-" + Guid.NewGuid().ToString("N");
        string endTargetName = "DesktopManagerAutomationBasicsTests-DragEndTarget-" + Guid.NewGuid().ToString("N");
        string startBaselineName = "DesktopManagerAutomationBasicsTests-DragStartBaseline-" + Guid.NewGuid().ToString("N");
        string endBaselineName = "DesktopManagerAutomationBasicsTests-DragEndBaseline-" + Guid.NewGuid().ToString("N");
        string[] cleanupPaths = [
            DesktopStateStore.GetTargetPath(startTargetName),
            DesktopStateStore.GetTargetPath(endTargetName),
            DesktopStateStore.GetVisualBaselinePath(startBaselineName),
            DesktopStateStore.GetVisualBaselineImagePath(startBaselineName),
            DesktopStateStore.GetVisualBaselinePath(endBaselineName),
            DesktopStateStore.GetVisualBaselineImagePath(endBaselineName)
        ];

        try {
            SaveClientAreaTargetForBounds(automation, startTargetName, readyStatus.DragSourceBounds, geometry, "Drag source visual anchor");
            SaveClientAreaTargetForBounds(automation, endTargetName, readyStatus.DropTargetBounds, geometry, "Drop target visual anchor");
            automation.SaveVisualBaseline(startBaselineName, CreateCurrentWindowQuery(session), startTargetName, clientArea: false, description: "Drag source baseline");
            automation.SaveVisualBaseline(endBaselineName, CreateCurrentWindowQuery(session), endTargetName, clientArea: false, description: "Drop target baseline");

            WindowChangeResult result = DesktopOperations.DragWindowVisualBaselines(
                new WindowSelectionCriteria {
                    Handle = $"0x{session.WindowHandle.ToInt64():X}",
                    IncludeHidden = false,
                    IncludeCloaked = false,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                },
                startBaselineName,
                endBaselineName,
                "left",
                stepDelayMilliseconds: 15,
                activate: true,
                clientArea: true,
                maxAverageDifference: 12.0,
                differenceThreshold: 18,
                scanStep: 6,
                artifactOptions: null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("visual-baseline-pair", result.TargetKind);
            Assert.AreEqual($"{startBaselineName}->{endBaselineName}", result.TargetName);
            Assert.AreEqual(2, result.ResolvedTargets.Count);
            Assert.IsTrue(result.ResolvedTargets.Any(target => string.Equals(target.Role, "start", StringComparison.Ordinal) && string.Equals(target.Name, startBaselineName, StringComparison.Ordinal)));
            Assert.IsTrue(result.ResolvedTargets.Any(target => string.Equals(target.Role, "end", StringComparison.Ordinal) && string.Equals(target.Name, endBaselineName, StringComparison.Ordinal)));

            DesktopManagerTestAppStatus status = session.WaitForStatus(
                candidate => candidate.DragDropCount > 0 && string.Equals(candidate.DroppedText, candidate.DragPayload, StringComparison.Ordinal),
                StatusTimeoutMilliseconds,
                "The automation basics harness did not report a completed drag/drop after the visual-baseline drag.");
            Assert.AreEqual(status.DragPayload, status.DroppedText);
            Assert.AreEqual("Drop completed.", status.DragDropStatus);
        } finally {
            foreach (string cleanupPath in cleanupPaths) {
                if (File.Exists(cleanupPath)) {
                    File.Delete(cleanupPath);
                }
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the public drag flow can resolve visible text through OCR for both the source and destination.
    /// </summary>
    public void DesktopOperations_DragWindowText_CompletesDropFlow() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-drag-ocr-text");

        session.WaitForStatus(
            candidate => candidate.DragSourceBounds.Width > 0 && candidate.DropTargetBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the drag/drop bounds in time.");

        WindowChangeResult result = DesktopOperations.DragWindowText(
            new WindowSelectionCriteria {
                Handle = $"0x{session.WindowHandle.ToInt64():X}",
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            "Drag Source",
            "Drop Target",
            "left",
            stepDelayMilliseconds: 15,
            activate: true,
            contains: true,
            startTargetName: null,
            endTargetName: null,
            clientArea: true,
            languageTag: "en-US",
            artifactOptions: null);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ocr-text-pair", result.TargetKind);
        Assert.AreEqual("Drag Source->Drop Target", result.TargetName);
        Assert.AreEqual(2, result.ResolvedTargets.Count);
        Assert.IsTrue(result.ResolvedTargets.Any(target => string.Equals(target.Role, "start", StringComparison.Ordinal) && string.Equals(target.Kind, "ocr-text", StringComparison.Ordinal) && string.Equals(target.Name, "Drag Source", StringComparison.Ordinal)));
        Assert.IsTrue(result.ResolvedTargets.Any(target => string.Equals(target.Role, "end", StringComparison.Ordinal) && string.Equals(target.Kind, "ocr-text", StringComparison.Ordinal) && string.Equals(target.Name, "Drop Target", StringComparison.Ordinal)));

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.DragDropCount > 0 && string.Equals(candidate.DroppedText, candidate.DragPayload, StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not report a completed drag/drop after the OCR drag.");
        Assert.AreEqual(status.DragPayload, status.DroppedText);
        Assert.AreEqual("Drop completed.", status.DragDropStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures OCR-driven drags can return built-in visual-change verification metrics in the same result.
    /// </summary>
    public void DesktopOperations_DragWindowText_ReturnsVisualChangeObservation() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-drag-ocr-text-visual");

        session.WaitForStatus(
            candidate => candidate.DragSourceBounds.Width > 0 && candidate.DropTargetBounds.Width > 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the drag/drop bounds in time.");

        WindowChangeResult result = DesktopOperations.DragWindowText(
            new WindowSelectionCriteria {
                Handle = $"0x{session.WindowHandle.ToInt64():X}",
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            "Drag Source",
            "Drop Target",
            "left",
            stepDelayMilliseconds: 15,
            activate: true,
            contains: true,
            startTargetName: null,
            endTargetName: null,
            clientArea: true,
            languageTag: "en-US",
            artifactOptions: new MutationArtifactOptions {
                WaitForVisualChange = true,
                VisualClientArea = true,
                VisualTimeoutMilliseconds = StatusTimeoutMilliseconds,
                VisualIntervalMilliseconds = 100,
                VisualMinimumChangedRatio = 0.002,
                VisualDifferenceThreshold = 16
            });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ocr-text-pair", result.TargetKind);
        Assert.IsNotNull(result.VisualChange, "The OCR drag result should include a visual-change verification block.");
        Assert.IsTrue(result.VisualChange!.ChangedSampleCount > 0);
        Assert.IsTrue(result.VisualChange.ClientArea);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.DragDropCount > 0 && string.Equals(candidate.DroppedText, candidate.DragPayload, StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The automation basics harness did not report a completed drag/drop after the OCR drag.");
        Assert.AreEqual(status.DragPayload, status.DroppedText);
        Assert.AreEqual("Drop completed.", status.DragDropStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures OCR-driven scrolls can return built-in visual-change verification metrics and update the shared scroll harness state.
    /// </summary>
    public void DesktopOperations_ScrollWindowText_ReturnsVisualChangeObservation() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start("automation-basics-scroll-ocr-text-visual");

        DesktopManagerTestAppStatus readyStatus = session.WaitForStatus(
            candidate => candidate.ScrollListBounds.Width > 0 && candidate.ScrollTopIndex >= 0,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not publish the scroll surface bounds in time.");

        WindowChangeResult result = DesktopOperations.ScrollWindowText(
            new WindowSelectionCriteria {
                Handle = $"0x{session.WindowHandle.ToInt64():X}",
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            "Scroll Item 03",
            delta: -240,
            activate: true,
            contains: true,
            targetName: null,
            clientArea: true,
            languageTag: "en-US",
            artifactOptions: new MutationArtifactOptions {
                WaitForVisualChange = true,
                VisualClientArea = true,
                VisualTimeoutMilliseconds = StatusTimeoutMilliseconds,
                VisualIntervalMilliseconds = 100,
                VisualMinimumChangedRatio = 0.002,
                VisualDifferenceThreshold = 16
            });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ocr-text", result.TargetKind);
        Assert.IsNotNull(result.VisualChange, "The OCR scroll result should include a visual-change verification block.");
        Assert.IsTrue(result.VisualChange!.ChangedSampleCount > 0);
        Assert.IsTrue(result.VisualChange.ClientArea);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.ScrollTopIndex > readyStatus.ScrollTopIndex,
            StatusTimeoutMilliseconds,
            "The automation basics harness did not report a changed top item after the OCR scroll.");
        Assert.IsTrue(status.ScrollTopIndex > readyStatus.ScrollTopIndex);
        Assert.AreNotEqual(readyStatus.ScrollTopItemText, status.ScrollTopItemText);
    }

    private static WindowControlTargetInfo WaitForSingleControl(DesktopAutomationService automation, DesktopManagerTestAppSession session, WindowControlQueryOptions controlQuery, string failureMessage) {
        DesktopManagerTestAppStatus status = session.WaitForStatus(
            _ => {
                IReadOnlyList<WindowControlTargetInfo> controls = automation.GetControls(CreateCurrentWindowQuery(session), controlQuery, allWindows: false);
                return controls.Count == 1;
            },
            StatusTimeoutMilliseconds,
            failureMessage);

        IReadOnlyList<WindowControlTargetInfo> resolvedControls = automation.GetControls(CreateCurrentWindowQuery(session), controlQuery, allWindows: false);
        Assert.AreEqual(1, resolvedControls.Count, failureMessage + " Status window title: " + status.WindowTitle);
        return resolvedControls.Single();
    }

    private static WindowQueryOptions CreateCurrentWindowQuery(DesktopManagerTestAppSession session) {
        DesktopManagerTestAppStatus status = session.ReadStatus();
        return new WindowQueryOptions {
            Handle = status.WindowHandle == 0 ? session.WindowHandle : new IntPtr(status.WindowHandle),
            ProcessId = status.ProcessId,
            TitlePattern = status.WindowTitle,
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    private static void SaveClientAreaTargetForBounds(DesktopAutomationService automation, string targetName, DesktopManagerTestAppControlBounds bounds, DesktopWindowGeometry geometry, string description) {
        int relativeLeft = bounds.Left - geometry.ClientLeft;
        int relativeTop = bounds.Top - geometry.ClientTop;
        automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
            XRatio = relativeLeft / (double)geometry.ClientWidth,
            YRatio = relativeTop / (double)geometry.ClientHeight,
            WidthRatio = bounds.Width / (double)geometry.ClientWidth,
            HeightRatio = bounds.Height / (double)geometry.ClientHeight,
            ClientArea = true,
            Description = description
        });
    }

    private static void RequireLiveTestAppHarness() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows.");
        }

        TestHelper.RequireExternalDesktopApplicationTests();
    }
}
