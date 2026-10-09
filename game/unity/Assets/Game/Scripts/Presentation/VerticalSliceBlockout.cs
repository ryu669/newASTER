using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;

namespace NewAster.Presentation
{
    /// <summary>独自アセット制作までの間、縦切りの距離感・部位可読性・箱庭規模を確認する3Dブロックアウト。</summary>
    public sealed class VerticalSliceBlockout : MonoBehaviour
    {
        private Transform dragon;
        private Transform garden;
        private GameObject ground;
        private readonly List<Transform> party = new List<Transform>();
        private readonly List<GameObject> placedFurniture = new List<GameObject>();
        private float clock;
        private bool frozen;
        private BattleStageEffects effects;
        private SlayerModelView slayer;
        private bool portrait;
        private Transform[] originalParty;
        private int slayerActor;
        public void SetFormation(string[] defaults,string[] ids)
        {
            if(originalParty==null)originalParty=party.ToArray();party.Clear();
            var used=new HashSet<int>();var indices=ids.Select(id=>System.Array.IndexOf(defaults,id)).ToArray();foreach(int index in indices.Where(i=>i>=0))used.Add(index);
            for(int i=0;i<indices.Length;i++){if(indices[i]<0){indices[i]=Enumerable.Range(0,originalParty.Length).First(n=>!used.Contains(n));used.Add(indices[i]);}party.Add(originalParty[indices[i]]);}
            slayerActor=System.Array.IndexOf(ids,defaults[0]);
        }
        public void SetPortraitView(bool value)
        {
            portrait=value;
            if(!value) return;
            dragon.gameObject.SetActive(false); garden.gameObject.SetActive(false); ground.SetActive(false);
            for(int i=0;i<party.Count;i++) party[i].gameObject.SetActive(i==slayerActor);
            foreach(var item in placedFurniture) item.SetActive(false);
            effects.Clear();
        }
        public bool SetSlayerOutfit(string id) => slayer!=null && slayer.TryEquip(id);
        public string SlayerOutfitId => slayer==null?"":slayer.OutfitId;
        public bool SetSlayerExpression(string id) => slayer!=null && slayer.SetExpression(id);
        public void BeginPresentation(BattlePresentationEvent e)
        {
            Vector3 destination=dragon.position;
            foreach(Transform part in dragon) {
                if(e.Target.Contains("horn") && part.name.Contains("Horn") || e.Target.Contains("left") && part.name.Contains("Left") || e.Target.Contains("right") && part.name.Contains("Right") || e.Target.Contains("tail") && part.name.Contains("Tail")) destination=part.position;
            }
            effects.Begin(e,e.Actor>=0?party[e.Actor].position:dragon.position,destination);
            if(slayer!=null) slayer.PlayEvent(e,slayerActor);
        }
        private LineRenderer attackTrail;
        private float trailUntil;
        private readonly List<LineRenderer> enemyTrails = new List<LineRenderer>();
        private float enemyTrailUntil;
        private readonly List<LineRenderer> healingTrails = new List<LineRenderer>();
        private float healingTrailUntil;
        public void PlayHealing(int actor,IReadOnlyList<int> targets)
        {
            while(healingTrails.Count<targets.Count) {
                var line=new GameObject("Healing trail").AddComponent<LineRenderer>();
                line.material=new Material(Resources.Load<Shader>("StageSurface")); line.positionCount=2;
                line.startWidth=.1f; line.endWidth=.05f; line.startColor=line.endColor=new Color(.3f,1f,.65f);
                healingTrails.Add(line);
            }
            for(int i=0;i<healingTrails.Count;i++) {
                var line=healingTrails[i]; line.enabled=i<targets.Count;
                if(i<targets.Count) { line.SetPosition(0,party[actor].position+Vector3.up); line.SetPosition(1,party[targets[i]].position+Vector3.up*(targets[i]==actor?2:1)); }
            }
            healingTrailUntil=clock+.65f;
        }
        public void PlayEnemyAction(bool major)
        {
            if(enemyTrails.Count==0) foreach(var member in party) {
                var line=new GameObject("Enemy strike").AddComponent<LineRenderer>();
                line.material=new Material(Resources.Load<Shader>("StageSurface")); line.positionCount=2;
                enemyTrails.Add(line);
            }
            for(int i=0;i<enemyTrails.Count;i++) {
                var line=enemyTrails[i]; line.SetPosition(0,dragon.position+Vector3.up);
                line.SetPosition(1,party[i].position);
                line.startWidth=major?.2f:.08f; line.endWidth=.03f;
                line.startColor=line.endColor=major?new Color(1f,.35f,.12f):new Color(.95f,.5f,.35f);
                line.enabled=true;
            }
            enemyTrailUntil=clock+(major?.8f:.4f);
        }
        public void ClearActionEffects()
        {
            if(effects!=null) effects.Clear();
            if(attackTrail!=null) attackTrail.enabled=false;
            foreach(var line in enemyTrails) line.enabled=false;
            foreach(var line in healingTrails) line.enabled=false;
        }
        public void Synchronize(bool gardenView, bool gardenUnlocked, PlayableProgress progress, PlayableBattle battle, string target, bool pause,BattlePresentationEvent visual=null)
        {
            frozen=pause;
            if(effects!=null) effects.Synchronize(party,battle,visual,pause);
            if(slayer!=null) slayer.Synchronize(pause,battle!=null && slayerActor>=0 && slayerActor<party.Count && (visual?.Casting[slayerActor]??battle.IsCasting(slayerActor)));
            if(ground!=null) ground.SetActive(battle==null);
            garden.gameObject.SetActive(gardenView && gardenUnlocked);
            dragon.gameObject.SetActive(!gardenView);
            foreach(var member in party) member.gameObject.SetActive(!gardenView || gardenUnlocked);
            for(int i=0;i<party.Count;i++) foreach(var renderer in party[i].GetComponentsInChildren<Renderer>()) foreach(var material in renderer.sharedMaterials) material.SetColor("_EmissionColor",battle!=null && (visual?.Casting[i]??battle.IsCasting(i))?new Color(.35f,.12f,.55f):Color.black);
            for(int slot=0;slot<placedFurniture.Count;slot++) {
                var furniture=placedFurniture[slot]; int type=progress.Slots[slot];
                furniture.SetActive(gardenView && gardenUnlocked && type>=0);
                if(type>=0) {
                    furniture.transform.localScale=type==0?new Vector3(1.4f,.3f,.5f):type==1?new Vector3(.3f,.7f,.3f):new Vector3(.8f,.6f,.8f);
                    furniture.GetComponent<Renderer>().material.color=type==0?new Color(.45f,.25f,.12f):type==1?new Color(.6f,.9f,.55f):new Color(.85f,.55f,.65f);
                }
            }
            foreach(Transform part in dragon) {
                string partId=part.name.Contains("Horn")?"crystal-horn-crown":part.name.Contains("Left")?"left-wing-root":part.name.Contains("Right")?"right-wing-root":part.name.Contains("Tail")?"vine-wrapped-tail":null;
                int index=battle==null || partId==null?-1:battle.State.Parts.ToList().FindIndex(p=>p.Id==partId);
                var renderer=part.GetComponent<Renderer>(); if(renderer==null) continue;
                renderer.enabled=battle==null || index<0 || (visual!=null?visual.PartHp[index]>0:!battle.State.Parts[index].IsBroken);
                bool selected=battle!=null && (index<0?target=="body":target==battle.State.Parts[index].Id);
                renderer.material.SetColor("_EmissionColor",selected?new Color(.2f,.8f,.45f):Color.black);
                if(selected) renderer.material.EnableKeyword("_EMISSION"); else renderer.material.DisableKeyword("_EMISSION");
            }
        }
        public void PlayAction(int hero, bool supportAction, string target, int allyTarget = -1)
        {
            if(hero<0 || hero>=party.Count) return;
            if(attackTrail==null) {
                attackTrail=new GameObject("Action trail").AddComponent<LineRenderer>();
                attackTrail.material=new Material(Resources.Load<Shader>("StageSurface"));
                attackTrail.positionCount=2; attackTrail.startWidth=.12f; attackTrail.endWidth=.03f;
            }
            Vector3 destination=dragon.position;
            foreach(Transform part in dragon) {
                if(target.Contains("horn") && part.name.Contains("Horn") || target.Contains("left") && part.name.Contains("Left") || target.Contains("right") && part.name.Contains("Right") || target.Contains("tail") && part.name.Contains("Tail")) destination=part.position;
            }
            attackTrail.SetPosition(0,party[hero].position);
            attackTrail.SetPosition(1,supportAction?(allyTarget>=0 && allyTarget<party.Count?party[allyTarget].position+Vector3.up:party[hero].position+Vector3.up*2f):destination);
            attackTrail.startColor=attackTrail.endColor=supportAction?new Color(.3f,1f,.65f):new Color(1f,.8f,.3f);
            trailUntil=clock+.45f; attackTrail.enabled=true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => new GameObject("Vertical Slice Blockout").AddComponent<VerticalSliceBlockout>();

        private void Awake()
        {
            CreateHeroine();
            dragon = CreateGreenReturnDragon();
            garden = CreateGrasslandForestGarden();
            CreateSky();
            effects=new GameObject("Battle Stage Effects").AddComponent<BattleStageEffects>();
            effects.Initialize(party.Count);
            for(int slot=0;slot<3;slot++) placedFurniture.Add(Primitive("Placed Furniture "+slot,PrimitiveType.Cube,new Vector3(-5f+slot*1.4f,.45f,1.5f),Vector3.one,new Color(.4f,.25f,.15f)));
        }

        private void Update()
        {
            if(clock>healingTrailUntil) foreach(var line in healingTrails) line.enabled=false;
            if(!frozen) clock+=Time.deltaTime;
            var time = clock;
            if(attackTrail!=null && time>=trailUntil) attackTrail.enabled=false;
            if(time>=enemyTrailUntil) foreach(var line in enemyTrails) line.enabled=false;
            if (dragon != null)
            {
                dragon.position = new Vector3(2.5f, 1.4f + Mathf.Sin(time * .8f) * .22f, 1.2f);
                dragon.rotation = Quaternion.Euler(0f, Mathf.Sin(time * .35f) * 8f, Mathf.Sin(time * .7f) * 3f);
            }
            for (var index = 0; index < party.Count; index++)
            {
                var member = party[index];
                if (member == null) continue;
                member.localPosition = new Vector3(member.localPosition.x, 1.05f + (portrait?0:Mathf.Sin(time * 1.4f + index) * .08f), member.localPosition.z);
            }
            if (garden != null) garden.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * .18f) * 2f, 0f);
        }

