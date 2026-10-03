using System;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void ValidateCandidateAudio(string name,AudioClip clip)
        {
            int samples=(int)(44100*(name=="bgm"?16:name=="victory"?2.4:name=="heal"?1.2:.65));
            if(clip==null || clip.frequency!=44100 || clip.channels!=1 || clip.samples!=samples)throw new InvalidOperationException("PLAN7_AUDIO_FORMAT_FAIL "+name);
            var pcm=new float[clip.samples];if(!clip.GetData(pcm,0))throw new InvalidOperationException("PLAN7_AUDIO_READ_FAIL "+name);
            if(pcm[0]!=0 || pcm[pcm.Length-1]!=0)throw new InvalidOperationException("PLAN7_AUDIO_ENDPOINT_FAIL "+name);
            float peak=0;foreach(float value in pcm){if(float.IsNaN(value) || float.IsInfinity(value))throw new InvalidOperationException("PLAN7_AUDIO_NONFINITE "+name);peak=Math.Max(peak,Math.Abs(value));}
            if(peak<=0 || peak>=1)throw new InvalidOperationException("PLAN7_AUDIO_PEAK_FAIL "+name);
            double maxError=0;int boundaries=0;
            if(name=="bgm" || name=="heal" || name=="victory"){
                double interval=name=="bgm"?.5:name=="heal"?.2:.3;int limit=name=="victory"?5:(int)Math.Floor((pcm.Length-1)/44100d/interval);
                for(int step=1;step<=limit;step++){
                    int index=(int)Math.Round(step*interval*44100);
                    double jump=pcm[index]-pcm[index-1],left=pcm[index-1]-pcm[index-2],right=pcm[index+1]-pcm[index];
                    maxError=Math.Max(maxError,Math.Abs(jump-(left+right)/2)*32768);boundaries++;
                }
                if(boundaries!=limit || maxError>8)throw new InvalidOperationException("PLAN7_AUDIO_TRANSITION_FAIL "+name+" error="+maxError);
            }
            Debug.Log("PLAN7_AUDIO_WAVEFORM_PASS "+name+" v2 samples="+samples+" boundaries="+boundaries+" maxBoundarySlopeError="+maxError.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
