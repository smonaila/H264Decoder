using System;
using System.IO;

namespace decoder.utilities;

public static class Utilities
{
    /// <summary>
    /// Creates a diretory at the specified directory Path.
    /// </summary>
    /// <param name="directoryPath">the path to create the directory.</param>
    /// <returns>the information related to the created directory.</returns>
    /// <exception cref="Exception">the exeption that will be generated for any error that may occur during the directory creation process.</exception>
    public static DirectoryInfo GetDirectory(string directoryPath)
    {
        try
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            if (!Directory.Exists(directoryPath))
            {
                directoryInfo.Create();
            }
            return directoryInfo;
        }
        catch (System.Exception ex)
        {
            throw new Exception("Directory could not be created!", ex);
        }
    }

    public static bool SaveFile(string directoryPath, string fileName, byte[] bytesFiles)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(string.Format(@"{0}\{1}", directoryPath, fileName));               
            }
            else
            {
                if (Path.Exists(string.Format(@"{0}\{1}", directoryPath, fileName)))
                {
                    File.Delete(string.Format(@"{0}\{1}", directoryPath, fileName));
                }                
            }            
            File.WriteAllBytes(string.Format(@"{0}\{1}", directoryPath, fileName), bytesFiles); 
            return true;
        }
        catch (System.Exception ex)
        {            
            return false;
        }
    }
}