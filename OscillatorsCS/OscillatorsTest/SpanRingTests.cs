using System;
using System.Diagnostics.CodeAnalysis;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using System.Collections;

namespace OscillatorsTest;

[TestClass]
public class SpanRingTests
{
    [TestMethod]
    public void ConstructorSanityCheck()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpanRing<float>(-1));
    }

    [TestMethod]
    public void ReadNothing()
    {
        var sr = new SpanRing<float>(10);
        var rs = sr.GetReadSpan(10);
        Assert.AreEqual(0, rs.Length);
        rs = sr.GetReadSpan(10);
        Assert.AreEqual(0, rs.Length);
    }

    [TestMethod]
    public void WriteRead()
    {
        var sr = new SpanRing<int>(5);

        var ws = sr.GetWriteSpan(3);
        new int[]{ 0, 1, 2 }.CopyTo(ws);

        var rs = sr.GetReadSpan(2);
        Assert.AreEqual(0, rs.Length);

        sr.AdvanceWrite(3);

        rs = sr.GetReadSpan(2);
        Assert.AreEqual(2, rs.Length);

        rs = sr.GetReadSpan(2);
        Assert.AreEqual(2, rs.Length);
        sr.AdvanceRead(2);

        rs = sr.GetReadSpan(2);
        Assert.AreEqual(1, rs.Length);
    }


    [TestMethod]
    public void WriteReadWraps()
    {
        var sr = new SpanRing<int>(5);

        var ws = sr.GetWriteSpan(3);
        new int[] { 0, 1, 2 }.CopyTo(ws);
        sr.AdvanceWrite(ws.Length);

        var rs = sr.GetReadSpan(3);
        Assert.AreEqual(3, rs.Length);
        sr.AdvanceRead(rs.Length);

        ws = sr.GetWriteSpan(3);
        Assert.AreEqual(2, ws.Length);
        sr.AdvanceWrite(ws.Length);

        ws = sr.GetWriteSpan(3);
        Assert.AreEqual(3, ws.Length);
        sr.AdvanceWrite(ws.Length);

        rs = sr.GetReadSpan(5);
        Assert.AreEqual(2, rs.Length);
        sr.AdvanceRead(rs.Length);

        rs = sr.GetReadSpan(5);
        Assert.AreEqual(3, rs.Length);
        sr.AdvanceRead(rs.Length);
    }
}