        private void CreateHeroine()
        {
            var root = new GameObject("Five Heroine Blockouts");
            var colors = new[] { new Color(.88f,.92f,.88f), new Color(.75f,.86f,.98f), new Color(.94f,.78f,.64f), new Color(.68f,.91f,.73f), new Color(.83f,.70f,.95f) };
            for (var index = 0; index < 5; index++)
            {
                var heroine = new GameObject("Heroine " + (index + 1));
                heroine.transform.position=new Vector3(-4.8f + index * .7f,1.05f,-1.5f+index*.08f);
                heroine.transform.SetParent(root.transform);
                party.Add(heroine.transform);
                if(index==0) { slayer=SlayerModelView.Create(heroine.transform); if(slayer!=null) continue; }
                Piece(heroine.transform,"Dress",PrimitiveType.Capsule,new Vector3(0f,0f,0f),new Vector3(.32f,.3f,.24f),colors[index]);
                Piece(heroine.transform,"Head",PrimitiveType.Sphere,new Vector3(0f,.48f,0f),new Vector3(.23f,.26f,.23f),new Color(.96f,.82f,.73f));
                Piece(heroine.transform,"Hair",PrimitiveType.Sphere,new Vector3(0f,.54f,.04f),new Vector3(.25f,.2f,.24f),new Color(.28f+.08f*index,.2f,.16f));
                for(int side=-1;side<=1;side+=2) {
                    Piece(heroine.transform,"Boot",PrimitiveType.Capsule,new Vector3(side*.1f,-.4f,0f),new Vector3(.1f,.22f,.12f),new Color(.12f,.2f,.23f));
                    Piece(heroine.transform,"Arm",PrimitiveType.Capsule,new Vector3(side*.23f,.03f,0f),new Vector3(.08f,.23f,.08f),colors[index]);
                    var wing=Piece(heroine.transform,"Flight Wing",PrimitiveType.Cube,new Vector3(side*.45f,.17f,.18f),new Vector3(.65f,.06f,.25f),new Color(.75f,.93f,.88f));
                    wing.transform.localRotation=Quaternion.Euler(0f,0f,side*25f);
                }
                Piece(heroine.transform,"Weapon",PrimitiveType.Cube,new Vector3(.32f,.2f,-.07f),new Vector3(.05f,.75f,.05f),new Color(.8f,.86f,.95f));
            }
        }

