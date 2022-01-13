using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AppKit;
using AVFoundation;
using Foundation;
using SmartGlass;
using SmartGlass.Channels;
using SmartGlass.Common;
using SmartGlass.Nano.FFmpeg;
using SmartGlass.Nano;


using SmartGlass.Nano.Packets;
using xRemote.Services;
using SmartGlass.Nano.FFmpeg.Producer;
using XboxWebApi.Authentication;

namespace xRemoteApp
{
    public partial class ViewController : NSViewController
    {
        static string _userHash = null;
        static string _xToken = null;

        static AudioFormat _audioFormat = null;
        static VideoFormat _videoFormat = null;
        static AudioFormat _chatAudioFormat = null;

        static bool VerifyIpAddress(string address)
        {
            return System.Net.IPAddress.TryParse(
                address, out System.Net.IPAddress tmp);
        }

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var foo = new SmartGlassConnection();

            IEnumerable<Device> discovered = foo.DiscoverConsoles().Result;

            foreach (var device in discovered)
            {
                DevicesFind.AddItem($"{device.Address}");
            }
        }

        //static bool Authenticate(string tokenPath)
        //{
        //    if (String.IsNullOrEmpty(tokenPath))
        //    {
        //        return false;
        //    }

        //    FileStream fs = new FileStream(tokenPath, FileMode.Open);
        //    AuthenticationService authenticator = AuthenticationService.LoadFromFile(fs);
        //    try
        //    {
        //        authenticator.Authenticate();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine($"Failed to refresh XBL tokens, error: {e.Message}");
        //        return false;
        //    }

        //    _userHash = authenticator.UserInformation.Userhash;
        //    _xToken = authenticator.XToken.Jwt;
        //    return true;
        //}

        /// <summary>
        /// Connect to console, request Broadcast Channel and start gamestream
        /// </summary>
        /// <param name="ipAddress">IP address of console</param>
        /// <param name="gamestreamConfig">Desired gamestream configuration</param>
        /// <returns></returns>
        public static async Task<GamestreamSession> ConnectToConsole(string ipAddress, GamestreamConfiguration gamestreamConfig)
        {
            try
            {
                SmartGlassClient client = await SmartGlassClient.ConnectAsync(ipAddress);
                return await client.BroadcastChannel.StartGamestreamAsync(gamestreamConfig);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Connection timed out! msg: {e.Message}");
                return null;
            }
        }


        public static async Task<NanoClient> InitNano(string ipAddress, GamestreamSession session)
        {
            NanoClient nano = new NanoClient(ipAddress, session);

            try
            {
                // General Handshaking & Opening channels
                //await nano.InitializeProtocolAsync();
                //await nano.OpenInputChannelAsync(1280, 720);

                //sasssssssss
                var tb = Task.Run(() => nano.InitializeProtocolAsync());
                tb.Wait();

                var atb = Task.Run(() => nano.OpenInputChannelAsync(1280, 720));
                atb.Wait();

                // Audio & Video client handshaking
                // Sets desired AV formats
                _audioFormat = nano.AudioFormats[0];
                _videoFormat = nano.VideoFormats[0];

                //await nano.InitializeStreamAsync(_audioFormat, _videoFormat);
                var aa = Task.Run(() => nano.InitializeStreamAsync(_audioFormat, _videoFormat));
                aa.Wait();

                // TODO: Send opus audio chat samples to console
                _chatAudioFormat = new AudioFormat(1, 24000, AudioCodec.Opus);
                //await nano.OpenChatAudioChannelAsync(_chatAudioFormat);
                var fg = Task.Run(() => nano.OpenChatAudioChannelAsync(_chatAudioFormat));
                fg.Wait();

                // Tell console to start sending AV frames
                //await nano.StartStreamAsync();
                var j = Task.Run(() => nano.StartStreamAsync());
                j.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to init Nano, error: {e}");
                return null;
            }

            return nano;
        }


