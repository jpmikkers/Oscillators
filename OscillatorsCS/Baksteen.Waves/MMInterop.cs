using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Baksteen.Waves;

public static class MMInterop
{
    /* flags for dwSupport field of WAVEOUTCAPS */
    [Flags]
    public enum WAVEOUTSUPPORTFLAGS : uint
    {
        PITCH = 0x0001,  /* supports pitch control */
        PLAYBACKRATE = 0x0002,   /* supports playback rate control */
        VOLUME = 0x0004,   /* supports volume control */
        LRVOLUME = 0x0008,  /* separate left-right volume control */
        SYNC = 0x0010,
        SAMPLEACCURATE = 0x0020,
    };

    /* defines for dwFormat field of WAVEINCAPS and WAVEOUTCAPS */
    [Flags]
    public enum WAVEFORMATFLAGS
    {
        INVALIDFORMAT = 0x00000000,       /* invalid format */
        FORMAT_1M08 = 0x00000001,       /* 11.025 kHz, Mono,   8-bit  */
        FORMAT_1S08 = 0x00000002,       /* 11.025 kHz, Stereo, 8-bit  */
        FORMAT_1M16 = 0x00000004,       /* 11.025 kHz, Mono,   16-bit */
        FORMAT_1S16 = 0x00000008,       /* 11.025 kHz, Stereo, 16-bit */
        FORMAT_2M08 = 0x00000010,       /* 22.05  kHz, Mono,   8-bit  */
        FORMAT_2S08 = 0x00000020,       /* 22.05  kHz, Stereo, 8-bit  */
        FORMAT_2M16 = 0x00000040,       /* 22.05  kHz, Mono,   16-bit */
        FORMAT_2S16 = 0x00000080,       /* 22.05  kHz, Stereo, 16-bit */
        FORMAT_4M08 = 0x00000100,       /* 44.1   kHz, Mono,   8-bit  */
        FORMAT_4S08 = 0x00000200,       /* 44.1   kHz, Stereo, 8-bit  */
        FORMAT_4M16 = 0x00000400,       /* 44.1   kHz, Mono,   16-bit */
        FORMAT_4S16 = 0x00000800,       /* 44.1   kHz, Stereo, 16-bit */
        FORMAT_44M08 = 0x00000100,       /* 44.1   kHz, Mono,   8-bit  */
        FORMAT_44S08 = 0x00000200,       /* 44.1   kHz, Stereo, 8-bit  */
        FORMAT_44M16 = 0x00000400,       /* 44.1   kHz, Mono,   16-bit */
        FORMAT_44S16 = 0x00000800,       /* 44.1   kHz, Stereo, 16-bit */
        FORMAT_48M08 = 0x00001000,       /* 48     kHz, Mono,   8-bit  */
        FORMAT_48S08 = 0x00002000,       /* 48     kHz, Stereo, 8-bit  */
        FORMAT_48M16 = 0x00004000,       /* 48     kHz, Mono,   16-bit */
        FORMAT_48S16 = 0x00008000,       /* 48     kHz, Stereo, 16-bit */
        FORMAT_96M08 = 0x00010000,       /* 96     kHz, Mono,   8-bit  */
        FORMAT_96S08 = 0x00020000,       /* 96     kHz, Stereo, 8-bit  */
        FORMAT_96M16 = 0x00040000,       /* 96     kHz, Mono,   16-bit */
        FORMAT_96S16 = 0x00080000,       /* 96     kHz, Stereo, 16-bit */
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WAVEOUTCAPS2
    {
        public ushort wMid; // Manufacturer ID
        public ushort wPid; // Product ID
        public uint vDriverVersion; // Driver version
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname; // Product name
        public WAVEFORMATFLAGS dwFormats; // Supported formats
        public ushort wChannels; // Number of channels
        public ushort wReserved1; // Reserved
        public WAVEOUTSUPPORTFLAGS dwSupport; // Supported functionality
        public Guid ManufacturerGuid; // Manufacturer GUID
        public Guid ProductGuid;
        public Guid NameGuid;   /* for name lookup in registry */
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WAVEINCAPS2
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public WAVEFORMATFLAGS dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
        public Guid ManufacturerGuid;
        public Guid ProductGuid;
        public Guid NameGuid;
    }

