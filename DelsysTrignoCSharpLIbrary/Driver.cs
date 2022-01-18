/* Author: Tyler Desplenter
 * Creation: 15-06-2015
 * Last Modified: 07-07-2015
 * Summary: the Driver object encompasses the required functionality to connect and stream data from the 
 * Delsys Trigno Wireless EMG System. * 
 */


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace DelsysTrignoCSharpLibrary
{
    /// <summary>
    /// The Driver object is the main interface for interaction with the Delsys Trigno Wireless EMG system.
    /// </summary>
    public class Driver
    {
        private TcpClient commandSocket; //command server socket
        private TcpClient emgSocket; //emg server socket
        private TcpClient accSocket; //accelerometer server socket
        private const int commandPort = 50040;  //server command port
        private const int emgPort = 50041;  //port for EMG data
        private const int accPort = 50042;  //port for acc data

        //The following are streams and readers/writers for communication
        private NetworkStream commandStream; //stream for command server socket
        private NetworkStream emgStream; //stream for emg server socket
        private NetworkStream accStream; //stream for accelerometer server socket
        private StreamReader commandReader; //stream reader for command server
        private StreamWriter commandWriter; //stream writer for command server

        private float[] emgData = new float[16]; //array for one packet of EMG data
        private float[] accXData = new float[16]; //array for one packet of accelerometer data (X direction)
        private float[] accYData = new float[16]; //array for one packet of accelerometer data (Y direction)
        private float[] accZData = new float[16]; //array for one packet of accelerometer data (Z direction)

        private bool connected = false; //true if connected to server
        private bool running = false;   //true when acquiring data

        //Server commands
        private const string COMMAND_QUIT = "QUIT";
        private const string COMMAND_GETTRIGGERS = "TRIGGER?";
        private const string COMMAND_SETSTARTTRIGGER = "TRIGGER START";
        private const string COMMAND_SETSTOPTRIGGER = "TRIGGER STOP";
        private const string COMMAND_START = "START";
        private const string COMMAND_STOP = "STOP";

        private Thread accelerometerCollectionThread; //thread used to collect accelerometer data
        private Thread emgCollectionThread; //thread used to collect emg data

        private Logger logger; //logger for driver events
        private Logger timing; //logger for timing information
        private string serverURL; //URL of the command. emg and accelerometer servers
        private Queue<EMGSample> emg_samples; //a queue of EMGSamples from the servers
        private Queue<AccelerometerSample> accelerometer_samples; //a queue of AccelerometerSamples from the servers
        private string emgqueueLock = ""; //a lock for the queue
        private string accqueueLock = ""; //a lock for the queue
        private Stopwatch sample_timer = new Stopwatch(); //timer for calculating duration of data collection
        private Stopwatch totaltime = new Stopwatch();
        private Stopwatch done_timer = new Stopwatch();
        private bool first_time = true;
        private bool done = false;

        /// <summary> A method that will connect to the data servers, send the START command to the system and begin the data collection thread. </summary>
        public void BeginSampling()
        {
            if (!connected)
            {
                logger.Log("BeginSampling(): Not connected, cannot get samples.");
                return;
            }

            //Clear stale data
            emgData = new float[16];
            accXData = new float[16];
            accYData = new float[16];
            accZData = new float[16];

            //Establish data connections and creat streams
            emgSocket = new TcpClient(serverURL, emgPort);
            accSocket = new TcpClient(serverURL, accPort);
            emgStream = emgSocket.GetStream();
            accStream = accSocket.GetStream();

            //Create data acquisition threads
            accelerometerCollectionThread = new Thread(AccelerometerCollectionThread);
            accelerometerCollectionThread.IsBackground = true;
            accelerometerCollectionThread.Priority = ThreadPriority.Lowest;
            emgCollectionThread = new Thread(EMGCollectionThread);
            emgCollectionThread.IsBackground = true;
            emgCollectionThread.Priority = ThreadPriority.Lowest;

            //Send start command to server to stream data
            string response = SendCommand(COMMAND_START);
            logger.Log("Command: " + COMMAND_START);
            logger.Log("Reponse:" + response);

            //Indicate we are running and start up the acquisition threads
            while (!emgStream.DataAvailable && !accStream.DataAvailable)
            {
                //wait for data to be ready
            }
            running = true;
            first_time = true;
            accelerometerCollectionThread.Start();
            emgCollectionThread.Start();
        }

        /// <summary>
        /// A method for initialzing the Loggers and connecting to the command server.
        /// </summary>
        /// <param name="URL"> The URL of the servers. </param>
        public void Connect(string URL)
        {
            try
            {
                //Initialize the logger
                logger = new Logger();
                logger.Initialize("log.txt");
                timing = new Logger();
                timing.Initialize("timer.txt");

                //Create new queues
                emg_samples = new Queue<EMGSample>();
                accelerometer_samples = new Queue<AccelerometerSample>();

                //Establish TCP/IP connection to server using URL entered
                serverURL = URL;
                commandSocket = new TcpClient(serverURL, commandPort);

                //Set up communication streams
                commandStream = commandSocket.GetStream();
                commandReader = new StreamReader(commandStream, Encoding.ASCII);
                commandWriter = new StreamWriter(commandStream, Encoding.ASCII);

                //Get initial response from server and display
                logger.Log("Initial Server Reponse: " + commandReader.ReadLine());
                commandReader.ReadLine();   //get extra line terminator
                connected = true;   //iindicate that we are connected
                done = false;
                done_timer.Stop();
                done_timer.Reset();
            }
            catch (Exception connectException)
            {
                //connection failed, display error message
                logger.Log("Connect(): Could not connect-> " + connectException.Message);
                logger.Close();
                timing.Close();
            }
        }

        /// <summary>
        /// A thread function that will collect both EMG and accelerometer data samples once the server begins to send them.
        /// </summary>
        private void AccelerometerCollectionThread()
        {
            //Accelerometer sampling frequency = 148 Hz
            accStream.ReadTimeout = 100;

            //Create a binary reader to read the data
            BinaryReader accReader = new BinaryReader(accStream);

            while (running)
            {
                try
                {
                    while (accStream.DataAvailable)
                    {
                        //Demultiplex the data and save for UI display
                        for (int sn = 0; sn < 16; ++sn)
                        {
                            accXData[sn] = accReader.ReadSingle();
                            accYData[sn] = accReader.ReadSingle();
                            accZData[sn] = accReader.ReadSingle();
                        }
                        AccelerometerSample accs = new AccelerometerSample(accXData, accYData, accZData);
                        lock (accqueueLock)
                        {
                            accelerometer_samples.Enqueue(accs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //ignore timeouts, but force a check of the running flag
                    logger.Log("CollectionThread(): " + ex.Message);
                }
            }
            accReader.Close();
        }

        /// <summary>
        /// A thread function that will collect both EMG and accelerometer data samples once the server begins to send them.
        /// </summary>
        private void EMGCollectionThread()
        {
            //EMG sampling frequency = 2000 Hz
            //Accelerometer sampling frequency = 148 Hz
            emgStream.ReadTimeout = 100;    //set timeout

            //Create a binary reader to read the data
            BinaryReader emgReader = new BinaryReader(emgStream);

            while (running)
            {
                try
                {
                    if (done_timer.Elapsed.TotalMilliseconds > 1000)
                    {
                        done = true;
                        running = false;
                    }
                    while (emgStream.DataAvailable)
                    {
                        done_timer.Stop();
                        done_timer.Reset();
                        sample_timer.Reset();
                        sample_timer.Start();
                        if (first_time)
                        {
                            totaltime.Reset();
                            totaltime.Start();
                            first_time = false;
                        }
                        // Demultiplex the data and save for UI display 
                        for (int sn = 0; sn < 16; ++sn)
                        {
                            emgData[sn] = emgReader.ReadSingle();
                        }

                        EMGSample emgs = new EMGSample(emgData);
                        lock (emgqueueLock)
                        {
                            emg_samples.Enqueue(emgs);
                        }
                        sample_timer.Stop();
                        long microseconds = sample_timer.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
                        //timing.Log(timer.ElapsedMilliseconds.ToString());
                        //timing.Log(Convert.ToString(microseconds));
                    }
                    if (!emgStream.DataAvailable)
                        if (done_timer.Elapsed.TotalMilliseconds == 0.0)
                            done_timer.Start();
                }
                catch (Exception ex)
                {
                    //ignore timeouts, but force a check of the running flag
                    logger.Log("CollectionThread(): " + ex.Message);
                }
            }

            emgReader.Close(); //close the reader. This also disconnects
        }

        /// <summary>
        /// A method to send the QUIT command to the server and close all connections to the system.
        /// </summary>
        public void Disconnect()
        {
            //Check if running and display error message if not
            if (running)
            {
                Console.WriteLine("Disconnect(): Can't quit while acquiring data!");
                return;
            }
            if (connected)
            {
                //send QUIT command
                string response = SendCommand(COMMAND_QUIT);
                logger.Log("Command: " + COMMAND_QUIT);
                logger.Log("Reponse:" + response);

                connected = false;  //no longer connected

                //Close all streams and connections
                commandReader.Close();
                commandWriter.Close();
                commandStream.Close();
                commandSocket.Close();
                emgStream.Close();
                emgSocket.Close();
                accStream.Close();
                accSocket.Close();
            }

            logger.Close();
            timing.Close();
        }

        /// <summary>
        /// A method that will return one AcclerometerSample from the queue.
        /// </summary>
        /// <returns> One DataSample object. </returns>
        public AccelerometerSample GetAccelerometerSample()
        {
            AccelerometerSample accs = new AccelerometerSample();
            lock (accqueueLock)
            {
                if (accelerometer_samples.Count > 0)
                    accs = accelerometer_samples.Dequeue();
            }
            return accs;
        }

        /// <summary>
        /// A method that will return one EMGSample from the queue.
        /// </summary>
        /// <returns> One DataSample object. </returns>
        public EMGSample GetEMGSample()
        {
            EMGSample emgs = new EMGSample();
            lock (emgqueueLock)
            {
                if (emg_samples.Count > 0)
                    emgs = emg_samples.Dequeue();
            }
            return emgs;
        }

        /// <summary>
        /// Return the sampling time in milliseconds.
        /// </summary>
        /// <returns></returns>
        public int GetSamplingTime()
        {
            return Convert.ToInt32(emg_samples.Count / 2);
        }

        /// <summary>
        /// A method that reveals the connection status of the driver with the system.
        /// </summary>
        /// <returns> True - connected, False - disconnected. </returns>
        public bool IsConnected()
        {
            return connected;
        }

        /// <summary>
        /// A method that reveals the status of the data collection between the driver and the server.
        /// </summary>
        /// <returns> True - streaming data, False - idle. </returns>
        public bool IsStreaming()
        {
            return running;
        }

        public bool IsDone()
        {
            return done;
        }

        /// <summary>
        /// A method that reveals the numbers of accelerometer samples that are available from the driver.
        /// </summary>
        /// <returns> The number of samples available in the queue. </returns>
        public int NumberOfAccelerometerSamples()
        {
            return accelerometer_samples.Count;
        }

        /// <summary>
        /// A method that reveals the numbers of EMG samples that are available from the driver.
        /// </summary>
        /// <returns> The number of samples available in the queue. </returns>
        public int NumberOfEMGSamples()
        {
            return emg_samples.Count;
        }

        /// <summary>
        /// A method for sending a command to the system.
        /// </summary>
        /// <param name="command"> A string containing the server command. </param>
        /// <returns> A string containing the server response. </returns>
        string SendCommand(string command)
        {
            string response = "";

            //Check if connected
            if (connected)
            {
                //Send the command
                commandWriter.WriteLine(command);
                commandWriter.WriteLine();  //terminate command
                commandWriter.Flush();  //make sure command is sent immediately

                //Read the response line and display    
                response = commandReader.ReadLine();
                commandReader.ReadLine();   //get extra line terminator
            }
            else
                logger.Log("SendCommand(): Not connected.");
            return response;    //return the response we got
        }

        /// <summary>
        /// A method that stops the data collection thread and sends a STOP command to the system.
        /// </summary>
        public void StopSampling()
        {
            //running = false;    //no longer running
            //Wait for threads to terminate
            //accelerometerCollectionThread.Join();
            //emgCollectionThread.Join();

            totaltime.Stop();
            timing.Log("Total Collection Time: " + totaltime.ElapsedMilliseconds);
            first_time = true;

            //Send stop command to server
            string response = SendCommand(COMMAND_STOP);
            logger.Log("Command: " + COMMAND_STOP);
            logger.Log("Reponse:" + response);
            if (!response.StartsWith("OK"))
                logger.Log("StopSampling(): Server failed to stop. Further actions may fail.");
        }       
    }
}
