/* Author: Tyler Desplenter
 * Creation: 15-06-2015
 * Last Modified: 24-06-2015
 * Summary: AcclerometerSample is an object to encapsulate accelerometer data into one package.
 */

using System;

namespace DelsysTrignoCSharpLibrary
{
    /// <summary>
    /// The AccelerometerSample object is a container for accelerometer data samples.
    /// </summary>
    public class AccelerometerSample
    {
        /// <summary>
        /// An array for one packet of accelerometer data (X direction).
        /// </summary>
        public float[] accXData; 
        /// <summary>
        /// An array for one packet of accelerometer data (Y direction).
        /// </summary>
        public float[] accYData; 
        /// <summary>
        /// An array for one packet of accelerometer data (Z direction)
        /// </summary>
        public float[] accZData;

        public AccelerometerSample()
        {
            accXData = new float[16];
            accYData = new float[16];
            accZData = new float[16];
            for (int i = 0; i < 16; i++)
            {
                accXData[i] = 0;
                accYData[i] = 0;
                accZData[i] = 0;
            }
        }

        /// <summary>
        /// A constructor that initializes the internal arrays based on the data collected from the system.
        /// </summary>
        /// <param name="acc_x_data"> An array of floating point accelerometer values (X direction). </param>
        /// <param name="acc_y_data"> An array of floating point accelerometer values (Y direction). </param>
        /// <param name="acc_z_data"> An array of floating point accelerometer values (Z direction). </param>
        public AccelerometerSample(float[] acc_x_data, float [] acc_y_data, float [] acc_z_data)
        {
            accXData = new float[16];
            accYData = new float[16];
            accZData = new float[16];
            Array.Copy(acc_x_data,accXData,16);
            Array.Copy(acc_y_data,accYData,16);
            Array.Copy(acc_z_data,accZData,16);
        }        
    }
}
