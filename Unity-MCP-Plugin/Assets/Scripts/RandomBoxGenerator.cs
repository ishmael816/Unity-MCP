using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityMCP.Tools
{
    /// <summary>
    /// 随机立方体生成器 - 在场景中随机位置生成立方体
    /// </summary>
    public static class RandomBoxGenerator
    {
        // 配置参数
        private const int DEFAULT_COUNT = 10;
        private const float DEFAULT_RANGE = 10f;
        private const float DEFAULT_SIZE = 1f;
        private const string DEFAULT_PARENT_NAME = "RandomBoxes";

        /// <summary>
        /// 生成随机立方体
        /// </summary>
        [MenuItem("Tools/Generate Random Boxes")]
        public static void GenerateRandomBoxes()
        {
            // 获取或创建父对象
            GameObject parent = GetOrCreateParent();

            // 生成立方体
            int count = DEFAULT_COUNT;
            float range = DEFAULT_RANGE;
            float size = DEFAULT_SIZE;

            for (int i = 0; i < count; i++)
            {
                CreateRandomBox(parent.transform, i, range, size);
            }

            Debug.Log($"已生成 {count} 个随机立方体，父对象: {parent.name}");
            EditorUtility.SetDirty(parent);
        }

        /// <summary>
        /// 清空所有随机生成的立方体
        /// </summary>
        [MenuItem("Tools/Clear Random Boxes")]
        public static void ClearRandomBoxes()
        {
            GameObject parent = GameObject.Find(DEFAULT_PARENT_NAME);
            if (parent != null)
            {
                Undo.DestroyObjectImmediate(parent);
                Debug.Log("已清空所有随机立方体");
            }
            else
            {
                Debug.LogWarning("未找到随机立方体容器，可能已经被清空或手动删除");
            }
        }

        /// <summary>
        /// 获取或创建父对象
        /// </summary>
        private static GameObject GetOrCreateParent()
        {
            GameObject parent = GameObject.Find(DEFAULT_PARENT_NAME);
            if (parent == null)
            {
                parent = new GameObject(DEFAULT_PARENT_NAME);
                Undo.RegisterCreatedObjectUndo(parent, "Create Random Boxes Parent");
            }
            return parent;
        }

        /// <summary>
        /// 创建一个随机立方体
        /// </summary>
        private static void CreateRandomBox(Transform parent, int index, float range, float size)
        {
            // 创建立方体
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"RandomBox_{index:000}";
            box.transform.SetParent(parent);

            // 设置随机位置
            Vector3 randomPosition = new Vector3(
                Random.Range(-range, range),
                size * 0.5f, // 确保立方体底部在地面以上
                Random.Range(-range, range)
            );
            box.transform.position = randomPosition;

            // 设置随机大小
            float randomSize = size * Random.Range(0.5f, 1.5f);
            box.transform.localScale = Vector3.one * randomSize;

            // 设置随机旋转（可选）
            box.transform.rotation = Random.rotation;

            // 设置随机颜色材质
            SetRandomColor(box);

            // 注册撤销操作
            Undo.RegisterCreatedObjectUndo(box, $"Create Random Box {index}");
        }

        /// <summary>
        /// 为立方体设置随机颜色
        /// </summary>
        private static void SetRandomColor(GameObject box)
        {
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 生成随机颜色
                Color randomColor = new Color(
                    Random.value,
                    Random.value,
                    Random.value,
                    1f
                );

                // 创建新材质以避免修改共享材质
                Material newMaterial = new Material(Shader.Find("Standard"));
                newMaterial.color = randomColor;
                newMaterial.SetFloat("_Glossiness", 0.5f); // 设置光滑度
                newMaterial.SetFloat("_Metallic", 0.0f);   // 设置金属度

                renderer.material = newMaterial;
            }
        }

        /// <summary>
        /// 生成自定义配置的随机立方体（供程序化调用）
        /// </summary>
        public static void GenerateWithConfig(int count, float range, float minSize, float maxSize)
        {
            GameObject parent = GetOrCreateParent();

            for (int i = 0; i < count; i++)
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = $"RandomBox_{i:000}";
                box.transform.SetParent(parent.transform);

                // 随机位置
                Vector3 randomPosition = new Vector3(
                    Random.Range(-range, range),
                    Random.Range(minSize, maxSize) * 0.5f,
                    Random.Range(-range, range)
                );
                box.transform.position = randomPosition;

                // 随机大小
                float randomSize = Random.Range(minSize, maxSize);
                box.transform.localScale = Vector3.one * randomSize;

                // 随机旋转
                box.transform.rotation = Random.rotation;

                // 随机颜色
                SetRandomColor(box);

                Undo.RegisterCreatedObjectUndo(box, $"Create Random Box {i}");
            }

            Debug.Log($"已生成 {count} 个随机立方体（自定义配置）");
            EditorUtility.SetDirty(parent);
        }
    }
}