    /* wFormatTag field of WAVEFORMAT */
    public enum WAVEFORMATTAG : ushort
    {
        UNKNOWN = 0x0000, /* Microsoft Corporation */
        PCM = 0x0001, // PCM format
        ADPCM = 0x0002, /* Microsoft Corporation */
        IEEE_FLOAT = 0x0003, /* Microsoft Corporation */
        VSELP = 0x0004, /* Compaq Computer Corp. */
        IBM_CVSD = 0x0005, /* IBM Corporation */
        ALAW = 0x0006, /* Microsoft Corporation */
        MULAW = 0x0007, /* Microsoft Corporation */
        EXTENSIBLE = 0xFFFE, /* Microsoft Corporation */
    }

    // MMREG.h has WAVE_FORMAT_IEEE_FLOAT and WAVEFORMATEXTENSIBLE definitions 
    // see also: https://learn.microsoft.com/en-us/windows/win32/api/mmreg/ns-mmreg-waveformatextensible?redirectedfrom=MSDN

    // declare WAVEOUTOPEN interop
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
    public struct WAVEFORMATEX
    {
        public WAVEFORMATTAG wFormatTag;       // Format type
        public ushort nChannels;        // Number of channels (mono, stereo, etc.)
        public uint nSamplesPerSec;     // Sample rate (e.g., 44100 Hz)
        public uint nAvgBytesPerSec;    // Average bytes per second
        public ushort nBlockAlign;      // Block alignment (bytes per sample frame)
        public ushort wBitsPerSample;   // Bits per sample (e.g., 16 bits)
        public ushort cbSize;           // Size of extra format information (if any)
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SamplesUnion
    {
        [FieldOffset(0)] // Starting at byte offset 0
        public ushort wValidBitsPerSample;       /* bits of precision  */

        [FieldOffset(0)] // Overlapping at byte offset 0
        public ushort wSamplesPerBlock;          /* valid if wBitsPerSample==0 */

        [FieldOffset(0)] // Overlapping at byte offset 0
        public ushort wReserved;                 /* If neither applies, set to zero. */
    }

    // Define the WAVEFORMATEXTENSIBLE structure for interop
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WAVEFORMATEXTENSIBLE
    {
        public WAVEFORMATEX Format; // Base structure
        public SamplesUnion Samples; // Valid bits per sample or samples per block
        public uint dwChannelMask; // Speaker channel configuration
        public Guid SubFormat; // Format GUID for audio type
    }

    [Flags]
    public enum ChannelFlags : uint
    {
        // Speaker Positions for dwChannelMask in WAVEFORMATEXTENSIBLE:
        SPEAKER_FRONT_LEFT              =0x1     ,
        SPEAKER_FRONT_RIGHT             =0x2     ,
        SPEAKER_FRONT_CENTER            =0x4     ,
        SPEAKER_LOW_FREQUENCY           =0x8     ,
        SPEAKER_BACK_LEFT               =0x10    ,
        SPEAKER_BACK_RIGHT              =0x20    ,
        SPEAKER_FRONT_LEFT_OF_CENTER    =0x40    ,
        SPEAKER_FRONT_RIGHT_OF_CENTER   =0x80    ,
        SPEAKER_BACK_CENTER             =0x100   ,
        SPEAKER_SIDE_LEFT               =0x200   ,
        SPEAKER_SIDE_RIGHT              =0x400   ,
        SPEAKER_TOP_CENTER              =0x800   ,
        SPEAKER_TOP_FRONT_LEFT          =0x1000  ,
        SPEAKER_TOP_FRONT_CENTER        =0x2000  ,
        SPEAKER_TOP_FRONT_RIGHT         =0x4000  ,
        SPEAKER_TOP_BACK_LEFT           =0x8000  ,
        SPEAKER_TOP_BACK_CENTER         =0x10000 ,
        SPEAKER_TOP_BACK_RIGHT          =0x20000 ,

        // Bit mask locations reserved for future use
        //#define SPEAKER_RESERVED                0x7FFC0000
        // Used to specify that any possible permutation of speaker configurations
        //#define SPEAKER_ALL                     0x80000000

    }

    public static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new("00000003-0000-0010-8000-00aa00389b71");

