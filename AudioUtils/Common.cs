using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUtils
{
  public static class Common
  {
    private const string DEFAULT_PLAYBACK_DEVICE_NAME = "- Default Playback Device -";

    public static string[] GetAllPlaybackDevices(bool includeDefaultPlayback)
    {
      List<string> stringList = new List<string>();
      for (int devNumber = 0; devNumber < WaveOut.DeviceCount; ++devNumber)
      {
        WaveOutCapabilities capabilities = WaveOut.GetCapabilities(devNumber);
        stringList.Add(capabilities.ProductName);
      }
      stringList.Sort();
      if (includeDefaultPlayback)
        stringList.Insert(0, "- Default Playback Device -");
      return stringList.ToArray();
    }

    public static int GetPlaybackDeviceFromDeviceName(string deviceName)
    {
      if (deviceName == "- Default Playback Device -")
        return -1;
      for (int devNumber = -1; devNumber < WaveOut.DeviceCount; ++devNumber)
      {
        WaveOutCapabilities capabilities = WaveOut.GetCapabilities(devNumber);
        if (deviceName == capabilities.ProductName)
          return devNumber;
      }
      return -1;
    }

    public static Task PlaySound(string fileName, int audioDeviceNumber = -1) => Task.Run((Action) (() =>
    {
      using (AudioFileReader audioFileReader = new AudioFileReader(fileName))
      {
        using (WaveOutEvent waveOutEvent = new WaveOutEvent())
        {
          waveOutEvent.DeviceNumber = audioDeviceNumber;
          waveOutEvent.Init((IWaveProvider) audioFileReader);
          waveOutEvent.Play();
          while (waveOutEvent.PlaybackState == PlaybackState.Playing)
            Thread.Sleep(1000);
          waveOutEvent.Stop();
        }
      }
    }));

    public static Task PlaySound(string fileName, string audioDeviceName)
    {
      int deviceFromDeviceName = Common.GetPlaybackDeviceFromDeviceName(audioDeviceName);
      return Common.PlaySound(fileName, deviceFromDeviceName);
    }

    public static Task PlayStream(MemoryStream streamAudio, string audioDeviceName)
    {
      int deviceFromDeviceName = Common.GetPlaybackDeviceFromDeviceName(audioDeviceName);
      return Common.PlayStream(streamAudio, deviceFromDeviceName);
    }

    public static Task PlayStream(MemoryStream streamAudio, int audioDeviceNumber = -1) => Task.Run((Action) (() =>
    {
      if (streamAudio == null)
        return;
      streamAudio.Seek(0L, SeekOrigin.Begin);
      using (WaveFileReader waveFileReader = new WaveFileReader((Stream) streamAudio))
      {
        using (WaveOutEvent waveOutEvent = new WaveOutEvent())
        {
          waveOutEvent.DeviceNumber = audioDeviceNumber;
          waveOutEvent.Init((IWaveProvider) waveFileReader);
          waveOutEvent.Play();
          while (waveOutEvent.PlaybackState == PlaybackState.Playing)
            Thread.Sleep(1000);
          waveOutEvent.Stop();
        }
      }
    }));
  }
}
