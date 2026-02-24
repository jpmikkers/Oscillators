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
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpanRing<float>(255, 256));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpanRing<float>(-1, 256));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpanRing<float>(255, -1));
    }

    [TestMethod]
    public void ReadSanity()
    {
        var sr = new SpanRing<float>(20,10);
        Assert.Throws<ArgumentOutOfRangeException>(() => sr.GetReadSpan(11));   // Can't request more than max span
        var rs = sr.GetReadSpan(10);
        Assert.AreEqual(0, rs.Length);
        Assert.Throws<InvalidOperationException>(() => sr.GetReadSpan(10));     // Can't request read span twice without commit or cancel
        sr.ReadCommit();                                                  
        rs = sr.GetReadSpan(10);                                                // read after commit should succeed, but still return empty span
        Assert.AreEqual(0, rs.Length);
        Assert.Throws<InvalidOperationException>(() => sr.GetReadSpan(10));     // Can't request read span twice without commit or cancel
        sr.ReadCancel();
        rs = sr.GetReadSpan(10);                                                // read after cancel should succeed, but still return empty span
        Assert.AreEqual(0, rs.Length);
    }

    [TestMethod]
    public void WriteRead()
    {
        var sr = new SpanRing<int>(5, 5);

        var ws = sr.GetWriteSpan(3);
        new int[]{ 0, 1, 2 }.CopyTo(ws);

        var rs = sr.GetReadSpan(2);
        Assert.AreEqual(0, rs.Length);
        sr.ReadCancel();

        sr.WriteCommit();

        rs = sr.GetReadSpan(2);
        Assert.AreEqual(2, rs.Length);
        sr.ReadCancel();

        rs = sr.GetReadSpan(2);
        Assert.AreEqual(2, rs.Length);
        CollectionAssert.AreEqual(new int[] { 0, 1 }, rs.ToArray());
        sr.ReadCommit();

        rs = sr.GetReadSpan(2);
        Assert.AreEqual(1, rs.Length);
        CollectionAssert.AreEqual(new int[] { 2 }, rs.ToArray());
        sr.ReadCommit();

        rs = sr.GetReadSpan(2);
        Assert.AreEqual(0, rs.Length);
        sr.ReadCommit();

        ws = sr.GetWriteSpan(5);
        new int[] { 3, 4, 5, 6, 7 }.CopyTo(ws);
        sr.WriteCommit();

        rs = sr.GetReadSpan(5);
        Assert.AreEqual(5, rs.Length);
        CollectionAssert.AreEqual(new int[] { 3,4,5,6,7 }, rs.ToArray());
    }
}