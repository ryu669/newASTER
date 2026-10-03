using System;
using System.IO;
// Original procedural composition; no samples or third-party melodies.
public static class Plan7AudioSource
{
    const int Rate=44100;
    static double Note(int midi)=>440*Math.Pow(2,(midi-69)/12.0);
    static double Tone(double t,double frequency)=>Math.Sin(2*Math.PI*frequency*t)+.18*Math.Sin(4*Math.PI*frequency*t);
    static double Fade(double value)=>.5-.5*Math.Cos(Math.PI*Math.Max(0,Math.Min(1,value)));
    static double Window(double phase,double length,double attack,double release)=>Fade(phase/attack)*Fade((length-phase)/release);
    public static void Generate(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach(var name in new[]{"hit","shield","heal","break","victory","bgm"})Write(Path.Combine(directory,"candidate-"+name+"-v2.wav"),name);
    }
    static void Write(string path,string kind)
    {
        double duration=kind=="bgm"?16:kind=="victory"?2.4:kind=="heal"?1.2:.65;
        int count=(int)(Rate*duration);uint noise=71893;
        using(var stream=File.Create(path))using(var writer=new BinaryWriter(stream)){
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(Rate);writer.Write(Rate*2);writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            int[] melody={74,77,81,79,77,74,72,69,72,76,79,81,79,76,74,72};int[] roots={50,46,53,48};
            for(int i=0;i<count;i++){
                double t=i/(double)Rate,value=0;noise=1664525*noise+1013904223;double random=(noise/(double)uint.MaxValue)*2-1;
                if(kind=="bgm"){
                    int step=(int)(t/.5);double phase=t% .5;value=.095*Tone(phase,Note(melody[step%melody.Length]))*Math.Exp(-phase*7)*Window(phase,.5,.012,.04);
                    int root=roots[(int)(t/4)%4];double chordTime=t%4;
                    double pad=Math.Sin(Math.PI*chordTime/4);foreach(int interval in new[]{0,7,12})value+=.025*Tone(chordTime,Note(root+interval))*pad*pad;
                }else if(kind=="hit")value=(.13*random+.16*Tone(t,180-100*t))*Math.Exp(-t*12);
                else if(kind=="break")value=(.16*random+.12*Tone(t,110))*Math.Exp(-t*8);
                else if(kind=="shield")value=.18*Tone(t,660)*Math.Exp(-t*7)+.05*Tone(t,990)*Math.Exp(-t*5);
                else if(kind=="heal"){double phase=t% .2;value=.13*Tone(phase,Note(72+(int)(t/.2)*2))*Math.Exp(-t*2)*Window(phase,.2,.01,.025);}
                else {int[] notes={62,65,69,74,77,81};int step=Math.Min(5,(int)(t/.3));double phase=t-step*.3;double length=step==5?duration-1.5:.3;value=.17*Tone(phase,Note(notes[step]))*Math.Exp(-phase*6)*Window(phase,length,.012,.04);}
                if(kind!="bgm")value*=Window(t,duration,.01,.04);
                if(i==0 || i==count-1)value=0;
                writer.Write((short)(Math.Max(-.45,Math.Min(.45,value))*short.MaxValue));
            }
        }
    }
}
