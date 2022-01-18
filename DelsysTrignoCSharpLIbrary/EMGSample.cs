/* Author: Tyler Desplenter
 * Creation: 15-06-2015
 * Last Modified: 24-06-2015
 * Summary: EMGSample is an object to encapsulate EMG data into one package.
 */

using System;

namespace DelsysTrignoCSharpLibrary
{
    /// <summary>
    /// The EMGSample object is a container for EMG data samples.
    /// </summary>
    public class EMGSample
    {

        /// <summary>
        /// An array for one packet of accelerometer data (X direction).
        /// </summary>
        public float[] emgData;

        /// <summary>
        /// A constructor that initializes the internal arrays to 0.
        /// </summary>
        public EMGSample()
        {
            emgData = new float[16];
            for (int i = 0; i < 16; i++)
            {
                emgData[i] = 0;
            }
        }

        /// <summary>
        /// A constructor that initializes the internal arrays based on the data collected from the system.
        /// </summary>
        /// <param name="emg_data"> An array of floating point EMG values. </param>
        public EMGSample(float[] emg_data)
        {
            emgData = new float[16];
            Array.Copy(emg_data, emgData, 16);
        }
    }
}

