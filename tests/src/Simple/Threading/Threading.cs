// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

// TODO: Move these tests to CoreFX once they can be run on CoreRT

internal static class Runner
{
    private const int Pass = 100;

    public static int Main()
    {
        Console.WriteLine("    WaitSubsystemTests.DoubleSetOnEventWithTimedOutWaiterShouldNotStayInWaitersList");
        WaitSubsystemTests.DoubleSetOnEventWithTimedOutWaiterShouldNotStayInWaitersList();

        Console.WriteLine("    WaitSubsystemTests.ManualResetEventTest");
        WaitSubsystemTests.ManualResetEventTest();

        Console.WriteLine("    WaitSubsystemTests.AutoResetEventTest");
        WaitSubsystemTests.AutoResetEventTest();

        Console.WriteLine("    WaitSubsystemTests.SemaphoreTest");
        WaitSubsystemTests.SemaphoreTest();

        Console.WriteLine("    WaitSubsystemTests.MutexTest");
        WaitSubsystemTests.MutexTest();

        Console.WriteLine("    WaitSubsystemTests.WaitDurationTest");
        WaitSubsystemTests.WaitDurationTest();

        // This test takes a long time to run in release and especially in debug builds. Enable for manual testing.
        //Console.WriteLine("    WaitSubsystemTests.MutexMaximumReacquireCountTest");
        //WaitSubsystemTests.MutexMaximumReacquireCountTest();

        return Pass;
    }
}

internal static class WaitSubsystemTests
{
    private const int AcceptableEarlyWaitTerminationDiffMilliseconds = -100;
    private const int AcceptableLateWaitTerminationDiffMilliseconds = 300;

