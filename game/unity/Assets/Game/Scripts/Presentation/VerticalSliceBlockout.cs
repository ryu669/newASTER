using UnityEngine;
using System.Collections.Generic;

namespace NewAster.Presentation
{
    /// <summary>独自アセット制作までの間、縦切りの距離感・部位可読性・箱庭規模を確認する3Dブロックアウト。</summary>
    public sealed class VerticalSliceBlockout : MonoBehaviour
    {
        private Transform dragon;
        private Transform garden;
        private readonly List<Transform> party = new List<Transform>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => new GameObject("Vertical Slice Blockout").AddComponent<VerticalSliceBlockout>();

        private void Awake()
        {
            CreateHeroine();
            dragon = CreateGreenReturnDragon();
            garden = CreateGrasslandForestGarden();
            CreateSky();
        }

        private void Update()
        {
            var time = Time.time;
            if (dragon != null)
            {
                dragon.position = new Vector3(2.5f, 1.4f + Mathf.Sin(time * .8f) * .22f, 1.2f);
                dragon.rotation = Quaternion.Euler(0f, Mathf.Sin(time * .35f) * 8f, Mathf.Sin(time * .7f) * 3f);
            }
            for (var index = 0; index < party.Count; index++)
            {
                var member = party[index];
                if (member == null) continue;
                member.localPosition = new Vector3(member.localPosition.x, 1.05f + Mathf.Sin(time * 1.4f + index) * .08f, member.localPosition.z);
            }
            if (garden != null) garden.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * .18f) * 2f, 0f);
        }

        private void CreateHeroine()
        {
            var root = new GameObject("Five Heroine Blockouts");
            var colors = new[] { new Color(.88f,.92f,.88f), new Color(.75f,.86f,.98f), new Color(.94f,.78f,.64f), new Color(.68f,.91f,.73f), new Color(.83f,.70f,.95f) };
            for (var index = 0; index < 5; index++)
            {
                var heroine = Primitive("Heroine " + (index + 1), PrimitiveType.Capsule, new Vector3(-4.8f + index * .7f, 1.05f, -1.5f + index * .08f), new Vector3(.36f, .62f, .36f), colors[index]);
                heroine.transform.SetParent(root.transform);
                party.Add(heroine.transform);
                Primitive("Flight Harness", PrimitiveType.Cube, heroine.transform.position + new Vector3(0f, .24f, .22f), new Vector3(.34f, .09f, .08f), new Color(.05f, .35f, .38f)).transform.SetParent(heroine.transform);
            }
        }

        private static Transform CreateGreenReturnDragon()
        {
            var root = new GameObject("Green Return Dragon Blockout");
            root.transform.position = new Vector3(2.5f, 1.4f, 1.2f);
            Primitive("Dragon Body", PrimitiveType.Capsule, root.transform.position, new Vector3(1.4f, 2.2f, 1.4f), new Color(.12f, .42f, .2f)).transform.SetParent(root.transform);
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
            Primitive("Root Bench", PrimitiveType.Cube, new Vector3(-4f, .45f, 1.5f), new Vector3(2f, .35f, .65f), new Color(.3f, .18f, .08f)).transform.SetParent(root.transform);
            return root.transform;
        }

        private static void CreateSky()
        {
            Primitive("New Star Ground", PrimitiveType.Plane, new Vector3(0f, 0f, 1.5f), new Vector3(2f, 1f, 2f), new Color(.15f, .30f, .22f));
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
            item.GetComponent<Renderer>().material.color = color;
            return item;
        }
    }
}
