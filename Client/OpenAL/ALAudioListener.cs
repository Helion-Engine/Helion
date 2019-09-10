using System.Numerics;
using Helion.Audio;
using OpenTK.Audio.OpenAL;

namespace Helion.Client.OpenAL
{
    public class ALAudioListener : IAudioListener
    {
        private Vector3 m_position = Vector3.Zero;
        private Vector3 m_velocity = Vector3.Zero;
        
        public Vector3 Position
        {
            get => m_position; 
            set => SetPosition(value); 
        }
        
        public Vector3 Velocity
        {
            get => m_velocity;
            set => SetVelocity(value); 
        }

        private void SetPosition(Vector3 pos)
        {
            m_position = pos;
            AL.Listener(ALListener3f.Position, pos.X, pos.Y, pos.Z);
        }
        
        private void SetVelocity(Vector3 vel)
        {
            m_velocity = vel;
            AL.Listener(ALListener3f.Velocity, vel.X, vel.Y, vel.Z);
        }
    }
}