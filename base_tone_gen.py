#!/usr/bin/env python3
import struct
import math
import os

def create_simple_wav(filename, freq=440, duration=0.5, volume=0.3):
    """Create a simple sine wave WAV file"""
    sample_rate = 44100
    samples = int(sample_rate * duration)
    
    # Create WAV header
    header = struct.pack('<4sI4s4sIHHIIHH4sI',
        b'RIFF', 36 + samples * 2, b'WAVE', b'fmt ', 16, 1, 1,
        sample_rate, sample_rate * 2, 2, 16, b'data', samples * 2)
    
    # Create audio samples
    audio_data = b''
    for i in range(samples):
        sample = volume * math.sin(2 * math.pi * freq * i / sample_rate)
        audio_data += struct.pack('<h', int(sample * 32767))
    
    # Write file
    with open(filename, 'wb') as f:
        f.write(header + audio_data)
    
    return filename

# Create the base tone
if __name__ == "__main__":
    create_simple_wav("base_tone.wav")
    print("Created base_tone.wav")
