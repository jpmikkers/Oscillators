# Oscillators C#
This is a port of the oscillator repo to C#/dotnet, initial translation was performed using Claude Haiku 4.5. Further optimisations (including AVX vectorization) and GUI were done manually. 

If you're interested in the Swift or C++ versions, check out the excellent original repo:
https://github.com/alexandrefrancois/Oscillators

For more details on the algorithms and implementation, see the following youtube presentation by the author, Alexandre François:
https://www.youtube.com/watch?v=QbNPA5QJ6OU

## Oscillators GUI

The package includes a simple GUI to visualize a realtime constant Q spectrogram from your soundcard input. This was built using Avalonia UI, a cross-platform UI framework for .NET. The soundcard library uses windows MMDevice API, so the GUI is only available on Windows for now. 

## Sample video
spectrogram of someone speaking. Try to guess the sentence :)

<video width="640" height="360" controls>
  <source src="https://github.com/user-attachments/assets/afdb183e-9566-4a78-b4fb-2d3e753d1b1e" type="video/mp4">
  Your browser does not support the video tag.  
</video>

https://github.com/user-attachments/assets/afdb183e-9566-4a78-b4fb-2d3e753d1b1e

