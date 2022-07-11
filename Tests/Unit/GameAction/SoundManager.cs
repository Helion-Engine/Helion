using FluentAssertions;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Helion.World.Sound;
using System;
using System.Collections.Generic;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class SoundManager
    {
        private readonly SinglePlayerWorld World;

        public SoundManager()
        {
            World = WorldAllocator.LoadMap("Resources/sound.zip", "sound.wad", "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
        }

        private void WorldInit(SinglePlayerWorld world)
        {

        }

        [Fact(DisplayName = "Looping sector sound")]
        public void LoopSound()
        {
            World.SoundManager.SetMaxConcurrentSounds(1);
            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            ActivateSpecialLine(2);

            sounds.Count.Should().Be(1);
            var sound = sounds.First!;
            sound.Should().NotBeNull();

            GameActions.TickWorld(World, 35, () =>
            {
                sound.Value.AudioData.Attenuation.Should().Be(Attenuation.Default);
                sound.Value.AudioData.SoundChannelType.Should().Be(SoundChannelType.Auto);
                sound.Value.AudioData.Loop.Should().BeTrue();
                sound.Value.AudioData.SoundInfo.EntryName.Should().Be("dsstnmov");
                sound.Value.AudioData.SoundSource.Should().Be(GameActions.GetSectorByTag(World, 1).Floor);
            });
        }

        [Fact(DisplayName = "Looping sector sounds")]
        public void LoopSounds()
        {
            World.SoundManager.SetMaxConcurrentSounds(1);
            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            World.SoundManager.SetMaxConcurrentSounds(2);
            ActivateSpecialLine(2);
            ActivateSpecialLine(0);

            sounds.Count.Should().Be(2);
            var sound = sounds.First!;
            sound.Should().NotBeNull();

            var secondSound = sound.Next!;
            secondSound.Should().NotBeNull();

            GameActions.TickWorld(World, 35, () =>
            {
                sound.Value.AudioData.Attenuation.Should().Be(Attenuation.Default);
                sound.Value.AudioData.SoundChannelType.Should().Be(SoundChannelType.Auto);
                sound.Value.AudioData.Loop.Should().BeTrue();
                sound.Value.AudioData.SoundInfo.EntryName.Should().Be("dsstnmov");
                sound.Value.AudioData.SoundSource.Should().Be(GameActions.GetSectorByTag(World, 1).Floor);

                secondSound.Value.AudioData.Attenuation.Should().Be(Attenuation.Default);
                secondSound.Value.AudioData.SoundChannelType.Should().Be(SoundChannelType.Auto);
                secondSound.Value.AudioData.Loop.Should().BeTrue();
                secondSound.Value.AudioData.SoundInfo.EntryName.Should().Be("dsstnmov");
                secondSound.Value.AudioData.SoundSource.Should().Be(GameActions.GetSectorByTag(World, 2).Floor);
            });
        }

        [Fact(DisplayName = "Sound is stopped when another is created from same source")]
        public void OneSoundPerSource()
        {
            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            ActivateSpecialLine(2);
           
            sounds.Count.Should().Be(2);
            AssertSound(sounds, "dsswtchn");
            AssertSound(sounds, "dsstnmov");

            var sector = GameActions.GetSectorByTag(World, 1);
            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () => { }, TimeSpan.FromSeconds(280));

            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dspstop");
        }

        [Fact(DisplayName = "Looping sector sound is bumped by player sound")]
        public void BumpLoopSound()
        {
            World.SoundManager.SetMaxConcurrentSounds(1);

            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            ActivateSpecialLine(2);

            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dsstnmov");

            // Player sounds are highest priority with no attenuation
            GameActions.PlayerFirePistol(World, World.Player).Should().BeTrue();

            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dspistol");

            // Player firing pistol bumps out moving floor sound
            var waitingSounds = World.SoundManager.GetWaitingSounds();
            waitingSounds.Count.Should().Be(1);
            AssertSound(waitingSounds.First, "dsstnmov");

            // Since the moving floor is a looping sound, it should come back when the pistol completes
            GameActions.TickWorld(World, 70);
            waitingSounds.Count.Should().Be(0);
            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dsstnmov");
        }

        [Fact(DisplayName = "Looping sector sound is bumped by a closer looping sound")]
        public void SoundBumpedByDistance()
        {
            World.SoundManager.SetMaxConcurrentSounds(1);

            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            ActivateSpecialLine(2);

            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dsstnmov");
            AssertSoundSource(sounds.First, GameActions.GetSectorByTag(World, 1).Floor);

            // Move closer to sector tag 2 sound
            ActivateSpecialLine(0);
            GameActions.SetEntityPosition(World, World.Player, new Vec2D(-256, -352));
            World.Tick();

            AssertSound(sounds, "dsstnmov");
            AssertSoundSource(sounds.First, GameActions.GetSectorByTag(World, 2).Floor);
        }

        [Fact(DisplayName = "Sound is not created because of sound limit being hit while other sounds are closer")]
        public void SoundPriority()
        {
            World.SoundManager.SetMaxConcurrentSounds(1);

            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            ActivateSpecialLine(2);

            var entity = GameActions.GetEntity(World, 1);
            IAudioSource? audioSource = World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity));
            audioSource.Should().BeNull();

            // Move closer to entity, sound should be created
            GameActions.SetEntityPosition(World, World.Player, new Vec2D(-96, -224));
            audioSource = World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity));
            audioSource.Should().NotBeNull();

            World.Tick();
            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dsbgsit1");
            AssertSoundSource(sounds.First, entity);
        }

        [Fact(DisplayName = "Sound created because no attenuation has higher priority")]
        public void SoundPriorityNoAttenuation()
        {
            World.SoundManager.SetMaxConcurrentSounds(1);

            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            ActivateSpecialLine(2);

            var entity = GameActions.GetEntity(World, 1);
            IAudioSource? audioSource = World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity));
            audioSource.Should().BeNull();

            // No attenuation overrides distance priority
            audioSource = World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity, attenuation: Attenuation.None));
            audioSource.Should().NotBeNull();
            World.Tick();
            AssertSound(sounds, "dsbgsit1");
            AssertSoundSource(sounds.First, entity);
        }

        [Fact(DisplayName = "Players can create multiple sounds on different channels")]
        public void PlayerSoundChannels()
        {
            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            foreach (var sound in World.Player.SoundChannels)
                sound.Should().BeNull();

            var weaponSound = World.SoundManager.CreateSoundOn(World.Player, "weapons/pistol", SoundChannelType.Weapon, new SoundParams(World.Player))!;
            var firstPickupSound = World.SoundManager.CreateSoundOn(World.Player, "misc/i_pkup", SoundChannelType.Item, new SoundParams(World.Player))!;
            weaponSound.Should().NotBeNull();
            firstPickupSound.Should().NotBeNull();
            World.Tick();

            World.Player.SoundChannels[(int)SoundChannelType.Weapon].Should().Be(weaponSound);
            World.Player.SoundChannels[(int)SoundChannelType.Item].Should().Be(firstPickupSound);

            sounds.Count.Should().Be(2);

            // Second pickup should only overwrite on the item channel
            var secondPickupSound = World.SoundManager.CreateSoundOn(World.Player, "misc/i_pkup", SoundChannelType.Item, new SoundParams(World.Player))!;
            secondPickupSound.Should().NotBeNull();
            World.Tick();
            sounds.Count.Should().Be(2);
            sounds.Contains(secondPickupSound).Should().BeTrue();
            sounds.Contains(firstPickupSound).Should().BeFalse();

            World.Player.SoundChannels[(int)SoundChannelType.Weapon].Should().Be(weaponSound);
            World.Player.SoundChannels[(int)SoundChannelType.Item].Should().Be(secondPickupSound);
        }

        [Fact(DisplayName = "One sound per channel for entity")]
        public void EntitySoundChannels()
        {
            var sounds = World.SoundManager.GetSoundsToPlay();
            sounds.Count.Should().Be(0);
            var entity = GameActions.GetEntity(World, 1);

            IAudioSource? audioSource = World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity));
            audioSource.Should().NotBeNull();

            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dsbgsit1");
            AssertSoundSource(sounds.First, entity);
            entity.SoundChannels[(int)SoundChannelType.Auto].Should().Be(audioSource);

            audioSource = World.SoundManager.CreateSoundOn(entity, "imp/active", SoundChannelType.Auto, new SoundParams(entity));
            audioSource.Should().NotBeNull();

            sounds.Count.Should().Be(1);
            AssertSound(sounds, "dsbgact");
            AssertSoundSource(sounds.First, entity);
            entity.SoundChannels[(int)SoundChannelType.Auto].Should().Be(audioSource);
        }

        [Fact(DisplayName = "Sound not created because it's too far away")]
        public void SoundTooFar()
        {
            var sounds = World.SoundManager.GetSoundsToPlay();
            sounds.Count.Should().Be(0);
            var entity = GameActions.GetEntity(World, 1);

            Vec3D pos = World.Player.Position;
            pos.X += Constants.MaxSoundDistance + 1;
            entity.SetPosition(pos);
            World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity, attenuation: Attenuation.Default)).Should().BeNull();
            sounds.Count.Should().Be(0);

            pos = World.Player.Position;
            pos.X += Constants.MaxSoundDistance;
            entity.SetPosition(pos);
            World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity, attenuation: Attenuation.Default)).Should().NotBeNull();
            sounds.Count.Should().Be(1);
        }

        [Fact(DisplayName = "Sound with no attenuation is created past max sound distance")]
        public void NoAttenuationPastMaxDistance()
        {
            var sounds = World.SoundManager.GetSoundsToPlay();
            sounds.Count.Should().Be(0);
            var entity = GameActions.GetEntity(World, 1);

            Vec3D pos = World.Player.Position;
            pos.X += Constants.MaxSoundDistance;
            entity.SetPosition(pos);
            World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity, attenuation: Attenuation.Default)).Should().NotBeNull();
            sounds.Count.Should().Be(1);
        }

        [Fact(DisplayName = "Loop sound past max distances is added to wait list")]
        public void LoopSoundPastMaxDistance()
        {
            var sounds = World.SoundManager.GetSoundsToPlay();
            var waitingSounds = World.SoundManager.GetWaitingSounds();
            sounds.Count.Should().Be(0);
            waitingSounds.Count.Should().Be(0);
            var entity = GameActions.GetEntity(World, 1);

            Vec3D pos = World.Player.Position;
            pos.X += Constants.MaxSoundDistance + 1;
            entity.SetPosition(pos);
            World.SoundManager.CreateSoundOn(entity, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity, loop: true, attenuation: Attenuation.Default)).Should().BeNull();
            sounds.Count.Should().Be(0);
            waitingSounds.Count.Should().Be(1);
        }

        [Fact(DisplayName = "Loop sound is stopped when sector movement is paused")]
        public void LoopSoundStoppedOnPause()
        {
            var sounds = World.SoundManager.GetPlayingSounds();
            var waitingSounds = World.SoundManager.GetWaitingSounds();
            sounds.Count.Should().Be(0);
            waitingSounds.Count.Should().Be(0);

            ActivateSpecialLine(12);
            AssertSound(sounds, "dsstnmov");
            AssertSoundSource(sounds.First, GameActions.GetSectorByTag(World, 3).Ceiling);

            ActivateSpecialLine(15);
            // No stop sound is created on pause
            sounds.Count.Should().Be(0);
        }

        [Fact(DisplayName = "Sounds are paused and resumed")]
        public void PauseAndResume()
        {
            var sounds = World.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(0);

            ActivateSpecialLine(2);
            ActivateSpecialLine(0);

            sounds.Count.Should().Be(4);
            AssertSoundsPlaying(sounds, true);

            World.SoundManager.Pause();
            sounds.Count.Should().Be(4);
            AssertSoundsPlaying(sounds, false);

            World.SoundManager.Resume();
            sounds.Count.Should().Be(4);
            AssertSoundsPlaying(sounds, true);
        }

        [Fact(DisplayName = "Sounds are paused and resumed")]
        public void SoundsAreCleared()
        {
            var sounds = World.SoundManager.GetPlayingSounds();
            var waitingSounds = World.SoundManager.GetWaitingSounds();
            sounds.Count.Should().Be(0);
            waitingSounds.Count.Should().Be(0);

            ActivateSpecialLine(2);
            ActivateSpecialLine(0);

            World.SoundManager.CreateSoundOn(World.Player, "weapons/pistol", SoundChannelType.Weapon, new SoundParams(World.Player));

            var entity1 = GameActions.GetEntity(World, 1);
            var entity2 = GameActions.GetEntity(World, 2);
            World.SoundManager.CreateSoundOn(entity1, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity1, attenuation: Attenuation.Default));

            // Create a loop sound past max distance so it's added to the waiting list
            Vec3D pos = World.Player.Position;
            pos.X += Constants.MaxSoundDistance + 1;
            entity2.SetPosition(pos);
            World.SoundManager.CreateSoundOn(entity2, "imp/sight1", SoundChannelType.Auto, new SoundParams(entity2, loop: true, attenuation: Attenuation.Default)).Should().BeNull();

            World.Tick();
            sounds.Count.Should().Be(6);
            waitingSounds.Count.Should().Be(1);

            World.SoundManager.ClearSounds();
            sounds.Count.Should().Be(0);
            waitingSounds.Count.Should().Be(0);
        }

        private static void AssertSoundsPlaying(LinkedList<IAudioSource> list, bool playing)
        {
            var node = list.First;
            while (node != null)
            {
                node.Value.IsPlaying().Should().Be(playing);
                node = node.Next;
            }
        }

        private static void AssertSound(LinkedList<IAudioSource> list, string soundName)
        {
            bool found = false;
            var node = list.First;
            while (node != null)
            {
                if (node.Value.AudioData.SoundInfo.EntryName.Equals(soundName, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }

                node = node.Next;
            }

            found.Should().BeTrue();
        }

        private static void AssertSoundSource(LinkedListNode<IAudioSource>? node, object source)
        {
            node.Should().NotBeNull();
            ReferenceEquals(node!.Value.AudioData.SoundSource, source).Should().BeTrue();
        }

        private static void AssertSound(LinkedListNode<WaitingSound>? node, string soundName)
        {
            node.Should().NotBeNull();
            node!.Value.SoundInfo.EntryName.Should().Be(soundName);
        }

        private void ActivateSpecialLine(int lineId)
        {
            GameActions.ActivateLine(World, World.Player, lineId, ActivationContext.UseLine).Should().BeTrue();
            // Advance so the movement sound is actively playing
            World.Tick();
        }
    }
}