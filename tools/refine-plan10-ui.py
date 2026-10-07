from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];S=ROOT/'game/unity/Assets/Game/Scripts'
def edit(path,old,new):
 p=S/path;t=p.read_text(encoding='utf-8-sig')
 if new not in t:assert old in t,(path,old);p.write_text(t.replace(old,new),encoding='utf8')
p=S/'Presentation/Plan10AnnihilatorRecruitment.cs';t=p.read_text(encoding='utf8');old='new[]{"heroine.r","heroine.annihilator","heroine.annihilator-holy","heroine.shell","heroine.oriflamme","heroine.nighthawk"}'
t=t.replace(old,'combatDefinitions.HeroineIds.Skip(5)').replace('private bool expansionRecruitmentOpen;','private bool expansionRecruitmentOpen;\n        private int expansionRecruitmentPage;').replace('通常版と聖夜版は同じ人物です。','衣装違いは同じ人物として扱います。').replace('var forms=UnownedExpansionForms();','var allForms=UnownedExpansionForms();int pages=Math.Max(1,(allForms.Length+4)/5);expansionRecruitmentPage=Mathf.Clamp(expansionRecruitmentPage,0,pages-1);var forms=allForms.Skip(expansionRecruitmentPage*5).Take(5).ToArray();').replace('if(expansionRecruitError!=null)', '''if(GrowthButton(330,610,260,48,"‹ 前の5形態",expansionRecruitmentPage>0 && expansionRecruitRequest==null))expansionRecruitmentPage--;
            Label(660,619,260,40,(expansionRecruitmentPage+1)+" / "+pages,growthSmallStyle);
            if(GrowthButton(1010,610,260,48,"次の5形態 ›",expansionRecruitmentPage+1<pages && expansionRecruitRequest==null))expansionRecruitmentPage++;
            if(expansionRecruitError!=null)''');p.write_text(t,encoding='utf8')
edit(Path('Data/HeroineIdentityCatalog.cs'),'icon="resource",description=resource','icon="resource."+job.Replace("job.",""),description=resource')
edit(Path('Presentation/HeroineSanctuaryArt.cs'),'if(sanctuaryIcons.TryGetValue(kind,out var texture))return texture;var c=', '''if(sanctuaryIcons.TryGetValue(kind,out var texture))return texture;
            if(kind.StartsWith("resource.",StringComparison.Ordinal)){texture=Resources.Load<Texture2D>("UI/Jobs/"+kind.Substring(9));if(texture==null)throw new InvalidOperationException("Missing distinct job resource image: "+kind);sanctuaryIcons[kind]=texture;return texture;}
            var c=''' )
edit(Path('Presentation/HeroineSanctuaryArt.cs'),'GUI.color=color;GUI.DrawTexture(rect,SanctuaryIcon(kind)','GUI.color=kind.StartsWith("resource.",StringComparison.Ordinal)?Color.white:color;GUI.DrawTexture(rect,SanctuaryIcon(kind)')
edit(Path('Presentation/HeroineSanctuaryArt.cs'),'GrowthFrame(treeRect.x,treeRect.y,treeRect.width,treeRect.height);GUI.DrawTexture(new Rect(610,166,875,637),SanctuaryTree(hero,nodes),ScaleMode.StretchToFill,true);', '''GrowthFrame(treeRect.x,treeRect.y,treeRect.width,treeRect.height);
            var tree=SanctuaryTree(hero,nodes);var imageRect=ContainImage(new Rect(600,186,906,608),tree.width,tree.height);GUI.DrawTexture(imageRect,tree,ScaleMode.ScaleToFit,true);
            var branchRect=new Rect(imageRect.x+imageRect.width*.08f,imageRect.y+imageRect.height*.035f,imageRect.width*.84f,imageRect.height*.88f);''')
edit(Path('Presentation/HeroineSanctuaryArt.cs'),'644+parent.treePosition.x*794,199+parent.treePosition.y*536,644+n.treePosition.x*794,199+n.treePosition.y*536','branchRect.x+parent.treePosition.x*branchRect.width,branchRect.y+parent.treePosition.y*branchRect.height,branchRect.x+n.treePosition.x*branchRect.width,branchRect.y+n.treePosition.y*branchRect.height')
edit(Path('Presentation/HeroineSanctuaryArt.cs'),'float x=644+n.treePosition.x*794,y=199+n.treePosition.y*536','float x=branchRect.x+n.treePosition.x*branchRect.width,y=branchRect.y+n.treePosition.y*branchRect.height')
edit(Path('Presentation/HeroineSanctuaryArt.cs'),'growthScreen=GrowthScreen.Overview;return;','ReturnFromFormationWeapon();return;')
edit(Path('Presentation/HeroineSanctuaryArt.cs'),'homeOperation.Kind=="formation"?"編成の変更を確認":"神器の変更を確認"','homeOperation.Kind=="formation" || homeOperation.Kind=="commander" || homeOperation.Kind=="sniper-support"?"編成・役割の変更を確認":"神器の変更を確認"')
edit(Path('Presentation/GrowthExperience.cs'),'if(formationOpen){formationOpen=false;return;}','if(formationOpen){BackFormationLayer();return;}\n            if(returnToFormationFromWeapon && growthScreen==GrowthScreen.Weapons){ReturnFromFormationWeapon();return;}')
edit(Path('Presentation/HeroineSanctuaryView.cs'),'portrait,ScaleMode.ScaleAndCrop,true','portrait,ScaleMode.ScaleToFit,true')
(S/'Presentation/ImageAspectLayout.cs').write_text('''using UnityEngine;
namespace NewAster.Presentation {
 public sealed partial class PrototypeBootstrap {
  private static Rect ContainImage(Rect box,float width,float height){if(width<=0 || height<=0)return box;float scale=Mathf.Min(box.width/width,box.height/height);float w=width*scale,h=height*scale;return new Rect(box.x+(box.width-w)/2,box.y+(box.height-h)/2,w,h);}
 }
}
''',encoding='utf8')
print('Aspect / tree-node registration / paged recruitment refined.')
