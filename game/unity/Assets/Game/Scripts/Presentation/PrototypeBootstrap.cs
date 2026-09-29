using NewAster.Core;
using NewAster.Data;
using UnityEngine;
using System.Linq;

namespace NewAster.Presentation
{
    /// <summary>正式素材が届くまで、万物の書の導線と3D表示面を検証する起動点。</summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private BookNavigationState book;
        private CampaignState campaign;
        private string status;
        private BattleState battle;
        private string battleTarget = "body";
        private string activeColossusId;
        private int battleSequence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => new GameObject("newASTER Bootstrap").AddComponent<PrototypeBootstrap>();

        private void Awake()
        {
            book = new BookNavigationState(WorldCatalog.BookSubjects);
            campaign = new CampaignState(WorldCatalog.ColossusIds);
            status = "万物の書：巨神獣のしおり";
            CreatePresentationPlane("World Root", Vector3.zero, new Vector3(20f, 0.2f, 12f), new Color(0.025f, 0.04f, 0.08f));
            CreatePresentationPlane("Book Cover", new Vector3(0f, 0.35f, 0f), new Vector3(6f, 0.3f, 4f), new Color(0.16f, 0.09f, 0.06f));
            CreateCameraAndLight();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { book.ChangeBookmark(BookBookmark.Colossi); status = "しおり：巨神獣"; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { book.ChangeBookmark(BookBookmark.Heroines); status = "しおり：ヒロイン"; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { book.ChangeBookmark(BookBookmark.Gardens); status = "しおり：箱庭"; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { book.ChangeBookmark(BookBookmark.Stories); status = "しおり：物語"; }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { book.TurnPage(1); status = $"対象：{book.SubjectId}"; }
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { book.TurnPage(-1); status = $"対象：{book.SubjectId}"; }
            if (Input.GetKeyDown(KeyCode.Space)) { book.FlipPage(); status = $"情報面：{book.Face}"; }
            if (Input.GetKeyDown(KeyCode.B)) StartPrototypeBattle();
            if (Input.GetKeyDown(KeyCode.T)) ToggleBattleTarget();
            if (Input.GetKeyDown(KeyCode.A)) ResolvePrototypeAttack();
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(28, 28, 900, 28), "巨神と誓女2 / newASTER — 縦切り基盤", GUI.skin.label);
            GUI.Label(new Rect(28, 56, 900, 28), status, GUI.skin.label);
            GUI.Label(new Rect(28, 84, 900, 28), $"しおり: {book.Bookmark} / 対象: {book.SubjectId} / 面: {book.Face}", GUI.skin.label);
            GUI.Label(new Rect(28, 112, 900, 28), "1〜4: しおり（大分類）　←→: ページをめくる（対象変更）　Space: ページを裏返す（情報変更）", GUI.skin.label);
            GUI.Label(new Rect(28, 320, 900, 28), $"新天地に定着した環境：{(campaign.Terraforming.EnvironmentTags.Count == 0 ? "まだありません" : string.Join("・", campaign.Terraforming.EnvironmentTags))}", GUI.skin.label);
            GUI.Label(new Rect(28, 348, 900, 28), $"解放済み箱庭区画：{(campaign.Gardens.UnlockedGardenIds.Count == 0 ? "まだありません" : string.Join("・", campaign.Gardens.UnlockedGardenIds))}", GUI.skin.label);
            if (book.Bookmark == BookBookmark.Colossi)
            {
                var colossus = WorldCatalog.Colossi.First(item => item.Id == book.SubjectId);
                var detail = book.Face == BookFace.Overview
                    ? $"{colossus.DisplayName}  /  記憶元: {colossus.WorldLineId}"
                    : $"新天地へ定着: {string.Join("・", colossus.EnvironmentTags)}";
                GUI.Label(new Rect(28, 148, 900, 28), detail, GUI.skin.label);
                GUI.Label(new Rect(28, 176, 900, 28), campaign.ColossusUnlocks.IsUnlocked(colossus.Id) ? "ページ状態：解放済み・再召喚可能" : "ページ状態：未解放（前の巨神獣を初回討伐）", GUI.skin.label);
                if (colossus.IsIntegrationBoss)
                    GUI.Label(new Rect(28, 204, 900, 28), "No.15：14体の初回討伐後に解放。過去ではなく新天地の統合記憶。", GUI.skin.label);
            }
            if (battle == null)
                GUI.Label(new Rect(28, 222, 900, 28), "B: 選択中の巨神獣へLv1で出撃", GUI.skin.label);
            else
            {
                GUI.Label(new Rect(28, 222, 900, 28), $"戦闘試作：本体HP {battle.BossHitPoints}/{battle.BossMaxHitPoints}　対象: {battleTarget}", GUI.skin.label);
                GUI.Label(new Rect(28, 250, 900, 28), "T: 本体／部位を切替　A: 先頭ヒロインの固有スキルを実行", GUI.skin.label);
                GUI.Label(new Rect(28, 278, 900, 28), $"部位：{string.Join(" / ", battle.Parts.Select(part => $"{part.Id}:{part.HitPoints}"))}", GUI.skin.label);
            }
        }

        private void StartPrototypeBattle()
        {
            if (book.Bookmark != BookBookmark.Colossi)
            {
                status = "巨神獣のしおりで対象を選んでください";
                return;
            }
            if (!campaign.ColossusUnlocks.IsUnlocked(book.SubjectId))
            {
                status = "このページは未解放です。前の巨神獣を初回討伐してください";
                return;
            }

            activeColossusId = book.SubjectId;
            battleSequence++;
            battle = new BattleState(
                selectedLevel: 1,
                heroes: new[]
                {
                    new BattleHero("hero-01", 100, 20, 10), new BattleHero("hero-02", 100, 18, 10),
                    new BattleHero("hero-03", 100, 16, 10), new BattleHero("hero-04", 100, 14, 10),
                    new BattleHero("hero-05", 100, 12, 10)
                },
                parts: new[]
                {
                    new BattlePart("part-01", 25, "gauge-down"), new BattlePart("part-02", 25, ""),
                    new BattlePart("part-03", 25, ""), new BattlePart("part-04", 25, "")
                },
                bossHitPoints: 100,
                bossGaugeMax: 3);
            battle.BeginTurn(seed: 1, turnBonusChance: 0.25m);
            battleTarget = "body";
            status = $"{WorldCatalog.Colossi.First(item => item.Id == book.SubjectId).DisplayName}へ出撃しました";
        }

        private void ToggleBattleTarget()
        {
            if (battle == null) return;
            var availablePart = battle.Parts.FirstOrDefault(part => !part.IsBroken);
            battleTarget = battleTarget == "body" && availablePart != null ? availablePart.Id : "body";
            status = $"対象を{battleTarget}に変更しました";
        }

        private void ResolvePrototypeAttack()
        {
            if (battle == null)
            {
                status = "先にBで出撃してください";
                return;
            }
            var result = BattleActionResolver.Resolve(battle, "hero-01", new BattleSkill("prototype-strike", 1.0m, 0), battleTarget);
            status = result.Accepted ? $"スキル実行：{result.Damage}ダメージ" : $"スキル未実行：{result.Reason}";
            if (result.Victory && result.Accepted)
            {
                var colossus = WorldCatalog.Colossi.First(item => item.Id == activeColossusId);
                var resolution = campaign.ClaimColossusVictory(
                    activeColossusId,
                    colossus.EnvironmentTags,
                    new VictoryReward($"prototype-{battleSequence}", 1, 1, 1, System.Array.Empty<string>()),
                    System.Array.Empty<StoryRequirement>(),
                    System.Array.Empty<TerraformingMilestone>(),
                    GardenCatalog.Requirements);
                if (resolution.FirstClear)
                {
                    status += $"　初回討伐：次ページ解放／環境定着 {string.Join("・", resolution.NewEnvironmentTags)}";
                    if (resolution.NewGardenIds.Count > 0) status += $"／箱庭解放 {string.Join("・", resolution.NewGardenIds)}";
                }
                else status += "　討伐成功：再召喚報酬を獲得";
            }
        }

        private static void CreatePresentationPlane(string name, Vector3 position, Vector3 scale, Color color)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = name;
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().material.color = color;
        }

        private static void CreateCameraAndLight()
        {
            var camera = new GameObject("Presentation Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 7f, -9f);
            camera.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.06f);
            var light = new GameObject("Presentation Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            light.intensity = 1.1f;
        }
    }
}
