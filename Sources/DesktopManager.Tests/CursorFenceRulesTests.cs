namespace DesktopManager.Tests;

[TestClass]
/// <summary>Tests the cursor fence's pure entry decisions.</summary>
public class CursorFenceRulesTests {
    private static readonly RECT Strip = new() { Left = -546, Top = 4340, Right = 2014, Bottom = 5060 };

    [TestMethod]
    [DataRow(-550, 4500, -540, 4500, -547, 4500)]
    [DataRow(2020, 4500, 2010, 4500, 2014, 4500)]
    [DataRow(0, 4330, 0, 4350, 0, 4339)]
    [DataRow(0, 5070, 0, 5050, 0, 5060)]
    [DataRow(-556, 4330, -536, 4350, -547, 4340)]
    [DataRow(2023, 5069, 2003, 5049, 2014, 5059)]
    [DataRow(0, 4300, 0, 5100, 0, 4339)]
    /// <summary>Entry stops just outside the first edge crossed, including diagonal moves and jumps across the rectangle.</summary>
    public void Decide_StopsAtFirstEdge_IncludingDiagonalsAndJumps(int x, int y, int nextX, int nextY, int allowedX, int allowedY) {
        foreach (CursorFenceMode mode in new[] { CursorFenceMode.Locked, CursorFenceMode.Sticky }) {
            FenceDecision result = CursorFenceRules.Decide(new FencePoint(x, y), new FencePoint(nextX, nextY), Strip, mode, false, 0, 1000, default);

            Assert.AreEqual(new FencePoint(allowedX, allowedY), result.Point, mode.ToString());
            Assert.IsFalse(CursorFenceRules.Contains(Strip, result.Point), mode.ToString());
        }
    }

    [TestMethod]
    /// <summary>Sticky entry needs both enough push distance and sustained time.</summary>
    public void Decide_Sticky_NeedsDistanceAndSustainedPressure() {
        FenceDecision first = Move(new FencePoint(0, 4339), new FencePoint(0, 4399), 1000);
        Assert.AreEqual(new FencePoint(0, 4339), first.Point);
        FenceDecision distanceOnly = Move(first.Point, new FencePoint(0, 4399), 1050, first.Push);
        Assert.AreEqual(first.Point, distanceOnly.Point);
        FenceDecision deliberate = Move(distanceOnly.Point, new FencePoint(0, 4340), 1150, distanceOnly.Push);
        Assert.AreEqual(new FencePoint(0, 4340), deliberate.Point);
        Assert.AreEqual(default(FencePush), deliberate.Push);

        FenceDecision gentle = Move(new FencePoint(0, 4339), new FencePoint(0, 4340), 1000);
        FenceDecision timeOnly = Move(gentle.Point, new FencePoint(0, 4340), 1200, gentle.Push);
        Assert.AreEqual(gentle.Point, timeOnly.Point);
    }

    [TestMethod]
    [DataRow("pause")]
    [DataRow("retreat")]
    [DataRow("slide")]
    [DataRow("different edge")]
    /// <summary>Sticky pressure resets when the push pauses, retreats, slides along or moves to another edge.</summary>
    public void Decide_Sticky_ResetsPressureWhenPushIsInterrupted(string interruption) {
        FenceDecision initial = Move(new FencePoint(0, 4339), new FencePoint(0, 4459), 1000);
        FenceDecision changed = interruption switch {
            "pause" => Move(initial.Point, new FencePoint(0, 4340), 1300, initial.Push),
            "retreat" => Move(initial.Point, new FencePoint(0, 4338), 1100, initial.Push),
            "slide" => Move(initial.Point, new FencePoint(10, 4339), 1100, initial.Push),
            _ => Move(new FencePoint(-547, 4500), new FencePoint(-546, 4500), 1200, initial.Push),
        };

        if (interruption == "retreat" || interruption == "slide") {
            Assert.AreEqual(default(FencePush), changed.Push);
        } else {
            Assert.IsFalse(CursorFenceRules.Contains(Strip, changed.Point));
            Assert.AreEqual(1d, changed.Push.Distance);
            Assert.AreEqual(changed.Push.Last, changed.Push.Started);
        }
    }

