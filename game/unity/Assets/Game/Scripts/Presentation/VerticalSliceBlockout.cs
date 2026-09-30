using UnityEngine;

namespace NewAster.Presentation
{
    /// <summary>独自アセット制作までの間、縦切りの距離感・部位可読性・箱庭規模を確認する3Dブロックアウト。</summary>
    public sealed class VerticalSliceBlockout : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => new GameObject("Vertical Slice Blockout").AddComponent<VerticalSliceBlockout>();

        private void Awake()
        {
            CreateHeroine();
            CreateGreenReturnDragon();
            CreateGrasslandForestGarden();
        }

        private static void CreateHeroine()
        {
            var heroine = Primitive("Heroine Blockout", PrimitiveType.Capsule, new Vector3(-3.5f, 1.2f, -1.5f), new Vector3(.75f, 1.2f, .75f), new Color(.85f, .9f, .88f));
            Primitive("Heroine Flight Harness", PrimitiveType.Cube, heroine.transform.position + new Vector3(0f, .5f, .42f), new Vector3(.7f, .18f, .16f), new Color(.05f, .35f, .38f));
        }

        private static void CreateGreenReturnDragon()
        {
            var root = new GameObject("Green Return Dragon Blockout");
            root.transform.position = new Vector3(2.5f, 1.4f, 1.2f);
            Primitive("Dragon Body", PrimitiveType.Capsule, root.transform.position, new Vector3(1.4f, 2.2f, 1.4f), new Color(.12f, .42f, .2f)).transform.SetParent(root.transform);
            Primitive("Crystal Horn Crown [Break]", PrimitiveType.Cylinder, root.transform.position + new Vector3(0f, 2.5f, .1f), new Vector3(.45f, .75f, .45f), new Color(.1f, .8f, .6f)).transform.SetParent(root.transform);
            Wing("Left Wing Root [Break]", root.transform.position + new Vector3(-1.8f, 1f, 0f), -25f, root.transform);
            Wing("Right Wing Root [Break]", root.transform.position + new Vector3(1.8f, 1f, 0f), 25f, root.transform);
            Primitive("Vine Tail [Break]", PrimitiveType.Capsule, root.transform.position + new Vector3(0f, -.7f, 2.4f), new Vector3(.55f, 1.5f, .55f), new Color(.2f, .3f, .08f)).transform.SetParent(root.transform);
        }

        private static void CreateGrasslandForestGarden()
        {
            for (var index = 0; index < 5; index++)
            {
                var x = -7f + index * 2.4f;
                Primitive("Garden Tree Trunk", PrimitiveType.Cylinder, new Vector3(x, 1f, 3.5f), new Vector3(.35f, 1f, .35f), new Color(.22f, .12f, .05f));
                Primitive("Garden Tree Canopy", PrimitiveType.Sphere, new Vector3(x, 2.5f, 3.5f), new Vector3(1.5f, 1.3f, 1.5f), new Color(.12f, .45f, .18f));
            }
            Primitive("Root Bench", PrimitiveType.Cube, new Vector3(-4f, .45f, 1.5f), new Vector3(2f, .35f, .65f), new Color(.3f, .18f, .08f));
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
