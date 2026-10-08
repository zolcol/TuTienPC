using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Hệ thống Object Pool quản lý tái sử dụng ProjectileController (Missile)
    /// Triệt tiêu 100% Instantiate / Destroy và AddComponent trong combat
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        private static ProjectilePool instance;
        private static bool isApplicationQuitting = false;

        public static bool HasInstance => instance != null && !isApplicationQuitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            instance = null;
            isApplicationQuitting = false;
        }

        public static ProjectilePool Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    instance = FindObjectOfType<ProjectilePool>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[ProjectilePool]");
                        instance = go.AddComponent<ProjectilePool>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        [Tooltip("Số lượng đạn khởi tạo sẵn ban đầu")]
        [SerializeField] private int initialPoolSize = 16;

        private readonly Queue<ProjectileController> availablePool = new Queue<ProjectileController>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
                Prewarm(initialPoolSize);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ProjectileController proj = CreateNewProjectile();
                proj.gameObject.SetActive(false);
                availablePool.Enqueue(proj);
            }
        }

        private ProjectileController CreateNewProjectile()
        {
            GameObject go = new GameObject("[Missile_Pooled]");
            go.transform.SetParent(transform);
            ProjectileController controller = go.AddComponent<ProjectileController>();
            return controller;
        }

        /// <summary>
        /// Lấy một ProjectileController từ Pool để bắn
        /// </summary>
        public ProjectileController Get()
        {
            while (availablePool.Count > 0)
            {
                ProjectileController proj = availablePool.Dequeue();
                if (proj != null)
                {
                    proj.gameObject.SetActive(true);
                    return proj;
                }
            }

            ProjectileController newProj = CreateNewProjectile();
            newProj.gameObject.SetActive(true);
            return newProj;
        }

        /// <summary>
        /// Thu hồi ProjectileController về Pool sau khi nổ/hết hạn
        /// </summary>
        public void Release(ProjectileController projectile)
        {
            if (projectile == null) return;
            if (isApplicationQuitting || !Application.isPlaying) return;

            projectile.gameObject.SetActive(false);
            if (this != null && gameObject != null && projectile.transform.parent != transform)
            {
                projectile.transform.SetParent(transform);
            }
            availablePool.Enqueue(projectile);
        }
    }
}
