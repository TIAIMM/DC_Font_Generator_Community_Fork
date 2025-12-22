using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace INI_RW
{
    /// <summary>

    /// Create a New INI file to store or load data

    /// </summary>
    public class IniFile
    {
        public bool Enable = false;
        public string path;
        public long buffersize = 0;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,string key, string def, StringBuilder retVal,int size, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileSection(string section, IntPtr lpReturnedString,int nSize, string lpFileName);


        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public IniFile(string INIPath)
        {
            path = INIPath;
            if (File.Exists(path))
            {
                FileInfo fInfo = new FileInfo(path);
                buffersize = fInfo.Length;
                Enable = true;
            }
            
        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section">Section name</PARAM>
        /// <PARAM name="Key">Key Name</PARAM>
        /// <PARAM name="Value">Value Name</PARAM>
        public void IniWriteValue(string Section, string Key, string Value)
        {
            try
            {
                WritePrivateProfileString(Section, Key, Value, this.path);
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }
        }


        
        // The Function called to obtain the EntryKey Value from
        // the given SectionHeader and EntryKey string passed, then returned
        public string IniReadValue(string section, string entry)
        {
            try
            {
                //    Sets the maxsize buffer to 250, if the more
                //    is required then doubles the size each time. 
                for (int maxsize = 250; true; maxsize *= 2)
                {
                    //    Obtains the EntryValue information and uses the StringBuilder
                    //    Function to and stores them in the maxsize buffers (result).
                    //    Note that the SectionHeader and EntryKey values has been passed.
                    StringBuilder result = new StringBuilder(maxsize);
                    int size = GetPrivateProfileString(section, entry, "", result, maxsize, path);
                    if (size < maxsize - 1)
                    {
                        // Returns the value gathered from the EntryKey
                        return result.ToString();
                    }
                }
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
                return "";
            }
        }

        /// <summary>
        /// Reads a whole section of the INI file.
        /// </summary>
        /// <param name="section">Section to read.</param>
        public string[] ReadSection(string section)
        {
            //const int bufferSize = 2048;

            StringBuilder returnedString = new StringBuilder();
            
            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)buffersize);
            try
            {
                int bytesReturned = GetPrivateProfileSection(section, pReturnedString, (int)buffersize, path);

                byte[] byte_data = new byte[bytesReturned];
                int byte_count = 0;
                for (int i = 0; i < bytesReturned; i++) //bytesReturned -1 to remove trailing \0
                {
                    byte data = Marshal.ReadByte(new IntPtr((uint)pReturnedString + (uint)i));
                    if (data == 0) //string end
                    {
                        if (i != bytesReturned - 1) byte_data[byte_count++] = data;
                        byte[] tmp = new byte[byte_count];
                        for (int s = 0; s < byte_count; s++)
                            tmp[s] = byte_data[s];

                        returnedString.Append(Encoding.Default.GetString(tmp));
                        byte_count = 0;
                    }
                    else
                    {
                        byte_data[byte_count++] = data;
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pReturnedString);
            }

            string sectionData = returnedString.ToString();
            return sectionData.Split('\0');
        }

    }
}