        private static Transform CreateGreenReturnDragon()
        {
            var root = new GameObject("Green Return Dragon Blockout");
            root.transform.position = new Vector3(2.5f, 1.4f, 1.2f);
            Primitive("Dragon Body", PrimitiveType.Capsule, root.transform.position, new Vector3(1.4f, 2.2f, 1.4f), new Color(.12f, .42f, .2f)).transform.SetParent(root.transform);
            Piece(root.transform,"Dragon Head",PrimitiveType.Sphere,new Vector3(0f,1.3f,-.9f),new Vector3(1.1f,.8f,1.4f),new Color(.12f,.42f,.2f));
            Piece(root.transform,"Dragon Muzzle",PrimitiveType.Cube,new Vector3(0f,1.15f,-1.65f),new Vector3(.65f,.35f,.8f),new Color(.18f,.5f,.3f));
            for(int side=-1;side<=1;side+=2) {
                Piece(root.transform,"Dragon Eye",PrimitiveType.Sphere,new Vector3(side*.47f,1.5f,-1.3f),new Vector3(.12f,.12f,.12f),new Color(1f,.8f,.25f));
                Piece(root.transform,"Dragon Claw",PrimitiveType.Capsule,new Vector3(side*.8f,-.5f,-.3f),new Vector3(.35f,.8f,.4f),new Color(.16f,.32f,.13f));
            }
            Primitive("Crystal Horn Crown [Break]", PrimitiveType.Cylinder, root.transform.position + new Vector3(0f, 2.5f, .1f), new Vector3(.45f, .75f, .45f), new Color(.1f, .8f, .6f)).transform.SetParent(root.transform);
            Wing("Left Wing Root [Break]", root.transform.position + new Vector3(-1.8f, 1f, 0f), -25f, root.transform);
            Wing("Right Wing Root [Break]", root.transform.position + new Vector3(1.8f, 1f, 0f), 25f, root.transform);
            Primitive("Vine Tail [Break]", PrimitiveType.Capsule, root.transform.position + new Vector3(0f, -.7f, 2.4f), new Vector3(.55f, 1.5f, .55f), new Color(.2f, .3f, .08f)).transform.SetParent(root.transform);
            return root.transform;
        }