    [Fact]
    public static void ManualResetEventTest()
    {
        // Constructor and dispose

        var e = new ManualResetEvent(false);
        Assert.False(e.WaitOne(0));
        e.Dispose();
        Assert.Throws<ObjectDisposedException>(() => e.Reset());
        Assert.Throws<ObjectDisposedException>(() => e.Set());
        Assert.Throws<ObjectDisposedException>(() => e.WaitOne(0));

        e = new ManualResetEvent(true);
        Assert.True(e.WaitOne(0));

        // Set and reset

        e = new ManualResetEvent(true);
        e.Reset();
        Assert.False(e.WaitOne(0));
        Assert.False(e.WaitOne(0));
        e.Reset();
        Assert.False(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne(0));
        Assert.True(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne(0));

        // Wait

        e.Set();
        Assert.True(e.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.True(e.WaitOne());

        e.Reset();
        Assert.False(e.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));

        e = null;

        // Multi-wait with all indexes set
        var es =
            new ManualResetEvent[]
            {
                new ManualResetEvent(true),
                new ManualResetEvent(true),
                new ManualResetEvent(true),
                new ManualResetEvent(true)
            };
        Assert.Equal(0, WaitHandle.WaitAny(es, 0));
        Assert.Equal(0, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.Equal(0, WaitHandle.WaitAny(es));
        Assert.True(WaitHandle.WaitAll(es, 0));
        Assert.True(WaitHandle.WaitAll(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.True(WaitHandle.WaitAll(es));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.True(es[i].WaitOne(0));
        }

        // Multi-wait with indexes 1 and 2 set
        es[0].Reset();
        es[3].Reset();
        Assert.Equal(1, WaitHandle.WaitAny(es, 0));
        Assert.Equal(1, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 1 || i == 2, es[i].WaitOne(0));
        }

        // Multi-wait with all indexes reset
        es[1].Reset();
        es[2].Reset();
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, 0));
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
        }
    }

    [Fact]
    public static void AutoResetEventTest()
    {
        // Constructor and dispose

        var e = new AutoResetEvent(false);
        Assert.False(e.WaitOne(0));
        e.Dispose();
        Assert.Throws<ObjectDisposedException>(() => e.Reset());
        Assert.Throws<ObjectDisposedException>(() => e.Set());
        Assert.Throws<ObjectDisposedException>(() => e.WaitOne(0));

        e = new AutoResetEvent(true);
        Assert.True(e.WaitOne(0));

        // Set and reset

        e = new AutoResetEvent(true);
        e.Reset();
        Assert.False(e.WaitOne(0));
        Assert.False(e.WaitOne(0));
        e.Reset();
        Assert.False(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne(0));
        Assert.False(e.WaitOne(0));
        e.Set();
        e.Set();
        Assert.True(e.WaitOne(0));

        // Wait

        e.Set();
        Assert.True(e.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.False(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne());
        Assert.False(e.WaitOne(0));

        e.Reset();
        Assert.False(e.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));

        e = null;

        // Multi-wait with all indexes set
        var es =
            new AutoResetEvent[]
            {
                new AutoResetEvent(true),
                new AutoResetEvent(true),
                new AutoResetEvent(true),
                new AutoResetEvent(true)
            };
        Assert.Equal(0, WaitHandle.WaitAny(es, 0));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i > 0, es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.Equal(0, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i > 0, es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.Equal(0, WaitHandle.WaitAny(es));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i > 0, es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.True(WaitHandle.WaitAll(es, 0));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.True(WaitHandle.WaitAll(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.True(WaitHandle.WaitAll(es));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
        }

        // Multi-wait with indexes 1 and 2 set
        es[1].Set();
        es[2].Set();
        Assert.Equal(1, WaitHandle.WaitAny(es, 0));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 2, es[i].WaitOne(0));
        }
        es[1].Set();
        es[2].Set();
        Assert.Equal(1, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 2, es[i].WaitOne(0));
        }
        es[1].Set();
        es[2].Set();
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 1 || i == 2, es[i].WaitOne(0));
        }

        // Multi-wait with all indexes reset
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, 0));
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
        }
    }

    [Fact]
    public static void SemaphoreTest()
    {
        // Constructor and dispose

        Assert.Throws<ArgumentOutOfRangeException>(() => new Semaphore(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Semaphore(0, 0));
        Assert.Throws<ArgumentException>(() => new Semaphore(2, 1));

        var s = new Semaphore(0, 1);
        Assert.False(s.WaitOne(0));
        s.Dispose();
        Assert.Throws<ObjectDisposedException>(() => s.Release());
        Assert.Throws<ObjectDisposedException>(() => s.WaitOne(0));

        s = new Semaphore(1, 1);
        Assert.True(s.WaitOne(0));

        // Signal and unsignal

        Assert.Throws<ArgumentOutOfRangeException>(() => s.Release(0));

        s = new Semaphore(1, 1);
        Assert.True(s.WaitOne(0));
        Assert.False(s.WaitOne(0));
        Assert.False(s.WaitOne(0));
        Assert.Equal(0, s.Release());
        Assert.True(s.WaitOne(0));
        Assert.Throws<SemaphoreFullException>(() => s.Release(2));
        Assert.Equal(0, s.Release());
        Assert.Throws<SemaphoreFullException>(() => s.Release());

        s = new Semaphore(1, 2);
        Assert.Throws<SemaphoreFullException>(() => s.Release(2));
        Assert.Equal(1, s.Release(1));
        Assert.True(s.WaitOne(0));
        Assert.True(s.WaitOne(0));
        Assert.Throws<SemaphoreFullException>(() => s.Release(3));
        Assert.Equal(0, s.Release(2));
        Assert.Throws<SemaphoreFullException>(() => s.Release());

        // Wait

        s = new Semaphore(1, 2);
        Assert.True(s.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.False(s.WaitOne(0));
        s.Release();
        Assert.True(s.WaitOne());
        Assert.False(s.WaitOne(0));
        s.Release(2);
        Assert.True(s.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        s.Release();
        Assert.True(s.WaitOne());

        s = new Semaphore(0, 2);
        Assert.False(s.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));

        s = null;

        // Multi-wait with all indexes signaled
        var ss =
            new Semaphore[]
            {
                new Semaphore(1, 1),
                new Semaphore(1, 1),
                new Semaphore(1, 1),
                new Semaphore(1, 1)
            };
        Assert.Equal(0, WaitHandle.WaitAny(ss, 0));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i > 0, ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.Equal(0, WaitHandle.WaitAny(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i > 0, ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.Equal(0, WaitHandle.WaitAny(ss));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i > 0, ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.True(WaitHandle.WaitAll(ss, 0));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.True(WaitHandle.WaitAll(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.True(WaitHandle.WaitAll(ss));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
        }

        // Multi-wait with indexes 1 and 2 signaled
        ss[1].Release();
        ss[2].Release();
        Assert.Equal(1, WaitHandle.WaitAny(ss, 0));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i == 2, ss[i].WaitOne(0));
        }
        ss[1].Release();
        ss[2].Release();
        Assert.Equal(1, WaitHandle.WaitAny(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i == 2, ss[i].WaitOne(0));
        }
        ss[1].Release();
        ss[2].Release();
        Assert.False(WaitHandle.WaitAll(ss, 0));
        Assert.False(WaitHandle.WaitAll(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i == 1 || i == 2, ss[i].WaitOne(0));
        }

        // Multi-wait with all indexes unsignaled
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(ss, 0));
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(ss, 0));
        Assert.False(WaitHandle.WaitAll(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
        }
    }

    // There is a race condition between a timed out WaitOne and a Set call not clearing the waiters list
    // in the wait subsystem (Unix only). More information can be found at 
    // https://github.com/dotnet/corert/issues/3616 and https://github.com/dotnet/corert/pull/3782.
    [Fact]
    public static void DoubleSetOnEventWithTimedOutWaiterShouldNotStayInWaitersList()
    {
        AutoResetEvent threadStartedEvent = new AutoResetEvent(false);
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        Thread thread = new Thread(() => {
            threadStartedEvent.Set();
            Thread.Sleep(50);
            resetEvent.Set();
            resetEvent.Set();
        });

        thread.IsBackground = true;
        thread.Start();
        threadStartedEvent.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
        resetEvent.WaitOne(50);
        thread.Join(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
    }

    [Fact]
    public static void MutexTest()
    {
        // Constructor and dispose

        var m = new Mutex();
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        m.Dispose();
        Assert.Throws<ObjectDisposedException>(() => m.ReleaseMutex());
        Assert.Throws<ObjectDisposedException>(() => m.WaitOne(0));

        m = new Mutex(false);
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();

        m = new Mutex(true);
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        m.ReleaseMutex();

        m = new Mutex(true);
        Assert.True(m.WaitOne(0));
        m.Dispose();
        Assert.Throws<ObjectDisposedException>(() => m.ReleaseMutex());
        Assert.Throws<ObjectDisposedException>(() => m.WaitOne(0));

        // Acquire and release

        m = new Mutex();
        VerifyThrowsApplicationException(() => m.ReleaseMutex());
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        VerifyThrowsApplicationException(() => m.ReleaseMutex());
        Assert.True(m.WaitOne(0));
        Assert.True(m.WaitOne(0));
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        m.ReleaseMutex();
        m.ReleaseMutex();
        VerifyThrowsApplicationException(() => m.ReleaseMutex());

        // Wait

        Assert.True(m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.True(m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        m.ReleaseMutex();
        m.ReleaseMutex();
        Assert.True(m.WaitOne());
        Assert.True(m.WaitOne());
        m.ReleaseMutex();
        m.ReleaseMutex();

        m = null;

        // Multi-wait with all indexes unlocked
        var ms =
            new Mutex[]
            {
                new Mutex(),
                new Mutex(),
                new Mutex(),
                new Mutex()
            };
        Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
        ms[0].ReleaseMutex();
        for (int i = 1; i < ms.Length; ++i)
        {
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
        Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        ms[0].ReleaseMutex();
        for (int i = 1; i < ms.Length; ++i)
        {
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
        Assert.Equal(0, WaitHandle.WaitAny(ms));
        ms[0].ReleaseMutex();
        for (int i = 1; i < ms.Length; ++i)
        {
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
        Assert.True(WaitHandle.WaitAll(ms, 0));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }

        // Multi-wait with indexes 0 and 3 locked
        ms[0].WaitOne(0);
        ms[3].WaitOne(0);
        Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        Assert.Equal(0, WaitHandle.WaitAny(ms));
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        Assert.True(WaitHandle.WaitAll(ms, 0));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        ms[3].ReleaseMutex();

        // Multi-wait with all indexes locked
        for (int i = 0; i < ms.Length; ++i)
        {
            Assert.True(ms[i].WaitOne(0));
        }
        Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
        ms[0].ReleaseMutex();
        Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        ms[0].ReleaseMutex();
        Assert.Equal(0, WaitHandle.WaitAny(ms));
        ms[0].ReleaseMutex();
        Assert.True(WaitHandle.WaitAll(ms, 0));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
    }

    private static void VerifyThrowsApplicationException(Action action)
    {
        // TODO: netstandard2.0 - After switching to ns2.0 contracts, use Assert.Throws<T> instead of this function
        // TODO: Enable this verification. There currently seem to be some reliability issues surrounding exceptions on Unix.
        //try
        //{
        //    action();
        //}
        //catch (Exception ex)
        //{
        //    if (ex.HResult == unchecked((int)0x80131600))
        //        return;
        //    Console.WriteLine(
        //        "VerifyThrowsApplicationException: Assertion failure - Assert.Throws<ApplicationException>: got {1}",
        //        ex.GetType());
        //    throw new AssertionFailureException(ex);
        //}
        //Console.WriteLine(
        //    "VerifyThrowsApplicationException: Assertion failure - Assert.Throws<ApplicationException>: got no exception");
        //throw new AssertionFailureException();
    }

    [Fact]
    [OuterLoop]
    public static void WaitDurationTest()
    {
        VerifyWaitDuration(
            new ManualResetEvent(false),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds);
        VerifyWaitDuration(
            new ManualResetEvent(true),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);

        VerifyWaitDuration(
            new AutoResetEvent(false),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds);
        VerifyWaitDuration(
            new AutoResetEvent(true),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);

        VerifyWaitDuration(
            new Semaphore(0, 1),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds);
        VerifyWaitDuration(
            new Semaphore(1, 1),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);

        VerifyWaitDuration(
            new Mutex(true),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);
        VerifyWaitDuration(
            new Mutex(false),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);
    }

    private static void VerifyWaitDuration(
        WaitHandle wh,
        int waitTimeoutMilliseconds,
        int expectedWaitTerminationAfterMilliseconds)
    {
        Assert.True(waitTimeoutMilliseconds > 0);
        Assert.True(expectedWaitTerminationAfterMilliseconds >= 0);

        var sw = new Stopwatch();
        sw.Restart();
        Assert.Equal(expectedWaitTerminationAfterMilliseconds == 0, wh.WaitOne(waitTimeoutMilliseconds));
        sw.Stop();
        int waitDurationDiffMilliseconds = (int)sw.ElapsedMilliseconds - expectedWaitTerminationAfterMilliseconds;
        Assert.True(waitDurationDiffMilliseconds >= AcceptableEarlyWaitTerminationDiffMilliseconds);
        Assert.True(waitDurationDiffMilliseconds <= AcceptableLateWaitTerminationDiffMilliseconds);
    }

    //[Fact] // This test takes a long time to run in release and especially in debug builds. Enable for manual testing.
    public static void MutexMaximumReacquireCountTest()
    {
        // Create a mutex with the maximum reacquire count
        var m = new Mutex();
        int tenPercent = int.MaxValue / 10;
        int progressPercent = 0;
        Console.Write("        0%");
        for (int i = 0; i >= 0; ++i)
        {
            if (i >= (progressPercent + 1) * tenPercent)
            {
                ++progressPercent;
                if (progressPercent < 10)
                {
                    Console.Write(" {0}%", progressPercent * 10);
                }
            }
            Assert.True(m.WaitOne(0));
        }
        Assert.Throws<OverflowException>(
            () =>
            {
                // Windows allows a slightly higher maximum reacquire count than this implementation
                Assert.True(m.WaitOne(0));
                Assert.True(m.WaitOne(0));
            });
        Console.WriteLine(" 100%");

        // Single wait fails
        Assert.Throws<OverflowException>(() => m.WaitOne(0));
        Assert.Throws<OverflowException>(() => m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.Throws<OverflowException>(() => m.WaitOne());

        var e0 = new AutoResetEvent(false);
        var s0 = new Semaphore(0, 1);
        var e1 = new AutoResetEvent(false);
        var s1 = new Semaphore(0, 1);
        var h = new WaitHandle[] { e0, s0, m, e1, s1 };
        Action<bool, bool, bool, bool> init =
            (signale0, signals0, signale1, signals1) =>
            {
                if (signale0)
                    e0.Set();
                else
                    e0.Reset();
                s0.WaitOne(0);
                if (signals0)
                    s0.Release();
                if (signale1)
                    e1.Set();
                else
                    e1.Reset();
                s1.WaitOne(0);
                if (signals1)
                    s1.Release();
            };
        Action<bool, bool, bool, bool> verify =
            (e0signaled, s0signaled, e1signaled, s1signaled) =>
            {
                Assert.Equal(e0signaled, e0.WaitOne(0));
                Assert.Equal(s0signaled, s0.WaitOne(0));
                Assert.Throws<OverflowException>(() => m.WaitOne(0));
                Assert.Equal(e1signaled, e1.WaitOne(0));
                Assert.Equal(s1signaled, s1.WaitOne(0));
                init(e0signaled, s0signaled, e1signaled, s1signaled);
            };

        // WaitAny succeeds when a signaled object is before the mutex
        init(true, true, true, true);
        Assert.Equal(0, WaitHandle.WaitAny(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(false, true, true, true);
        Assert.Equal(1, WaitHandle.WaitAny(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(false, false, true, true);

        // WaitAny fails when all objects before the mutex are unsignaled
        init(false, false, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAny(h, 0));
        verify(false, false, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAny(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(false, false, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAny(h));
        verify(false, false, true, true);

        // WaitAll fails and does not unsignal any signaled object
        init(true, true, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAll(h, 0));
        verify(true, true, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAll(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(true, true, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAll(h));
        verify(true, true, true, true);

        m.Dispose();
    }
}

internal static class ThreadTestHelpers
{
    public const int ExpectedTimeoutMilliseconds = 50;
    public const int ExpectedMeasurableTimeoutMilliseconds = 500;
    public const int UnexpectedTimeoutMilliseconds = 1000 * 30;
}

internal sealed class Stopwatch
{
    private DateTime _startTimeTicks;
    private DateTime _endTimeTicks;

    public void Restart() => _startTimeTicks = DateTime.UtcNow;
    public void Stop() => _endTimeTicks = DateTime.UtcNow;
    public long ElapsedMilliseconds => (long)(_endTimeTicks - _startTimeTicks).TotalMilliseconds;
}

internal static class Assert
{
    public static void False(bool condition)
    {
        if (!condition)
            return;
        Console.WriteLine("Assertion failure - Assert.False");
        throw new AssertionFailureException();
    }

    public static void True(bool condition)
    {
        if (condition)
            return;
        Console.WriteLine("Assertion failure - Assert.True");
        throw new AssertionFailureException();
    }

    public static void Same<T>(T expected, T actual) where T : class
    {
        if (expected == actual)
            return;
        Console.WriteLine("Assertion failure - Assert.Same({0}, {1})", expected, actual);
        throw new AssertionFailureException();
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
            return;
        Console.WriteLine("Assertion failure - Assert.Equal({0}, {1})", expected, actual);
        throw new AssertionFailureException();
    }

    public static void NotEqual<T>(T notExpected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(notExpected, actual))
            return;
        Console.WriteLine("Assertion failure - Assert.NotEqual({0}, {1})", notExpected, actual);
        throw new AssertionFailureException();
    }

    public static void Throws<T>(Action action) where T : Exception
    {
        // TODO: Enable Assert.Throws<T> tests. There currently seem to be some reliability issues surrounding exceptions on Unix.
        //try
        //{
        //    action();
        //}
        //catch (T ex)
        //{
        //    if (ex.GetType() == typeof(T))
        //        return;
        //    Console.WriteLine("Assertion failure - Assert.Throws<{0}>: got {1}", typeof(T), ex.GetType());
        //    throw new AssertionFailureException(ex);
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Assertion failure - Assert.Throws<{0}>: got {1}", typeof(T), ex.GetType());
        //    throw new AssertionFailureException(ex);
        //}
        //Console.WriteLine("Assertion failure - Assert.Throws<{0}>: got no exception", typeof(T));
        //throw new AssertionFailureException();
    }

    public static void Null(object value)
    {
        if (value == null)
            return;
        Console.WriteLine("Assertion failure - Assert.Null");
        throw new AssertionFailureException();
    }

    public static void NotNull(object value)
    {
        if (value != null)
            return;
        Console.WriteLine("Assertion failure - Assert.NotNull");
        throw new AssertionFailureException();
    }
}

internal class AssertionFailureException : Exception
{
    public AssertionFailureException()
    {
    }

    public AssertionFailureException(string message) : base(message)
    {
    }

    public AssertionFailureException(Exception innerException) : base(null, innerException)
    {
    }
}

internal class FactAttribute : Attribute
{
}

internal class OuterLoopAttribute : Attribute
{
}
