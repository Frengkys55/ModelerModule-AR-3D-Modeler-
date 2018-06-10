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

namespace ModelerModule
{
    class Program
    {
        #region Modeler info
        static Configuration.App savedInformations = new Configuration.App();
        static Image<Bgra, byte> receivedImage;


        #region Cursor info
        static int cursorXPosition = 0;
        static int cursorYPosition = 0;
        static int cursorZPosition = 0;
        static string gestureInfo = string.Empty;

        #endregion Cursor info

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
        static string mmfFileName = string.Empty;

        static MMF imageFile = null;
        #endregion Memory-mapped file info

        #endregion Inter-process communication objects

        #region Performance info
        static float overallPerformance;
        static float sendResultPerformance;
        static float receiverPerformance;
        static float sceneConstructionPerformance;
        static float conversionPerfotmance;
        #endregion Performance info
        #endregion Modeler info

        #region Data loader
        static void DataLoader()
        {
            kursor3DReceiverChannel = savedInformations.Kursor3DReceiverChannel;
            PTAMReceiverChannel = savedInformations.PTAMReceiverChannel;
            HUBNotifierChannel = savedInformations.HUBNotifierChannel;
        }
        #endregion Data loader

        static void Main(string[] args)
        {
            DataLoader();
        }

        static void SendResult(string PipeName, string Content)
        {
            imageNotifierSender = new NamedPipeClient(PipeName);
            if (!imageNotifierSender.CheckConnection())
            {
                imageNotifierSender.ConnectToServer();
            }
            byte[] tempLocation = new byte[Content.Length];
            int i = 0;
            foreach (char character in Content)
            {
                tempLocation[i] = (byte)character;
                i++;
            }
            imageNotifierSender.WriteToServer(tempLocation, 0, tempLocation.Length);
            imageNotifierSender.DisconnectToServer();
            
        }

        #region Threaded process
        static void Kursor3DReceiver()
        {
            string tempMessage = string.Empty;
            string[] cursorInfo;
            // Receive kursor information
            kursor3DNotifierReceiver = new NamedPipesServer();
            kursor3DNotifierReceiver.CreateNewServerPipe(kursor3DReceiverChannel, NamedPipesServer.PipeDirection.DirectionInOut, NamedPipesServer.SendMode.MessageMode);
            kursor3DNotifierReceiver.WaitForConnection();
            tempMessage = kursor3DNotifierReceiver.ReadMessage();
            kursor3DNotifierReceiver.WaitForPipeDrain();
            kursor3DNotifierReceiver.Disconnect();
            kursor3DNotifierReceiver.ClosePipe();

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
            // Receive kursor information
            PTAMNotifiereceiver = new NamedPipesServer();
            PTAMNotifiereceiver.CreateNewServerPipe(PTAMReceiverChannel, NamedPipesServer.PipeDirection.DirectionInOut, NamedPipesServer.SendMode.MessageMode);
            PTAMNotifiereceiver.WaitForConnection();
            tempMessage = PTAMNotifiereceiver.ReadMessage();
            PTAMNotifiereceiver.WaitForPipeDrain();
            PTAMNotifiereceiver.Disconnect();
            PTAMNotifiereceiver.ClosePipe();
            
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
            imageFile = new MMF();
            imageFile.OpenExisting(mmfFileName);
            receivedImage = Convert.FromBase64String(imageFile.ReadContent(MMF.DataType.DataString));
            Program.receivedImage = new Image<Bgra, byte>(new Bitmap(new MemoryStream(receivedImage)));
        }

        #endregion Threaded process
    }
}
