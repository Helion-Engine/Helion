using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Geometry.Vectors;
using Helion.Input;
using Helion.Render.OpenGL.Legacy.Commands;
using Helion.Render.OpenGL.Legacy.Commands.Alignment;
using Helion.Render.OpenGL.Legacy.Shared.Drawers;
using Helion.Render.OpenGL.Legacy.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using Helion.Util.Timing;
using Helion.World;
using NLog;

namespace Helion.Layer
{
    public class EndGameLayer : GameLayer
    {
        private enum DrawState
        {
            Text,
            TextComplete,
            Image,
            ImageScroll,
            Delay,
            TheEnd,
            Complete
        }

        private const int LettersPerSecond = 10;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static readonly IEnumerable<string> EndGameMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EndPic", "EndGame1", "EndGame2", "EndGameW", "EndGame4", "EndGameC", "EndGame3",
            "EndDemon", "EndGameS", "EndChess", "EndTitle", "EndSequence", "EndBunny"
        };

        public event EventHandler? Exited;

        public IWorld World { get; private set; }
        public MapInfoDef? NextMapInfo { get; private set; }

        private readonly string m_flatImage;
        private readonly IList<string> m_displayText;
        private readonly Ticker m_ticker = new(LettersPerSecond);
        private readonly EndGameDrawer m_drawer;
        private readonly Stopwatch m_stopwatch = new();
        private readonly Stopwatch m_scroller = new();
        private readonly TimeSpan m_scrollTimespan = TimeSpan.FromMilliseconds(40);
        private readonly ArchiveCollection m_archiveCollection;
        private readonly IMusicPlayer m_musicPlayer;
        private readonly SoundManager m_soundManager;
        private bool m_invokedNextMapFunc;

        private DrawState m_drawState = DrawState.Text;
        private IList<string> m_images = Array.Empty<string>();
        private readonly IList<string> m_theEndImages = new string[] { "END0", "END1", "END2", "END3", "END4", "END5", "END6" };
        private TimeSpan m_timespan;
        private bool m_initRenderPages;
        private bool m_shouldScroll;
        private bool m_forceState;
        private int m_xOffset;
        private int m_xOffsetStop;
        private int m_theEndImageIndex;
        private Vec2I m_theEndOffset = Vec2I.Zero;

        protected override double Priority => 0.675;

        public EndGameLayer(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, SoundManager soundManager, IWorld world,
            ClusterDef cluster, MapInfoDef? nextMapInfo)
        {
            World = world;
            NextMapInfo = nextMapInfo;
            var language = archiveCollection.Definitions.Language;

            m_archiveCollection = archiveCollection;
            m_musicPlayer = musicPlayer;
            m_soundManager = soundManager;
            m_drawer = new(archiveCollection);
            m_flatImage = language.GetMessage(cluster.Flat);
            m_displayText = LookUpDisplayText(language, cluster);
            m_timespan = GetPageTime();

            m_ticker.Start();
            string music = cluster.Music;
            if (music.Empty())
                music = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.FinaleMusic;
            PlayMusic(music);
        }

        private TimeSpan GetPageTime() =>
            TimeSpan.FromSeconds(m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.PageTime);

        private static IList<string> LookUpDisplayText(LanguageDefinition language, ClusterDef cluster)
        {
            if (cluster.ExitText.Count != 1)
                return cluster.ExitText;
            
            return language.GetMessages(cluster.ExitText[0]);
        }

        private void PlayMusic(string music)
        {
            m_musicPlayer.Stop();
            if (music.Empty())
                return;

            music = m_archiveCollection.Definitions.Language.GetMessage(music);
            
            Entry? entry = m_archiveCollection.Entries.FindByName(music);
            if (entry == null)
            {
                Log.Warn($"Cannot find end game music file: {music}");
                return;
            }

            byte[] data = entry.ReadData();
            // Eventually we'll need to not assume .mus all the time.
            byte[]? midiData = MusToMidi.Convert(data);

            if (midiData != null)
                m_musicPlayer.Play(midiData);
            else
                Log.Warn($"Cannot decode end game music file: {music}");
        }

