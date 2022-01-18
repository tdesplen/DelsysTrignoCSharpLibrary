/* Author: Tyler Desplenter
 * Creation: 15-06-2015
 * Last Modified: 17-06-2015
 * Summary: The Logger object is used to log driver events and data collection timing information.
 */

using System;
using System.IO;

namespace DelsysTrignoCSharpLibrary
{
    class Logger
    {
        private StreamWriter sw; //stream writer for writing to file
        private string loglock = ""; //lock for writing

        /// <summary>
        /// A method that initializes the logger by opening a stream writer to the desired file.
        /// </summary>
        /// <param name="logFileName"> A string containing the filename. </param>
        public void Initialize(string logFileName)
        {
            try
            {
                sw = new StreamWriter(logFileName, false); //open and overwrite
                sw.AutoFlush = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// A method to write a line to the log file.
        /// </summary>
        /// <param name="contents"> A string containing the data to be written to file. </param>
        public void Log(string contents)
        {
            lock (loglock)
            {
                sw.WriteLine(contents);
                Console.WriteLine(contents); //write to console as well as file
            }
        }

        /// <summary>
        /// A method to close the log file.
        /// </summary>
        public void Close()
        {
            try
            {
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


    }
}