    /* flags for dwFlags parameter in waveOutOpen() and waveInOpen() */
    [Flags]
    // waveOutOpen dwFlags enum
    public enum WAVEINOUTOPENFLAGS : uint
    {
        WAVE_FORMAT_QUERY = 0x0001,
        WAVE_ALLOWSYNC = 0x0002,
        WAVE_MAPPED = 0x0004,
        WAVE_FORMAT_DIRECT = 0x0008,
        //WAVE_FORMAT_DIRECT_QUERY                  = (WAVE_FORMAT_QUERY | WAVE_FORMAT_DIRECT)
        WAVE_MAPPED_DEFAULT_COMMUNICATION_DEVICE = 0x0010,
        CALLBACK_NULL = 0x00000000, // No callback
        CALLBACK_WINDOW = 0x00010000, // Window callback
        //CALLBACK_TASK = 0x00020000, // Task callback
        CALLBACK_THREAD = 0x00020000, // thread ID replaces 16 bit task
        CALLBACK_FUNCTION = 0x00030000, // Function callback
        CALLBACK_EVENT = 0x00050000, // Event callback
        //CALLBACK_TYPEMASK = 0x00070000, // Mask for callback type
        //CALLBACK_ALL = 0x000F0000, // All callbacks
    }

    public enum MMMessage : uint
    {
        MM_WOM_OPEN = 0x3BB,           /* waveform output */
        MM_WOM_CLOSE = 0x3BC,
        MM_WOM_DONE = 0x3BD,
        MM_WIM_OPEN = 0x3BE,           /* waveform input */
        MM_WIM_CLOSE = 0x3BF,
        MM_WIM_DATA = 0x3C0,
    }

    /* flags for dwFlags field of WAVEHDR */
    [Flags]
    public enum WAVEHDRFLAGS : uint
    {
        WHDR_DONE = 0x00000001,  /* done bit */
        WHDR_PREPARED = 0x00000002,  /* set if this header has been prepared */
        WHDR_BEGINLOOP = 0x00000004,  /* loop start block */
        WHDR_ENDLOOP = 0x00000008,  /* loop end block */
        WHDR_INQUEUE = 0x00000010,  /* reserved for driver */
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEHDR
    {
        public IntPtr lpData;          // Pointer to the waveform buffer
        public uint dwBufferLength;    // Length of the buffer
        public uint dwBytesRecorded;   // Number of bytes recorded (used for input)
        public IntPtr dwUser;          // User-defined data
        public WAVEHDRFLAGS dwFlags;           // Flags for the buffer
        public uint dwLoops;           // Number of loop iterations (for output)
        public IntPtr lpNext;          // Reserved for linked headers
        public IntPtr reserved;        // Reserved
    }

    [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint waveOutGetDevCaps(uint uDeviceID, ref WAVEOUTCAPS2 pwoc, uint cbwoc);

    [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint waveOutGetNumDevs();

    [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint waveInGetDevCaps(uint uDeviceID, ref WAVEINCAPS2 pwic, uint cbwic);

    [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint waveInGetNumDevs();

    [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool mciGetErrorString(uint fdwError, System.Text.StringBuilder lpszErrorText, uint cchErrorText);
    // Delegate for the callback function
    public delegate void WaveOutProc(IntPtr hWaveOut, MMMessage uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);
    public delegate void WaveInProc(IntPtr hWaveIn, MMMessage uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveOutOpen(out IntPtr hWaveOut, uint uDeviceID, ref WAVEFORMATEXTENSIBLE lpFormat, WaveOutProc dwCallback, IntPtr dwInstance, WAVEINOUTOPENFLAGS dwFlags);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveInOpen(out IntPtr hWaveIn, uint uDeviceID, ref WAVEFORMATEXTENSIBLE lpFormat, WaveInProc dwCallback, IntPtr dwInstance, WAVEINOUTOPENFLAGS dwFlags);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveInPrepareHeader(IntPtr hWaveIn, IntPtr lpWaveHdr, uint cbWaveHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveInUnprepareHeader(IntPtr hWaveIn, IntPtr lpWaveHdr, uint cbWaveHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveInAddBuffer(IntPtr hWaveIn, IntPtr lpWaveHdr, uint cbWaveHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveInStart(IntPtr hWaveIn);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveInStop(IntPtr hWaveIn);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveInClose(IntPtr hWaveIn);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveHdr, uint cbWaveHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveHdr, uint cbWaveHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveHdr, uint cbWaveHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    public static extern uint waveOutClose(IntPtr hWaveOut);

    public static string GetErrorString(uint errorCode)
    {
        System.Text.StringBuilder errorText = new System.Text.StringBuilder(256);
        if (mciGetErrorString(errorCode, errorText, (uint)errorText.Capacity))
        {
            return errorText.ToString();
        }
        else
        {
            return $"Unknown error code: {errorCode}";
        }
    }

    public static void AssertSuccess(uint errorCode)
    {
        if (errorCode != 0) // MMSYSERR_NOERROR
        {
            throw new MMSysException(GetErrorString(errorCode)) { Code = errorCode };
        }
    }
}