        private void AdvanceState()
        {
            m_forceState = true;

            if (m_invokedNextMapFunc) 
                return;

            if (m_drawState == DrawState.TextComplete)
            {
                m_invokedNextMapFunc = true;
                Exited?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CheckState()
        {
            if (m_scroller.IsRunning && m_scroller.Elapsed >= m_scrollTimespan)
            {
                m_scroller.Restart();
                m_xOffset += 1;
                if (m_xOffset >= m_xOffsetStop)
                {
                    m_xOffset = m_xOffsetStop;
                    m_scroller.Stop();
                }
            }

            if (!m_forceState && (m_drawState == DrawState.Complete || !m_stopwatch.IsRunning || m_stopwatch.Elapsed < m_timespan))
                return;

            if (m_drawState == DrawState.TextComplete)
            {
                if (m_shouldScroll)
                {
                    m_drawState++;
                    m_timespan += TimeSpan.FromSeconds(2);
                    PlayMusic("D_BUNNY");
                }
                else
                {
                    m_drawState = DrawState.Complete;
                }
            }
            else if (m_drawState == DrawState.ImageScroll)
            {
                if (m_forceState)
                {
                    m_scroller.Stop();
                    m_xOffset = m_xOffsetStop;
                    m_drawState = DrawState.TheEnd;
                    m_theEndImageIndex = m_theEndImages.Count - 1;
                    return;
                }

                if (m_scroller.IsRunning)
                    return;

                m_drawState++;
                m_timespan = GetPageTime();
            }
            else if (m_drawState == DrawState.TheEnd)
            {
                m_timespan = TimeSpan.FromMilliseconds(150);
                if (m_theEndImageIndex < m_theEndImages.Count - 1)
                {
                    m_theEndImageIndex++;
                    m_soundManager.PlayStaticSound("weapons/pistol");
                }
            }
            else
            {
                m_drawState++;
            }

            if (m_drawState == DrawState.ImageScroll)
                m_scroller.Start();

            m_forceState = false;
            m_stopwatch.Restart();
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.HasAnyKeyPressed())
                AdvanceState();
            
            base.HandleInput(input);
        }

        public override void Render(RenderCommands renderCommands)
        {
            DrawHelper draw = new(renderCommands);
            CheckState();

            if (!m_initRenderPages)
            {
                SetPage(draw);
                if (m_theEndImages.Count > 0)
                    m_theEndOffset.Y = -draw.DrawInfoProvider.GetImageDimension(m_theEndImages[0]).Height;
            }

            if (m_drawState <= DrawState.TextComplete)
            {
                m_drawer.Draw(m_flatImage, m_displayText, m_ticker, m_drawState > DrawState.Text, renderCommands, draw);
            }
            else
            {
                m_drawer.DrawBackgroundImages(m_images, m_xOffset, renderCommands, draw);
                if (m_drawState == DrawState.TheEnd)
                {
                    draw.AtResolution(DoomHudHelper.DoomResolutionInfo, () =>
                    {
                        draw.Image(m_theEndImages[m_theEndImageIndex], m_theEndOffset.X, m_theEndOffset.Y, window: Align.Center);
                    });
                }
            }

            base.Render(renderCommands);
        }

        private void SetPage(DrawHelper draw)
        {
            m_initRenderPages = true;

            string next = World.MapInfo.Next;
            if (next.Equals("EndPic", StringComparison.OrdinalIgnoreCase))
            {
                m_images = new string[] { World.MapInfo.EndPic };
            }
            else if (next.Equals("EndGame2", StringComparison.OrdinalIgnoreCase))
            {
                m_images = new string[] { "VICTORY2" };
            }
            else if (next.Equals("EndGame3", StringComparison.OrdinalIgnoreCase) || next.Equals("EndBunny", StringComparison.OrdinalIgnoreCase))
            {
                m_images = new string[] { "PFUB1", "PFUB2" };
                m_xOffsetStop = draw.DrawInfoProvider.GetImageDimension(m_images[0]).Width;
                m_shouldScroll = true;
            }
            else if (next.Equals("EndGame4", StringComparison.OrdinalIgnoreCase))
            {
                m_images = new string[] { "ENDPIC" };
            }
            else
            {
                var pages = LayerUtil.GetRenderPages(draw, m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.CreditPages, false);
                if (pages.Count > 0)
                    m_images = new string[] { pages[pages.Count - 1] };
            }
        }
    }
}