        private static Transform CreateGrasslandForestGarden()
        {
            var root = new GameObject("Grassland Forest Garden Blockout");
            for (var index = 0; index < 5; index++)
            {
                var x = -7f + index * 2.4f;
                Primitive("Garden Tree Trunk", PrimitiveType.Cylinder, new Vector3(x, 1f, 3.5f), new Vector3(.35f, 1f, .35f), new Color(.22f, .12f, .05f)).transform.SetParent(root.transform);
                Primitive("Garden Tree Canopy", PrimitiveType.Sphere, new Vector3(x, 2.5f, 3.5f), new Vector3(1.5f, 1.3f, 1.5f), new Color(.12f, .45f, .18f)).transform.SetParent(root.transform);
            }
            return root.transform;
        }

        private void CreateSky()
        {
            ground=Primitive("New Star Ground", PrimitiveType.Plane, new Vector3(0f, 0f, 1.5f), new Vector3(2f, 1f, 2f), new Color(.15f, .30f, .22f));
            var random=new System.Random(42);
            for(int i=0;i<50;i++) {
                var star=Primitive("Memory Star",PrimitiveType.Sphere,new Vector3((float)random.NextDouble()*30-15,(float)random.NextDouble()*12-2,12+(float)random.NextDouble()*5),Vector3.one*.035f,new Color(.65f,.8f,.95f));
                star.GetComponent<Renderer>().material.SetColor("_EmissionColor",new Color(.6f,.8f,1f));
            }
            var moon = Primitive("New Star Moon", PrimitiveType.Sphere, new Vector3(6f, 6f, 7f), new Vector3(1.6f, 1.6f, 1.6f), new Color(.55f, .86f, .78f));
            var light = moon.AddComponent<Light>(); light.type = LightType.Point; light.range = 12f; light.intensity = 3f; light.color = new Color(.38f, .9f, .65f);
        }

        private static void Wing(string name, Vector3 position, float angle, Transform parent)
        {
            var wing = Primitive(name, PrimitiveType.Cube, position, new Vector3(2.4f, .15f, 1.15f), new Color(.2f, .65f, .38f));
            wing.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            wing.transform.SetParent(parent);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().material = new Material(Resources.Load<Shader>("StageSurface")) { color=color };
            return item;
        }
        private static GameObject Piece(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color)
        {
            var piece=Primitive(name,type,Vector3.zero,scale,color);
            piece.transform.SetParent(parent,false); piece.transform.localPosition=position;
            return piece;
        }
    }
}
