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
        private string status;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => new GameObject("newASTER Bootstrap").AddComponent<PrototypeBootstrap>();

        private void Awake()
        {
            book = new BookNavigationState(WorldCatalog.BookSubjects);
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
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(28, 28, 900, 28), "巨神と誓女2 / newASTER — 縦切り基盤", GUI.skin.label);
            GUI.Label(new Rect(28, 56, 900, 28), status, GUI.skin.label);
            GUI.Label(new Rect(28, 84, 900, 28), $"しおり: {book.Bookmark} / 対象: {book.SubjectId} / 面: {book.Face}", GUI.skin.label);
            GUI.Label(new Rect(28, 112, 900, 28), "1〜4: しおり（大分類）　←→: ページをめくる（対象変更）　Space: ページを裏返す（情報変更）", GUI.skin.label);
            if (book.Bookmark == BookBookmark.Colossi)
            {
                var colossus = WorldCatalog.Colossi.First(item => item.Id == book.SubjectId);
                var detail = book.Face == BookFace.Overview
                    ? $"{colossus.DisplayName}  /  記憶元: {colossus.WorldLineId}"
                    : $"新天地へ定着: {string.Join("・", colossus.EnvironmentTags)}";
                GUI.Label(new Rect(28, 148, 900, 28), detail, GUI.skin.label);
                if (colossus.IsIntegrationBoss)
                    GUI.Label(new Rect(28, 176, 900, 28), "No.15：14体の初回討伐後に解放。過去ではなく新天地の統合記憶。", GUI.skin.label);
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
