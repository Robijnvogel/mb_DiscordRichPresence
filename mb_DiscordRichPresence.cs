using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using DiscordInterface;
using Util;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Discord Rich Presence";
            about.Description = "Sets currently playing song as Discord Rich Presence";
            about.Author = "Harmon758, Robijnvogel";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 3;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            InitialiseDiscord();
            
            return about;
        }

        private void InitialiseDiscord()
        {
            DiscordRPC.DiscordEventHandlers handlers = new DiscordRPC.DiscordEventHandlers();
            handlers.readyCallback = HandleReadyCallback;
            handlers.errorCallback = HandleErrorCallback;
            handlers.disconnectedCallback = HandleDisconnectedCallback;
            DiscordRPC.Initialize("381981355539693579", ref handlers, true, null);
        }

        private void HandleReadyCallback() { }
        private void HandleErrorCallback(int errorCode, string message) { }
        private void HandleDisconnectedCallback(int errorCode, string message) { }

        private void UpdatePresence(string song, string details, int position, string artwork, string state = "Listening to: ")
        {
            DiscordRPC.RichPresence presence = new DiscordRPC.RichPresence();
            if(string.IsNullOrEmpty(artwork)) {
            	artwork = "artwork is null or empty";
            } else {
            	artwork = "something else is wrong with artwork";
            }
            //song = song + " " + artwork;
            song = Utility.Utf16ToUtf8(song);
            presence.details = state + song.Substring(0, song.Length - 1);
            presence.state = details;
            presence.largeImageKey = "musicbee";
            long now = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            presence.startTimestamp = now - position;
            // string[] durations = duration.Split(':');
            // long end = now + System.Convert.ToInt64(durations[0]) * 60 + System.Convert.ToInt64(durations[1]);
            // presence.endTimestamp = end;
            DiscordRPC.UpdatePresence(ref presence);
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }
       
        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            DiscordRPC.Shutdown();
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            string trackTitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
            string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
            string rating = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Rating);
            string playcount = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.PlayCount);
            
            
            string song = trackTitle + " ~ " + artist;
            if (string.IsNullOrEmpty(artist)) { song = trackTitle; }
            string details = rating + "out of 5 stars - played " + playcount + " times";
            //string duration = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Duration);
            int position = mbApiInterface.Player_GetPosition();
            string artwork = mbApiInterface.NowPlaying_GetArtwork();
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                case NotificationType.PlayStateChanged:
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                            UpdatePresence(song, details, position / 1000, artwork);
                            break;
                        case PlayState.Paused:
                            UpdatePresence(song, details, 0, artwork, state: "Paused: ");
                            break;
                    }
                    break;
                case NotificationType.TrackChanged:
                    UpdatePresence(song, details, 0, artwork);
                    break;
            }
        }
   }
}