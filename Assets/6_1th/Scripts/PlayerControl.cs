using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Generation_6_1
{
    public class PlayerControl : MonoBehaviour
    {
        [SerializeField]
        Vector3 pos;
        public Vector3 rot;
        public Vector2 rot_sensitivity = new Vector2(4, 4);
        public float moveSpeed = 15f;

        public Vector3 Pos => pos_temp;
        Vector3 pos_temp;
        Vector3 rot_temp;

        readonly float PI = Mathf.PI / 180f;
        //readonly int mapSize = 256;


        public static PlayerControl instance;

        private void Awake()
        {
            instance = this;
            pos_temp = pos;
            rot_temp = rot;
        }

        public void SetMoveSpeed(float f)
        {
            moveSpeed = f;
        }
        public void EnableSetMoveSpeed()
        {
            Slider3Controller.instance.SetSliderFunction("Set Move Speed", SetMoveSpeed, 0, 1, moveSpeed);
        }
        public Vector3 Move(Vector3 d)
        {
            d *= moveSpeed * Time.deltaTime;
            float sin = Mathf.Sin(-rot_temp.y * PI);
            float cos = Mathf.Cos(-rot_temp.y * PI);
            pos += new Vector3(d.x * cos - d.z * sin, d.y, d.x * sin + d.z * cos);
            pos_temp = Vector3.Lerp(pos_temp, pos, 0.3f);
            return (VisionController.instance.rtxOn ? pos : pos_temp);
        }

        public Vector3 Rotate(Vector2 delta)
        {
            rot.x = Mathf.Clamp(rot.x - delta.y * rot_sensitivity.y, -90, 90);
            rot.y = rot.y + delta.x * rot_sensitivity.x;

            rot_temp = Vector3.Lerp(rot_temp, rot, 0.3f);
            return VisionController.instance.rtxOn ? rot : rot_temp;
        }
    }
}