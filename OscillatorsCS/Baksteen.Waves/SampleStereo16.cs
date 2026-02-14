namespace Baksteen.Waves;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0, Size = 4)]
public struct SampleStereo16
{
    public short Left;
    public short Right;
}
//        return;


//        Console.ReadLine();

//        // also check out https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativememory?view=net-9.0

//        nint ptr;
//    unsafe
//        {
//            ptr = (nint) NativeMemory.AlignedAlloc(65536, 4);
//    NativeMemory.Clear((void*) ptr, 65536);
//        }

//        //var memory = Marshal.AllocHGlobal(65536); // Allocate memory for the buffer
//        //unsafe
//        //{
//        //    // Create a Span<byte> over the allocated memory
//        //    Span<byte> span = new Span<byte>((void*)memory, 65536);

//        //    // Zero out the memory
//        //    span.Clear();
//        //}

//        var waveHeader = new WAVEHDR
//        {
//            lpData = ptr, // Allocate memory for the buffer
//            dwBufferLength = 65536,
//            dwBytesRecorded = 0,
//            dwUser = IntPtr.Zero,
//            dwFlags = WAVEHDRFLAGS.WHDR_BEGINLOOP | WAVEHDRFLAGS.WHDR_ENDLOOP,
//            dwLoops = 20,
//            lpNext = IntPtr.Zero,
//            reserved = IntPtr.Zero
//        };

//            // Prepare the header
//        err = waveOutPrepareHeader(hWaveOut, ref waveHeader, (uint) Marshal.SizeOf<WAVEHDR>());
//AssertSuccess(err);

//err = waveOutWrite(hWaveOut, ref waveHeader, (uint) Marshal.SizeOf<WAVEHDR>());
//AssertSuccess(err);

//Console.ReadLine();
//        err = waveOutPrepareHeader(hWaveOut, ref waveHeader, (uint) Marshal.SizeOf<WAVEHDR>());
//AssertSuccess(err);

//err = waveOutWrite(hWaveOut, ref waveHeader, (uint) Marshal.SizeOf<WAVEHDR>());
//AssertSuccess(err);

//Console.ReadLine();
//    }
//        // Unprepare the header
//        result = waveOutUnprepareHeader(hWaveOut, ref waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
//        if (result == 0) // MMSYSERR_NOERROR
//        {
//            Console.WriteLine("Header unprepared successfully.");
//        }
//        else
//        {
//            Console.WriteLine($"Failed to unprepare header. Error code: {result}");
//        }

//        // Free the allocated memory
//        Marshal.FreeHGlobal(waveHeader.lpData);
//    }
//}