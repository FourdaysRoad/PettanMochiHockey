using System.Collections.Generic;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// Core で合成した音を AudioClip にして、プレイ中ずっと使い回す（シーンを移っても作り直さない）。
    /// </summary>
    public static class SoundBank
    {
        private static readonly Dictionary<(SoundId, int), AudioClip> s_sounds = new Dictionary<(SoundId, int), AudioClip>();
        private static readonly Dictionary<(int, float), AudioClip> s_bgm = new Dictionary<(int, float), AudioClip>();

        public static AudioClip Sound(SoundId id, int sampleRate)
        {
            if (!s_sounds.TryGetValue((id, sampleRate), out var clip) || clip == null)
            {
                clip = ToClip($"SE_{id}", SoundSynth.Generate(id, sampleRate), sampleRate);
                s_sounds[(id, sampleRate)] = clip;
            }

            return clip;
        }

        public static AudioClip Bgm(int sampleRate, float tempo)
        {
            if (!s_bgm.TryGetValue((sampleRate, tempo), out var clip) || clip == null)
            {
                clip = ToClip("BGM_Match", BgmSynth.Generate(sampleRate, tempo), sampleRate);
                s_bgm[(sampleRate, tempo)] = clip;
            }

            return clip;
        }

        private static AudioClip ToClip(string name, float[] samples, int sampleRate)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            // シーンを移るときの「使っていないアセットの片付け」で消されないようにする
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return clip;
        }
    }
}
