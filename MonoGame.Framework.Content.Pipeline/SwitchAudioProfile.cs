// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;

namespace Microsoft.Xna.Framework.Content.Pipeline.Audio
{
    internal class SwitchAudioProfile : AudioProfile
    {
        public override bool Supports(TargetPlatform platform)
        {
            return platform == TargetPlatform.Switch;
        }

        public override ConversionQuality ConvertAudio(TargetPlatform platform, ConversionQuality quality, AudioContent content)
        {
            // Best quality remains uncompressed PCM because
            // Switch has a pretty good amount of system memory.
            if (quality == ConversionQuality.Best)
                return DefaultAudioProfile.ConvertToFormat(content, ConversionFormat.Pcm, quality, null);

            ConvertAudio(quality, content);
            return quality;
        }

        private void ConvertAudio(ConversionQuality quality, AudioContent content)
        {
            // Write out the input content in the required target quality.
            var channels = content.Format.ChannelCount;
            var inputFile = Path.GetTempFileName() + ".wav";
            {
                var targetRate = QualityToBitRate(quality);
                if (channels == 1)
                    targetRate /= 2;
                var targetSampleRate = QualityToSampleRate(quality, content.Format.SampleRate);
                DefaultAudioProfile.WritePcmFile(content, inputFile, targetRate, targetSampleRate);
            }

            var outputFile = Path.GetTempFileName() + ".adpcm";

            // Pass the data thru to Nintendo's ADPCM tool to convert.
            var toolPath = Environment.ExpandEnvironmentVariables(@"%NINTENDO_SDK_ROOT%\Tools\Audio\AdpcmTools\AdpcmEncoder.exe");

            try
            {
                // TODO: Work out loop markers eventually.

                string stdout;
                string stderr;
                var result = ExternalTool.Run(toolPath, "-o " + outputFile + " -v " + inputFile, out stdout, out stderr);

                // A positive value means errors and we need to stop processing
                if (result > 0)
                    throw new PipelineException("AdpcmEncoder exited with non-zero exit code: \n" + stdout + "\n" + stderr);
                
                // Load up the converted audio.
                var data = File.ReadAllBytes(outputFile);
                                
                // For stereo sounds append the second channel to the end of the first.
                if (channels == 2)
                {
                    var path2 = Path.Combine(Path.GetDirectoryName(outputFile), Path.GetFileNameWithoutExtension(outputFile) + "_2.adpcm");
                    var data2 = File.ReadAllBytes(path2);
                    var data3 = new byte[data.Length + data2.Length];
                    Array.Copy(data, data3, data.Length);
                    Array.Copy(data2, 0, data3, data.Length, data2.Length);
                    data = data3;
                }

                // Set the new audio data.
                var audioFormat = new AudioFormat(0, 0, 0, channels, -1, 0);

                content.SetData(data, audioFormat, content.Duration, 0, 0);
            }
            finally
            {
                // Cleanup.
                ExternalTool.DeleteFile(inputFile);
                ExternalTool.DeleteFile(outputFile);
            }
        }

        public override ConversionQuality ConvertStreamingAudio(TargetPlatform platform, ConversionQuality quality, AudioContent content, ref string outputFileName)
        {
            // Make sure the output folder for the file exists.
            Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));
            outputFileName = Path.ChangeExtension(outputFileName, ".opus");

            // Write out the input audio file to a high quality PCM file first.
            var inputFile = Path.GetTempFileName() + ".wav";
            DefaultAudioProfile.WritePcmFile(content, inputFile, 192000, 48000);

            // Get a target bitrate from the quality value.
            var bitrate = QualityToBitRate(quality);

            var toolPath = Environment.ExpandEnvironmentVariables(@"%NINTENDO_SDK_ROOT%\Tools\Codec\AudioCodec\OpusEncoder.exe");

            try
            {
                string stdout;
                string stderr;
                var result = ExternalTool.Run(toolPath, "-o " + outputFileName + " --bitrate=" + bitrate + " --bitrate-control=cvbr -v " + inputFile, out stdout, out stderr);
                if (result != 0)
                    throw new PipelineException("OpusEncoder exited with non-zero exit code: \n" + stdout + "\n" + stderr);
            }
            finally
            {
                ExternalTool.DeleteFile(inputFile);
            }

            return quality;
        }
    }
}
