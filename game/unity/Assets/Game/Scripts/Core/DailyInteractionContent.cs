using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public static class DailyInteractionContent
    {
        public static DailyInteractionDef[] Definitions => Build().Select(x=>x.Item1).ToArray();
        public static DailyPresentationDef[] Presentations => Build().Select(x=>x.Item2).ToArray();
        private static Tuple<DailyInteractionDef,DailyPresentationDef>[] Build()
        {
            string[] commonNames={"朝の挨拶","並んで歩く","空を見上げる","ベンチでひと息","風を感じる","飲み物を分ける","花を眺める","水面のきらめき","本を開く","小さな発見","木陰の休息","同じ景色","手を振る","足取りを合わせる","星を探す","食卓の時間","雲のかたち","雨音に耳を澄ます","庭の見回り","別れ際の会釈"};
            string[] commonText={"顔を向け、穏やかな会釈を交わす。","隣へ移動し、歩幅を揃えて進む。","少し顔を上げ、空の色を一緒に眺める。","腰掛けた隣に少し間を空け、静かに休む。","風の通る方へ向き直り、揺れる木々を見守る。","飲み物を手元に置き、互いの様子を見ながらひと息つく。","花の近くへ寄り、同じ花へ視線を向ける。","水面へ視線を落とし、きらめきを目で追う。","開いた頁を示し、同じ箇所へ目を向ける。","足元の小さな変化を指し示し、相手の反応を待つ。","木陰に立ち、肩の力を抜いて休む。","同じ方向へ向き直り、景色を静かに分かち合う。","少し離れた場所から手を振り、顔を見合わせる。","相手の速さを確かめて、歩くリズムを揃える。","夜空へ目を向け、見つけた星を指し示す。","食卓に向き合い、食事の間をゆっくりと過ごす。","空の一点を指し、流れる雲を一緒に見送る。","屋根の下で立ち止まり、雨の音に耳を傾ける。","庭の端から端へ視線を巡らせ、変化を探す。","相手へ向き直り、会釈してから静かに離れる。"};
            string[] topics={"greeting","walk","sky","rest","wind","tea","flowers","water","books","discovery","rest","scenery","greeting","walk","stars","meal","sky","rain","garden","farewell"};
            string[] traits={"hair.blonde","hair.black","hair.silver","hair.red","body.slender","body.glamorous","body.muscular","body.petite","taste.muscle","taste.books","taste.sweets","taste.cooking","taste.mechanics","taste.flowers","personality.cool","personality.active","personality.shy","personality.night","appearance.animal","appearance.glasses"};
            string[] traitText={"陽の当たる場所で立ち止まり、明るい髪の揺れを整える。","髪をそっと整え、落ち着いた表情で相手へ向き直る。","淡い光を見上げ、髪に触れたあと景色へ目を戻す。","光の下で髪を整え、軽く顔を見合わせる。","軽やかに身体を伸ばし、歩く前の準備をする。","楽な姿勢へ座り直し、ゆったりと肩の力を抜く。","腕を伸ばして姿勢を整え、相手にも同じ動作を示す。","背伸びをして景色を確かめ、見つけたものを指す。","互いに姿勢を確かめながら、軽い体操をする。","本の頁を静かにめくり、気になった箇所を示す。","小さな菓子を見比べ、ひとつを相手の前へ置く。","食卓の品を整え、相手の分をそっと取り分ける。","手元の道具を眺め、動く部分を指で示す。","花を傷つけないよう身を寄せ、咲いた場所を示す。","静かな間を保ち、相手の視線へ小さく頷く。","先へ数歩進み、振り返って相手を待つ。","少し目を伏せたあと、短く顔を上げて会釈する。","静かな空を見上げ、月のある方へ視線を導く。","物音のする方へ顔を向け、相手と様子を確かめる。","眼鏡を整え、手元と相手の顔へ順に目を向ける。"};
            string[] partners={"personality.cool","personality.active","personality.night","personality.shy","taste.muscle","taste.flowers","taste.muscle","body.muscular","body.muscular","taste.books","taste.cooking","taste.sweets","taste.mechanics","taste.flowers","personality.active","personality.shy","personality.cool","hair.silver","appearance.horns","taste.books"};
            var result=new List<Tuple<DailyInteractionDef,DailyPresentationDef>>();
            for(int i=0;i<20;i++){
                var conditions=new List<InteractionCondition>();
                if(i==14)conditions.Add(new InteractionCondition{kind="time",value="night"});
                if(i==17)conditions.Add(new InteractionCondition{kind="weather",value="rain"});
                result.Add(Part("daily.common."+(i+1),"common",commonNames[i],commonText[i],topics[i],conditions.ToArray(),i==3?"sit":i==1 || i==13?"walk":"look"));
                result.Add(Part("daily.trait."+(i+1),"trait",InteractionTraitCatalog.Get(traits[i]).displayName+"のひととき",traitText[i],traits[i],new[]{new InteractionCondition{kind="trait",value=traits[i]}},"gesture"));
                result.Add(Part("daily.pair."+(i+1),"pair","ふたりの"+InteractionTraitCatalog.Get(traits[i]).displayName,traitText[i]+"相手はその様子に顔を向け、頷きや身振りを返す。",traits[i],new[]{new InteractionCondition{kind="trait",value=traits[i]},new InteractionCondition{kind="trait",actor="partner",value=partners[i]}},"pair_position",i%2==0));
            }
            return result.ToArray();
        }
        private static Tuple<DailyInteractionDef,DailyPresentationDef> Part(string id,string category,string title,string text,string topic,InteractionCondition[] conditions,string gesture,bool symmetric=false)=>
            Tuple.Create(new DailyInteractionDef{id=id,category=category,topicTags=new[]{topic},conditions=conditions,presentationId=id,weight=DailyInteractionSelector.BaseWeight(category),symmetric=symmetric},new DailyPresentationDef{id=id,title=title,description=text,gesture=gesture,seconds=10+(int.Parse(id.Split('.').Last())%3)*5,capabilities=new[]{"gaze","expression",gesture=="pair_position"?"pair_position":"gesture"}});
    }
    [Serializable] public sealed class DailyDateDef
    {
        public string id,name,topic,ownerId;
        public int minimumAffection=10;
    }
    public static class DailyDateCatalog
    {
        public static DailyDateDef[] Definitions=>new[]{
            new DailyDateDef{id="date.walk",name="散歩",topic="walk"},new DailyDateDef{id="date.meal",name="食事",topic="meal"},
            new DailyDateDef{id="date.stars",name="星見",topic="stars"},new DailyDateDef{id="date.tea",name="お茶",topic="tea"},new DailyDateDef{id="date.water",name="水辺",topic="water"}};
    }
    public sealed class DailyInteractionCompletion
    {
        public string sessionId,kind,contentId;
        public string[] forms;
        public bool GivesAffection=>kind=="conversation" || kind=="together" || kind=="date" || kind=="activity";
    }
    public sealed class DailyInteractionSession
    {
        private readonly DailyPresentationDef[] parts;
        private readonly Action release;
        private readonly Action<DailyInteractionCompletion> notify;
        private readonly string[] forms;
        private float elapsed;
        private int index;
        public string Id{get;} public string GardenId{get;} public string Kind{get;} public string ContentId{get;}
        public bool Completed{get;private set;} public bool Cancelled{get;private set;}
        public DailyPresentationState State{get;}
        public DailyPresentationDef Current=>parts[Math.Min(index,parts.Length-1)];
        public float Duration=>parts.Sum(p=>p.seconds);
        public bool Transitioning{get;private set;}
        public DailyInteractionSession(string id,string garden,string kind,string content,string[] participants,DailyPresentationDef[] sequence,DailyPresentationState state,Action release,Action<DailyInteractionCompletion> notify)
        {
            if(!HomeExperienceCatalog.Id(id) || !HomeExperienceCatalog.Id(garden) || !HomeExperienceCatalog.Id(content) || !new[]{"conversation","together","date","social","activity","replay"}.Contains(kind) || participants==null || participants.Length<1 || participants.Any(f=>!HomeExperienceCatalog.Id(f)) || participants.Distinct().Count()!=participants.Length || kind!="activity" && participants.Length>2 || kind=="date" && participants.Length!=1 || sequence==null || sequence.Length==0 || sequence.Any(p=>p==null || p.seconds<1 || p.seconds>60))throw new ArgumentException("Invalid daily session.");
            Id=id;GardenId=garden;Kind=kind;ContentId=content;forms=(string[])participants.Clone();parts=sequence.Select(p=>new DailyPresentationDef{id=p.id,title=p.title,description=p.description,gesture=p.gesture,expression=p.expression,requiredFurniture=p.requiredFurniture,seconds=p.seconds,capabilities=p.capabilities.ToArray()}).ToArray();State=state??new DailyPresentationState();this.release=release??throw new ArgumentNullException(nameof(release));this.notify=notify??throw new ArgumentNullException(nameof(notify));Enter();
        }
        private void Enter()
        {
            Transitioning=Current.requiredFurniture!=null && State.furniture!=Current.requiredFurniture;
            if(!Transitioning){State.gesture=Current.gesture;State.expression=Current.expression??State.expression;}
        }
        public void Tick(float seconds,bool active)
        {
            if(Completed || Cancelled || !active || seconds<=0 || seconds>2 || float.IsNaN(seconds))return;
            elapsed+=seconds;
            if(Transitioning){if(elapsed<1)return;elapsed-=1;State.furniture=Current.requiredFurniture;State.gesture=Current.gesture;State.expression=Current.expression??State.expression;Transitioning=false;}
            if(elapsed<Current.seconds)return;
            elapsed-=Current.seconds;index++;
            if(index<parts.Length){Enter();return;}
            Completed=true;release();notify(new DailyInteractionCompletion{sessionId=Id,kind=Kind,contentId=ContentId,forms=(string[])forms.Clone()});
        }
        public void Cancel(){if(Completed || Cancelled)return;Cancelled=true;release();}
    }
}
