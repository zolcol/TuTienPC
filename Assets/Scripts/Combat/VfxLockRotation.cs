using System;
using UnityEngine;

namespace TopDownGame.Combat
{
    public enum VfxRotationMode
    {
        FollowBoneFull = 0,     // Xoay 100% theo xương (Hands, Weapon, Wings, Back)
        UprightBody = 1,        // Khóa Pitch & Roll, chỉ xoay theo hướng mặt phẳng OXZ của nhân vật (Spine1, Pelvis, Buff thân)
        FlatGround = 2,         // Khóa phẳng hoàn toàn Oxz (Mặt đất Foot 19/20, LockRotate = 1)
        FixedWorld = 3          // Khóa cố định cả 3 trục thế giới (Head Dummy 15/16)
    }

    /// <summary>
    /// Quản lý cơ chế bám theo xương/model và áp dụng 3 Chế độ Xoay (Rotation Mode)
    /// chuẩn hóa theo DATA_CONVENTIONS.md Mục 11, PartSlot.csv và cột LockRotate trong EffectRes.csv.
    /// </summary>
    public class VfxLockRotation : MonoBehaviour
    {
        [Tooltip("Transform gốc của nhân vật (Player hoặc Monster)")]
        [SerializeField] private Transform characterRoot;

        [Tooltip("Transform khớp xương cụ thể mà VFX bám theo")]
        [SerializeField] private Transform targetBone;

        [Tooltip("Chế độ xoay của VFX")]
        [SerializeField] private VfxRotationMode rotationMode = VfxRotationMode.UprightBody;

        private Quaternion initialWorldRotation;

        public void Initialize(Transform targetBone, VfxRotationMode mode = VfxRotationMode.UprightBody)
        {
            Transform root = targetBone != null && targetBone.root != null ? targetBone.root : targetBone;
            Initialize(root, targetBone, mode);
        }

        public void Initialize(Transform targetBone, bool lockHorizontal)
        {
            Transform root = targetBone != null && targetBone.root != null ? targetBone.root : targetBone;
            Initialize(root, targetBone, lockHorizontal ? VfxRotationMode.FlatGround : VfxRotationMode.FollowBoneFull);
        }

        public void Initialize(Transform characterRoot, Transform targetBone, VfxRotationMode mode)
        {
            this.characterRoot = characterRoot;
            this.targetBone = targetBone != null ? targetBone : characterRoot;
            this.rotationMode = mode;
            this.initialWorldRotation = transform.rotation;

            if (mode == VfxRotationMode.FollowBoneFull)
            {
                // 1. Follow Bone: Gán làm con trực tiếp của xương, xoay và chuyển động 100% theo xương
                transform.SetParent(this.targetBone);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            else if (mode == VfxRotationMode.FlatGround)
            {
                // 2. Flat Ground: Tọa độ mặt đất phẳng dưới chân nhân vật, không kế thừa góc nghiêng của xương
                transform.SetParent(null);
                Vector3 groundPos = (this.characterRoot != null) ? this.characterRoot.position : this.targetBone.position;
                transform.position = new Vector3(this.targetBone.position.x, groundPos.y, this.targetBone.position.z);
                transform.rotation = Quaternion.Euler(0f, this.characterRoot != null ? this.characterRoot.eulerAngles.y : this.targetBone.eulerAngles.y, 0f);
            }
            else if (mode == VfxRotationMode.FixedWorld)
            {
                // 3. Fixed World: Tọa độ bám theo xương (đỉnh đầu), góc xoay cố định theo thế giới
                transform.SetParent(null);
                transform.position = this.targetBone.position;
                transform.rotation = initialWorldRotation;
            }
            else // UprightBody
            {
                // 4. Upright Body: Tọa độ bám theo ngực/thân, triệt tiêu góc nghiêng Pitch/Roll của xương
                transform.SetParent(null);
                transform.position = this.targetBone.position;
                transform.rotation = Quaternion.Euler(0f, this.characterRoot != null ? this.characterRoot.eulerAngles.y : this.targetBone.eulerAngles.y, 0f);
            }
        }

        private void LateUpdate()
        {
            // Tự động dọn dẹp nếu thực thể sở hữu đã bị tiêu hủy
            if (targetBone == null && characterRoot == null)
            {
                Destroy(gameObject);
                return;
            }

            Transform root = characterRoot != null ? characterRoot : targetBone;
            Transform bone = targetBone != null ? targetBone : characterRoot;

            switch (rotationMode)
            {
                case VfxRotationMode.FollowBoneFull:
                    // Khi là con của targetBone, Unity đã tự động tính toán theo ma trận xương
                    break;

                case VfxRotationMode.UprightBody:
                    // Vị trí bám theo xương ngực/thân, góc xoay chỉ xoay theo hướng mặt ngang OXZ của nhân vật
                    transform.position = bone.position;
                    transform.rotation = Quaternion.Euler(0f, root.eulerAngles.y, 0f);
                    break;

                case VfxRotationMode.FlatGround:
                    // Vị trí bám theo mặt đất của nhân vật (Y = root.position.y, X/Z theo bone)
                    transform.position = new Vector3(bone.position.x, root.position.y, bone.position.z);
                    transform.rotation = Quaternion.Euler(0f, root.eulerAngles.y, 0f);
                    break;

                case VfxRotationMode.FixedWorld:
                    // Vị trí bám theo đỉnh đầu, góc xoay cố định
                    transform.position = bone.position;
                    transform.rotation = initialWorldRotation;
                    break;
            }
        }
    }
}