    [TestMethod]
    /// <summary>The wrapping message clock neither releases nor resets a continuous push.</summary>
    public void Decide_Sticky_SurvivesMessageClockWrap() {
        FenceDecision first = Move(new FencePoint(0, 4339), new FencePoint(0, 4459), uint.MaxValue - 100);
        FenceDecision next = Move(first.Point, new FencePoint(0, 4340), 49, first.Push);

        Assert.AreEqual(new FencePoint(0, 4340), next.Point);
    }

    [TestMethod]
    [DataRow(CursorFenceMode.Off, false, 0UL)]
    [DataRow(CursorFenceMode.Sticky, true, 0UL)]
    [DataRow(CursorFenceMode.Locked, true, 0UL)]
    [DataRow(CursorFenceMode.Sticky, false, 0xFF515700UL)]
    [DataRow(CursorFenceMode.Locked, false, 0xFF515780UL)]
    [DataRow(CursorFenceMode.Locked, false, 0x12345678FF5157FFUL)]
    /// <summary>Off, a held Control key and touch or pen promoted to mouse pass through and clear pressure.</summary>
    public void Decide_PassesOffControlAndTouchOrPen(CursorFenceMode mode, bool control, ulong extraInfo) {
        FenceDecision result = CursorFenceRules.Decide(new FencePoint(0, 4339), new FencePoint(0, 4500), Strip, mode, control,
            extraInfo, 1200, new FencePush(FenceEdge.Top, 120, 1000, 1100));

        Assert.AreEqual(new FencePoint(0, 4500), result.Point);
        Assert.AreEqual(default(FencePush), result.Push);
    }

    [TestMethod]
    /// <summary>Locked builds no pressure and does not mistake unrelated extra information for touch.</summary>
    public void Decide_Locked_BuildsNoPressureAndIgnoresLookalikeSignature() {
        FenceDecision result = CursorFenceRules.Decide(new FencePoint(0, 4339), new FencePoint(0, 4500), Strip, CursorFenceMode.Locked,
            false, 0xFF515600UL, 2000, new FencePush(FenceEdge.Top, 120, 1000, 1900));

        Assert.AreEqual(new FencePoint(0, 4339), result.Point);
        Assert.AreEqual(default(FencePush), result.Push);
    }

    [TestMethod]
    [DataRow(0, 4300)]
    [DataRow(0, 5100)]
    [DataRow(-600, 4500)]
    [DataRow(2100, 4500)]
    [DataRow(20, 4500)]
    /// <summary>A pointer already inside can always move or leave.</summary>
    public void Decide_PointerInside_CanMoveOrLeave(int x, int y) {
        FenceDecision result = CursorFenceRules.Decide(new FencePoint(0, 4500), new FencePoint(x, y), Strip, CursorFenceMode.Locked,
            false, 0, 2000, default);

        Assert.AreEqual(new FencePoint(x, y), result.Point);
    }

    [TestMethod]
    [DataRow(-600, 4300, 2200, 4300)]
    [DataRow(-547, 4300, -547, 5100)]
    [DataRow(2014, 4300, 2014, 5100)]
    [DataRow(-600, 5060, 2200, 5060)]
    /// <summary>Motion along or away from the outer edge is free.</summary>
    public void Decide_MotionAlongOuterEdge_IsFree(int x, int y, int nextX, int nextY) {
        FenceDecision result = Move(new FencePoint(x, y), new FencePoint(nextX, nextY), 1000);

        Assert.AreEqual(new FencePoint(nextX, nextY), result.Point);
        Assert.AreEqual(default(FencePush), result.Push);
    }

    private static FenceDecision Move(FencePoint current, FencePoint proposed, uint time, FencePush push = default) =>
        CursorFenceRules.Decide(current, proposed, Strip, CursorFenceMode.Sticky, false, 0, time, push);
}
