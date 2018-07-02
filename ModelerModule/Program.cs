using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPC;
using ImageProcessor;
using OpenTK;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace ModelerModule
{
    class Program
    {
        #region Modeler info
        static Configuration.App savedInformations = new Configuration.App();
        

        #region Cursor info
        static int cursorXPosition = 0;
        static int cursorYPosition = 0;
        static int cursorZPosition = 0;
        static string gestureInfo = string.Empty;

        #endregion Cursor info

        #region Pose info
        #region Position
        static int hmdXPosition = 0;
        static int hmdYPosition = 0;
        static int hmdZPosition = 0;
        #endregion Position
        #region Orientation
        static double hmdXOrientation = 0;
        static double hmdYOrientation = 0;
        static double hmdZOrientation = 0;
        #endregion Orientation
        #endregion Pose info

        #region Images
        static Image<Bgra, byte> receivedImage;
        static Image<Bgra, byte> processedImage;

        #endregion Images

        #region Inter-process communication objects
        #region Named pipe info
        static string HUBReceiverNotifier       = string.Empty;
        static string kursor3DReceiverChannel   = string.Empty;
        static string PTAMReceiverChannel       = string.Empty;
        static string HUBNotifierChannel        = string.Empty;

        static NamedPipesServer HUBNotifierReceiver;
        static NamedPipesServer kursor3DNotifierReceiver;
        static NamedPipesServer PTAMNotifiereceiver;
        static NamedPipeClient  imageNotifierSender;
        #endregion Named pipe info

        #region Memory-mapped file info
        static string mmfSourceFileName = string.Empty;
        static string mmfProcessedFileName = string.Empty;

        static MMF receivedImageFile = null;
        static MMF sentImageFile = null;
        #endregion Memory-mapped file info

        #endregion Inter-process communication objects

        #region Performance info
        #region Performance data
        static float overallPerformance;
        static float sendResultPerformance;
        static float receiverPerformance;
        static float sceneConstructionPerformance;
        static float conversionPerformance;
        #endregion Performance data
        #region Performance watcher
        Stopwatch overallPerformanceWatcher;
        Stopwatch sendResultPerformanceWatcher;
        Stopwatch receiverPerformanceWatcher;
        Stopwatch sceneConstructionPerformanceWatcher;
        Stopwatch conversionPerformanceWatcher;
        #endregion Performance watcher

        #endregion Performance info
        #endregion Modeler info

        #region Data loader
        static void DataLoader()
        {
            kursor3DReceiverChannel = savedInformations.Kursor3DReceiverChannel;
            PTAMReceiverChannel = savedInformations.PTAMReceiverChannel;
            HUBNotifierChannel = savedInformations.HUBNotifierChannel;

            mmfSourceFileName = savedInformations.SourceImageMemoryMappedFileName;
            mmfProcessedFileName = savedInformations.ProcessedImageMemoryMappedFileName;
        }
        #endregion Data loader

        static void Main(string[] args)
        {
            Console.WriteLine("Loading saved configurations");
            DataLoader();
            Console.WriteLine("Configuration loaded");
            Console.WriteLine("Main process now start");
        }

        static void SendResult(string PipeName, string Content)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                pipeClient.Connect();
                using (StreamWriter sw = new StreamWriter(pipeClient))
                {
                    sw.AutoFlush = true;
                    sw.WriteLine(Content);
                }
            }

            #region Old named pipe client notifier
            //imageNotifierSender = new NamedPipeClient(PipeName);
            //if (!imageNotifierSender.CheckConnection())
            //{
            //    imageNotifierSender.ConnectToServer();
            //}
            //byte[] tempLocation = new byte[Content.Length];
            //int i = 0;
            //foreach (char character in Content)
            //{
            //    tempLocation[i] = (byte)character;
            //    i++;
            //}
            //imageNotifierSender.WriteToServer(tempLocation, 0, tempLocation.Length);
            //imageNotifierSender.DisconnectToServer();
            #endregion Old named  pipe client notifier
        }

        #region Threaded process
        static void Kursor3DReceiver()
        {
            string tempMessage = string.Empty;
            string[] cursorInfo;

            #region Reveiving code
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(kursor3DReceiverChannel, PipeDirection.In))
            {
                pipeServer.WaitForConnection();
                try
                {
                    using (StreamReader sr = new StreamReader(pipeServer))
                    {
                        string temp;
                        while ((temp = sr.ReadLine()) != null)
                        {
                            tempMessage = temp;
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }
            }
            #endregion Receiving code

            #region Old named pipe server notifier
            // Receive kursor information
            //kursor3DNotifierReceiver = new NamedPipesServer();
            //kursor3DNotifierReceiver.CreateNewServerPipe(kursor3DReceiverChannel, NamedPipesServer.PipeDirection.DirectionInOut, NamedPipesServer.SendMode.MessageMode);
            //kursor3DNotifierReceiver.WaitForConnection();
            //tempMessage = kursor3DNotifierReceiver.ReadMessage();
            //kursor3DNotifierReceiver.WaitForPipeDrain();
            //kursor3DNotifierReceiver.Disconnect();
            //kursor3DNotifierReceiver.ClosePipe();
            #endregion Old named pipe server notifier

            cursorInfo = tempMessage.Split('|');
            cursorXPosition = Convert.ToInt32(cursorInfo[0]);
            cursorYPosition = Convert.ToInt32(cursorInfo[1]);
            cursorZPosition = Convert.ToInt32(cursorInfo[2]);
            gestureInfo = cursorInfo[3];
        }

        static void PTAMReceiver()
        {
            string tempMessage = string.Empty;
            string[] poseInfo;
            #region Reveiving code
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PTAMReceiverChannel, PipeDirection.In))
            {
                pipeServer.WaitForConnection();
                try
                {
                    using (StreamReader sr = new StreamReader(pipeServer))
                    {
                        string temp;
                        while ((temp = sr.ReadLine()) != null)
                        {
                            tempMessage = temp;
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }
            }
            #endregion Receiving code

            poseInfo = tempMessage.Split('|');

            hmdXPosition = Convert.ToInt32(poseInfo[0]);
            hmdYPosition = Convert.ToInt32(poseInfo[1]);
            hmdZPosition = Convert.ToInt32(poseInfo[2]);

            hmdXOrientation = Convert.ToDouble(poseInfo[3]);
            hmdYOrientation = Convert.ToDouble(poseInfo[4]);
            hmdZOrientation = Convert.ToDouble(poseInfo[5]);

            #region Old PTAM named pipe server notifier
            // Receive kursor information
            //PTAMNotifiereceiver = new NamedPipesServer();
            //PTAMNotifiereceiver.CreateNewServerPipe(PTAMReceiverChannel, NamedPipesServer.PipeDirection.DirectionInOut, NamedPipesServer.SendMode.MessageMode);
            //PTAMNotifiereceiver.WaitForConnection();
            //tempMessage = PTAMNotifiereceiver.ReadMessage();
            //PTAMNotifiereceiver.WaitForPipeDrain();
            //PTAMNotifiereceiver.Disconnect();
            //PTAMNotifiereceiver.ClosePipe();
            #endregion Old PTAM named pipe server notifier
        }

        static void HUBReceiver()
        {
            char tempMessage;
            string[] poseInfo;
            // Receive kursor information
            HUBNotifierReceiver = new NamedPipesServer();
            HUBNotifierReceiver.CreateNewServerPipe(HUBNotifierChannel, NamedPipesServer.PipeDirection.DirectionInOut, NamedPipesServer.SendMode.ByteMode);
            HUBNotifierReceiver.WaitForConnection();
            tempMessage = (char)HUBNotifierReceiver.ReadByte();
            HUBNotifierReceiver.WaitForPipeDrain();
            HUBNotifierReceiver.Disconnect();
            HUBNotifierReceiver.ClosePipe();

            if (tempMessage == 'y')
            {
                ImageLoader();
            }
        }

        static void ImageLoader()
        {
            byte[] receivedImage;
            receivedImageFile = new MMF();
            receivedImageFile.OpenExisting(mmfSourceFileName);
            receivedImage = Convert.FromBase64String(receivedImageFile.ReadContent(MMF.DataType.DataString));
            Program.receivedImage = new Image<Bgra, byte>(new Bitmap(new MemoryStream(receivedImage)));
        }

        static void ImageWriter()
        {
            sentImageFile = new MMF();
            sentImageFile.CreateNewFile(mmfProcessedFileName, 10000000);
            sentImageFile.AddInformation(Convert.ToBase64String(processedImage.Bytes));
        }

        #endregion Threaded process

        static void DummyData()
        {

        }
    }
}
