using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace PickersUtil
{
  public static class Pickers
  {
    public static string OpenFilePicker(string title, string initialDirectory, string fileFilter)
    {
      string result = (string) null;
      Thread thread = new Thread((ThreadStart) (() =>
      {
        using (OpenFileDialog openFileDialog = new OpenFileDialog()
        {
          InitialDirectory = initialDirectory,
          Title = title,
          Filter = fileFilter,
          RestoreDirectory = true
        })
        {
          if (openFileDialog.ShowDialog() != DialogResult.OK)
            return;
          result = openFileDialog.FileName;
        }
      }));
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();
      return result;
    }

    public static string SaveFilePicker(string title, string initialDirectory, string fileFilter)
    {
      string result = (string) null;
      Thread thread = new Thread((ThreadStart) (() =>
      {
        using (SaveFileDialog saveFileDialog = new SaveFileDialog()
        {
          InitialDirectory = initialDirectory,
          Title = title,
          Filter = fileFilter,
          RestoreDirectory = true
        })
        {
          if (saveFileDialog.ShowDialog() != DialogResult.OK)
            return;
          result = saveFileDialog.FileName;
        }
      }));
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();
      return result;
    }

    public static string FolderPicker(string title, Environment.SpecialFolder? initialDirecotry)
    {
      string result = (string) null;
      Thread thread = new Thread((ThreadStart) (() =>
      {
        using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog()
        {
          ShowNewFolderButton = true,
          Description = title
        })
        {
          if (initialDirecotry.HasValue)
            folderBrowserDialog.RootFolder = initialDirecotry.Value;
          if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            return;
          result = folderBrowserDialog.SelectedPath;
        }
      }));
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();
      return result;
    }

    public static bool SetJsonPropertyValue(object obj, string propertyName, object value)
    {
      try
      {
        if (obj == null || string.IsNullOrEmpty(propertyName))
          return false;
        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
          foreach (object customAttribute in property.GetCustomAttributes(true))
          {
            if (customAttribute is JsonPropertyAttribute propertyAttribute && propertyAttribute.PropertyName == propertyName)
            {
              property.SetValue(obj, value);
              return true;
            }
          }
        }
      }
      catch (Exception ex)
      {
      }
      return false;
    }
  }
}