        partial void ConnectButton(Foundation.NSObject sender)
        {

            var printHelp = false;
            var fullscreen = false;
            var useController = false;
            var ipAddress = DevicesFind.Title;
            var tokenPath = String.Empty;

            //if (!Authenticate(tokenPath))
            //    Console.WriteLine("Connecting anonymously to console, no XBL token available");

            string hostName = ipAddress;

            if (!String.IsNullOrEmpty(hostName))
            {
                Console.WriteLine($"Connecting to console {hostName}...");
                GamestreamConfiguration config = GamestreamConfiguration.GetStandardConfig();

                //GamestreamSession session = ConnectToConsole(ipAddress, config).GetAwaiter().GetResult();
                //CONNECT
                var t = Task.Run(() => SmartGlassClient.ConnectAsync(ipAddress));
                t.Wait();

                var Client = t.Result;

                //GAME STREAM SESSION
                var tb = Task.Run(() => Client.BroadcastChannel.StartGamestreamAsync(config));
                tb.Wait();

                var session = tb.Result;
                if (session == null)
                {
                    Console.WriteLine("Failed to connect to console!");
                    return;
                }

                Console.WriteLine(
                    $"Connecting to NANO // TCP: {session.TcpPort}, UDP: {session.UdpPort}");

                NanoClient nano = InitNano(hostName, session).GetAwaiter().GetResult();

                if (nano == null)
                {
                    Console.WriteLine("Nano failed!");
                    return;
                }

                // SDL / FFMPEG setup
                SdlProducer producer = new SdlProducer(nano, _audioFormat, _videoFormat, fullscreen, useController);

                nano.AudioFrameAvailable += producer.Decoder.ConsumeAudioData;
                nano.VideoFrameAvailable += producer.Decoder.ConsumeVideoData;

                producer.MainLoop();
            }

            //finally (dirty)        
            //NSApplication.SharedApplication.Terminate(this);      
        }

        //public void StreamToFile()
        //{
        //    var IP = DevicesFind.Title;
        //    GamestreamConfiguration config = GamestreamConfiguration.GetStandardConfig();

        //    //SmartGlassClient client = AsyncContext.Run(() => SmartGlassClient.ConnectAsync(IP));                                       =           

        //    if (ButtonConnect.Title == "Connect")
        //    {
        //        //CONNECT
        //        var t = Task.Run(() => SmartGlassClient.ConnectAsync(IP));
        //        t.Wait();

        //        Client = t.Result;

        //        //GAME STREAM SESSION
        //        var tb = Task.Run(() => Client.BroadcastChannel.StartGamestreamAsync(config));
        //        tb.Wait();

        //        Session = tb.Result;

        //        ButtonConnect.Title = "Disconnect";

        //        Console.WriteLine(
        //            $"Connecting to NANO // TCP: {Session.TcpPort}, UDP: {Session.UdpPort}");

        //        NanoClient nano = new NanoClient(IP, Session);

        //        //GAME STREAM SESSION
        //        var ni = Task.Run(() => nano.InitializeProtocolAsync());
        //        ni.Wait();

        //        // Audio & Video client handshaking
        //        // Sets desired AV formats
        //        AudioFormat audioFormat = nano.AudioFormats[0];
        //        VideoFormat videoFormat = nano.VideoFormats[0];
        //        var nis = Task.Run(() => nano.InitializeStreamAsync(audioFormat, videoFormat));
        //        nis.Wait();

        //        IConsumer consumer = new FileConsumer("cu", true);
        //        nano.AddConsumer(consumer);

        //        AudioFormat chatAudioFormat = new AudioFormat(1, 24000, AudioCodec.Opus);
        //        var nochat = Task.Run(() => nano.OpenChatAudioChannelAsync(chatAudioFormat));
        //        nochat.Wait();

        //        var startStream = Task.Run(() => nano.StartStreamAsync());
        //        startStream.Wait();

        //        var openChannel = Task.Run(() => nano.OpenInputChannelAsync(1280, 720));
        //        openChannel.Wait();

        //        //var url = NSUrl.FromString("https://www.youtube.com/watch?v=w5jUGDTu2sg&ab_channel=Not%C3%ADciasMaromba");

        //        //StreamVideo.view

        //    }
        //    else
        //    {
        //        //Task.CompletedTask.Wait();
        //        //
        //        //ButtonConnect.Title = "Connect";
        //    }
        //}


        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }      
      
    }
}
