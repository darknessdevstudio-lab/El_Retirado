using UnityEngine;

namespace ElRetirado.Enemies
{
    // Add to a door or prop and call EmitNoise from its interaction/animation event.
    public class NoiseEmitter : MonoBehaviour
    {
        [Min(0)] public float radius = 8f;
        public AudioSource source;
        public bool emitWhenAudioStarts;
        bool wasPlaying;
        void Update()
        {
            bool playing = source && source.isPlaying;
            if (emitWhenAudioStarts && playing && !wasPlaying) EmitNoise();
            wasPlaying = playing;
        }
        public void EmitNoise() { NoiseSystem.Emit(transform.position, radius); }
    }
}
